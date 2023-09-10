using HtmlAgilityPack;
using SpiderInterface;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace SpiderEngine
{
    public class Engine : IEngine
    {
        public ScanResultCollection ScanResultCollection { get; set; } = new ();
        public List<ISpiderExtension> Extensions { get; set; } = new List<ISpiderExtension>();
        public Action<Exception, Uri?, Uri>? ExceptionLogger { get; set; }
        public Action<string, MessageSeverity>? Logger { get; set; }
        public Uri? BaseUri { get; set; }
        private EngineConfig? config;

        public EngineConfig? Config
        {
            get => config;
            set
            {
                config = value;
                if (config != null)
                {
                    // load extensions when config is set
                    foreach (var extension in config.Extensions)
                    {
                        extension.Engine = this;
                        Extensions.Add(extension);
                    }
                }
            }
        }
        private Stopwatch stopwatch = new Stopwatch();
        public async Task StartAsync()
        {
            Uri? startUri = Config?.StartUri;
            ArgumentNullException.ThrowIfNull(startUri);
            BaseUri = new Uri(startUri.GetLeftPart(UriPartial.Authority));
            Init();
            try
            {
                await Process(new List<CrawlStep>(), parentUri: null, uri: startUri, pageMayContainsLink: true);
            }
            catch (Exception ex)
            {
                LogException(ex, null, BaseUri);
            }
            Done();
        }
        private void Init()
        {
            Logger?.Invoke($"Starting crawl at {DateTime.Now}", MessageSeverity.Info);
            stopwatch.Start();
            foreach (var extension in Extensions)
            {
                extension.Init();
            }
        }
        private void Done()
        {
            foreach (var extension in Extensions)
            {
                extension.Done();
            }
            stopwatch.Stop();
            Logger?.Invoke($"Finished crawling at {DateTime.Now}", MessageSeverity.Success);
            Logger?.Invoke($"Elapsed Time {stopwatch.Elapsed}", MessageSeverity.Info);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="steps">Stack of the previously visited uri</param>
        /// <param name="parentUri">The referrer uri = the uri from which we arrive on the current uri</param>
        /// <param name="uri">Current uri to process for existence and inside links</param>
        /// <param name="pageMayContainsLink">Indication if the current page may contains some link.
        /// If true page is dowloaded. If false, content of page is not downloaded, but existence of page is checked.
        /// It prevents downloading all images of a web site.</param>
        /// <param name="processChildrenLinks">Set to true if an extension needs to use the engine to check a link</param>
        /// 
        /// <returns>true if page is found</returns>
        public async Task<bool> Process(List<CrawlStep>? steps, Uri? parentUri, Uri uri, bool pageMayContainsLink, bool processChildrenLinks = true)
        {
            bool result = true;
            // Make a copy to be thread safe
            steps = steps == null ? new List<CrawlStep>() : new List<CrawlStep>(steps);
            steps.Add(new CrawlStep(uri));
            if (!CheckSupportedUri(uri))
                return false;
            try
            {
                ScanResult? scanResult = null;

                if (!this.ScanResultCollection.ContainsKey(uri))
                {
                    scanResult = new ScanResult();
                    this.ScanResultCollection.Add(uri, scanResult);
                }
                else
                {
                    scanResult = this.ScanResultCollection[uri];
                }

                HttpClient client = new HttpClient();
                HttpResponseMessage responseMessage;
                if (pageMayContainsLink)
                {
                    responseMessage = await client.GetAsync(uri);
                }
                else
                {
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, uri);
                    responseMessage = await client.SendAsync(request);
                }
                scanResult.Status = responseMessage.StatusCode;
                HttpStatusCode statusCode = responseMessage.StatusCode;
                result = responseMessage.IsSuccessStatusCode;
                MessageSeverity severity = responseMessage.IsSuccessStatusCode ? MessageSeverity.Info : MessageSeverity.Error;
                Logger?.Invoke($"{statusCode} {uri}", severity);
                int status = (int)statusCode;
                switch (status)
                {
                    case int s when s >= 200 && s < 300:
                        if (!pageMayContainsLink)
                            break;
                        ArgumentNullException.ThrowIfNull(BaseUri);
                        bool isStillInSite = this.BaseUri.IsBaseOf(uri);
                        string? contentType = responseMessage.Content.Headers.ContentType?.MediaType;
                        bool isHtml = contentType == "text/html";
                        using (Stream stream = await responseMessage.Content.ReadAsStreamAsync())
                        {
                            HtmlDocument? doc = null;
                            if (isHtml)
                            {
                                doc = GetHtmlDocument(responseMessage, stream);
                                if (isStillInSite && processChildrenLinks)
                                {
                                    await ProcessLinksAsync(steps, uri, responseMessage, doc);
                                }
                            }
                            foreach (var extension in Extensions)
                            {
                                extension.Process(steps, uri, responseMessage, doc);
                            }
                        }
                        //if (isCssLink)
                        //{
                        //  await CheckCss(uri, responseMessage, stream);
                        //}
                        break;
                    case (int)HttpStatusCode.MovedPermanently:
                    case (int)HttpStatusCode.Found:
                    case (int)HttpStatusCode.RedirectKeepVerb:
                    case (int)HttpStatusCode.SeeOther:
                        // TODO process redirections
                        Debug.Assert(false, "Redirected");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogException(ex, parentUri, uri);
                this.ScanResultCollection[uri] = new ScanResult { Exception = ex };
            }
            return result;
        }
        //private void CheckCss(Uri uri, HttpResponseMessage responseMessage, Stream stream)
        //{
        //  using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        //  {
        //  }
        //}
        private bool CheckSupportedUri(Uri uri)
        {
            string scheme = uri.Scheme;
            if (!supportedSchemes.Contains(scheme.ToLower()))
            {
                this.ScanResultCollection.Add(uri, new ScanResult { IsUnsupportedScheme = true });
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Unsupported scheme {scheme} for {uri}");
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }
            return true;
        }
        private static List<string> supportedSchemes = new List<string> { "http", "https" };
        private static Dictionary<string, string> tags2Attribute = new Dictionary<string, string>
        {
            { "a","href" },
            { "script","src" },
            { "link","href" },
            { "img","src" },
            // TODO : ajouter frame, iframe, meta, form
        };
        private static HtmlDocument GetHtmlDocument(HttpResponseMessage responseMessage, Stream stream)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.Load(stream, Encoding.UTF8);
            return doc;
        }
        private async Task ProcessLinksAsync(List<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument doc)
        {
            // Pour obtenir l'encodage
            // Attention si on avance le curseur, on ne peut plus lire le flux
            //try
            //{
            //  string encodingName = doc.DocumentNode.Element("html").Element("head").Element("meta").GetAttributeValue("charset", "");
            //  Encoding encoding = Encoding.GetEncoding(encodingName);
            //  doc = new HtmlDocument();
            //  doc.Load(stream, encoding);
            //}
            //catch (Exception ex)
            //{ 
            //  LogException(ex, uri);
            //  /* pas grave */
            //}

            HtmlNode documentNode = doc.DocumentNode;
            foreach (var pair in tags2Attribute)
            {
                string tagName = pair.Key;
                string attributeName = pair.Value;
                IEnumerable<HtmlNode> links = documentNode.Descendants(tagName);

                List<Task> tasks = links.Select(link => ScanLinkAsync(steps, uri, attributeName, link)).ToList();
                // Equivalent à 
                //List<Task> tasks = new List<Task>();
                //foreach (var link in links)
                //{
                //    tasks.Add(ScanLinkAsync(steps, uri, attributeName, link));
                //}
                await Task.WhenAll(tasks);
            }
        }
        private async Task ScanLinkAsync(List<CrawlStep> steps, Uri uri, string attributeName, HtmlNode link)
        {
            bool mayContainLink = link.Name.ToLower() == "a";
            //bool isCssLink = link.Name.ToLower() == "link" && link.GetAttributeValue("type", "") == "text/css";
            //mayContainLink |= isCssLink;
            string ATTR_DEFAULT_VALUE = "";
            string attributeValue = link.GetAttributeValue(attributeName, def: ATTR_DEFAULT_VALUE);
            if (attributeValue != ATTR_DEFAULT_VALUE)
            {
                Uri derivedUri;
                if (attributeValue.ToLower().StartsWith("http"))
                {
                    derivedUri = new Uri(attributeValue);
                }
                else
                {
                    derivedUri = new Uri(uri, attributeValue);
                }
                bool alreadyVisited = this.ScanResultCollection.ContainsKey(derivedUri);
                if (!alreadyVisited)
                {
                    ArgumentNullException.ThrowIfNull(Config);
                    if (Config.OnlyCheckInnerLinks)
                    {
                        ArgumentNullException.ThrowIfNull(BaseUri);
                        bool isStillInSite = BaseUri.IsBaseOf(derivedUri);
                        if (!isStillInSite)
                            return;
                    }
                    await Task.Run(async () => await Process(steps, uri, derivedUri, mayContainLink));
                }
            }
        }
        public void LogException(Exception ex, Uri? parentUri, Uri uri)
        {
            ExceptionLogger?.Invoke(ex, parentUri, uri);
        }
    }
}

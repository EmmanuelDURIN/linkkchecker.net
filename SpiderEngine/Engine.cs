using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using SpiderInterface;



// TODO Extension : Collecter les stats Google Page Speed Insights

// TODO : Créer le batch et mettre LinkChecker en repo Github

// Gérer les liens vers les images dans le CSS comme background : url('logo.gif')
// Gérer les liens vers autre CSS 

// Faire un mode sans Head mais avec des GET réels pour avoir le warmup du site

// Faire des warning sur les redirection, pb d'expiration header

// https

// plugin pour lister les images (trouver les images en trop sur le site ? )

// TODO checker longueurs des descriptions à 160 caractères pour rentrer dans les sitemap de Google

namespace SpiderEngine
{
    public class Engine : IEngine
    {
        public Dictionary<Uri, ScanResult> ScanResults { get; set; } = new Dictionary<Uri, ScanResult>();
        public List<ISpiderExtension> Extensions { get; set; } = new List<ISpiderExtension>();
        public Action<Exception, Uri, Uri> ExceptionLogger { get; set; }
        public Action<String, MessageSeverity> Logger { get; set; }
        private EngineConfig config;
        public Uri BaseUri { get; set; }

        public EngineConfig Config
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
        public async Task Start()
        {
            BaseUri = new Uri(Config.StartUri.GetLeftPart(UriPartial.Authority));
            Init();
            try
            {
                await Process(new List<CrawlStep>(), parentUri: null, uri: Config.StartUri, pageMayContainsLink: true);
            }
            catch (Exception ex)
            {
                LogException(ex, null, BaseUri);
            }
            Done();
        }
        private void Init()
        {
            Logger($"Starting crawl at {DateTime.Now}", MessageSeverity.Info);
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
            Logger($"Finished crawling at {DateTime.Now}", MessageSeverity.Success);
            Logger($"Elapsed Time {stopwatch.Elapsed}", MessageSeverity.Info);
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
        public async Task<bool> Process(List<CrawlStep> steps, Uri parentUri, Uri uri, bool pageMayContainsLink, bool processChildrenLinks = true)
        {
            bool result = true;
            // Make a copy to be thread safe
            steps = steps == null ? new List<CrawlStep>() : new List<CrawlStep>(steps);
            steps.Add(new CrawlStep { Uri = uri });
            if (!CheckSupportedUri(uri))
                return false;
            try
            {
                ScanResult scanResult = null;

                if (!this.ScanResults.ContainsKey(uri))
                {
                    scanResult = new ScanResult();
                    this.ScanResults.Add(uri, scanResult);
                }
                else
                {
                    scanResult = this.ScanResults[uri];
                }

                HttpResponseMessage responseMessage;
                using (HttpClient client = new HttpClient())
                {

                    if (pageMayContainsLink)
                    {
                        responseMessage = await client.GetAsync(uri);
                    }
                    else
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, uri);
                        responseMessage = await client.SendAsync(request);
                    }
                }
                scanResult.Status = responseMessage.StatusCode;
                HttpStatusCode statusCode = responseMessage.StatusCode;
                result = responseMessage.IsSuccessStatusCode;
                MessageSeverity severity = responseMessage.IsSuccessStatusCode ? MessageSeverity.Info : MessageSeverity.Error;
                Logger($"{statusCode} {uri}", severity);
                int status = (int)statusCode;
                switch (status)
                {
                    case int s when s >= 200 && s < 300:
                        if (!pageMayContainsLink)
                            break;
                        bool isStillInSite = this.BaseUri.IsBaseOf(uri);
                        string contentType = responseMessage.Content.Headers.ContentType.MediaType;
                        bool isHtml = contentType == "text/html";
                        using (Stream stream = await responseMessage.Content.ReadAsStreamAsync()) 
                        {
                            HtmlDocument doc = null;
                            if (isHtml)
                            {
                                doc = GetHtmlDocument(responseMessage, stream);
                                if (isStillInSite && processChildrenLinks)
                                {
                                    await ProcessLinks(steps, uri, responseMessage, doc);
                                }
                            }
                            foreach (var extension in Extensions)
                            {
                                Task _ = Task.Run ( async () => await extension.Process(steps, uri, responseMessage, doc));
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
                this.ScanResults[uri] = new ScanResult { Exception = ex };
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
                this.ScanResults.Add(uri, new ScanResult { IsUnsupportedScheme = true });
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
        private async Task ProcessLinks(List<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument doc)
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
            List<Task> tasks = new List<Task>();
            foreach (var pair in tags2Attribute)
            {
                string tagName = pair.Key;
                string attributeName = pair.Value;
                IEnumerable<HtmlNode> links = documentNode.Descendants(tagName);
                foreach (var link in links)
                {
                    Task t = ScanLink(steps, uri, attributeName, link);
                    tasks.Add(t);
                }
            }
            await Task.WhenAll(tasks);
        }

        private async Task ScanLink(List<CrawlStep> steps, Uri uri, string attributeName, HtmlNode link)
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
                bool alreadyVisited = this.ScanResults.ContainsKey(derivedUri);
                if (!alreadyVisited)
                {
                    if (Config.OnlyCheckInnerLinks)
                    {
                        bool isStillInSite = this.BaseUri.IsBaseOf(derivedUri);
                        if (!isStillInSite)
                            return;
                    }
                    await Task.Run( async () => await Process(steps, uri, derivedUri, mayContainLink) );
                }
            }
        }
        public void LogException(Exception ex, Uri parentUri, Uri uri)
        {
            ExceptionLogger?.Invoke(ex, parentUri, uri);
        }
    }
}

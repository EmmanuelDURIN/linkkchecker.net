using HtmlAgilityPack;
using SpiderInterface;
using System.Collections.Immutable;
using System.Xml.Linq;


namespace SpiderEngine
{
    public class SiteMapChecker : ISpiderExtension
    {
        private IEnumerable<string> pageUrls = new List<string>();
        public IEngine? Engine { get; set; }
        public void Init()
        {
            ArgumentNullException.ThrowIfNull(Engine);
            try
            {
                Uri? baseUri = Engine.BaseUri;
                ArgumentNullException.ThrowIfNull(baseUri);
                Uri sitemapUri = new Uri(new Uri(baseUri.GetLeftPart(UriPartial.Authority)), "sitemap.xml");
                XElement xmlRoot = XElement.Load(sitemapUri.ToString());
                pageUrls = xmlRoot.Descendants(XName.Get(localName: "loc", namespaceName: "http://www.sitemaps.org/schemas/sitemap/0.9")).Select(elt => elt.Value.Trim());
            }
            catch (Exception ex)
            {
                Engine.Logger?.Invoke($"Error loading/reading sitemap.xml {ex.Message}", MessageSeverity.Error);
            }
        }
        public void Process(ImmutableList<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument? doc, CancellationToken c)
        {
        }
        public async Task Done(CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(Engine);
            foreach (string pageUrl in pageUrls)
            {
                Uri uriToCheck = new Uri(pageUrl);
                if (Engine.ScanResultCollection.ContainsKey(uriToCheck))
                {
                    Engine.Logger?.Invoke($"Sitemap url ok {pageUrl}", MessageSeverity.Success);
                }
                else
                {
                    bool result = await Engine.Process(steps: null, parentUri: null, uri: uriToCheck, pageMayContainsLink: false, processChildrenLinks: false, cancellationToken);
                    if (result)
                        Engine.Logger?.Invoke($"Sitemap url ok {pageUrl}", MessageSeverity.Success);
                    else
                        Engine.Logger?.Invoke($"Sitemap url not ok {pageUrl}", MessageSeverity.Error);
                }
            }
        }
    }
}
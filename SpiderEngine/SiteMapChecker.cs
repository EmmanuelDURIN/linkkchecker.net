using HtmlAgilityPack;
using SpiderInterface;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SpiderEngine
{
    public class SiteMapChecker : ISpiderExtension
  {
    private IEnumerable<string> pageUrls = new List<string>();
    public IEngine Engine { get; set; }
    public Task Init()
    {
      try
      {
        Uri sitemapUri = new Uri(new Uri(Engine.BaseUri.GetLeftPart(UriPartial.Authority)), "sitemap.xml");
        XElement xmlRoot = XElement.Load(sitemapUri.ToString());
        pageUrls = xmlRoot.Descendants(XName.Get(localName: "loc", namespaceName: "http://www.sitemaps.org/schemas/sitemap/0.9")).Select(elt => elt.Value.Trim());
      }
      catch (Exception ex)
      {
        Engine.Logger($"Error loading/reading sitemap.xml {ex.Message}", MessageSeverity.Error);
      }
      return Task.FromResult(0);
    }
    public Task Process(ImmutableStack<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument doc)
    {
      return Task.FromResult(0);
    }
    public async Task Done()
    {
      foreach (string pageUrl in pageUrls)
      {
        Uri uriToCheck = new Uri(pageUrl);
        if (Engine.ScanResults.ContainsKey(uriToCheck))
        {
          Engine.Logger($"Sitemap url ok {pageUrl}", MessageSeverity.Success);
        }
        else
        {
          bool result = await Task.Run( 
            async() => await Engine.Process
                (
                    steps: null, 
                    parentUri: null, 
                    uri: uriToCheck, 
                    pageMayContainsLink: false, 
                    processChildrenLinks: false
                )
          );
          if (result)
            Engine.Logger($"Sitemap url ok {pageUrl}", MessageSeverity.Success);
          else
            Engine.Logger($"Sitemap url not ok {pageUrl}", MessageSeverity.Error);
        }
      }
    }
  }
}
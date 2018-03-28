using ExCSS;
using HtmlAgilityPack;
using SpiderInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace SpiderEngine
{
  public class SiteMapChecker : ISpiderExtension
  {
    public CancellationToken CancellationToken { get; set; }
    private IEnumerable<String> pageUrls;
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
        Engine.Log($"Error loading/reading sitemap.xml {ex.Message}", MessageSeverity.Error);
      }
      return Task<int>.FromResult(0);
    }
    public Task ProcessHtml(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage, HtmlDocument doc)
    {
      return Task.FromResult<int>(0);
    }
    public async Task Done()
    {
      foreach (string pageUrl in pageUrls)
      {
        Uri uriToCheck = new Uri(pageUrl);
        if (Engine.ScanResults.ContainsKey(uriToCheck))
        {
          Engine.Log($"Sitemap url ok {pageUrl}", MessageSeverity.Success);
        }
        else
        {
          HttpStatusCode? status = await Engine.Process(steps: null, uri: uriToCheck, pageContainsLink: false, cancellationToken: CancellationToken, processChildrenLinks: false);
          if (status.HasValue && status.Value.IsSuccess() )
            Engine.Log($"Sitemap url ok {pageUrl}", MessageSeverity.Success);
          else
            Engine.Log($"Sitemap url not ok {pageUrl}", MessageSeverity.Error);
        }
      }
    }

    public Task ProcessCss(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage, StyleSheet styleSheet)
    {
      return Task.FromResult<int>(0);
    }

    public Task ProcessOther(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage)
    {
      return Task.FromResult<int>(0);
    }
  }
}
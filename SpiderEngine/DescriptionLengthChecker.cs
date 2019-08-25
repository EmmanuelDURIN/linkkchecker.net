using ExCSS;
using HtmlAgilityPack;
using SpiderInterface;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;



namespace SpiderEngine
{
  public class DescriptionLengthChecker : ISpiderExtension
  {
    private struct LengthCheckResult
    {
      public int Length { get; set; }
      public Uri Url { get; set; }
    }
    private List<LengthCheckResult> results = new List<LengthCheckResult>();
    // Longueur minimale des descriptions selon Google pour génération Sitemap.xml
    private readonly int DESCRIPTION_MIN_LENGTH = 160;

    public CancellationToken CancellationToken { get; set; }
    public IEngine Engine { get; set; }

    public Task Init()
    {
      return Task<int>.FromResult(0);
    }
    public Task ProcessHtml(Uri uri, ImmutableList<CrawlStep> steps, HttpResponseMessage responseMessage, HtmlDocument doc)
    {
      bool isStillInSite = steps[0].Uri.IsBaseOf(uri);
      if (!isStillInSite)
        return Task.FromResult<int>(0);
      HtmlNode documentNode = doc.DocumentNode;
      int length = 0;
      HtmlNode metaDescription = documentNode.SelectSingleNode("//meta[@name='description']");
      if (metaDescription != null)
      {
        String content = metaDescription.GetAttributeValue("content", "");
        if (content != null)
          length = content.Length;
      }
      results.Add(new LengthCheckResult { Url = uri, Length = length });
      return Task.FromResult<int>(0);
    }
    public Task Done()
    {
      foreach (var result in results.OrderByDescending(r => r.Length))
      {
        if (result.Length >= DESCRIPTION_MIN_LENGTH)
        {
          Engine.Log($"Description length for {result.Url} ok ({result.Length} chars)", MessageSeverity.Success);
        }
        else if (result.Length == 0)
        {
          Engine.Log($"No description for {result.Url} ", MessageSeverity.Error);
        }
        else
        {
          Engine.Log($"Description for {result.Url} is too short ({result.Length} chars)", MessageSeverity.Error);
        }
      }
      return Task<int>.FromResult(0);
    }
    public Task ProcessCss(Uri uri, ImmutableList<CrawlStep> steps, HttpResponseMessage responseMessage, StyleSheet styleSheet)
    {
      return Task.FromResult<int>(0);
    }
    public Task ProcessOther(Uri uri, ImmutableList<CrawlStep> steps, HttpResponseMessage responseMessage)
    {
      return Task.FromResult<int>(0);
    }
  }
}
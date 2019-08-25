using ExCSS;
using HtmlAgilityPack;
using SpiderInterface;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;



namespace SpiderEngine
{
  public class CanonicalChecker : ISpiderExtension
  {
    public CancellationToken CancellationToken { get; set; }
    public IEngine Engine { get; set; }
    public Task Done()
    {
      return Task<int>.FromResult(0);
    }
    public Task Init()
    {
      return Task<int>.FromResult(0);
    }

    public Task ProcessCss(Uri uri, ImmutableList<CrawlStep> steps, HttpResponseMessage responseMessage, StyleSheet styleSheet)
    {
      return Task.FromResult<int>(0);
    }
    public Task ProcessHtml(Uri uri, ImmutableList<CrawlStep> steps, HttpResponseMessage responseMessage, HtmlDocument doc)
    {
      bool isStillInSite = steps[0].Uri.IsBaseOf(uri);
      if (!isStillInSite)
        return Task.FromResult<int>(0);
      HtmlNode documentNode = doc.DocumentNode;
      HtmlNode canonicalLink = documentNode.SelectSingleNode("//link[@rel='canonical']");
      bool isChecked = false;
      if (canonicalLink != null)
      {
        string canonicalHref = canonicalLink.GetAttributeValue("href", "");
        if (!String.IsNullOrEmpty(canonicalHref))
        {
          isChecked = true;
          Uri canonicalHrefUri = new Uri(canonicalHref);
          bool allIsOk = true;
          if (canonicalHrefUri.Segments.Length != uri.Segments.Length)
            allIsOk = false;
          else
          {
            for (int i = 1; i < canonicalHrefUri.Segments.Length; i++)
            {
              String segmentRequestedUri = Uri.EscapeUriString(uri.Segments[i]).ToLower();
              String segmentCanonicalUri = Uri.EscapeUriString(canonicalHrefUri.Segments[i]).ToLower();
              if (segmentCanonicalUri != segmentRequestedUri)
              {
                allIsOk = false;
                break;
              }
            }
          }
          if (!allIsOk)
          {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Canonical link  : {canonicalHref} doesn't match for page {uri}");
            Console.ForegroundColor = ConsoleColor.White;
          }
          else
          {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Canonical link of {uri} is ok : {canonicalHref}");
            Console.ForegroundColor = ConsoleColor.White;
          }
        }
      }
      if (!isChecked)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"No canonical link in page {uri}");
        Console.ForegroundColor = ConsoleColor.White;
      }
      return Task.FromResult<int>(0);
    }

    public Task ProcessOther(Uri uri, ImmutableList<CrawlStep> steps, HttpResponseMessage responseMessage)
    {
      return Task.FromResult<int>(0);
    }
  }
}
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



namespace SpiderEngine
{
  public class CssLinkChecker : ISpiderExtension
  {
    public CancellationToken CancellationToken { get; set; }
    public IEngine Engine { get; set; }

    public Task Init()
    {
      return Task<int>.FromResult(0);
    }
    public async Task Process(List<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument doc)
    {
      try
      {
        Uri parentUri = null;
        if (steps.Count > 1)
          parentUri = steps[steps.Count - 2].Uri;
        String contentType = responseMessage.Content.Headers.ContentType.MediaType;
        bool isCss = contentType == "text/css";
        if (isCss)
        {
          var parser = new Parser();
          StyleSheet styleSheet = parser.Parse(await responseMessage.Content.ReadAsStringAsync());
          foreach (StyleRule rule in styleSheet.StyleRules)
          {
            foreach (Property property in rule.Declarations)
            {
              PrimitiveTerm primitiveTerm = property.Term as PrimitiveTerm;
              if (primitiveTerm != null && property.Name == "background-image")
              {
                if (primitiveTerm.ToString().Contains("url"))
                {
                  Uri imageUrl = new Uri(primitiveTerm.Value.ToString());
                  if (!Engine.ScanResults.ContainsKey(imageUrl))
                  {
                    HttpStatusCode? status = await Engine.Process(new List<CrawlStep>(steps), uri: imageUrl, pageContainsLink: false, cancellationToken: CancellationToken, processChildrenLinks: false);
                    Engine.ScanResults.AddOrReplace(imageUrl, new ScanResult { Status = status, });
                    Engine.LogResult(uri,parentUri, status.Value);
                  }
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        Engine.Logger($"Exception checking CSS for ${uri} : {ex.Message}", MessageSeverity.Error);
      }
    }
    public Task Done()
    {
      return Task<int>.FromResult(0);
    }
  }
}
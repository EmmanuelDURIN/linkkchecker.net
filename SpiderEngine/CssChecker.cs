using ExCSS;
using SpiderInterface;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderEngine
{
  internal class CssChecker
  {
    internal Engine Engine { get; set; }
    private Regex urlExtractor = new Regex(@"url\('?([^']+)'?\)", RegexOptions.ECMAScript |RegexOptions.Compiled);

    internal async Task<StyleSheet> ParseCss(ImmutableList<CrawlStep> steps, Uri uri, string cssContent, CancellationToken cancellationToken)
    {
      // TIP : embeded Css could be prevented from rescanning with computed hash 
      if (cssContent == null)
        return null;
      // Note : the parser should not be shared between threads
      StyleSheet styleSheet = null;
      try
      {
        styleSheet = new ExCSS.Parser().Parse(cssContent);
        await CheckBackgroundUrls(steps, uri, styleSheet, cancellationToken);
        await CheckFonts(steps, uri, styleSheet, cancellationToken);
      }
      catch (Exception ex)
      {
        Engine.Log($"Error checking css in {uri}, exception is {ex}", MessageSeverity.Error);
      }
      return styleSheet;
    }
    private async Task CheckFonts(ImmutableList<CrawlStep> steps, Uri uri, StyleSheet styleSheet, CancellationToken cancellationToken)
    {
      //@font - face {
      //  font - family: 'RalewayMedium';
      //  src: url('/fonts/raleway/Raleway-Medium.ttf') format('truetype'), url('/fonts/Raleway/Raleway-Medium.woff') format('woff');
      //}
      foreach (FontFaceRule rule in styleSheet.FontFaceDirectives)
      {
        if (rule.Src != null)
        {
          MatchCollection matches = urlExtractor.Matches(rule.Src);
          if (matches.Count > 1)
          {
            foreach (Match match in matches)
            {
              // Groups[0] contains full expression
              // Groups[1] contains Url
              String relativeFontUrl = match.Groups[1].Value;
              Uri absoluteFontUrl = uri.GetDerivedUri(relativeFontUrl);
              if (!Engine.ScanResults.ContainsKey(absoluteFontUrl))
              {
                HttpStatusCode? status = await Engine.Process(steps, uri: absoluteFontUrl, pageContainsLink: false, cancellationToken: cancellationToken, processChildrenLinks: false);
              }
            }
          }
        }
      }
    }

    private async Task CheckBackgroundUrls(ImmutableList<CrawlStep> steps, Uri uri, StyleSheet styleSheet, CancellationToken cancellationToken)
    {
      foreach (StyleRule rule in styleSheet.StyleRules)
      {
        foreach (Property property in rule.Declarations)
        {
          PrimitiveTerm primitiveTerm = property.Term as PrimitiveTerm;
          if (primitiveTerm != null && property.Name == "background-image")
          {
            if (primitiveTerm.ToString().Contains("url"))
            {
              Uri derivedImageUrl = uri.GetDerivedUri(primitiveTerm.Value.ToString());
              if (!Engine.ScanResults.ContainsKey(derivedImageUrl))
              {
                HttpStatusCode? status = await Engine.Process(steps, uri: derivedImageUrl, pageContainsLink: false, cancellationToken: cancellationToken, processChildrenLinks: false);
              }
            }
          }
        }
      }
    }
  }
}
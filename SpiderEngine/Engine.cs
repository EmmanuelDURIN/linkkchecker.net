﻿using HtmlAgilityPack;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SpiderInterface;
using ExCSS;

// TODO Extension : Collecter les stats Google Page Speed Insights

// TODO checker qu'une page ne pointe pas sur elle même

// Gérer les liens vers autre CSS 

// Faire des warning sur les redirection, pb d'expiration header

// TODO  CSS inlined in page not parsed versus CSS linked to is parsed
// TODO UsedImagesChecker should use list of images from Engine.ScanResults instead of building own list

// TODO Check ico links

// DONE Faire un mode sans Head mais avec des GET réels pour avoir le warmup du site :pas utile le warmup a lieu avec des head

namespace SpiderEngine
{
  public class Engine : IEngine
  {
    public ScanResults ScanResults { get; set; } = new ScanResults();
    public List<ISpiderExtension> Extensions { get; set; } = new List<ISpiderExtension>();
    public Action<Exception, Uri, Uri> ExceptionLogger { get; set; }
    public Uri BaseUri { get; set; }
    public Action<String, MessageSeverity> Log { get; set; }
    private EngineConfig config;

    public EngineConfig Config
    {
      get => config;
      set
      {
        config = value;
        if (config != null)
        {
          foreach (var extension in config.Extensions)
          {
            extension.Engine = this;
            Extensions.Add(extension);
          }
        }
      }
    }
    private Stopwatch stopwatch = new Stopwatch();
    public async Task Start(CancellationToken cancellationToken)
    {
      BaseUri = new Uri(Config.StartUri.GetLeftPart(UriPartial.Authority));
      await Init(cancellationToken);
      try
      {
        await Process(new List<CrawlStep>(), uri: Config.StartUri, pageContainsLink: true, cancellationToken: cancellationToken);
      }
      catch (TaskCanceledException)
      {
      }
      catch (Exception ex)
      {
        LogException(ex, null, BaseUri);
      }
      await Done();
    }
    private async Task Init(CancellationToken cancellationToken)
    {
      Log($"Starting crawl at {DateTime.Now}", MessageSeverity.Info);
      foreach (var extension in config.Extensions)
        extension.CancellationToken = cancellationToken;
      stopwatch.Start();
      foreach (var extension in Extensions)
      {
        Task t = await Task.Factory.StartNew(
          async () =>
          {
            await extension.Init();
          }
        );
      }
    }
    private async Task Done()
    {
      foreach (var extension in Extensions)
      {
        Task t = await Task.Factory.StartNew(
          async () =>
          {
            await extension.Done();
          }
        );
      }
      stopwatch.Stop();
      Log($"Finished crawling at {DateTime.Now}", MessageSeverity.Success);
      Log($"Elapsed Time {stopwatch.Elapsed}", MessageSeverity.Info);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="steps"></param>
    /// <param name="uri"></param>
    /// <param name="pageContainsLink"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="processChildrenLinks">Set to true if an extension needs to use the engine to check a link</param>
    /// <returns>true if page is found</returns>
    public async Task<HttpStatusCode?> Process(List<CrawlStep> steps, Uri uri, bool pageContainsLink, CancellationToken cancellationToken, bool processChildrenLinks = true)
    {
      Uri parentUri = null;
      if (steps.Count > 1)
        parentUri = steps[steps.Count - 2].Uri;

      HttpStatusCode? statusCode = null;
      // Make a copy to be thread safe
      steps = new List<CrawlStep>(steps);
      steps.Add(new CrawlStep { Uri = uri });
      if (!CheckSupportedUri(uri))
        return null;
      try
      {
        ScanResult scanResult = null;
        if (ScanResults.TryGetScanResult(uri, out scanResult))
          return null;
        HttpResponseMessage responseMessage = await RequestDocument(uri, pageContainsLink, cancellationToken);
        statusCode = scanResult.Status = responseMessage.StatusCode;
        LogResult(uri, parentUri, responseMessage.StatusCode);
        switch ((int)responseMessage.StatusCode)
        {
          case int s when s >= 200 && s < 300:
            bool isStillInSite = this.BaseUri.IsBaseOf(uri);
            String contentType = responseMessage.Content.Headers.ContentType.MediaType;
            scanResult.ContentType = contentType;

            HtmlDocument doc = null;
            if (contentType == "text/html" && pageContainsLink)
            {
              using (Stream stream = await responseMessage.Content.ReadAsStreamAsync())
                doc = await GetHtmlDocument(responseMessage, stream);
              if (isStillInSite && processChildrenLinks)
              {
                await ScanHtmlLinks(steps, uri, responseMessage, doc, cancellationToken);
              }
              await ProcessEmbededCss(steps, uri, doc, cancellationToken);
            }

            StyleSheet styleSheet = null;
            if (contentType == "text/css")
              styleSheet = await ParseCss(steps, uri, await responseMessage.Content.ReadAsStringAsync(), cancellationToken);

            await ApplyExtensions(steps, uri, responseMessage, doc, styleSheet);

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
      catch (TaskCanceledException) { }
      catch (Exception ex)
      {
        LogException(ex, parentUri, uri);
        ScanResults.Replace(uri, new ScanResult { Exception = ex });
      }
      return statusCode;
    }
    private async Task ApplyExtensions(List<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument doc, StyleSheet styleSheet)
    {
      foreach (var extension in Extensions)
      {
        Task t = await Task.Factory.StartNew(
          async () =>
          {
            try
            {
              if (doc != null)
                await extension.ProcessHtml(uri, steps, responseMessage, doc);
              else if (styleSheet != null)
                await extension.ProcessCss(uri, steps, responseMessage, styleSheet);
              else
                await extension.ProcessOther(uri, steps, responseMessage);
            }
            catch (Exception ex)
            {
              Log($"Processing Error with extension {extension.GetType()} {ex}", MessageSeverity.Error);
            }
          }
        );
      }
    }

    private async Task<HttpResponseMessage> RequestDocument(Uri uri, bool pageContainsLink, CancellationToken cancellationToken)
    {
      HttpClient client = new HttpClient();
      // No need to ask page contents with no link
      HttpMethod method = pageContainsLink ? HttpMethod.Get : HttpMethod.Head;
      HttpRequestMessage request = new HttpRequestMessage(method, uri);
      //Log($"\tRequesting {method} {uri}", MessageSeverity.Debug);
      HttpResponseMessage responseMessage = await client.SendAsync(request, cancellationToken);
      //Log($"\tRequested  {method} {uri}" + uri, MessageSeverity.Debug);
      return responseMessage;
    }

    public void LogResult(Uri uri, Uri parentUri, HttpStatusCode? statusCode)
    {
      MessageSeverity severity = MessageSeverity.Error;
      if ( statusCode?.IsSuccess() == true)
        severity = MessageSeverity.Info;
      if (parentUri != null)
        Log($"{statusCode} for {uri} in {parentUri}", severity);
      else
        Log($"{statusCode} for {uri}", severity);
    }
    private bool CheckSupportedUri(Uri uri)
    {
      string scheme = uri.Scheme;
      if (!supportedSchemes.Contains(scheme.ToLower()))
      {
        ScanResults.AddOrReplace(uri, new ScanResult { IsUnsupportedScheme = true });
        Log($"Unsupported scheme {scheme} for {uri}", MessageSeverity.Warn);
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
        // TODO : add frame, iframe, meta, form, ...
      };
    private static Task<HtmlDocument> GetHtmlDocument(HttpResponseMessage responseMessage, Stream stream)
    {
      HtmlDocument doc = new HtmlDocument();
      doc.Load(stream, Encoding.UTF8);
      return Task<HtmlDocument>.FromResult(doc);
    }
    private async Task<StyleSheet> ParseCss(List<CrawlStep> steps, Uri uri, string cssContent, CancellationToken cancellationToken)
    {
      if (cssContent == null)
        return null;
      // TODO follow font files
      // Note : the parser should not be shared between threads
      StyleSheet styleSheet = new ExCSS.Parser().Parse(cssContent);
      String sImageUrl = null;
      try
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
                sImageUrl = primitiveTerm.Value.ToString();
                Uri derivedImageUrl = uri.GetDerivedUri(sImageUrl);
                if (!ScanResults.ContainsKey(derivedImageUrl))
                {
                  HttpStatusCode? status = await Process(new List<CrawlStep>(steps), uri: derivedImageUrl, pageContainsLink: false, cancellationToken: cancellationToken, processChildrenLinks: false);
                }
              }
            }
          }
        }
      }
      catch (Exception ex)
      {
        Log($"Error checking css in {uri}, exception is {ex}", MessageSeverity.Error);
      }
      return styleSheet;
    }

    private async Task ProcessEmbededCss(List<CrawlStep> steps, Uri uri, HtmlDocument doc, CancellationToken cancellationToken)
    {
      HtmlNode documentNode = doc.DocumentNode;
      IEnumerable<HtmlNode> styleTags = documentNode.Descendants("style");
      foreach (var styleTag in styleTags)
      {
        String cssContent = styleTag.InnerHtml;
        await this.ParseCss(steps, uri, cssContent, cancellationToken);
      }
    }
    private async Task ScanHtmlLinks(List<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument doc, CancellationToken cancellationToken)
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
        String tagName = pair.Key;
        String attributeName = pair.Value;
        IEnumerable<HtmlNode> links = documentNode.Descendants(tagName);
        List<Task> tasks = new List<Task>();
        foreach (var link in links)
        {
          Task task = ProcessLink(steps, uri, attributeName, link, cancellationToken);
          tasks.Add(task);
        }
        await Task.WhenAll(tasks);
      }
    }

    private async Task ProcessLink(List<CrawlStep> steps, Uri uri, string attributeName, HtmlNode link, CancellationToken cancellationToken)
    {
      bool mayContainLink = link.Name.ToLower() == "a";
      bool isCssLink = link.Name.ToLower() == "link" && link.GetAttributeValue("rel", "") == "stylesheet";
      mayContainLink |= isCssLink;
      string relativeUrl = link.GetAttributeValue(attributeName, def: null);
      if (relativeUrl != null)
      {
        Uri derivedUri = uri.GetDerivedUri(relativeUrl);
        bool alreadyVisited = ScanResults.ContainsKey(derivedUri);
        if (!alreadyVisited)
        {
          Task t = await Task.Factory.StartNew(
            async () =>
            {
              try
              {
                await Process(steps, derivedUri, mayContainLink, cancellationToken);
              }
              catch (TaskCanceledException)
              {
              }
            }
          );
          await t;
        }
      }
    }
    public void LogException(Exception ex, Uri parentUri, Uri uri)
    {
      ExceptionLogger?.Invoke(ex, parentUri, uri);
    }
  }
}

using HtmlAgilityPack;
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

// TODO Extension : Collecter les stats Google Page Speed Insights

// TODO : Créer le batch et mettre LinkChecker en repo Github

// TODO checker qu'une page ne pointe pas sur elle même

// Gérer les liens vers autre CSS 

// Faire un mode sans Head mais avec des GET réels pour avoir le warmup du site

// Faire des warning sur les redirection, pb d'expiration header

// https

// plugin pour lister les images (trouver les images en trop sur le site ? )

namespace SpiderEngine
{
  public class Engine : IEngine
  {
    public ScanResults ScanResults { get; set; } = new ScanResults();
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
      Logger($"Starting crawl at {DateTime.Now}", MessageSeverity.Info);
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
      Logger($"Finished crawling at {DateTime.Now}", MessageSeverity.Success);
      Logger($"Elapsed Time {stopwatch.Elapsed}", MessageSeverity.Info);
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
      HttpStatusCode? statusCode = null;
      // Make a copy to be thread safe
      steps = new List<CrawlStep>(steps);
      steps.Add(new CrawlStep { Uri = uri });
      HttpResponseMessage responseMessage = null;
      if (!CheckSupportedUri(uri))
        return null;
      try
      {
        ScanResult scanResult = ScanResults.FindOrCreateAndReturn(uri);
        HttpClient client = new HttpClient();
        if (pageContainsLink)
        {
          responseMessage = await client.GetAsync(uri, cancellationToken);
        }
        else
        {
          HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, uri);
          responseMessage = await client.SendAsync(request, cancellationToken);
        }
        scanResult.Status = responseMessage.StatusCode;
        statusCode = responseMessage.StatusCode;
        LogResult(uri, responseMessage.StatusCode);
        int status = (int)responseMessage.StatusCode;
        switch (status)
        {
          case int s when s >= 200 && s < 300:
            if (!pageContainsLink)
              break;
            bool isStillInSite = this.BaseUri.IsBaseOf(uri);
            using (Stream stream = await responseMessage.Content.ReadAsStreamAsync())
            {
              HtmlDocument doc = null;
              String contentType = responseMessage.Content.Headers.ContentType.MediaType;
              bool isHtml = contentType == "text/html";
              if (isHtml)
              {
                doc = await GetHtmlDocument(responseMessage, stream);
                if (isStillInSite && processChildrenLinks)
                {
                  await ProcessLinks(steps, uri, responseMessage, doc, cancellationToken);
                }
              }
              foreach (var extension in Extensions)
              {
                Task t = await Task.Factory.StartNew(
                  async () =>
                  {
                    await extension.Process(steps, uri, responseMessage, doc);
                  }
                );
              }
            }
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
        Uri parentUri = null;
        if (steps.Count > 1)
          parentUri = steps[steps.Count - 2].Uri;
        LogException(ex, parentUri, uri);
        ScanResults.Replace(uri, new ScanResult { Exception = ex });
      }
      return statusCode;
    }
    public void LogResult(Uri uri, HttpStatusCode statusCode)
    {
      MessageSeverity severity = statusCode.IsSuccess() ? MessageSeverity.Info : MessageSeverity.Error;
      Logger($"{statusCode} {uri}", severity);
    }
    private bool CheckSupportedUri(Uri uri)
    {
      string scheme = uri.Scheme;
      if (!supportedSchemes.Contains(scheme.ToLower()))
      {
        ScanResults.AddOrReplace(uri, new ScanResult { IsUnsupportedScheme = true });
        Logger($"Unsupported scheme {scheme} for {uri}", MessageSeverity.Warn);
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
    private static Task<HtmlDocument> GetHtmlDocument(HttpResponseMessage responseMessage, Stream stream)
    {
      HtmlDocument doc = new HtmlDocument();
      doc.Load(stream, Encoding.UTF8);
      return Task<HtmlDocument>.FromResult(doc);
    }
    private async Task ProcessLinks(List<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument doc, CancellationToken cancellationToken)
    {
      // Pour obtenir l'encodage
      // Attention si on avance le curseur, on ne peut plus lirefP le flux
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
          Task task = ScanLink(steps, uri, attributeName, link, cancellationToken);
          tasks.Add(task);
        }
        await Task.WhenAll(tasks);
      }
    }

    private async Task ScanLink(List<CrawlStep> steps, Uri uri, string attributeName, HtmlNode link, CancellationToken cancellationToken)
    {
      bool mayContainLink = link.Name.ToLower() == "a";
      bool isCssLink = link.Name.ToLower() == "link" && link.GetAttributeValue("rel", "") == "stylesheet";
      mayContainLink |= isCssLink;
      String ATTR_DEFAULT_VALUE = "";
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

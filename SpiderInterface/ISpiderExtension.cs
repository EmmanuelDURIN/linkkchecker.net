using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ExCSS;
using HtmlAgilityPack;

namespace SpiderInterface
{
  public interface ISpiderExtension
  {
    IEngine Engine { get; set; }
    CancellationToken CancellationToken { get; set; }
    Task Init();
    Task ProcessHtml(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage, HtmlDocument doc);
    Task ProcessCss(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage, StyleSheet styleSheet);
    /// <summary>
    /// Process Non html and non css documents
    /// </summary>
    /// <returns></returns>
    Task ProcessOther(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage);
    //Task Process(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage, HtmlDocument doc, StyleSheet styleSheet);
    Task Done();
  }
}
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SpiderInterface
{
  public interface ISpiderExtension
  {
    IEngine Engine { get; set; }
    CancellationToken CancellationToken { get; set; }
    Task Init();
    Task Process(Uri uri, List<CrawlStep> steps, HttpResponseMessage responseMessage, HtmlDocument doc);
    Task Done();
  }
}
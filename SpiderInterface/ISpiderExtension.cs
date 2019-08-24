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

    Task Process(List<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument doc);
    Task Done();
    Task Init();
  }
}
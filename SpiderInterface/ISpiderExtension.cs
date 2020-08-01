using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SpiderInterface
{
    public interface ISpiderExtension
    {
        IEngine Engine { get; set; }

        Task Init();
        Task Process(ImmutableStack<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument doc);
        Task Done();
    }
}
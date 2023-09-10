using HtmlAgilityPack;
using System.Collections.Immutable;

namespace SpiderInterface
{
    public interface ISpiderExtension
    {
        IEngine? Engine { get; set; }
        void Init();
        void Process(ImmutableList<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument? doc);
        Task Done();
    }
}
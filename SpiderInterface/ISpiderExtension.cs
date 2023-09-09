using HtmlAgilityPack;

namespace SpiderInterface
{
    public interface ISpiderExtension
    {
        IEngine? Engine { get; set; }
        void Init();
        void Process(List<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument? doc);
        void Done();
    }
}
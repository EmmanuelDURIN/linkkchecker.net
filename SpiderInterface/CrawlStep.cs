namespace SpiderInterface
{
    public class CrawlStep
    {
        public CrawlStep(Uri uri)
        {
            Uri = uri;
        }
        public Uri Uri { get; set; }
    }
}
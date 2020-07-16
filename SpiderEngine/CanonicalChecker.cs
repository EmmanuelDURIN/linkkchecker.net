using HtmlAgilityPack;
using SpiderInterface;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SpiderEngine
{
    public class CanonicalChecker : ISpiderExtension
    {
        public IEngine Engine { get; set; }
        public Task Done()
        {
            return Task.FromResult(0);
        }
        public Task Init()
        {
            return Task.FromResult(0);
        }
        public Task Process(List<CrawlStep> steps, Uri uri, HttpResponseMessage responseMessage, HtmlDocument doc)
        {
            bool isStillInSite = steps[0].Uri.IsBaseOf(uri);
            if (!isStillInSite)
                return Task.FromResult(0);
            if (responseMessage.Content.Headers.ContentType.MediaType != "text/html")
                return Task.FromResult(0);
            //bool isCss = contentType == "text/css";
            HtmlNode documentNode = doc.DocumentNode;
            HtmlNode canonicalLink = documentNode.SelectSingleNode("//link[@rel='canonical']");
            bool isChecked = false;
            if (canonicalLink != null)
            {
                string canonicalHref = canonicalLink.GetAttributeValue("href", "");
                if (!String.IsNullOrEmpty(canonicalHref))
                {
                    isChecked = true;
                    Uri canonicalHrefUri = new Uri(canonicalHref);
                    bool allIsOk = true;
                    if (canonicalHrefUri.Segments.Length != uri.Segments.Length)
                        allIsOk = false;
                    else
                    {
                        for (int i = 1; i < canonicalHrefUri.Segments.Length; i++)
                        {
                            string segmentRequestedUri = Uri.EscapeUriString(uri.Segments[i]).ToLower();
                            string segmentCanonicalUri = Uri.EscapeUriString(canonicalHrefUri.Segments[i]).ToLower();
                            if (segmentCanonicalUri != segmentRequestedUri)
                            {
                                allIsOk = false;
                                break;
                            }
                        }
                    }
                    if (!allIsOk)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Canonical link  : {canonicalHref} doesn't match for page {uri}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Canonical link of {uri} is ok : {canonicalHref}");
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                }
            }
            if (!isChecked)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"No canonical link in page {uri}");
                Console.ForegroundColor = ConsoleColor.White;
            }
            return Task.FromResult(0);
        }
    }
}
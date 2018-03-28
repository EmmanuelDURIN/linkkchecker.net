using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderInterface
{
  public interface IEngine
  {
    Uri BaseUri { get; set; }
    Action<Exception, Uri, Uri> ExceptionLogger { get; set; }
    List<ISpiderExtension> Extensions { get; set; }
    Action<string, MessageSeverity> Log { get; set; }
    ScanResults ScanResults { get; set; } 
    Task<HttpStatusCode?> Process(List<CrawlStep> steps, Uri uri, bool pageContainsLink, CancellationToken cancellationToken, bool processChildrenLinks = true);
    void LogResult(Uri uri, Uri parentUri, HttpStatusCode? statusCode);
  }
}
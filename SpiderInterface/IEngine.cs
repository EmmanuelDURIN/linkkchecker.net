using System;
using System.Threading;
using System.Threading.Tasks;
using SpiderInterface;

namespace SpiderInterface
{
  public interface IEngine
  {
    Uri BaseUri { get; set; }
    Action<Exception, Uri, Uri> ExceptionLogger { get; set; }
    System.Collections.Generic.List<ISpiderExtension> Extensions { get; set; }
    Action<string, MessageSeverity> Logger { get; set; }
    System.Collections.Generic.Dictionary<Uri, ScanResult> ScanResults { get; set; }

    void LogException(Exception ex, Uri parentUri, Uri uri);
    bool Process(System.Collections.Generic.List<CrawlStep> steps, Uri parentUri, Uri uri, bool pageContainsLink, bool processChildrenLinks = true);
  }
}
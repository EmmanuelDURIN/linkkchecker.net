using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SpiderInterface
{
    public interface IEngine
    {
        Uri BaseUri { get; set; }
        Action<Exception, Uri, Uri> ExceptionLogger { get; set; }
        Action<string, MessageSeverity> Logger { get; set; }
        void LogException(Exception ex, Uri parentUri, Uri uri);
        List<ISpiderExtension> Extensions { get; set; }
        Dictionary<Uri, ScanResult> ScanResults { get; set; }
        Task<bool> Process(List<CrawlStep> steps, Uri parentUri, Uri uri, CancellationToken cancellationToken, bool pageMayContainsLink, bool processChildrenLinks = true);
    }
}
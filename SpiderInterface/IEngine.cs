using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        ConcurrentDictionary<Uri, ScanResult> ScanResults { get; set; }
        Task<bool> Process(ImmutableStack<CrawlStep> steps, Uri parentUri, Uri uri, bool pageMayContainsLink, bool processChildrenLinks = true);
    }
}
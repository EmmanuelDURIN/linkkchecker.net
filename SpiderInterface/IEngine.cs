﻿using System;
using System.Collections.Generic;

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
        bool Process(List<CrawlStep> steps, Uri parentUri, Uri uri, bool pageMayContainsLink, bool processChildrenLinks = true);
    }
}
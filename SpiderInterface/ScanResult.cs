using System;
using System.Net;

namespace SpiderInterface
{
  public class ScanResult
  {
    public HttpStatusCode? Status { get; set; }
    public Exception Exception { get; set; }
    public bool IsUnsupportedScheme { get; set; }
  }
}
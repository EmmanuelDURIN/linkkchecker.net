using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace SpiderEngine
{
  public static class UriExtensions
  {
    public static Uri GetDerivedUri(this Uri baseUrl, string relativeUrl)
    {
      Uri derivedUri = relativeUrl.ToLower().StartsWith("http") ? new Uri(relativeUrl) : new Uri(baseUrl, relativeUrl);
      // To unescape #xxx; Html entities :
      String decodedUri = WebUtility.HtmlDecode(derivedUri.ToString());
      // Remove internal links starting with #
      int indexOfHash = decodedUri.LastIndexOf('#');
      if (indexOfHash != -1)
      {
        decodedUri = decodedUri.Substring(0, indexOfHash);
      }
      return new Uri(decodedUri);
    }
  }
}
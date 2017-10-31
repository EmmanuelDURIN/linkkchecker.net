using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SpiderInterface
{
  public class ScanResults
  {
    private ReaderWriterLockSlim slimLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

    private Dictionary<Uri, ScanResult> results { get; set; } = new Dictionary<Uri, ScanResult>();
    public int FailureCount { get => results.Count(sr => (sr.Value.Status != null && sr.Value.Status.Value.IsSuccess()) || sr.Value.Exception != null); }
    public ScanResult FindOrCreateAndReturn(Uri uri)
    {
      ScanResult scanResult = null;
      slimLock.EnterWriteLock();
      try
      {
        if (!results.ContainsKey(uri))
        {
          scanResult = new ScanResult();
          results.Add(uri, scanResult);
        }
        else
        {
          scanResult = results[uri];
        }
      }
      finally
      {
        slimLock.ExitWriteLock();
      }
      return scanResult;
    }

    public void Replace(Uri uri, ScanResult scanResult)
    {
      slimLock.EnterWriteLock();
      try
      {
        if (results.ContainsKey(uri))
        {
          results.Remove(uri);
        }
        results.Add(uri, scanResult);
      }
      finally
      {
        slimLock.ExitWriteLock();
      }
    }
    public void AddOrReplace(Uri uri, ScanResult scanResult)
    {
      slimLock.EnterWriteLock();
      try
      {
        if (results.ContainsKey(uri))
        {
          results[uri] = scanResult;
        }
        else
        { 
          results.Add(uri, scanResult);
        }
      }
      finally
      {
        slimLock.ExitWriteLock();
      }
    }
    public bool ContainsKey(Uri uri)
    {
      slimLock.EnterReadLock();
      try
      {
        return results.ContainsKey(uri);
      }
      finally
      {
        slimLock.ExitReadLock();
      }
    }
  }
}
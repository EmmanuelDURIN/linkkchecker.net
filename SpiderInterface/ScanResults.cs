using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SpiderInterface
{
    public class ScanResults : IEnumerable<KeyValuePair<Uri, ScanResult>>
    {

        private ReaderWriterLockSlim scanResultsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private Dictionary<Uri, ScanResult> results { get; set; } = new Dictionary<Uri, ScanResult>();
        public int FailureCount { get => results.Count(sr => (sr.Value.Status != null && sr.Value.Status.Value.IsSuccess()) || sr.Value.Exception != null); }
        public bool TryGetScanResult(Uri uri, out ScanResult scanResult)
        {
            bool result;
            scanResultsLock.EnterUpgradeableReadLock();
            try
            {
                if (!(result = results.ContainsKey(uri)))
                {
                    scanResult = new ScanResult();
                    try
                    {
                        scanResultsLock.EnterWriteLock();
                        results.Add(uri, scanResult);
                    }
                    finally
                    {
                        scanResultsLock.ExitWriteLock();
                    }
                }
                else
                {
                    scanResult = results[uri];
                }
            }
            finally
            {
                scanResultsLock.ExitUpgradeableReadLock();
            }
            return result;
        }

        public void Replace(Uri uri, ScanResult scanResult)
        {
            scanResultsLock.EnterWriteLock();
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
                scanResultsLock.ExitWriteLock();
            }
        }
        public void AddOrReplace(Uri uri, ScanResult scanResult)
        {
            scanResultsLock.EnterWriteLock();
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
                scanResultsLock.ExitWriteLock();
            }
        }
        public bool ContainsKey(Uri uri)
        {
            scanResultsLock.EnterReadLock();
            try
            {
                return results.ContainsKey(uri);
            }
            finally
            {
                scanResultsLock.ExitReadLock();
            }
        }
        public IEnumerator<KeyValuePair<Uri, ScanResult>> GetEnumerator()
        {
            foreach (var sr in results)
            {
                yield return sr;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<Uri, ScanResult>>)this).GetEnumerator();
        }
    }
}
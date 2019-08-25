using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SpiderInterface
{
    public class ScanResults : IEnumerable<KeyValuePair<Uri, ScanResult>>
    {
        private ConcurrentDictionary<Uri, ScanResult> results { get; set; } = new ConcurrentDictionary<Uri, ScanResult>();
        public int FailureCount { get => results.Count(sr => (sr.Value.Status != null && sr.Value.Status.Value.IsSuccess()) || sr.Value.Exception != null); }
        public bool TryGetScanResult(Uri uri, out ScanResult scanResult)
        {
            return results.TryGetValue(uri, out scanResult);
        }
        //public void Replace(Uri uri, ScanResult scanResult)
        //{
        //    scanResultsLock.EnterWriteLock();
        //    try
        //    {
        //        if (results.ContainsKey(uri))
        //        {
        //            results.Remove(uri);
        //        }
        //        results.Add(uri, scanResult);
        //    }
        //    finally
        //    {
        //        scanResultsLock.ExitWriteLock();
        //    }
        //}
        public void AddOrReplace(Uri uri, ScanResult scanResult)
        {
            results.AddOrUpdate(
                key: uri, 
                addValueFactory: uriKey => scanResult,
                updateValueFactory: (key,oldValue) => scanResult
                );
        }
        public bool ContainsKey(Uri uri)
        {
            return results.ContainsKey(uri);
        }
        public IEnumerator<KeyValuePair<Uri, ScanResult>> GetEnumerator()
        {
            return results.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<Uri, ScanResult>>)this).GetEnumerator();
        }
    }
}
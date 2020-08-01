using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace SpiderInterface
{
    public class ScanResults : IEnumerable<KeyValuePair<Uri, ScanResult>>
    {
        private ReaderWriterLockSlim scanResultsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private Dictionary<Uri, ScanResult> results = new Dictionary<Uri, ScanResult>();

        public void Add(Uri key, ScanResult value)
        {
            scanResultsLock.EnterWriteLock();
            try
            {
                results.Add(key, value);
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
            scanResultsLock.EnterReadLock();
            try
            {
                Dictionary<Uri, ScanResult> _results = new Dictionary<Uri, ScanResult>();
                foreach (var pair in results)
                {
                    _results.Add(pair.Key, pair.Value);
                }
                return _results.GetEnumerator();
            }
            finally
            {
                scanResultsLock.ExitReadLock();
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this).GetEnumerator();
        }
        public ScanResult this[Uri key]
        {
            get
            {
                scanResultsLock.EnterReadLock();
                try
                {
                    return results[key];
                }
                finally
                {
                    scanResultsLock.ExitReadLock();
                }
            }
            set
            {
                scanResultsLock.EnterWriteLock();
                try
                {
                    results[key] = value;
                }
                finally
                {
                    scanResultsLock.ExitWriteLock();
                }
            }
        }
        public int Count
        {
            get { return results.Count; }
        }
        public ScanResult FindOrAdd(Uri uri, Func<ScanResult> factory)
        {
            scanResultsLock.EnterUpgradeableReadLock();
            try
            {
                ScanResult scanResult;
                if (!results.ContainsKey(uri))
                {
                    scanResult = factory();
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
                return scanResult;
            }
            finally
            {
                scanResultsLock.ExitUpgradeableReadLock();
            }
        }
    }
}
using System.Collections;

namespace SpiderInterface
{
    public class ScanResultCollection : IEnumerable<KeyValuePair<Uri, ScanResult>>
    {
        private ReaderWriterLockSlim innerLock = new ReaderWriterLockSlim();
        private Dictionary<Uri, ScanResult> results = new Dictionary<Uri, ScanResult>();
        public void Add(Uri key, ScanResult value)
        {
            innerLock.EnterWriteLock();
            try
            {
                results.Add(key, value);
            }
            finally
            {
                innerLock.ExitWriteLock();
            }
        }
        public bool ContainsKey(Uri uriToCheck)
        {
            innerLock.EnterReadLock();
            try
            {
                bool isFound = results.ContainsKey(uriToCheck);
                return isFound;
            }
            finally
            {
                innerLock.ExitReadLock();
            }
        }
        public ScanResult this[Uri key]
        {
            get
            {
                innerLock.EnterReadLock();
                try
                {
                    ScanResult scanResult = results[key];
                    return scanResult;
                }
                finally
                {
                    innerLock.ExitReadLock();
                }
            }
            set
            {
                innerLock.EnterWriteLock();
                try
                {
                    results[key] = value;
                }
                finally
                {
                    innerLock.ExitWriteLock();
                }
            }
        }
        public IEnumerator<KeyValuePair<Uri, ScanResult>> GetEnumerator()
        {
            innerLock.EnterReadLock();
            try
            {
                Dictionary<Uri, ScanResult>.Enumerator enumerator = results.GetEnumerator();
                return enumerator;
            }
            finally
            {
                innerLock.ExitReadLock();
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<KeyValuePair<Uri, ScanResult>>)this).GetEnumerator();
        public ScanResult FindOrAdd(Uri uri, Func<ScanResult> factory)
        {
            innerLock.EnterUpgradeableReadLock();
            try
            {
                ScanResult? scanResult;
                if (!results.TryGetValue(uri, out scanResult))
                {
                    scanResult = factory();
                    innerLock.EnterWriteLock();
                    try
                    {
                        results.Add(uri, scanResult);
                    }
                    finally
                    {
                        innerLock.ExitWriteLock();
                    }
                }
                return scanResult;
            }
            finally
            {
                innerLock.ExitUpgradeableReadLock();
            }
        }
    }
}

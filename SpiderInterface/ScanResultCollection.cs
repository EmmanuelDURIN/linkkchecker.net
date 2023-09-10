using System.Collections;
using SpiderInterface.Concurrency;

namespace SpiderInterface
{
    public class ScanResultCollection : IEnumerable<KeyValuePair<Uri, ScanResult>>
    {

        private SafeReaderWriterLockProvider innerLock = new SafeReaderWriterLockProvider();
        private Dictionary<Uri, ScanResult> results = new Dictionary<Uri, ScanResult>();
        public void Add(Uri key, ScanResult value)
        {
            using (innerLock.GetWriteLock())
            {
                results.Add(key, value);
            }
        }
        public bool ContainsKey(Uri uriToCheck)
        {
            using (innerLock.GetReadLock())
            { 
                bool isFound = results.ContainsKey(uriToCheck);
                return isFound;
            }
        }
        public ScanResult this[Uri key]
        {
            get
            {
                using (innerLock.GetReadLock())
                {
                    ScanResult scanResult = results[key];
                    return scanResult;
                }
            }
            set
            {
                using (innerLock.GetWriteLock())
                {
                    results[key] = value;
                }
              }
        }
        public IEnumerator<KeyValuePair<Uri, ScanResult>> GetEnumerator()
        {
            using (innerLock.GetReadLock())
            {
                Dictionary<Uri, ScanResult>.Enumerator enumerator = results.GetEnumerator();
                return enumerator;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<KeyValuePair<Uri, ScanResult>>)this).GetEnumerator();
        public ScanResult FindOrAdd(Uri uri, Func<ScanResult> factory)
        {
            using (UpgradeableReadLock upgradableReadLock = innerLock.GetUpgradableReadLock())
            {
                ScanResult? scanResult;
                if (!results.TryGetValue(uri, out scanResult))
                {
                    scanResult = factory();
                    using (upgradableReadLock.GetWriteLock())
                    {
                        results.Add(uri, scanResult);
                    }
                }
                return scanResult;
            }
        }
    }
}

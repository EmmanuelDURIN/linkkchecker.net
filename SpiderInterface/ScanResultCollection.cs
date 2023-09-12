using System.Collections;

namespace SpiderInterface
{
    public class ScanResultCollection : IEnumerable<KeyValuePair<Uri, ScanResult>>
    {
        private object innerLock = new object();
        private Dictionary<Uri, ScanResult> results = new Dictionary<Uri, ScanResult>();
        public void Add(Uri key, ScanResult value)
        {
            lock (innerLock)
            {
                results.Add(key, value);
            }
        }
        public bool ContainsKey(Uri uriToCheck)
        {
            lock (innerLock)
            {
                return results.ContainsKey(uriToCheck);
            }
        }
        public ScanResult this[Uri key]
        {
            get
            {
                lock (innerLock)
                {
                    return results[key];
                }
            }
            set
            {
                lock (innerLock)
                {
                    results[key] = value;
                }
            }
        }
        public IEnumerator<KeyValuePair<Uri, ScanResult>> GetEnumerator()
        {
            lock (innerLock)
            {
                return new Dictionary<Uri, ScanResult>(results).GetEnumerator();
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<KeyValuePair<Uri, ScanResult>>)this).GetEnumerator();
        public ScanResult FindOrAdd(Uri uri, Func<ScanResult> factory)
        {
            lock (innerLock)
            {
                ScanResult? scanResult;
                if (!results.TryGetValue(uri, out scanResult))
                {
                    scanResult = factory();
                    results.Add(uri, scanResult);
                }
                return scanResult;
            }
        }
    }
}

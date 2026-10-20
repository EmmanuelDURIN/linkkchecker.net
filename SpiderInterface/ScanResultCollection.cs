using System.Collections;
using static System.Threading.Lock;

namespace SpiderInterface
{
    public class ScanResultCollection : IEnumerable<KeyValuePair<Uri, ScanResult>>
    {
        private Lock innerLock = new();
        private Dictionary<Uri, ScanResult> results = new Dictionary<Uri, ScanResult>();
        public void Add(Uri key, ScanResult value)
        {
            using Scope scope = innerLock.EnterScope();
            results.Add(key, value);
        }
        public bool ContainsKey(Uri uriToCheck)
        {
            using Scope scope = innerLock.EnterScope();
            return results.ContainsKey(uriToCheck);
        }
        public ScanResult this[Uri key]
        {
            get
            {
                using Scope scope = innerLock.EnterScope();
                return results[key];
            }
            set
            {
                using Scope scope = innerLock.EnterScope();
                results[key] = value;
            }
        }
        public IEnumerator<KeyValuePair<Uri, ScanResult>> GetEnumerator()
        {
            using Scope scope = innerLock.EnterScope();
            return new Dictionary<Uri, ScanResult>(results).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<KeyValuePair<Uri, ScanResult>>)this).GetEnumerator();
        public ScanResult FindOrAdd(Uri uri, Func<ScanResult> factory)
        {
            using Scope scope = innerLock.EnterScope();
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

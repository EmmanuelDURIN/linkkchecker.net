using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace SpiderInterface
{
    public class ScanResultCollection : IEnumerable<KeyValuePair<Uri, ScanResult>>
    {
        private ConcurrentDictionary<Uri, ScanResult> results = new();
        public void Add(Uri key, ScanResult value)
        {
            results.AddOrUpdate(key: key,
                                addValueFactory: key => value,
                                // peut être appelée plusieurs fois. Doit être rapide
                                updateValueFactory: (key, oldValue) => value);
        }
        public bool ContainsKey(Uri uriToCheck)
        {
            bool isFound = results.ContainsKey(uriToCheck);
            return isFound;
        }
        public ScanResult this[Uri key]
        {
            get
            {
                return results[key];
            }
            set
            {
                results[key] = value;
            }
        }
        public IEnumerator<KeyValuePair<Uri, ScanResult>> GetEnumerator()
            => results.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<KeyValuePair<Uri, ScanResult>>)this).GetEnumerator();
        public ScanResult FindOrAdd(Uri uri, Func<ScanResult> factory)
            => results.GetOrAdd(uri, factory());
    }
}

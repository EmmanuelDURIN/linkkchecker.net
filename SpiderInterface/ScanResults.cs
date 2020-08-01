using System;
using System.Collections;
using System.Collections.Generic;

namespace SpiderInterface
{
    public class ScanResults : IEnumerable<KeyValuePair<Uri, ScanResult>>
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
        public bool Remove(Uri key)
        {
            lock (innerLock)
            {
                return results.Remove(key);
            }
        }
        public bool ContainsKey(Uri key)
        {
            lock (innerLock)
            {
                return results.ContainsKey(key);
            }
        }
        public IEnumerator<KeyValuePair<Uri, ScanResult>> GetEnumerator()
        {
            lock (innerLock)
            {
                Dictionary<Uri, ScanResult> _results = new Dictionary<Uri, ScanResult>();
                foreach (var pair in results)
                {
                    _results.Add(pair.Key, pair.Value);
                }
                return _results.GetEnumerator();
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
        public int Count
        {
            get { return results.Count; }
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

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
            //bool lockTaken = false;
            //int count = 1;
            //do
            //{
            //    lockTaken = Monitor.TryEnter(innerLock, millisecondsTimeout: 0);
            //    if (!lockTaken)
            //    {
            //        Console.ForegroundColor = ConsoleColor.Yellow;
            //        Console.WriteLine("lock not acquired " + count);
            //        Console.ForegroundColor = ConsoleColor.White;
            //        count++;
            //    }
            //} while (!lockTaken);
            //results.Add(key, value);
            //Monitor.Exit(innerLock);
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
            //bool lockTaken = false;
            //int count = 1;
            //do
            //{
            //    lockTaken = Monitor.TryEnter(innerLock, millisecondsTimeout: 1);
            //    if (!lockTaken)
            //    {
            //        Console.ForegroundColor = ConsoleColor.Yellow;
            //        Console.WriteLine("lock not acquired " + count);
            //        Console.ForegroundColor = ConsoleColor.White;
            //        count++;
            //    }
            //} while (!lockTaken);
            //bool containsKey = results.ContainsKey(key);
            //Monitor.Exit(innerLock);
            //return containsKey;
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
        public ScanResult FindOrAdd(Uri uri, Func<ScanResult> factory)
        {
            lock (innerLock)
            {
                ScanResult scanResult;
                if (!results.ContainsKey(uri))
                {
                    scanResult = factory();
                    results.Add(uri, scanResult);
                }
                else
                {
                    scanResult = results[uri];
                }
                return scanResult;
            }
        }
    }
}
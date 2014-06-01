using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace RefactorThis.GraphDiff.Internal.Caching
{
    internal interface ICacheProvider
    {
        void Insert(string key, object value);
        T GetOrAdd<T>(string key, Func<T> onCacheMissed);
    }

    internal class CacheProvider : ICacheProvider
    {
        private static MemoryCache _cache = MemoryCache.Default;
        private static readonly object cacheLock = new object();

        public void Insert(string key, object value)
        {
            lock (cacheLock)
            {
                var result = _cache.Get(key);
                if (result == null)
                {
                    _cache.Add(key, value, new CacheItemPolicy());
                }
            }
        }

        public T GetOrAdd<T>(string key, Func<T> onCacheMissed)
        {
            var result = _cache.Get(key);
            if (result != null)
            {
                return (T)result;
            }

            lock (cacheLock)
            {
                // check again after lock.
                result = _cache.Get(key);
                if (result != null)
                {
                    return (T)result;
                }

                result = onCacheMissed();
                _cache.Add(key, result, new CacheItemPolicy());
            }
            
            return (T)result;
        }
    }
}

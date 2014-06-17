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
        void Insert(string register, string key, object value);
        void Clear(string register);
        bool TryGet<T>(string register, string key, out T element);
        T GetOrAdd<T>(string register, string key, Func<T> onCacheMissed);
    }

    internal class CacheProvider : ICacheProvider
    {
        private static MemoryCache _cache = MemoryCache.Default;
        private static readonly object cacheLock = new object();

        public void Insert(string register, string key, object value)
        {
            lock (cacheLock)
            {
                var fullKey = GenerateKey(register, key);
                var result = _cache.Get(fullKey);
                if (result == null)
                {
                    _cache.Add(fullKey, value, new CacheItemPolicy());
                }
            }
        }

        public void Clear(string register)
        {
            var items = _cache.Where(p => p.Key.Contains(register + ":"));
            foreach (var item in items)
            {
                _cache.Remove(item.Key);
            }
        }

        public T GetOrAdd<T>(string register, string key, Func<T> onCacheMissed)
        {
            var fullKey = GenerateKey(register, key);
            var result = _cache.Get(fullKey);
            if (result != null)
            {
                return (T)result;
            }

            lock (cacheLock)
            {
                // check again after lock.
                result = _cache.Get(fullKey);
                if (result != null)
                {
                    return (T)result;
                }

                result = onCacheMissed();
                _cache.Add(fullKey, result, new CacheItemPolicy());
            }
            
            return (T)result;
        }

        public bool TryGet<T>(string register, string key, out T element)
        {
            var item = _cache.Get(GenerateKey(register, key));
            if (item == null)
            {
                element = default(T);
                return false;
            }

            element = (T)item;
            return true;
        }

        private string GenerateKey(string register, string key)
        {
            return register + ":" + key;
        }
    }
}

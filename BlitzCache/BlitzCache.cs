using BlitzCache.LockDictionaries;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace BlitzCache
{
    public class BlitzCache// : IMemoryCache
    {
        private static readonly IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        public T GetThreadsafe<T>(string cacheKey, Func<T> function, double milliseconds)
        {
            if (memoryCache.TryGetValue(cacheKey, out T result)) return result;
            lock (LockDictionary.Get(cacheKey))
            {
                if (memoryCache.TryGetValue(cacheKey, out result)) return result;

                result = function.Invoke();
                memoryCache.Set(cacheKey, result, DateTime.Now.AddMilliseconds(milliseconds));
            }

            return result;
        }

        public async Task<T> GetThreadsafe<T>(string cacheKey, Func<Task<T>> function, double milliseconds)
        {
            if (memoryCache.TryGetValue(cacheKey, out T result)) return result;

            var semaphore = SemaphoreDictionary.Get(cacheKey);

            try
            {
                await semaphore.WaitAsync();
                if (!memoryCache.TryGetValue(cacheKey, out result))
                {
                    result = await function.Invoke();
                    memoryCache.Set(cacheKey, result, DateTime.Now.AddMilliseconds(milliseconds));
                }
            }
            finally
            {
                semaphore.Release();
            }

            return result;
        }

        //public bool TryGetValue(object key, out object value) => memoryCache.TryGetValue(key, out value);

        //public ICacheEntry CreateEntry(object key) => memoryCache.CreateEntry(key);

        //public void Remove(object key) => memoryCache.Remove(key);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose) => memoryCache.Dispose();
    }
}

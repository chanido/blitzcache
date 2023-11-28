using BlitzCacheCore.LockDictionaries;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlitzCacheCore
{
    public class BlitzCache : IBlitzCache
    {
        private static readonly IMemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        private readonly long defaultMilliseconds;
        public BlitzCache(long defaultMilliseconds = 60000)
        {
            this.defaultMilliseconds = defaultMilliseconds;
        }

        public T BlitzGet<T>(Func<T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "")
        {
            T f(Nuances nuances) => function();

            return BlitzGet(f, milliseconds, callerMemberName, sourceFilePath);
        }

        public T BlitzGet<T>(Func<Nuances, T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null)
        {
            T f(Nuances nuances) => function();

            return BlitzGet(cacheKey, f, milliseconds);
        }

        public T BlitzGet<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds = null)
        {
            if (memoryCache.TryGetValue(cacheKey, out T result)) return result;
            lock (LockDictionary.Get(cacheKey))
            {
                if (memoryCache.TryGetValue(cacheKey, out result)) return result;

                var nuances = new Nuances();
                result = function.Invoke(nuances);
                memoryCache.Set(cacheKey, result, DateTime.Now.AddMilliseconds(nuances.CacheRetention ?? milliseconds ?? defaultMilliseconds));
            }

            return result;
        }

        public Task<T> BlitzGet<T>(Func<Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "")
        {
            Task<T> f(Nuances nuances) => function();

            return BlitzGet(f, milliseconds, callerMemberName, sourceFilePath);
        }

        public Task<T> BlitzGet<T>(Func<Nuances, Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public Task<T> BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null)
        {
            Task<T> f(Nuances nuances) => function();

            return BlitzGet(cacheKey, f, milliseconds);
        }
        public async Task<T> BlitzGet<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds = null)
        {
            if (memoryCache.TryGetValue(cacheKey, out T result)) return result;

            var semaphore = SemaphoreDictionary.Get(cacheKey);

            try
            {
                await semaphore.WaitAsync();
                if (!memoryCache.TryGetValue(cacheKey, out result))
                {
                    var nuances = new Nuances();
                    result = await function.Invoke(nuances);
                    memoryCache.Set(cacheKey, result, DateTime.Now.AddMilliseconds(nuances.CacheRetention ?? milliseconds ?? defaultMilliseconds));
                }
            }
            finally
            {
                semaphore.Release();
            }

            return result;
        }

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds)
        {
            lock (LockDictionary.Get(cacheKey))
                memoryCache.Set(cacheKey, function(), DateTime.Now.AddMilliseconds(milliseconds));
        }

        public async void BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds)
        {
            var semaphore = SemaphoreDictionary.Get(cacheKey);

            try
            {
                await semaphore.WaitAsync();
                memoryCache.Set(cacheKey, await function.Invoke(), DateTime.Now.AddMilliseconds(milliseconds));
            }
            finally
            {
                semaphore.Release();
            }
        }

        public void Remove(string cacheKey)
        {
            lock (LockDictionary.Get(cacheKey))
                memoryCache.Remove(cacheKey);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose) => memoryCache.Dispose();
    }
}

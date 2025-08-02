using BlitzCacheCore.LockDictionaries;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlitzCacheCore
{
    public class BlitzCache : IBlitzCache
    {
        private static readonly IMemoryCache globalCache = new MemoryCache(new MemoryCacheOptions());
        private readonly IMemoryCache memoryCache;
        private readonly long defaultMilliseconds;
        private readonly bool usingGlobalCache;

        /// <summary>
        /// Creates a new BlitzCache instance.
        /// </summary>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds</param>
        /// <param name="useGlobalCache">Whether to use a global shared cache (true) or instance-specific cache (false)</param>
        public BlitzCache(long defaultMilliseconds = 60000, bool useGlobalCache = true)
        {
            this.defaultMilliseconds = defaultMilliseconds;
            this.usingGlobalCache = useGlobalCache;
            this.memoryCache = useGlobalCache ? globalCache : new MemoryCache(new MemoryCacheOptions());
        }

        /// <summary>
        /// Creates a new BlitzCache instance with a custom IMemoryCache implementation.
        /// </summary>
        /// <param name="memoryCache">The memory cache implementation to use</param>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds</param>
        public BlitzCache(IMemoryCache memoryCache, long defaultMilliseconds = 60000)
        {
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this.defaultMilliseconds = defaultMilliseconds;
            this.usingGlobalCache = false; // Custom cache is never global
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

            using (var smartSemaphore = SmartSemaphoreDictionary.GetSmartSemaphore(cacheKey))
            {
                smartSemaphore.WaitAsync().GetAwaiter().GetResult();
                try
                {
                    if (memoryCache.TryGetValue(cacheKey, out result)) return result;

                    var nuances = new Nuances();
                    result = function.Invoke(nuances);

                    var expirationTime = DateTime.Now.AddMilliseconds(nuances.CacheRetention ?? milliseconds ?? defaultMilliseconds);
                    memoryCache.Set(cacheKey, result, expirationTime);
                }
                finally
                {
                    smartSemaphore.Release();
                }
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

            using (var smartSemaphore = SmartSemaphoreDictionary.GetSmartSemaphore(cacheKey))
            {
                await smartSemaphore.WaitAsync();
                
                if (!memoryCache.TryGetValue(cacheKey, out result))
                {
                    var nuances = new Nuances();
                    result = await function.Invoke(nuances);
                    
                    var expirationTime = DateTime.Now.AddMilliseconds(nuances.CacheRetention ?? milliseconds ?? defaultMilliseconds);
                    memoryCache.Set(cacheKey, result, expirationTime);
                }
                
                smartSemaphore.Release();
            }

            return result;
        }

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds)
        {
            using (var smartSemaphore = SmartSemaphoreDictionary.GetSmartSemaphore(cacheKey))
            {
                smartSemaphore.WaitAsync().GetAwaiter().GetResult();
                try
                {
                    var expirationTime = DateTime.Now.AddMilliseconds(milliseconds);
                    memoryCache.Set(cacheKey, function(), expirationTime);
                }
                finally
                {
                    smartSemaphore.Release();
                }
            }
        }

        public async Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds)
        {
            using (var smartSemaphore = SmartSemaphoreDictionary.GetSmartSemaphore(cacheKey))
            {
                await smartSemaphore.WaitAsync();
                
                var expirationTime = DateTime.Now.AddMilliseconds(milliseconds);
                memoryCache.Set(cacheKey, await function.Invoke(), expirationTime);
                
                smartSemaphore.Release();
            }
        }

        public void Remove(string cacheKey)
        {
            using (var smartSemaphore = SmartSemaphoreDictionary.GetSmartSemaphore(cacheKey))
            {
                smartSemaphore.WaitAsync().GetAwaiter().GetResult();
                try
                {
                    memoryCache.Remove(cacheKey);
                }
                finally
                {
                    smartSemaphore.Release();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                // Only dispose the cache if it's not the global shared cache
                if (!usingGlobalCache && memoryCache != globalCache)
                {
                    memoryCache?.Dispose();
                }
            }
        }
    }
}

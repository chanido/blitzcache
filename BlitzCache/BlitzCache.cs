using BlitzCacheCore.LockDictionaries;
using BlitzCacheCore.Statistics;
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
        private readonly BlitzSemaphoreDictionary semaphoreDictionary;
        private readonly CacheStatistics statistics;

        /// <summary>
        /// Creates a new BlitzCache instance.
        /// </summary>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds</param>
        /// <param name="useGlobalCache">Whether to use a global shared cache (true) or instance-specific cache (false)</param>
        public BlitzCache(long defaultMilliseconds = 60000, bool useGlobalCache = true)
        {
            this.defaultMilliseconds = defaultMilliseconds;
            usingGlobalCache = useGlobalCache;
            memoryCache = useGlobalCache ? globalCache : new MemoryCache(new MemoryCacheOptions());
            semaphoreDictionary = new BlitzSemaphoreDictionary();
            statistics = new CacheStatistics(() => semaphoreDictionary.GetNumberOfLocks());
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
            usingGlobalCache = false; // Custom cache is never global
            semaphoreDictionary = new BlitzSemaphoreDictionary();
            statistics = new CacheStatistics(() => semaphoreDictionary.GetNumberOfLocks());
        }

        /// <summary>
        /// Gets the current number of semaphores for testing and monitoring purposes.
        /// </summary>
        public int GetSemaphoreCount() => semaphoreDictionary.GetNumberOfLocks();

        /// <summary>
        /// Gets cache performance statistics and monitoring information.
        /// Use these metrics to understand cache effectiveness and optimize cache configuration.
        /// </summary>
        public ICacheStatistics Statistics => statistics;

        public T BlitzGet<T>(Func<T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, nuances => function(), milliseconds);

        public T BlitzGet<T>(Func<Nuances, T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null) => BlitzGet(cacheKey, nuances => function(), milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds = null)
        {
            if (memoryCache.TryGetValue(cacheKey, out T result))
            {
                statistics.RecordHit();
                return result;
            }

            var semaphore = semaphoreDictionary.GetSemaphore(cacheKey);
            using (var lockHandle = semaphore.Acquire())
            {
                if (memoryCache.TryGetValue(cacheKey, out result))
                {
                    statistics.RecordHit();
                    return result;
                }

                statistics.RecordMiss();
                var nuances = new Nuances();
                result = function.Invoke(nuances);

                var expirationTime = DateTime.UtcNow.AddMilliseconds(nuances.CacheRetention ?? milliseconds ?? defaultMilliseconds);
                var cacheEntryOptions = statistics.CreateEntryOptions(expirationTime);

                memoryCache.Set(cacheKey, result, cacheEntryOptions);
                statistics.TrackEntry();
            }

            return result;
        }

        public Task<T> BlitzGet<T>(Func<Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, nuances => function(), milliseconds);

        public Task<T> BlitzGet<T>(Func<Nuances, Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public Task<T> BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null) =>
            BlitzGet(cacheKey, nuances => function(), milliseconds);
        public async Task<T> BlitzGet<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds = null)
        {
            if (memoryCache.TryGetValue(cacheKey, out T result))
            {
                statistics.RecordHit();
                return result;
            }

            var semaphore = semaphoreDictionary.GetSemaphore(cacheKey);
            using (var lockHandle = await semaphore.AcquireAsync())
            {
                if (memoryCache.TryGetValue(cacheKey, out result))
                {
                    statistics.RecordHit();
                    return result;
                }

                statistics.RecordMiss();
                var nuances = new Nuances();
                result = await function.Invoke(nuances);

                var expirationTime = DateTime.UtcNow.AddMilliseconds(nuances.CacheRetention ?? milliseconds ?? defaultMilliseconds);
                var cacheEntryOptions = statistics.CreateEntryOptions(expirationTime);

                memoryCache.Set(cacheKey, result, cacheEntryOptions);
                statistics.TrackEntry();
            }

            return result;
        }

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds)
        {
            var semaphore = semaphoreDictionary.GetSemaphore(cacheKey);
            using (var lockHandle = semaphore.Acquire())
            {
                var existsInCache = memoryCache.TryGetValue(cacheKey, out _);

                var expirationTime = DateTime.UtcNow.AddMilliseconds(milliseconds);
                var cacheEntryOptions = statistics.CreateEntryOptions(expirationTime);

                memoryCache.Set(cacheKey, function(), cacheEntryOptions);

                if (!existsInCache) statistics.TrackEntry();
            }
        }

        public async Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds)
        {
            var semaphore = semaphoreDictionary.GetSemaphore(cacheKey);
            using (var lockHandle = await semaphore.AcquireAsync())
            {
                var existsInCache = memoryCache.TryGetValue(cacheKey, out _);

                var expirationTime = DateTime.UtcNow.AddMilliseconds(milliseconds);
                var cacheEntryOptions = statistics.CreateEntryOptions(expirationTime);

                memoryCache.Set(cacheKey, await function.Invoke(), cacheEntryOptions);
                
                if (!existsInCache) statistics.TrackEntry();
            }
        }

        public void Remove(string cacheKey)
        {
            if (!memoryCache.TryGetValue(cacheKey, out _)) return;
            
            var semaphore = semaphoreDictionary.GetSemaphore(cacheKey);
            using (var lockHandle = semaphore.Acquire())
            {
                memoryCache.Remove(cacheKey);
            }
        }
        
        public void Dispose()
        {
            semaphoreDictionary?.Dispose();

            if (!usingGlobalCache && memoryCache != globalCache)
            {
                memoryCache?.Dispose();
            }
        }
    }
}

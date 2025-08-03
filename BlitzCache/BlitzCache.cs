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

        private T ExecuteWithCache<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds)
        {
            if (memoryCache.TryGetValue(cacheKey, out T result))
            {
                statistics.RecordHit();
                return result;
            }

            using (var lockHandle = semaphoreDictionary.Wait(cacheKey))
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
                return result;
            }
        }

        private async Task<T> ExecuteWithCacheAsync<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds)
        {
            if (memoryCache.TryGetValue(cacheKey, out T result))
            {
                statistics.RecordHit();
                return result;
            }

            using (var lockHandle = await semaphoreDictionary.WaitAsync(cacheKey))
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
                return result;
            }
        }

        public T BlitzGet<T>(Func<T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, nuances => function(), milliseconds);

        public T BlitzGet<T>(Func<Nuances, T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null) => BlitzGet(cacheKey, nuances => function(), milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds = null) => ExecuteWithCache(cacheKey, function, milliseconds);

        public Task<T> BlitzGet<T>(Func<Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, nuances => function(), milliseconds);

        public Task<T> BlitzGet<T>(Func<Nuances, Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public Task<T> BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null) =>
            BlitzGet(cacheKey, nuances => function(), milliseconds);

        public Task<T> BlitzGet<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds = null) => ExecuteWithCacheAsync(cacheKey, function, milliseconds);

        private void UpdateCacheEntry<T>(string cacheKey, T value, long milliseconds)
        {
            using (var lockHandle = semaphoreDictionary.Wait(cacheKey))
            {
                var existsInCache = memoryCache.TryGetValue(cacheKey, out _);

                var expirationTime = DateTime.UtcNow.AddMilliseconds(milliseconds);
                var cacheEntryOptions = statistics.CreateEntryOptions(expirationTime);

                memoryCache.Set(cacheKey, value, cacheEntryOptions);

                if (!existsInCache) statistics.TrackEntry();
            }
        }

        private async Task UpdateCacheEntryAsync<T>(string cacheKey, T value, long milliseconds)
        {
            using (var lockHandle = await semaphoreDictionary.WaitAsync(cacheKey))
            {
                var existsInCache = memoryCache.TryGetValue(cacheKey, out _);

                var expirationTime = DateTime.UtcNow.AddMilliseconds(milliseconds);
                var cacheEntryOptions = statistics.CreateEntryOptions(expirationTime);

                memoryCache.Set(cacheKey, value, cacheEntryOptions);
                
                if (!existsInCache) statistics.TrackEntry();
            }
        }

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds) => UpdateCacheEntry(cacheKey, function(), milliseconds);

        public async Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds) => await UpdateCacheEntryAsync(cacheKey, await function.Invoke(), milliseconds);

        public void Remove(string cacheKey)
        {
            using (var lockHandle = semaphoreDictionary.Wait(cacheKey))
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

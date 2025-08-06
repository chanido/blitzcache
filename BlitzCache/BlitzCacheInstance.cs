using BlitzCacheCore.LockDictionaries;
using BlitzCacheCore.Statistics;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlitzCacheCore
{
    public class BlitzCacheInstance : IBlitzCacheInstance
    {
        private readonly IMemoryCache memoryCache;
        private readonly long defaultMilliseconds;
        private readonly BlitzSemaphoreDictionary semaphoreDictionary;
        private CacheStatistics? statistics;

        /// <summary>
        /// Creates a new BlitzCache instance.
        /// </summary>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds</param>
        /// <param name="enableStatistics">Whether to enable statistics tracking (default: false for better performance)</param>
        /// <param name="cleanupInterval">Interval for automatic cleanup of unused semaphores (default: 10 seconds)</param>
        public BlitzCacheInstance(long? defaultMilliseconds = 60000, TimeSpan? cleanupInterval = null)
        {
            if (defaultMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(defaultMilliseconds), "Default milliseconds must be non-negative");

            this.defaultMilliseconds = defaultMilliseconds!.Value;
            memoryCache = new MemoryCache(new MemoryCacheOptions());
            semaphoreDictionary = new BlitzSemaphoreDictionary(cleanupInterval);
        }

        /// <summary>
        /// Gets the current number of semaphores for testing and monitoring purposes.
        /// </summary>
        public int GetSemaphoreCount() => semaphoreDictionary.GetNumberOfLocks();

        public void InitializeStatistics() => statistics = statistics ?? new CacheStatistics(() => semaphoreDictionary.GetNumberOfLocks());

        /// <summary>
        /// Gets cache performance statistics and monitoring information.
        /// Use these metrics to understand cache effectiveness and optimize cache configuration.
        /// Returns null if statistics are disabled for better performance.
        /// </summary>
        public ICacheStatistics? Statistics => statistics;

        private T ExecuteWithCache<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds)
        {
            if (TryGetFromCache<T>(cacheKey, out var cachedResult))
                return cachedResult;

            return ExecuteAndCache(cacheKey, function, milliseconds);
        }

        private bool TryGetFromCache<T>(string cacheKey, out T result)
        {
            if (memoryCache.TryGetValue(cacheKey, out var cachedValue))
            {
                statistics?.RecordHit();
                result = (T)cachedValue!;
                return true;
            }

            result = default!;
            return false;
        }

        private T ExecuteAndCache<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds)
        {
            using var lockHandle = semaphoreDictionary.Wait(cacheKey);

            if (TryGetFromCache<T>(cacheKey, out var cachedResult))
                return cachedResult;

            return ComputeAndStoreResult(cacheKey, function, milliseconds);
        }

        private T ComputeAndStoreResult<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds)
        {
            statistics?.RecordMiss();
            var nuances = new Nuances();
            var computedResult = function.Invoke(nuances);

            var expirationTime = DateTime.UtcNow.AddMilliseconds(nuances.CacheRetention ?? milliseconds ?? defaultMilliseconds);
            var cacheEntryOptions = statistics?.CreateEntryOptions(expirationTime) ??
                new MemoryCacheEntryOptions { AbsoluteExpiration = expirationTime };

            memoryCache.Set(cacheKey, computedResult, cacheEntryOptions);
            statistics?.TrackEntry();
            return computedResult;
        }

        private async Task<T> ExecuteWithCacheAsync<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds)
        {
            if (TryGetFromCache<T>(cacheKey, out var cachedResult))
                return cachedResult;

            return await ExecuteAndCacheAsync(cacheKey, function, milliseconds);
        }

        private async Task<T> ExecuteAndCacheAsync<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds)
        {
            using var lockHandle = await semaphoreDictionary.WaitAsync(cacheKey);

            if (TryGetFromCache<T>(cacheKey, out var cachedResult))
                return cachedResult;

            return await ComputeAndStoreResultAsync(cacheKey, function, milliseconds);
        }

        private async Task<T> ComputeAndStoreResultAsync<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds)
        {
            statistics?.RecordMiss();
            var nuances = new Nuances();
            var computedResult = await function.Invoke(nuances);

            var expirationTime = DateTime.UtcNow.AddMilliseconds(nuances.CacheRetention ?? milliseconds ?? defaultMilliseconds);
            var cacheEntryOptions = statistics?.CreateEntryOptions(expirationTime) ??
                new MemoryCacheEntryOptions { AbsoluteExpiration = expirationTime };

            memoryCache.Set(cacheKey, computedResult, cacheEntryOptions);
            statistics?.TrackEntry();
            return computedResult;
        }

        public T BlitzGet<T>(Func<T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") => BlitzGet(callerMemberName + sourceFilePath, nuances => function(), milliseconds);

        public T BlitzGet<T>(Func<Nuances, T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") => BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null) => BlitzGet(cacheKey, nuances => function(), milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds = null) => ExecuteWithCache(cacheKey, function, milliseconds);

        public Task<T> BlitzGet<T>(Func<Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") => BlitzGet(callerMemberName + sourceFilePath, nuances => function(), milliseconds);

        public Task<T> BlitzGet<T>(Func<Nuances, Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") => BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public Task<T> BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null) => BlitzGet(cacheKey, nuances => function(), milliseconds);

        public Task<T> BlitzGet<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds = null) => ExecuteWithCacheAsync(cacheKey, function, milliseconds);

        private void UpdateCacheEntry<T>(string cacheKey, T value, long milliseconds)
        {
            using var lockHandle = semaphoreDictionary.Wait(cacheKey);
            SetCacheValue(cacheKey, value, milliseconds);
        }

        private async Task UpdateCacheEntryAsync<T>(string cacheKey, T value, long milliseconds)
        {
            using var lockHandle = await semaphoreDictionary.WaitAsync(cacheKey);
            SetCacheValue(cacheKey, value, milliseconds);
        }

        private void SetCacheValue<T>(string cacheKey, T value, long milliseconds)
        {
            var existsInCache = memoryCache.TryGetValue(cacheKey, out _);
            var expirationTime = DateTime.UtcNow.AddMilliseconds(milliseconds);
            var cacheEntryOptions = statistics?.CreateEntryOptions(expirationTime) ??
                new MemoryCacheEntryOptions { AbsoluteExpiration = expirationTime };

            memoryCache.Set(cacheKey, value, cacheEntryOptions);

            if (!existsInCache)
                statistics?.TrackEntry();
        }

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds) => UpdateCacheEntry(cacheKey, function(), milliseconds);

        public async Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds) => await UpdateCacheEntryAsync(cacheKey, await function.Invoke(), milliseconds);

        public void Remove(string cacheKey)
        {
            // Early return if key doesn't exist to avoid unnecessary locking
            if (!memoryCache.TryGetValue(cacheKey, out _)) return;

            using var lockHandle = semaphoreDictionary.Wait(cacheKey);
            memoryCache.Remove(cacheKey);
            // There is no need to decrement entry count or record eviction here because that is handled by the memory cache PostEvictionCallbacks
        }

        public void Dispose()
        {
            semaphoreDictionary?.Dispose();
            memoryCache?.Dispose();
            statistics?.Reset();
        }
    }
}

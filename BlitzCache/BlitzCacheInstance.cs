using BlitzCacheCore.LockDictionaries;
using BlitzCacheCore.Statistics;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlitzCacheCore
{
    public class BlitzCacheInstance : IBlitzCacheInstance
    {
        private readonly IMemoryCache memoryCache;
        private readonly long defaultMilliseconds;
        private readonly int maxTopSlowest;
        private readonly BlitzSemaphoreDictionary semaphoreDictionary;
        private CacheStatistics? statistics;
        private readonly IValueSizer valueSizer;
        private readonly int maxTopHeaviest;
        private readonly long? configuredSizeLimitBytes; // store configured capacity for proactive compaction

        /// <summary>
        /// Creates a new BlitzCache instance.
        /// </summary>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds</param>
        /// <param name="cleanupInterval">Interval for automatic cleanup of unused semaphores (default: 10 seconds)</param>
        /// <param name="maxTopSlowest">Max number of top slowest queries to store (0 for improved performance) (default: 5 queries)</param>
        /// <param name="valueSizer">Strategy to estimate value sizes for memory accounting. If null, a default approximate sizer will be used.</param>
        /// <param name="maxTopHeaviest">Max number of heaviest entries to track (0 disables). Default 5.</param>
        /// <param name="maxCacheSizeBytes">Optional maximum cache size in bytes. When specified, MemoryCache enforces this limit via capacity-based eviction.</param>
        public BlitzCacheInstance(long? defaultMilliseconds = 60000, TimeSpan? cleanupInterval = null, int? maxTopSlowest = null, IValueSizer? valueSizer = null, int? maxTopHeaviest = 5, long? maxCacheSizeBytes = null)
        {
            if (defaultMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(defaultMilliseconds), "Default milliseconds must be non-negative");

            this.defaultMilliseconds = defaultMilliseconds!.Value;
            var options = new MemoryCacheOptions();
            if (maxCacheSizeBytes.HasValue && maxCacheSizeBytes.Value > 0)
            {
                options.SizeLimit = maxCacheSizeBytes.Value;
            }
            memoryCache = new MemoryCache(options);
            semaphoreDictionary = new BlitzSemaphoreDictionary(cleanupInterval);
            this.maxTopSlowest = maxTopSlowest ?? 5;
            this.valueSizer = valueSizer ?? new ApproximateValueSizer();
            this.maxTopHeaviest = maxTopHeaviest ?? 5;
            this.configuredSizeLimitBytes = maxCacheSizeBytes; // remember for proactive compaction
        }

        /// <summary>
        /// Backward-compatible constructor for callers compiled against older versions (pre heaviest/size-limit features).
        /// </summary>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds</param>
        /// <param name="cleanupInterval">Interval for automatic cleanup of unused semaphores</param>
        /// <param name="maxTopSlowest">Max number of top slowest queries to store</param>
        [Obsolete("Prefer using the extended constructor to enable sizing and capacity features. This overload remains for binary compatibility.")]
        public BlitzCacheInstance(long? defaultMilliseconds, TimeSpan? cleanupInterval, int? maxTopSlowest)
            : this(defaultMilliseconds, cleanupInterval, maxTopSlowest, null, null, null)
        {
        }

        /// <summary>
        /// Gets the current number of semaphores for testing and monitoring purposes.
        /// </summary>
        public int GetSemaphoreCount() => semaphoreDictionary.GetNumberOfLocks();

        public void InitializeStatistics() => statistics = statistics ?? new CacheStatistics(() => semaphoreDictionary.GetNumberOfLocks(), maxTopSlowest, maxTopHeaviest, valueSizer);

        /// <summary>
        /// Gets cache performance statistics and monitoring information.
        /// Use these metrics to understand cache effectiveness and optimize cache configuration.
        /// Returns null if statistics are disabled for better performance.
        /// </summary>
        public ICacheStatistics? Statistics => statistics;

        private void EnforceCapacityIfNeeded()
        {
            // Only enforce proactively when a capacity was configured and statistics are enabled
            if (!configuredSizeLimitBytes.HasValue || statistics == null) return;

            var limit = configuredSizeLimitBytes.Value;
            if (statistics.ApproximateMemoryBytes <= limit) return;

            // Try deterministic removals using known sizes to get under budget
            var sizes = statistics.GetKeySizesSnapshot();
            if (sizes.Length > 0)
            {
                // Remove smallest-first to maximize retained useful entries
                foreach (var kvp in sizes.OrderBy(k => k.Value))
                {
                    if (statistics.ApproximateMemoryBytes <= limit) break;
                    memoryCache.Remove(kvp.Key);
                }
            }

            // If still over, ask MemoryCache to compact the remainder percentage
            if (memoryCache is MemoryCache concrete && statistics.ApproximateMemoryBytes > limit)
            {
                var approx = statistics.ApproximateMemoryBytes;
                if (approx > 0)
                {
                    var over = approx - limit;
                    var percent = Math.Min(1.0, Math.Max(0.02, (double)over / approx));
                    concrete.Compact(percent);
                }
            }
        }

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

            var stopwatch = Stopwatch.StartNew();
            var computedResult = function.Invoke(nuances);
            stopwatch.Stop();

            var expirationTime = DateTime.UtcNow.AddMilliseconds(nuances.CacheRetention ?? milliseconds ?? defaultMilliseconds);

            // Determine if the key existed in the cache before this Set (for accurate entry count)
            var existed = memoryCache.TryGetValue(cacheKey, out _);

            // Compute size for MemoryCache capacity enforcement
            var newSize = valueSizer.GetSizeBytes(computedResult!);
            var cacheEntryOptions = statistics?.CreateEntryOptions(expirationTime) ?? new MemoryCacheEntryOptions { AbsoluteExpiration = expirationTime };
            cacheEntryOptions.Size = newSize;

            memoryCache.Set(cacheKey, computedResult, cacheEntryOptions);
            statistics?.RecordSetOrUpdate(cacheKey, computedResult!, existed);
            statistics?.TrackEntry(cacheKey, stopwatch);

            EnforceCapacityIfNeeded();
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
            var stopwatch = Stopwatch.StartNew();
            var computedResult = await function.Invoke(nuances);
            stopwatch.Stop();

            var expirationTime = DateTime.UtcNow.AddMilliseconds(nuances.CacheRetention ?? milliseconds ?? defaultMilliseconds);

            // Determine if the key existed in the cache before this Set (for accurate entry count)
            var existed = memoryCache.TryGetValue(cacheKey, out _);

            // Compute size for MemoryCache capacity enforcement
            var newSize = valueSizer.GetSizeBytes(computedResult!);
            var cacheEntryOptions = statistics?.CreateEntryOptions(expirationTime) ?? new MemoryCacheEntryOptions { AbsoluteExpiration = expirationTime };
            cacheEntryOptions.Size = newSize;

            memoryCache.Set(cacheKey, computedResult, cacheEntryOptions);
            statistics?.RecordSetOrUpdate(cacheKey, computedResult!, existed);
            statistics?.TrackEntry(cacheKey, stopwatch);

            EnforceCapacityIfNeeded();
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
            UpdateCacheValue(cacheKey, value, milliseconds);
        }

        private async Task UpdateCacheEntryAsync<T>(string cacheKey, T value, long milliseconds)
        {
            using var lockHandle = await semaphoreDictionary.WaitAsync(cacheKey);
            UpdateCacheValue(cacheKey, value, milliseconds);
        }

        private void UpdateCacheValue<T>(string cacheKey, T value, long milliseconds)
        {
            var existsInCache = memoryCache.TryGetValue(cacheKey, out _);
            var expirationTime = DateTime.UtcNow.AddMilliseconds(milliseconds);

            // Compute size for MemoryCache capacity enforcement
            var newSize = valueSizer.GetSizeBytes(value!);
            var cacheEntryOptions = statistics?.CreateEntryOptions(expirationTime) ?? new MemoryCacheEntryOptions { AbsoluteExpiration = expirationTime };
            cacheEntryOptions.Size = newSize;

            memoryCache.Set(cacheKey, value, cacheEntryOptions);

            if (statistics != null)
            {
                if (!existsInCache)
                    statistics.TrackEntryUpdate();
                statistics.RecordSetOrUpdate(cacheKey, value!, existsInCache);
            }

            EnforceCapacityIfNeeded();
        }

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds) => UpdateCacheEntry(cacheKey, function(), milliseconds);

        public async Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds) => await UpdateCacheEntryAsync(cacheKey, await function.Invoke(), milliseconds);

        public void Remove(string cacheKey)
        {
            // Early return if key doesn't exist to avoid unnecessary locking
            if (!memoryCache.TryGetValue(cacheKey, out _)) return;

            using var lockHandle = semaphoreDictionary.Wait(cacheKey);
            memoryCache.Remove(cacheKey);
            // No manual stats updates here: eviction callback handles it.
        }

        public void Dispose()
        {
            semaphoreDictionary?.Dispose();
            memoryCache?.Dispose();
            statistics?.Reset();
        }
    }
}

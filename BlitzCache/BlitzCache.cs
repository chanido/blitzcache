using BlitzCacheCore.Statistics;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlitzCacheCore
{
    public class BlitzCache : IBlitzCache
    {
        private static IBlitzCacheInstance? globalInstance; // global singleton instance
        private static readonly object globalLock = new object(); // protects lazy initialization
        private const string KeyDelimiter = "|"; // unify delimiter with BlitzCacheInstance construction style

        /// <summary>
        /// Creates a new BlitzCache instance.
        /// </summary>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds</param>
        /// <param name="cleanupInterval">Interval for automatic cleanup of unused semaphores (default: 10 seconds)</param>
        /// <param name="maxTopSlowest">Max number of top slowest queries to store (0 for improved performance) (default: 5 queries)</param>
        /// <param name="maxTopHeaviest">Max number of heaviest entries to track (0 disables). Default: 5.</param>
        /// <param name="maxCacheSizeBytes">Optional maximum cache size in bytes. When specified, enables capacity-based eviction.</param>
        public BlitzCache(long? defaultMilliseconds = 60000, TimeSpan? cleanupInterval = null, int? maxTopSlowest = 5, int? maxTopHeaviest = 5, long? maxCacheSizeBytes = null)
        {
            if (defaultMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(defaultMilliseconds), "Default milliseconds must be non-negative");

            EnsureGlobalInstance(defaultMilliseconds, cleanupInterval, maxTopSlowest, maxTopHeaviest, maxCacheSizeBytes, null);
        }

        /// <summary>
        /// Preferred constructor accepting <see cref="BlitzCacheOptions"/> for forward-compatible configuration.
        /// </summary>
        /// <param name="options">Configuration options.</param>
        public BlitzCache(BlitzCacheOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.Validate();
            
            EnsureGlobalInstance(options.DefaultMilliseconds, options.CleanupInterval, options.MaxTopSlowest, options.MaxTopHeaviest, options.MaxCacheSizeBytes, options);
        }

        [Obsolete("Prefer using the Configuration options constructor to enable sizing and capacity features. This overload remains for binary compatibility.")]
        public BlitzCache(long? defaultMilliseconds, TimeSpan? cleanupInterval, int? maxTopSlowest)
            : this(defaultMilliseconds, cleanupInterval, maxTopSlowest, 5, null)
        {
        }

        /// <summary>
        /// Gets the current number of semaphores for testing and monitoring purposes.
        /// </summary>
        public int GetSemaphoreCount() => globalInstance!.GetSemaphoreCount();

        /// <summary>
        /// Gets cache performance statistics and monitoring information.
        /// Use these metrics to understand cache effectiveness and optimize cache configuration.
        /// Returns null if statistics are disabled for better performance.
        /// </summary>
        public ICacheStatistics? Statistics => globalInstance?.Statistics;

        public void InitializeStatistics() => globalInstance?.InitializeStatistics();

        public T BlitzGet<T>(Func<T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            globalInstance!.BlitzGet(callerMemberName + KeyDelimiter + sourceFilePath, nuances => function(), milliseconds);

        public T BlitzGet<T>(Func<Nuances, T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            globalInstance!.BlitzGet(callerMemberName + KeyDelimiter + sourceFilePath, function, milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null) => globalInstance!.BlitzGet(cacheKey, nuances => function(), milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds = null) => globalInstance!.BlitzGet(cacheKey, function, milliseconds);

        public Task<T> BlitzGet<T>(Func<Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            globalInstance!.BlitzGet(callerMemberName + KeyDelimiter + sourceFilePath, nuances => function(), milliseconds);

        public Task<T> BlitzGet<T>(Func<Nuances, Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            globalInstance!.BlitzGet(callerMemberName + KeyDelimiter + sourceFilePath, function, milliseconds);

        public Task<T> BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null) =>
            globalInstance!.BlitzGet(cacheKey, nuances => function(), milliseconds);

        public Task<T> BlitzGet<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds = null) =>
            globalInstance!.BlitzGet(cacheKey, function, milliseconds);

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds) => globalInstance!.BlitzUpdate<T>(cacheKey, function, milliseconds);

        public Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds) => globalInstance!.BlitzUpdate<T>(cacheKey, function, milliseconds);

        public void Remove(string cacheKey) => globalInstance!.Remove(cacheKey);

#if DEBUG
        internal IBlitzCacheInstance? GetInternalInstance() => globalInstance;
        internal static bool IsInitialized => globalInstance != null;

        internal static void ClearGlobalForTesting()
        {
            if (globalInstance is null) return;

            globalInstance.Dispose();
            globalInstance = null;
        }
#endif

        private static void EnsureGlobalInstance(long? defaultMs, TimeSpan? cleanupInterval, int? maxTopSlowest, int? maxTopHeaviest, long? maxCacheSizeBytes, BlitzCacheOptions? options)
        {
            // NOTE: ValueSizer and EvictionStrategy currently applied at BlitzCacheInstance level when globalInstance created.
            // To remain backward compatible (global singleton lazily created), we only consider extended parameters if instance is still null.
            // If global instance does not yet exist and extended sizing options are provided, rebuild it using BlitzCacheInstance options-based constructor.
            if (globalInstance != null) return;
            lock (globalLock)
            {
                if (globalInstance != null) return;
                if (options != null)
                {
                    globalInstance = new BlitzCacheInstance(options);
                }
                else
                {
                    globalInstance = new BlitzCacheInstance(defaultMs, cleanupInterval, maxTopSlowest, null, maxTopHeaviest, maxCacheSizeBytes);
                }
            }
        }
    }
}

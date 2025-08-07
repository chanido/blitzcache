using System.Collections.Generic;

namespace BlitzCacheCore.Statistics
{
    /// <summary>
    /// Provides cache performance statistics and monitoring capabilities.
    /// Use these metrics to understand cache effectiveness and optimize cache configuration.
    /// </summary>
    public interface ICacheStatistics
    {
        /// <summary>
        /// Total number of cache hits since cache instance creation.
        /// A cache hit occurs when BlitzGet finds an existing cached value.
        /// </summary>
        long HitCount { get; }

        /// <summary>
        /// Total number of cache misses since cache instance creation.
        /// A cache miss occurs when BlitzGet needs to execute the factory function.
        /// </summary>
        long MissCount { get; }

        /// <summary>
        /// Cache hit ratio as a percentage (0.0 to 1.0).
        /// Higher values indicate better cache effectiveness.
        /// Formula: HitCount / (HitCount + MissCount)
        /// </summary>
        double HitRatio { get; }

        /// <summary>
        /// Current number of entries stored in the cache.
        /// This includes all cached values that haven't expired.
        /// </summary>
        long EntryCount { get; }

        /// <summary>
        /// Number of times cache entries have been explicitly removed or expired.
        /// This includes both manual removals and automatic expiration.
        /// </summary>
        long EvictionCount { get; }

        /// <summary>
        /// Number of active semaphores currently managing cache keys.
        /// This reflects the concurrency level and cleanup efficiency.
        /// </summary>
        int ActiveSemaphoreCount { get; }

        /// <summary>
        /// Total number of operations (hits + misses) performed by this cache instance.
        /// </summary>
        long TotalOperations { get; }
        IEnumerable<SlowQuery> TopSlowestQueries { get; }

        /// <summary>
        /// Resets all statistics counters to zero.
        /// Useful for monitoring cache performance over specific time periods.
        /// </summary>
        void Reset();
    }
}

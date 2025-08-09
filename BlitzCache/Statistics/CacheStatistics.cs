using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace BlitzCacheCore.Statistics
{
    /// <summary>
    /// Thread-safe implementation of cache statistics tracking.
    /// All counters are atomically updated to ensure accurate statistics in concurrent scenarios.
    /// Also manages cache entry tracking and eviction callbacks.
    /// </summary>
    internal class CacheStatistics : ICacheStatistics
    {
        private long hitCount;
        private long missCount;
        private long evictionCount;
        private long entryCount;
        private long approximateMemoryBytes;
        private readonly Func<int> getActiveSemaphoreCount;
        private readonly TopSlowestQueries? topSlowestQueries;
        private readonly TopHeaviestEntries? topHeaviestEntries;
        public long HitCount => Interlocked.Read(ref hitCount);
        public long MissCount => Interlocked.Read(ref missCount);
        public long EntryCount => Interlocked.Read(ref entryCount);
        public long EvictionCount => Interlocked.Read(ref evictionCount);
        public int ActiveSemaphoreCount => getActiveSemaphoreCount();
        public long TotalOperations => HitCount + MissCount;
        public IEnumerable<SlowQuery> TopSlowestQueries => topSlowestQueries is null ? Array.Empty<SlowQuery>() : topSlowestQueries.Get();
        public long ApproximateMemoryBytes => Interlocked.Read(ref approximateMemoryBytes);
        public IEnumerable<HeavyEntry> TopHeaviestEntries => topHeaviestEntries is null ? Array.Empty<HeavyEntry>() : topHeaviestEntries.Get();
        public double HitRatio
        {
            get
            {
                var hits = HitCount;
                var total = TotalOperations;
                return total == 0 ? 0.0 : (double)hits / total;
            }
        }

        /// <summary>
        /// Creates a new instance of CacheStatistics.
        /// </summary>
        /// <param name="getActiveSemaphoreCount">Function to retrieve active semaphores on demand</param>
        /// <param name="maxTopSlowest">Max number of top slowest queries to store (0 disables it for improved performance)</param>
        /// <param name="maxTopHeaviest">Max number of top heaviest entries to track (0 disables tracking)</param>
        public CacheStatistics(Func<int> getActiveSemaphoreCount, int maxTopSlowest, int maxTopHeaviest = 5)
        {
            this.getActiveSemaphoreCount = getActiveSemaphoreCount ?? throw new ArgumentNullException(nameof(getActiveSemaphoreCount));

            if (maxTopSlowest > 0)
            {
                topSlowestQueries = new TopSlowestQueries(maxTopSlowest);
            }
            if (maxTopHeaviest > 0)
            {
                topHeaviestEntries = new TopHeaviestEntries(maxTopHeaviest);
            }
        }

        /// <summary>
        /// Records a query execution time for slowest queries tracking.
        /// </summary>
        /// <param name="cacheKey">The cache key of the query.</param>
        /// <param name="durationMilliseconds">The execution duration in milliseconds.</param>
        public void RecordQueryDuration(string cacheKey, long durationMilliseconds) => topSlowestQueries?.Add(cacheKey, durationMilliseconds);

        /// <summary>
        /// Records a cache hit. Thread-safe.
        /// </summary>
        internal void RecordHit() => Interlocked.Increment(ref hitCount);

        /// <summary>
        /// Records a cache miss. Thread-safe.
        /// </summary>
        internal void RecordMiss() => Interlocked.Increment(ref missCount);

        /// <summary>
        /// Records a cache eviction. Thread-safe.
        /// </summary>
        internal void RecordEviction()
        {
            Interlocked.Increment(ref evictionCount);
            Interlocked.Decrement(ref entryCount);
        }

        internal void RecordAddOrUpdate(string cacheKey, long newSizeBytes, long? oldSizeBytes)
        {
            // adjust entry count on first creation
            if (!oldSizeBytes.HasValue)
                Interlocked.Increment(ref entryCount);

            // Update total bytes delta
            var delta = newSizeBytes - (oldSizeBytes ?? 0);
            Interlocked.Add(ref approximateMemoryBytes, delta);

            // Track heaviest list
            topHeaviestEntries?.AddOrUpdate(cacheKey, newSizeBytes);
        }

        internal void RecordRemove(string cacheKey, long sizeBytes)
        {
            // Entry count and eviction are handled by the MemoryCache eviction callback.
            // Here we only adjust memory accounting and heaviest list.
            Interlocked.Add(ref approximateMemoryBytes, -sizeBytes);
            topHeaviestEntries?.Remove(cacheKey);
        }

        /// <summary>
        /// Creates memory cache entry options with eviction callback for statistics tracking.
        /// </summary>
        /// <param name="expirationTime">When the entry should expire</param>
        /// <returns>MemoryCacheEntryOptions configured with eviction tracking</returns>
        internal MemoryCacheEntryOptions CreateEntryOptions(DateTime expirationTime) => new MemoryCacheEntryOptions
        {
            AbsoluteExpiration = expirationTime,
            PostEvictionCallbacks = { new PostEvictionCallbackRegistration
            {
                EvictionCallback = (key, value, reason, state) =>
                {
                    // Track automatic evictions (expiration, memory pressure, etc.)
                    // Don't count Replaced as eviction since it's just an update
                    if (reason != EvictionReason.Replaced)
                        RecordEviction();
                }
            }}
        };

        /// <summary>
        /// Tracks a new cache entry.
        /// </summary>
        internal void TrackEntry(string cacheKey, Stopwatch stopwatch)
        {
            // Entry count is handled in RecordAddOrUpdate. Only record query duration here.
            topSlowestQueries?.Add(cacheKey, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Tracks a new cache entry.
        /// </summary>
        internal void TrackEntryUpdate() { /* no-op: entry count handled in RecordAddOrUpdate */ }

        /// <summary>
        /// Resets all statistics counters to zero. Thread-safe.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref hitCount, 0);
            Interlocked.Exchange(ref missCount, 0);
            Interlocked.Exchange(ref evictionCount, 0);
            Interlocked.Exchange(ref entryCount, 0);
            Interlocked.Exchange(ref approximateMemoryBytes, 0);
            topSlowestQueries?.Clear();
            topHeaviestEntries?.Clear();
        }
    }
}

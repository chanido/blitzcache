using Microsoft.Extensions.Caching.Memory;
using System;
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
        private readonly Func<int> getActiveSemaphoreCount;

        public CacheStatistics(Func<int> getActiveSemaphoreCount)
        {
            this.getActiveSemaphoreCount = getActiveSemaphoreCount ?? throw new ArgumentNullException(nameof(getActiveSemaphoreCount));
        }

        public long HitCount => Interlocked.Read(ref hitCount);

        public long MissCount => Interlocked.Read(ref missCount);

        public double HitRatio
        {
            get
            {
                var hits = HitCount;
                var total = TotalOperations;
                return total == 0 ? 0.0 : (double)hits / total;
            }
        }

        public long EntryCount => Interlocked.Read(ref entryCount);

        public long EvictionCount => Interlocked.Read(ref evictionCount);

        public int ActiveSemaphoreCount => getActiveSemaphoreCount();

        public long TotalOperations => HitCount + MissCount;

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
        internal void TrackEntry() => Interlocked.Increment(ref entryCount);

        /// <summary>
        /// Resets all statistics counters to zero. Thread-safe.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref hitCount, 0);
            Interlocked.Exchange(ref missCount, 0);
            Interlocked.Exchange(ref evictionCount, 0);
            Interlocked.Exchange(ref entryCount, 0);
        }
    }
}

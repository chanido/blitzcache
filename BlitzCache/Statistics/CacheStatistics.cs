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
        private long _hitCount;
        private long _missCount;
        private long _evictionCount;
        private long _trackedKeysCount;
        private readonly Func<int> _getActiveSemaphoreCount;

        public CacheStatistics(Func<int> getActiveSemaphoreCount)
        {
            _getActiveSemaphoreCount = getActiveSemaphoreCount ?? throw new ArgumentNullException(nameof(getActiveSemaphoreCount));
        }

        public long HitCount => Interlocked.Read(ref _hitCount);

        public long MissCount => Interlocked.Read(ref _missCount);

        public double HitRatio
        {
            get
            {
                var hits = HitCount;
                var total = TotalOperations;
                return total == 0 ? 0.0 : (double)hits / total;
            }
        }

        public long CurrentEntryCount => Interlocked.Read(ref _trackedKeysCount);

        public long EvictionCount => Interlocked.Read(ref _evictionCount);

        public int ActiveSemaphoreCount => _getActiveSemaphoreCount();

        public long TotalOperations => HitCount + MissCount;

        /// <summary>
        /// Records a cache hit. Thread-safe.
        /// </summary>
        internal void RecordHit() => Interlocked.Increment(ref _hitCount);

        /// <summary>
        /// Records a cache miss. Thread-safe.
        /// </summary>
        internal void RecordMiss() => Interlocked.Increment(ref _missCount);

        /// <summary>
        /// Records a cache eviction. Thread-safe.
        /// </summary>
        internal void RecordEviction()
        {
            Interlocked.Increment(ref _evictionCount);
            Interlocked.Decrement(ref _trackedKeysCount);
        }

        /// <summary>
        /// Creates memory cache entry options with eviction callback for statistics tracking.
        /// </summary>
        /// <param name="cacheKey">The cache key</param>
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
                    if (reason == EvictionReason.Replaced) return;

                    RecordEviction();
                }
            }}
        };

        /// <summary>
        /// Tracks a new cache entry.
        /// </summary>
        /// <param name="cacheKey">The cache key to track</param>
        internal void TrackEntry() => Interlocked.Increment(ref _trackedKeysCount);

        /// <summary>
        /// Resets all statistics counters to zero. Thread-safe.
        /// </summary>
        public void Reset()
        {
            Interlocked.Exchange(ref _hitCount, 0);
            Interlocked.Exchange(ref _missCount, 0);
            Interlocked.Exchange(ref _evictionCount, 0);
            Interlocked.Exchange(ref _trackedKeysCount, 0);
        }
    }
}

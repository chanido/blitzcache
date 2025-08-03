using System;
using System.Threading;

namespace BlitzCacheCore
{
    /// <summary>
    /// Thread-safe implementation of cache statistics tracking.
    /// All counters are atomically updated to ensure accurate statistics in concurrent scenarios.
    /// </summary>
    internal class CacheStatistics : ICacheStatistics
    {
        private long _hitCount;
        private long _missCount;
        private long _evictionCount;
        private readonly Func<int> _getCurrentEntryCount;
        private readonly Func<int> _getActiveSemaphoreCount;

        public CacheStatistics(Func<int> getCurrentEntryCount, Func<int> getActiveSemaphoreCount)
        {
            _getCurrentEntryCount = getCurrentEntryCount ?? throw new ArgumentNullException(nameof(getCurrentEntryCount));
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

        public int CurrentEntryCount => _getCurrentEntryCount();

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
        /// Records a cache eviction (removal or expiration). Thread-safe.
        /// </summary>
        internal void RecordEviction() => Interlocked.Increment(ref _evictionCount);

        public void Reset()
        {
            Interlocked.Exchange(ref _hitCount, 0);
            Interlocked.Exchange(ref _missCount, 0);
            Interlocked.Exchange(ref _evictionCount, 0);
        }
    }
}

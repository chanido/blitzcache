using BlitzCacheCore.Statistics;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;

namespace BlitzCacheCore.Capacity
{
    /// <summary>
    /// Proactively enforces a configured capacity by deterministically removing entries when the approximate
    /// tracked memory usage exceeds the configured limit. Keeps policy concerns out of BlitzCacheInstance.
    /// Current strategy: smallest-first removal to maximize retained aggregate value density (can be swapped later).
    /// </summary>
    internal sealed class CapacityEnforcer
    {
        private readonly IMemoryCache memoryCache;
        private readonly CacheStatistics statistics;
        private readonly long sizeLimitBytes;
        private readonly CapacityEvictionStrategy strategy;

        public CapacityEnforcer(IMemoryCache memoryCache, CacheStatistics statistics, long sizeLimitBytes, CapacityEvictionStrategy strategy = CapacityEvictionStrategy.SmallestFirst)
        {
            this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            this.statistics = statistics ?? throw new ArgumentNullException(nameof(statistics));
            this.sizeLimitBytes = sizeLimitBytes;
            this.strategy = strategy;
        }

        /// <summary>
        /// Ensure the cache stays under the configured limit. Called after insert/update operations.
        /// </summary>
        public void EnsureUnderLimit()
        {
            // Fast path: already under limit.
            var current = statistics.ApproximateMemoryBytes;
            if (current <= sizeLimitBytes) return;
            if (sizeLimitBytes <= 0) return; // defensive; capacity feature disabled or misconfigured

            var overBytes = current - sizeLimitBytes;
            if (overBytes <= 0) return; // race – another thread already corrected

            var sizes = statistics.GetKeySizesSnapshot();
            if (sizes.Length != 0)
            {
                // Sort in-place to avoid LINQ allocations; O(n log n) worst case. If this becomes hot we can switch
                // to a partial selection (quickselect) to remove just enough. For now keep code simple & deterministic.
                if (strategy == CapacityEvictionStrategy.LargestFirst)
                {
                    Array.Sort(sizes, DescendingValueComparer.Instance);
                }
                else
                {
                    Array.Sort(sizes, AscendingValueComparer.Instance);
                }

                long simulatedRemaining = current; // local view; we optimistically subtract removed sizes
                for (int i = 0; i < sizes.Length && simulatedRemaining > sizeLimitBytes; i++)
                {
                    ref readonly var kvp = ref sizes[i];
                    simulatedRemaining -= kvp.Value;
                    statistics.OnExternalRemoval(kvp.Key, kvp.Value, countAsEviction: true); // adjust stats first
                    memoryCache.Remove(kvp.Key);
                }
            }

            // After proactive removals, if still above limit (due to estimation error or concurrent adds), compact.
            var after = statistics.ApproximateMemoryBytes;
            if (memoryCache is MemoryCache concrete && after > sizeLimitBytes)
            {
                var over = after - sizeLimitBytes;
                if (after > 0)
                {
                    // Compact proportional to overage, bounded to avoid overly small no-op or full wipe.
                    var percent = Math.Min(1.0, Math.Max(0.02, (double)over / after));
                    concrete.Compact(percent);
                }
            }

            // (Optional clamp) We intentionally do not clamp negative values here; atomic accounting should prevent them.
        }

        // Local comparers to avoid allocations each enforcement.
        private sealed class AscendingValueComparer : IComparer<KeyValuePair<string, long>>
        {
            internal static readonly AscendingValueComparer Instance = new AscendingValueComparer();
            public int Compare(KeyValuePair<string, long> x, KeyValuePair<string, long> y) => x.Value.CompareTo(y.Value);
        }

        private sealed class DescendingValueComparer : IComparer<KeyValuePair<string, long>>
        {
            internal static readonly DescendingValueComparer Instance = new DescendingValueComparer();
            public int Compare(KeyValuePair<string, long> x, KeyValuePair<string, long> y) => y.Value.CompareTo(x.Value);
        }
    }
}

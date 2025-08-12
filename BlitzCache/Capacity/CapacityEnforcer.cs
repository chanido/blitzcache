using BlitzCacheCore.Statistics;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;

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
            if (statistics.ApproximateMemoryBytes <= sizeLimitBytes) return;

            var sizes = statistics.GetKeySizesSnapshot();
            if (sizes.Length > 0)
            {
                var ordered = strategy switch
                {
                    CapacityEvictionStrategy.LargestFirst => sizes.OrderByDescending(k => k.Value),
                    _ => sizes.OrderBy(k => k.Value)
                };

                // Use a local projection of remaining bytes to avoid race with asynchronous eviction callbacks.
                long simulatedRemaining = statistics.ApproximateMemoryBytes;

                foreach (var kvp in ordered)
                {
                    if (simulatedRemaining <= sizeLimitBytes) break;
                    simulatedRemaining -= kvp.Value; // simulate removal immediately
                    memoryCache.Remove(kvp.Key);
                }
            }

            if (memoryCache is MemoryCache concrete && statistics.ApproximateMemoryBytes > sizeLimitBytes)
            {
                var approx = statistics.ApproximateMemoryBytes;
                if (approx > 0)
                {
                    var over = approx - sizeLimitBytes;
                    var percent = Math.Min(1.0, Math.Max(0.02, (double)over / approx));
                    concrete.Compact(percent);
                }
            }

            // Safety clamp: in case of any race conditions leading to slightly negative accounting, normalize to zero.
            if (statistics.ApproximateMemoryBytes < 0)
            {
                // This is defensive; should not happen after removal of double accounting.
                // If needed we could expose a method on CacheStatistics to clamp, but for now we rely on its atomic adds.
                // (Leaving as comment: expose ClampApproximateMemory() if future strategies mutate accounting directly.)
            }
        }
    }
}

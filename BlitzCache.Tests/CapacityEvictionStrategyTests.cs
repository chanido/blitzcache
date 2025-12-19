using NUnit.Framework;
using System;
using BlitzCacheCore.Tests.Helpers;
using BlitzCacheCore.Capacity;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class CapacityEvictionStrategyTests 
    { 
        [Test]
        public void LargestFirst_Removes_Fewer_Items_To_Reclaim_Same_Bytes() 
        { 
            const int valueBytesBase = 5_000; 
            const long maxCacheSizeBytes = 40_000; 
            // We'll insert mixed-size items so largest-first should evict fewer.
            var sizes = new[]{1,2,3,4,5,6,7,8}; // scaled by base -> 5k .. 40k

            using var smallestFirstCache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes,
                evictionStrategy: CapacityEvictionStrategy.SmallestFirst);
            smallestFirstCache.InitializeStatistics();

            using var largestFirstCache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes,
                evictionStrategy: CapacityEvictionStrategy.LargestFirst);
            largestFirstCache.InitializeStatistics();

            // Insert all entries (this will overshoot once we insert more than 8*5k == 40k because of overhead)
            foreach (var s in sizes)
            {
                var bytes = new byte[s * valueBytesBase];
                smallestFirstCache.BlitzGet($"sf{s}", () => bytes);
                largestFirstCache.BlitzGet($"lf{s}", () => bytes);
            }

            TestDelays.WaitUntil(() => smallestFirstCache.Statistics!.EvictionCount > 0 && largestFirstCache.Statistics!.EvictionCount > 0);

            var sfStats = smallestFirstCache.Statistics!;
            var lfStats = largestFirstCache.Statistics!;

            Assert.That(sfStats.EvictionCount, Is.GreaterThan(0), "Smallest-first should have evictions");
            Assert.That(lfStats.EvictionCount, Is.GreaterThan(0), "Largest-first should have evictions");
            Assert.That(sfStats.ApproximateMemoryBytes, Is.LessThanOrEqualTo(maxCacheSizeBytes));
            Assert.That(lfStats.ApproximateMemoryBytes, Is.LessThanOrEqualTo(maxCacheSizeBytes));

            // Largest-first should usually require fewer evictions (allow equality to avoid flakiness from overhead)
            Assert.That(lfStats.EvictionCount, Is.LessThanOrEqualTo(sfStats.EvictionCount), $"Expected largest-first to evict fewer or equal entries (sf={sfStats.EvictionCount}, lf={lfStats.EvictionCount})");
        }
    } 
}

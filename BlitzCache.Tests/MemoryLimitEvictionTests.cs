using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class MemoryLimitEvictionTests
    {
        [Test]
        public void CapacityLimit_TriggersEvictions_And_RespectsLimit_Sync()
        {
            const long maxCacheSizeBytes = 50_000; // ~5 entries of 10k each (plus small overhead)
            const int valueBytes = 10_000;
            const int totalEntries = 12; // go well over the limit

            using var cache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes);

            cache.InitializeStatistics();

            // Fill beyond capacity
            for (int i = 0; i < totalEntries; i++)
            {
                var key = $"k{i}";
                var val = cache.BlitzGet(key, () => new byte[valueBytes], TestConstants.LongTimeoutMs);
                Assert.AreEqual(valueBytes, val.Length);
            }

            // Give eviction callbacks a moment
            TestDelays.WaitForEvictionCallbacksSync();

            var stats = cache.Statistics!;
            Assert.Greater(stats.EvictionCount, 0, "Capacity limit should trigger evictions");
            Assert.LessOrEqual(stats.ApproximateMemoryBytes, maxCacheSizeBytes, "Approximate memory should be within configured limit");

            // Access some earliest keys; at least one should have been evicted and get recomputed
            int recomputed = 0;
            for (int i = 0; i < 4; i++)
            {
                var key = $"k{i}";
                int calls = 0;
                var result = cache.BlitzGet(key, () => { calls++; return new byte[valueBytes]; }, TestConstants.LongTimeoutMs);
                Assert.AreEqual(valueBytes, result.Length);
                recomputed += calls; // calls > 0 indicates it was evicted
            }

            Assert.GreaterOrEqual(recomputed, 1, "At least one of the earliest entries should have been evicted and recomputed");
        }

        [Test]
        public async Task CapacityLimit_TriggersEvictions_And_RespectsLimit_Async()
        {
            const long maxCacheSizeBytes = 80_000; // ~8 entries of 10k each (plus small overhead)
            const int valueBytes = 10_000;
            const int totalEntries = 15;

            using var cache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes);

            cache.InitializeStatistics();

            // Fill beyond capacity asynchronously
            for (int i = 0; i < totalEntries; i++)
            {
                var key = $"ak{i}";
                var val = await cache.BlitzGet(key, async () => await Task.FromResult(new byte[valueBytes]), TestConstants.LongTimeoutMs);
                Assert.AreEqual(valueBytes, val.Length);
            }

            await TestDelays.WaitForEvictionCallbacks();

            var stats = cache.Statistics!;
            Assert.Greater(stats.EvictionCount, 0, "Capacity limit should trigger evictions (async)");
            Assert.LessOrEqual(stats.ApproximateMemoryBytes, maxCacheSizeBytes, "Approximate memory should be within configured limit (async)");

            // Access some earliest async keys; expect at least one recomputation
            int recomputed = 0;
            for (int i = 0; i < 5; i++)
            {
                var key = $"ak{i}";
                int calls = 0;
                var result = await cache.BlitzGet(key, async () => { calls++; return await Task.FromResult(new byte[valueBytes]); }, TestConstants.LongTimeoutMs);
                Assert.AreEqual(valueBytes, result.Length);
                recomputed += calls;
            }

            Assert.GreaterOrEqual(recomputed, 1, "At least one of the earliest async entries should have been evicted and recomputed");
        }

        [Test]
        public void CapacityLimit_Works_When_Statistics_Disabled()
        {
            const long maxCacheSizeBytes = 40_000; // about 4 entries of 10k
            const int valueBytes = 10_000;
            const int totalEntries = 10;

            using var cache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes);

            // Intentionally NOT calling InitializeStatistics()

            for (int i = 0; i < totalEntries; i++)
            {
                var key = $"ns{i}";
                var val = cache.BlitzGet(key, () => new byte[valueBytes], TestConstants.LongTimeoutMs);
                Assert.AreEqual(valueBytes, val.Length);
            }

            TestDelays.WaitForEvictionCallbacksSync();

            // We cannot assert stats, but capacity-based eviction should still occur.
            // Verify by accessing the earliest entries; at least one should be recomputed.
            int recomputed = 0;
            for (int i = 0; i < 4; i++)
            {
                var key = $"ns{i}";
                int calls = 0;
                var result = cache.BlitzGet(key, () => { calls++; return new byte[valueBytes]; }, TestConstants.LongTimeoutMs);
                Assert.AreEqual(valueBytes, result.Length);
                recomputed += calls;
            }

            Assert.GreaterOrEqual(recomputed, 1, "Eviction should occur even with statistics disabled");
        }
    }
}

using NUnit.Framework;
using System;
using System.Threading.Tasks;
using BlitzCacheCore.Tests.Helpers;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class CapacityEnforcerDoubleCountTests
    {
        [Test]
        public void ProactiveRemovals_AreCountedOnce()
        {
            const long maxCacheSizeBytes = 60_000; // ~6 entries of 10k
            const int valueBytes = 10_000;
            const int totalEntries = 14; // ensure multiple proactive passes

            using var cache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes);

            cache.InitializeStatistics();

            for (int i = 0; i < totalEntries; i++)
            {
                var key = $"dc{i}";
                cache.BlitzGet(key, () => new byte[valueBytes]);
            }

            TestDelays.WaitForStandardExpiration().GetAwaiter().GetResult();

            var stats = cache.Statistics!;
            // EntryCount + EvictionCount should be close to totalEntries (allow 1 off for timing races)
            var accounted = stats.EntryCount + stats.EvictionCount;
            Assert.Greater(stats.EvictionCount, 0, "Should have evictions after exceeding size limit");
            Assert.LessOrEqual(stats.ApproximateMemoryBytes, maxCacheSizeBytes, "Approximate memory should be within limit");
            Assert.LessOrEqual(Math.Abs(accounted - totalEntries), 1, $"Inconsistent accounting: entries({stats.EntryCount}) + evictions({stats.EvictionCount}) vs inserted({totalEntries})");
        }

        [Test]
        public async Task ProactiveRemovals_AreCountedOnce_Async()
        {
            const long maxCacheSizeBytes = 70_000; // ~7 entries of 10k
            const int valueBytes = 10_000;
            const int totalEntries = 18;

            using var cache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes);

            cache.InitializeStatistics();

            for (int i = 0; i < totalEntries; i++)
            {
                var key = $"adc{i}";
                await cache.BlitzGet(key, async () => await Task.FromResult(new byte[valueBytes]));
            }

            await TestDelays.WaitForStandardExpiration();

            var stats = cache.Statistics!;
            var accounted = stats.EntryCount + stats.EvictionCount;
            Assert.Greater(stats.EvictionCount, 0, "Should have evictions after exceeding size limit (async)");
            Assert.LessOrEqual(stats.ApproximateMemoryBytes, maxCacheSizeBytes, "Approximate memory should be within limit (async)");
            Assert.LessOrEqual(Math.Abs(accounted - totalEntries), 1, $"Inconsistent accounting (async): entries({stats.EntryCount}) + evictions({stats.EvictionCount}) vs inserted({totalEntries})");
        }
    }
}

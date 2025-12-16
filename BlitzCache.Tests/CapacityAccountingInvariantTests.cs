using NUnit.Framework;
using System;
using System.Threading.Tasks;
using BlitzCacheCore.Tests.Helpers;
using BlitzCacheCore.Capacity;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class CapacityAccountingInvariantTests
    {
        private const int ValueBytes = 10_000;

        [Test]
        public void Overfill_Cache_Accounting_Invariants_Hold_Sync()
        {
            const long maxCacheSizeBytes = 60_000; // ~6 entries
            const int totalInsert = 16; // force multiple eviction rounds

            using var cache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes,
                evictionStrategy: CapacityEvictionStrategy.SmallestFirst);
            cache.InitializeStatistics();

            var beforeStats = cache.Statistics!;
            long entryCountBefore = beforeStats.EntryCount;
            long evictionCountBefore = beforeStats.EvictionCount;
            long memoryBefore = beforeStats.ApproximateMemoryBytes;

            for (int i = 0; i < totalInsert; i++)
            {
                cache.BlitzGet($"inv{i}", () => new byte[ValueBytes]);
            }

            TestDelays.WaitForStandardExpiration().GetAwaiter().GetResult();

            var after = cache.Statistics!;
            long entryCountAfter = after.EntryCount;
            long evictionCountAfter = after.EvictionCount;
            long memoryAfter = after.ApproximateMemoryBytes;

            Assert.That(evictionCountAfter, Is.GreaterThan(evictionCountBefore), "Eviction count should have increased");
            Assert.That(memoryAfter, Is.GreaterThanOrEqualTo(0), "Approximate memory should never be negative");
            Assert.That(memoryAfter, Is.LessThanOrEqualTo(maxCacheSizeBytes), "Approximate memory should be within limit");

            var inserted = totalInsert; // each call creates a new key
            var netEntryIncrease = entryCountAfter - entryCountBefore;
            var evicted = evictionCountAfter - evictionCountBefore;
            var expectedEvicted = inserted - netEntryIncrease;

            // Allow small race tolerance (+/-1) for timing of eviction callbacks
            Assert.That(Math.Abs(evicted - expectedEvicted), Is.LessThanOrEqualTo(1), $"Eviction delta mismatch. Inserted={inserted} NetIncrease={netEntryIncrease} Evicted={evicted} Expected={expectedEvicted}");
        }

        [Test]
        public async Task Overfill_Cache_Accounting_Invariants_Hold_Async()
        {
            const long maxCacheSizeBytes = 70_000; // ~7 entries
            const int totalInsert = 18;

            using var cache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes,
                evictionStrategy: CapacityEvictionStrategy.LargestFirst);
            cache.InitializeStatistics();

            var beforeStats = cache.Statistics!;
            long entryCountBefore = beforeStats.EntryCount;
            long evictionCountBefore = beforeStats.EvictionCount;
            long memoryBefore = beforeStats.ApproximateMemoryBytes;

            for (int i = 0; i < totalInsert; i++)
            {
                await cache.BlitzGet($"ainv{i}", async () => await Task.FromResult(new byte[ValueBytes]));
            }

            await TestDelays.WaitForStandardExpiration();

            var after = cache.Statistics!;
            long entryCountAfter = after.EntryCount;
            long evictionCountAfter = after.EvictionCount;
            long memoryAfter = after.ApproximateMemoryBytes;

            Assert.That(evictionCountAfter, Is.GreaterThan(evictionCountBefore), "Eviction count should have increased (async)");
            Assert.That(memoryAfter, Is.GreaterThanOrEqualTo(0), "Approximate memory should never be negative (async)");
            Assert.That(memoryAfter, Is.LessThanOrEqualTo(maxCacheSizeBytes), "Approximate memory should be within limit (async)");

            var inserted = totalInsert;
            var netEntryIncrease = entryCountAfter - entryCountBefore;
            var evicted = evictionCountAfter - evictionCountBefore;
            var expectedEvicted = inserted - netEntryIncrease;
            Assert.That(Math.Abs(evicted - expectedEvicted), Is.LessThanOrEqualTo(1), $"Eviction delta mismatch (async). Inserted={inserted} NetIncrease={netEntryIncrease} Evicted={evicted} Expected={expectedEvicted}");
        }
    }
}

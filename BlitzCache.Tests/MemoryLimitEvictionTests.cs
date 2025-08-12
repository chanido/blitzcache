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
                var val = cache.BlitzGet(key, () => new byte[valueBytes]);
                Assert.AreEqual(valueBytes, val.Length);
            }

            // Give eviction callbacks a moment
            TestDelays.WaitForStandardExpiration();

            var stats = cache.Statistics!;
            Assert.Greater(stats.EvictionCount, 0, "Capacity limit should trigger evictions");
            Assert.LessOrEqual(stats.ApproximateMemoryBytes, maxCacheSizeBytes, "Approximate memory should be within configured limit");

            // Verify that at least one key was evicted by scanning all inserted keys
            int recomputed = 0;
            for (int i = 0; i < totalEntries; i++)
            {
                var key = $"k{i}";
                int calls = 0;
                var result = cache.BlitzGet(key, () => { calls++; return new byte[valueBytes]; });
                Assert.AreEqual(valueBytes, result.Length);
                if (calls > 0) recomputed++;
            }

            Assert.GreaterOrEqual(recomputed, 1, "At least one of the entries should have been evicted and recomputed");
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
                var val = await cache.BlitzGet(key, async () => await Task.FromResult(new byte[valueBytes]));
                Assert.AreEqual(valueBytes, val.Length);
            }

            await TestDelays.WaitForStandardExpiration();

            var stats = cache.Statistics!;
            Assert.Greater(stats.EvictionCount, 0, "Capacity limit should trigger evictions (async)");
            Assert.LessOrEqual(stats.ApproximateMemoryBytes, maxCacheSizeBytes, "Approximate memory should be within configured limit (async)");

            // Verify that at least one key was evicted by scanning all inserted keys
            int recomputed = 0;
            for (int i = 0; i < totalEntries; i++)
            {
                var key = $"ak{i}";
                int calls = 0;
                var result = await cache.BlitzGet(key, async () => { calls++; return await Task.FromResult(new byte[valueBytes]); });
                Assert.AreEqual(valueBytes, result.Length);
                if (calls > 0) recomputed++;
            }

            Assert.GreaterOrEqual(recomputed, 1, "At least one of the async entries should have been evicted and recomputed");
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
                var val = cache.BlitzGet(key, () => new byte[valueBytes]);
                Assert.AreEqual(valueBytes, val.Length);
            }

            TestDelays.WaitForStandardExpiration();

            // We cannot assert stats, but capacity-based eviction should still occur.
            // Verify by scanning all earlier entries; at least one should be recomputed.
            int recomputed = 0;
            for (int i = 0; i < totalEntries; i++)
            {
                var key = $"ns{i}";
                int calls = 0;
                var result = cache.BlitzGet(key, () => { calls++; return new byte[valueBytes]; });
                Assert.AreEqual(valueBytes, result.Length);
                if (calls > 0) recomputed++;
            }

            Assert.GreaterOrEqual(recomputed, 1, "Eviction should occur even with statistics disabled");
        }

        [Test]
        public void CapacityAccounting_Invariants_Sync()
        {
            const long maxCacheSizeBytes = 60_000; // ~6 entries of 10k
            const int valueBytes = 10_000;
            const int totalInsert = 16;

            using var cache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes);
            cache.InitializeStatistics();

            var before = cache.Statistics!;
            long entryBefore = before.EntryCount;
            long evictBefore = before.EvictionCount;

            for (int i = 0; i < totalInsert; i++)
                cache.BlitzGet($"inv{i}", () => new byte[valueBytes]);

            TestDelays.WaitForStandardExpiration();

            var after = cache.Statistics!;
            long entryAfter = after.EntryCount;
            long evictAfter = after.EvictionCount;
            long memAfter = after.ApproximateMemoryBytes;

            // Allow eviction callbacks to register before asserting increase
            if (evictAfter == evictBefore)
            {
                TestDelays.WaitUntil(() => (evictAfter = cache.Statistics!.EvictionCount) > evictBefore);
            }
            Assert.Greater(evictAfter, evictBefore, "Eviction count should increase");
            Assert.GreaterOrEqual(memAfter, 0, "Approximate memory must be non-negative");
            if (!TestDelays.WaitUntil(() => (memAfter = cache.Statistics!.ApproximateMemoryBytes) <= maxCacheSizeBytes))
            {
                Assert.Fail($"Approximate memory exceeded limit after retries. mem={memAfter} limit={maxCacheSizeBytes}");
            }

            var inserted = totalInsert;
            var netIncrease = entryAfter - entryBefore;
            var evicted = evictAfter - evictBefore;
            var expectedEvicted = inserted - netIncrease;
            Assert.LessOrEqual(Math.Abs(evicted - expectedEvicted), 1, $"Eviction delta mismatch: inserted={inserted} net+={netIncrease} evicted={evicted} expected={expectedEvicted}");
        }

        [Test]
        public async Task CapacityAccounting_Invariants_Async()
        {
            const long maxCacheSizeBytes = 70_000; // ~7 entries
            const int valueBytes = 10_000;
            const int totalInsert = 18;

            using var cache = new BlitzCacheInstance(
                defaultMilliseconds: TestConstants.LongTimeoutMs,
                cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs),
                maxTopSlowest: 0,
                valueSizer: null,
                maxTopHeaviest: 0,
                maxCacheSizeBytes: maxCacheSizeBytes);
            cache.InitializeStatistics();

            var before = cache.Statistics!;
            long entryBefore = before.EntryCount;
            long evictBefore = before.EvictionCount;

            for (int i = 0; i < totalInsert; i++)
                await cache.BlitzGet($"ainv{i}", async () => await Task.FromResult(new byte[valueBytes]));

            await TestDelays.WaitForStandardExpiration();

            var after = cache.Statistics!;
            long entryAfter = after.EntryCount;
            long evictAfter = after.EvictionCount;
            long memAfter = after.ApproximateMemoryBytes;

            // Allow eviction callbacks to register before asserting increase (async)
            if (evictAfter == evictBefore)
            {
                await TestDelays.WaitUntilAsync(() => (evictAfter = cache.Statistics!.EvictionCount) > evictBefore);
            }
            Assert.Greater(evictAfter, evictBefore, "Eviction count should increase (async)");
            Assert.GreaterOrEqual(memAfter, 0, "Approximate memory must be non-negative (async)");
            if (!await TestDelays.WaitUntilAsync(() => (memAfter = cache.Statistics!.ApproximateMemoryBytes) <= maxCacheSizeBytes))
            {
                Assert.Fail($"Approximate memory exceeded limit after retries (async). mem={memAfter} limit={maxCacheSizeBytes}");
            }

            var inserted = totalInsert;
            var netIncrease = entryAfter - entryBefore;
            var evicted = evictAfter - evictBefore;
            var expectedEvicted = inserted - netIncrease;
            Assert.LessOrEqual(Math.Abs(evicted - expectedEvicted), 1, $"Eviction delta mismatch (async): inserted={inserted} net+={netIncrease} evicted={evicted} expected={expectedEvicted}");
        }
    }
}

using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Statistics
{
    /// <summary>
    /// Tests for cache statistics and monitoring functionality.
    /// Validates that BlitzCache correctly tracks hits, misses, and other performance metrics.
    /// </summary>
    [TestFixture]
    public class CacheStatisticsTests
    {
        private IBlitzCacheInstance cache;

        [SetUp]
        public void Setup()
        {
            cache = TestFactory.CreateBlitzCacheInstance();
            cache.InitializeStatistics();
        }

        [TearDown]
        public void TearDown() => cache?.Dispose();

        [Test]
        public void Statistics_InitialState_AllCountersAreZero()
        {
            // Act
            var stats = cache.Statistics;

            // Assert
            Assert.AreEqual(0, stats.HitCount, "Initial hit count should be zero");
            Assert.AreEqual(0, stats.MissCount, "Initial miss count should be zero");
            Assert.AreEqual(0.0, stats.HitRatio, 0.001, "Initial hit ratio should be zero");
            Assert.AreEqual(0, stats.EntryCount, "Initial entry count should be zero");
            Assert.AreEqual(0, stats.EvictionCount, "Initial eviction count should be zero");
            Assert.AreEqual(0, stats.ActiveSemaphoreCount, "Initial semaphore count should be zero");
            Assert.AreEqual(0, stats.TotalOperations, "Initial total operations should be zero");
        }

        [Test]
        public void Statistics_CacheMiss_IncrementsCounters()
        {
            // Arrange
            var callCount = 0;
            string TestFunction()
            {
                callCount++;
                return "test result";
            }
            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var entryCountBefore = cache.Statistics.EntryCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;

            // Act
            var result = cache.BlitzGet("test_key", TestFunction, TestConstants.StandardTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(hitCountBefore, stats.HitCount, "Should have no hits");
            Assert.AreEqual(missCountBefore + 1, stats.MissCount, "Should have one miss");
            Assert.AreEqual(0.0, stats.HitRatio, 0.001, "Hit ratio should be zero with only misses");
            Assert.AreEqual(entryCountBefore + 1, stats.EntryCount, "Should have one cached entry");
            Assert.AreEqual(totalOperationsBefore + 1, stats.TotalOperations, "Should have one total operation");
            Assert.AreEqual(1, callCount, "Function should be called once");
            Assert.AreEqual("test result", result, "Should return correct result");
        }

        [Test]
        public void Statistics_CacheHit_IncrementsHitCounter()
        {
            // Arrange
            var callCount = 0;
            string TestFunction()
            {
                callCount++;
                return "test result";
            }

            // Act - First call (miss)
            cache.BlitzGet("test_key", TestFunction, TestConstants.StandardTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var entryCountBefore = cache.Statistics.EntryCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;

            // Act - Second call (hit)
            var result = cache.BlitzGet("test_key", TestFunction, TestConstants.StandardTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(hitCountBefore + 1, stats.HitCount, "Should have one hit");
            Assert.AreEqual(missCountBefore, stats.MissCount, "Should have one miss");
            Assert.AreEqual(0.5, stats.HitRatio, 0.001, "Hit ratio should be 50%");
            Assert.AreEqual(entryCountBefore, stats.EntryCount, "Should still have one cached entry");
            Assert.AreEqual(totalOperationsBefore + 1, stats.TotalOperations, "Should have two total operations");
            Assert.AreEqual(1, callCount, "Function should only be called once");
            Assert.AreEqual("test result", result, "Should return cached result");
        }

        [Test]
        public void Statistics_MultipleHitsAndMisses_CalculatesCorrectRatio()
        {
            // Arrange
            var callCount = 0;
            string TestFunction(string key)
            {
                callCount++;
                return $"result for {key}";
            }
            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var entryCountBefore = cache.Statistics.EntryCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;

            // Act - Create multiple cache entries and hits
            cache.BlitzGet("key1", () => TestFunction("key1"), TestConstants.StandardTimeoutMs); // Miss
            cache.BlitzGet("key2", () => TestFunction("key2"), TestConstants.StandardTimeoutMs); // Miss
            cache.BlitzGet("key1", () => TestFunction("key1"), TestConstants.StandardTimeoutMs); // Hit
            cache.BlitzGet("key2", () => TestFunction("key2"), TestConstants.StandardTimeoutMs); // Hit
            cache.BlitzGet("key1", () => TestFunction("key1"), TestConstants.StandardTimeoutMs); // Hit
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(hitCountBefore + 3, stats.HitCount, "Should have 3 hits");
            Assert.AreEqual(missCountBefore + 2, stats.MissCount, "Should have 2 misses");
            Assert.AreEqual(0.6, stats.HitRatio, 0.001, "Hit ratio should be 60% (3/5)");
            Assert.AreEqual(entryCountBefore + 2, stats.EntryCount, "Should have 2 cached entries");
            Assert.AreEqual(totalOperationsBefore + 5, stats.TotalOperations, "Should have 5 total operations");
            Assert.AreEqual(2, callCount, "Function should be called twice");
        }

        [Test]
        public async Task Statistics_AsyncOperations_TracksCorrectly()
        {
            // Arrange
            var callCount = 0;
            async Task<string> TestFunctionAsync()
            {
                callCount++;
                await TestDelays.ShortDelay();
                return "async result";
            }
            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var entryCountBefore = cache.Statistics.EntryCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;

            // Act
            var result1 = await cache.BlitzGet("async_key", TestFunctionAsync, TestConstants.StandardTimeoutMs); // Miss
            await TestDelays.WaitForEvictionCallbacks();

            var hitCountAfterFirst = cache.Statistics.HitCount;
            var missCountAfterFirst = cache.Statistics.MissCount;
            var totalOperationsAfterFirst = cache.Statistics.TotalOperations;

            var result2 = await cache.BlitzGet("async_key", TestFunctionAsync, TestConstants.StandardTimeoutMs); // Hit
            await TestDelays.WaitForEvictionCallbacks();

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(hitCountAfterFirst + 1, stats.HitCount, "Should have one hit");
            Assert.AreEqual(missCountBefore + 1, stats.MissCount, "Should have one miss");
            Assert.AreEqual(0.5, stats.HitRatio, 0.001, "Hit ratio should be 50%");
            Assert.AreEqual(entryCountBefore + 1, stats.EntryCount, "Should have one cached entry");
            Assert.AreEqual(totalOperationsBefore + 2, stats.TotalOperations, "Should have two total operations");
            Assert.AreEqual(1, callCount, "Function should only be called once");
            Assert.AreEqual("async result", result1);
            Assert.AreEqual("async result", result2);
        }

        [Test]
        public void Statistics_RemoveOperation_IncrementsEvictionCount()
        {
            // Arrange
            cache.BlitzGet("test_key", () => "test value", TestConstants.StandardTimeoutMs);
            var evictionCountBefore = cache.Statistics.EvictionCount;

            // Act
            cache.Remove("test_key");
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            Assert.AreEqual(evictionCountBefore + 1, cache.Statistics.EvictionCount, "Eviction count should increase by 1");
            Assert.AreEqual(0, cache.Statistics.EntryCount, "Entry count should be zero after removal");
        }

        [Test]
        public void Statistics_RemoveNonExistentKey_DoesNotIncrementEvictionCount()
        {
            // Arrange
            var evictionCountBefore = cache.Statistics.EvictionCount;

            // Act
            cache.Remove("non_existent_key");
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            Assert.AreEqual(evictionCountBefore, cache.Statistics.EvictionCount, "Eviction count should not change");
        }

        [Test]
        public void Statistics_BlitzUpdate_DoesNotAffectHitMissCounters()
        {
            // Arrange
            cache.BlitzGet("test_key", () => "original", TestConstants.StandardTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;

            // Act
            cache.BlitzUpdate("test_key", () => "updated", TestConstants.StandardTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            var statsAfter = cache.Statistics;
            Assert.AreEqual(hitCountBefore, statsAfter.HitCount, "Hit count should not change");
            Assert.AreEqual(missCountBefore, statsAfter.MissCount, "Miss count should not change");
            Assert.AreEqual(totalOperationsBefore, statsAfter.TotalOperations, "Total operations should not change");
        }

        [Test]
        public void Statistics_Reset_ClearsAllCounters()
        {
            // Arrange - Generate some statistics
            cache.BlitzGet("key1", () => "value1", TestConstants.StandardTimeoutMs); // Miss
            cache.BlitzGet("key1", () => "value1", TestConstants.StandardTimeoutMs); // Hit
            cache.Remove("key1"); // Eviction
            TestDelays.WaitForEvictionCallbacksSync();

            // Verify we have some stats
            var statsBefore = cache.Statistics;
            Assert.Greater(statsBefore.TotalOperations, 0, "Should have operations before reset");

            // Act
            cache.Statistics.Reset();
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            var statsAfter = cache.Statistics;
            Assert.AreEqual(0, statsAfter.HitCount, "Hit count should be reset");
            Assert.AreEqual(0, statsAfter.MissCount, "Miss count should be reset");
            Assert.AreEqual(0, statsAfter.EvictionCount, "Eviction count should be reset");
            Assert.AreEqual(0.0, statsAfter.HitRatio, 0.001, "Hit ratio should be reset");
            Assert.AreEqual(0, statsAfter.TotalOperations, "Total operations should be reset");
            // Note: CurrentEntryCount and ActiveSemaphoreCount reflect actual state, not counters
        }

        [Test]
        public void Statistics_ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var tasks = new Task[TestConstants.SmallLoopCount];
            var totalOperations = TestConstants.ConcurrentOperationsCount;
            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;

            // Act - Multiple threads performing cache operations
            for (int i = 0; i < tasks.Length; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < totalOperations / tasks.Length; j++)
                    {
                        var key = $"thread_{threadId}_key_{j % 5}"; // Some keys will repeat (hits)
                        cache.BlitzGet(key, () => $"value_{threadId}_{j}", TestConstants.StandardTimeoutMs);
                    }
                });
            }

            Task.WaitAll(tasks);
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(totalOperationsBefore + totalOperations, stats.TotalOperations, "Should track all operations");
            Assert.Greater(stats.HitCount, hitCountBefore, "Should have some hits due to key repetition");
            Assert.Greater(stats.MissCount, missCountBefore, "Should have some misses");
            Assert.AreEqual(stats.HitCount + stats.MissCount, stats.TotalOperations, "Hits + misses should equal total operations");
        }

        [Test]
        public void Statistics_ActiveSemaphoreCount_ReflectsCurrentState()
        {
            // Arrange
            var initialSemaphoreCount = cache.Statistics.ActiveSemaphoreCount;

            // Act - Create some cache entries
            cache.BlitzGet("key1", () => "value1", TestConstants.StandardTimeoutMs);
            cache.BlitzGet("key2", () => "value2", TestConstants.StandardTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            var statsAfter = cache.Statistics;
            Assert.GreaterOrEqual(statsAfter.ActiveSemaphoreCount, initialSemaphoreCount, "Semaphore count should reflect active semaphores");
        }

        [Test]
        public async Task Statistics_AutomaticExpiration_TracksEvictionCorrectly()
        {
            // Arrange
            var evictionCountBefore = cache.Statistics.EvictionCount;
            var entryCountBefore = cache.Statistics.EntryCount;
            var missCountBefore = cache.Statistics.MissCount;

            // Act - Add cache entry with short expiration
            cache.BlitzGet("expiring_key", () => "value1", TestConstants.VeryShortTimeoutMs);
            await TestDelays.WaitForEvictionCallbacks();

            // Wait for automatic expiration
            await TestDelays.WaitForStandardExpiration();

            // Try to access the expired entry (this should trigger cleanup and create new entry)
            var result = cache.BlitzGet("expiring_key", () => "new_value", TestConstants.VeryShortTimeoutMs);
            await TestDelays.WaitForEvictionCallbacks();

            // Assert
            var stats = cache.Statistics;
            Assert.Greater(stats.EvictionCount, evictionCountBefore, "Should have tracked automatic evictions");
            Assert.AreEqual(entryCountBefore + 1, stats.EntryCount, "Should have 1 new entry after re-creation");
            Assert.AreEqual(missCountBefore + 2, stats.MissCount, "Should have 2 misses (1 initial + 1 after expiration)");
            Assert.AreEqual("new_value", result, "Should return new value");
        }

        [Test]
        public async Task Statistics_MixedEvictions_TracksAllCorrectly()
        {
            // Arrange
            var evictionCountBefore = cache.Statistics.EvictionCount;
            var entryCountBefore = cache.Statistics.EntryCount;

            // Act - Mix of automatic and manual evictions
            cache.BlitzGet("auto_expire", () => "value1", TestConstants.StandardTimeoutMs);
            cache.BlitzGet("manual_remove", () => "value2", TestConstants.StandardTimeoutMs);
            cache.BlitzGet("keep_alive", () => "value3", TestConstants.StandardTimeoutMs);
            await TestDelays.WaitForEvictionCallbacks();

            // Manual removal
            cache.Remove("manual_remove");
            await TestDelays.WaitForEvictionCallbacks();

            // Wait for automatic expiration
            await TestDelays.WaitForStandardExpiration();

            // Access expired key to trigger callback
            cache.BlitzGet("auto_expire", () => "new_value", TestConstants.StandardTimeoutMs);
            await TestDelays.WaitForEvictionCallbacks();

            // Assert
            var finalStats = cache.Statistics;
            Assert.AreEqual(entryCountBefore + 2, finalStats.EntryCount, "Should have 2 entries (keep_alive + new auto_expire)");
            Assert.Greater(finalStats.EvictionCount, evictionCountBefore + 1, "Should have tracked both manual and automatic evictions");
        }

        [Test]
        public void Statistics_CurrentEntryCount_StaysAccurate()
        {
            // Arrange
            var entryCountBefore = cache.Statistics.EntryCount;
            var evictionCountBefore = cache.Statistics.EvictionCount;

            // Act - Add some entries
            cache.BlitzGet("key1", () => "value1", TestConstants.StandardTimeoutMs);
            cache.BlitzGet("key2", () => "value2", TestConstants.StandardTimeoutMs);
            cache.BlitzGet("key3", () => "value3", TestConstants.StandardTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            var entryCountAfterAdding = cache.Statistics.EntryCount;

            // Remove one
            cache.Remove("key2");
            TestDelays.WaitForEvictionCallbacksSync();

            var entryCountAfterRemoval = cache.Statistics.EntryCount;
            var evictionCountAfterRemoval = cache.Statistics.EvictionCount;

            // Add another
            cache.BlitzGet("key4", () => "value4", TestConstants.StandardTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            Assert.AreEqual(entryCountBefore + 3, entryCountAfterAdding, "Should have 3 entries after adding");
            Assert.AreEqual(entryCountBefore + 2, entryCountAfterRemoval, "Should have 2 entries after removal");
            Assert.AreEqual(evictionCountBefore + 1, evictionCountAfterRemoval, "Should have 1 eviction after removal");
            Assert.AreEqual(entryCountBefore + 3, cache.Statistics.EntryCount, "Should have 3 entries again after final addition");
            Assert.AreEqual(evictionCountBefore + 1, cache.Statistics.EvictionCount, "Eviction count should remain at 1");
        }

        [Test]
        public void Statistics_WhenDisabled_ReturnsNull()
        {
            // Arrange
            var cacheWithoutStats = new BlitzCacheInstance();

            try
            {
                // Act & Assert
                Assert.IsNull(cacheWithoutStats.Statistics, "Statistics should be null when disabled");
            }
            finally
            {
                cacheWithoutStats.Dispose();
            }
        }

        [Test]
        public void Statistics_WhenDisabled_CacheStillWorks()
        {
            // Arrange
            var cacheWithoutStats = new BlitzCacheInstance();
            var callCount = 0;
            string TestFunction()
            {
                callCount++;
                return $"result-{callCount}";
            }

            try
            {
                // Act
                var result1 = cacheWithoutStats.BlitzGet("test-key", TestFunction, TestConstants.StandardTimeoutMs);
                var result2 = cacheWithoutStats.BlitzGet("test-key", TestFunction, TestConstants.StandardTimeoutMs);

                // Assert
                Assert.AreEqual("result-1", result1, "First call should return computed result");
                Assert.AreEqual("result-1", result2, "Second call should return cached result");
                Assert.AreEqual(1, callCount, "Function should only be called once (cached on second call)");
                Assert.IsNull(cacheWithoutStats.Statistics, "Statistics should remain null");
            }
            finally
            {
                cacheWithoutStats.Dispose();
            }
        }

        [Test]
        public async Task Statistics_WhenDisabled_AsyncCacheStillWorks()
        {
            // Arrange
            var cacheWithoutStats = new BlitzCacheInstance();
            var callCount = 0;
            async Task<string> TestFunction()
            {
                callCount++;
                await TestDelays.ShortDelay();
                return $"async-result-{callCount}";
            }

            try
            {
                // Act
                var result1 = await cacheWithoutStats.BlitzGet("async-test-key", TestFunction, TestConstants.StandardTimeoutMs);
                var result2 = await cacheWithoutStats.BlitzGet("async-test-key", TestFunction, TestConstants.StandardTimeoutMs);

                // Assert
                Assert.AreEqual("async-result-1", result1, "First async call should return computed result");
                Assert.AreEqual("async-result-1", result2, "Second async call should return cached result");
                Assert.AreEqual(1, callCount, "Async function should only be called once (cached on second call)");
                Assert.IsNull(cacheWithoutStats.Statistics, "Statistics should remain null");
            }
            finally
            {
                cacheWithoutStats.Dispose();
            }
        }

        [Test]
        public void Statistics_WhenDisabled_UpdateOperationsWork()
        {
            // Arrange
            var cacheWithoutStats = new BlitzCacheInstance();

            try
            {
                // Act
                cacheWithoutStats.BlitzUpdate("update-key", () => "initial-value", TestConstants.StandardTimeoutMs);
                var result1 = cacheWithoutStats.BlitzGet("update-key", () => "fallback-value", TestConstants.StandardTimeoutMs);

                cacheWithoutStats.BlitzUpdate("update-key", () => "updated-value", TestConstants.StandardTimeoutMs);
                var result2 = cacheWithoutStats.BlitzGet("update-key", () => "fallback-value", TestConstants.StandardTimeoutMs);

                // Assert
                Assert.AreEqual("initial-value", result1, "Should get initial updated value");
                Assert.AreEqual("updated-value", result2, "Should get updated value after second update");
                Assert.IsNull(cacheWithoutStats.Statistics, "Statistics should remain null");
            }
            finally
            {
                cacheWithoutStats.Dispose();
            }
        }

        [Test]
        public void TopSlowestQueries_TracksSlowQueriesCorrectly()
        {
            // Arrange: create cache with TopSlowestQueries enabled (e.g., 3)
            var cache = new BlitzCacheInstance(maxTopSlowest: 3);
            cache.InitializeStatistics();

            // Insert queries with varying durations
            cache.BlitzGet("q1", () => { Task.Delay(10).Wait(); return "v1"; }); // Simulate 10ms
            cache.BlitzGet("q2", () => { Task.Delay(30).Wait(); return "v2"; }); // Simulate 30ms
            cache.BlitzGet("q3", () => { Task.Delay(20).Wait(); return "v3"; }); // Simulate 20ms
            cache.BlitzGet("q4", () => { Task.Delay(40).Wait(); return "v4"; }); // Simulate 40ms

            // Act
            var topSlowest = cache.Statistics.TopSlowestQueries?.ToList();

            // Assert
            Assert.NotNull(topSlowest, "TopSlowestQueries should not be null");
            Assert.AreEqual(3, topSlowest.Count, "Should only keep the top 3 slowest queries");
            // Should be sorted descending by duration (worst case)
            var durations = topSlowest.Select(q => q.WorstCaseMs).ToList();
            for (int i = 1; i < durations.Count; i++)
                Assert.GreaterOrEqual(durations[i - 1], durations[i], "TopSlowestQueries should be sorted descending");
            // The slowest queries should be q4, q2, q3 (40, 30, 20)
            var keys = topSlowest.Select(q => q.CacheKey).ToList();
            CollectionAssert.AreEquivalent(new[] { "q4", "q2", "q3" }, keys, "Top slowest queries should be correct");
        }

        [Test]
        public async Task TopSlowestQueries_UpdatesOnRepeatedQueries()
        {
            // Arrange: create cache with TopSlowestQueries enabled (e.g., 2)
            var cache = new BlitzCacheInstance(maxTopSlowest: 2);
            cache.InitializeStatistics();

            // Insert a query, then update it with a slower duration
            cache.BlitzGet("q1", () => { Task.Delay(TestConstants.EvictionCallbackWaitMs).Wait(); return "v1"; }, TestConstants.EvictionCallbackWaitMs); //This call will take very little time and be disposed very quickly
            cache.BlitzGet("q2", () => { Task.Delay(TestConstants.StandardTimeoutMs).Wait(); return "v2"; });

            await TestDelays.ShortDelay(); // We wait until q1 is disposed

            // Now update q1 with a slower run
            cache.BlitzGet("q1", () => { Task.Delay(TestConstants.StandardTimeoutMs * 2).Wait(); return "v1b"; }); // Twice as slow as q2

            // Act
            var topSlowest = cache.Statistics.TopSlowestQueries?.ToList();

            // Assert
            Assert.NotNull(topSlowest, "TopSlowestQueries should not be null");
            Assert.AreEqual(2, topSlowest.Count, "Should only keep the top 2 slowest queries");
            // q1 should now be the slowest
            Assert.AreEqual("q1", topSlowest[0].CacheKey, "q1 should be the slowest after update");
            Assert.GreaterOrEqual(topSlowest[0].WorstCaseMs, topSlowest[1].WorstCaseMs, "Should be sorted descending");
        }

        [Test]
        public void TopSlowestQueries_EmptyWhenDisabled()
        {
            // Arrange: create cache with TopSlowestQueries disabled
            var cache = new BlitzCacheInstance(maxTopSlowest: 0);
            cache.InitializeStatistics();

            // Insert queries
            cache.BlitzGet("q1", () => { Task.Delay(10).Wait(); return "v1"; });
            cache.BlitzGet("q2", () => { Task.Delay(20).Wait(); return "v2"; });

            // Act
            var topSlowest = cache.Statistics.TopSlowestQueries;

            // Assert
            Assert.IsTrue(topSlowest == null || !topSlowest.Any(), "TopSlowestQueries should be null or empty when disabled");
        }

        [Test]
        public void MemoryTracking_ApproximateBytes_IncreasesOnAdd_DecreasesOnRemove()
        {
            // Arrange
            var initialBytes = cache.Statistics.ApproximateMemoryBytes;

            // Act
            cache.BlitzGet("size_key1", () => new byte[1024], TestConstants.StandardTimeoutMs);
            cache.BlitzGet("size_key2", () => new string('a', 100), TestConstants.StandardTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            var afterAdd = cache.Statistics.ApproximateMemoryBytes;
            Assert.Greater(afterAdd, initialBytes, "Memory should increase after adding entries");

            // Remove one key
            cache.Remove("size_key1");
            TestDelays.WaitForEvictionCallbacksSync();

            var afterRemove = cache.Statistics.ApproximateMemoryBytes;
            Assert.Less(afterRemove, afterAdd, "Memory should decrease after removing an entry");
        }

        [Test]
        public void TopHeaviestEntries_TracksLargest_ByApproximateSize()
        {
            // Arrange: create a dedicated cache to control config
            var local = new BlitzCacheInstance(maxTopHeaviest: 2);
            local.InitializeStatistics();

            // Add entries with different sizes
            local.BlitzGet("k_small", () => new byte[10], TestConstants.StandardTimeoutMs);
            local.BlitzGet("k_medium", () => new byte[500], TestConstants.StandardTimeoutMs);
            local.BlitzGet("k_large", () => new byte[2000], TestConstants.StandardTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            // Act
            var top = local.Statistics.TopHeaviestEntries.ToList();

            // Assert: only largest two are kept and ordered desc
            Assert.AreEqual(2, top.Count);
            Assert.AreEqual("k_large", top[0].CacheKey);
            Assert.AreEqual("k_medium", top[1].CacheKey);

            local.Dispose();
        }

         [Test]
        public async Task BlitzGet_AutoKey_WithNuances_SyncAndAsync_TrackStatistics()
        {
            var cache = new BlitzCacheInstance();
            cache.InitializeStatistics();

            // Sync with Nuances (auto-key)
            int syncCalls = 0;
            string SyncFunc(Nuances n)
            {
                syncCalls++;
                n.CacheRetention = TestConstants.VeryShortTimeoutMs;
                return "sync-value";
            }

            var s1 = cache.BlitzGet(SyncFunc);
            var s2 = cache.BlitzGet(SyncFunc);

            Assert.AreEqual("sync-value", s1);
            Assert.AreEqual("sync-value", s2);
            Assert.AreEqual(1, syncCalls, "Sync Nuances function should execute once with auto-key");

            var statsAfterSync = cache.Statistics;
            Assert.IsNotNull(statsAfterSync);
            Assert.AreEqual(1, statsAfterSync!.MissCount, "One miss recorded for first sync call");
            Assert.AreEqual(1, statsAfterSync.HitCount, "One hit recorded for second sync call");
            Assert.AreEqual(1, statsAfterSync.EntryCount, "One entry present after sync calls");

            // Async with Nuances (explicit key to avoid auto-key collision in same method)
            int asyncCalls = 0;
            Task<string> AsyncFunc(Nuances n)
            {
                asyncCalls++;
                n.CacheRetention = TestConstants.VeryShortTimeoutMs;
                return Task.FromResult("async-value");
            }

            var a1 = await cache.BlitzGet("async-key", AsyncFunc);
            var a2 = await cache.BlitzGet("async-key", AsyncFunc);

            Assert.AreEqual("async-value", a1);
            Assert.AreEqual("async-value", a2);
            Assert.AreEqual(1, asyncCalls, "Async Nuances function should execute once with explicit key");

            var statsAfterAsync = cache.Statistics;
            Assert.IsNotNull(statsAfterAsync);
            // Totals include previous sync operations too
            Assert.AreEqual(2, statsAfterAsync!.MissCount, "Two misses total (sync + async first calls)");
            Assert.AreEqual(2, statsAfterAsync.HitCount, "Two hits total (sync + async second calls)");
            Assert.AreEqual(2, statsAfterAsync.EntryCount, "Two distinct entries (sync and async) present");

            // Verify expiration interacts with stats for auto-key + Nuances
            await TestDelays.WaitForStandardExpiration();
            var s3 = cache.BlitzGet(SyncFunc); // Re-create after expiration
            await TestDelays.WaitForEvictionCallbacks();

            var statsAfterExpire = cache.Statistics;
            Assert.IsNotNull(statsAfterExpire);
            Assert.GreaterOrEqual(statsAfterExpire!.EvictionCount, 1, "Automatic expiration should increment eviction count");
            Assert.AreEqual(3, statsAfterExpire.MissCount, "Miss count should include re-creation after expiration");
            Assert.AreEqual(2, statsAfterExpire.EntryCount, "Entry count remains at two after re-creation");

            cache.Dispose();
        }

        [Test]
        public async Task InterfaceCoverage_BlitzUpdate_Remove_Overloads_And_Stats()
        {
            var cache = new BlitzCacheInstance();
            cache.InitializeStatistics();

            // 1) BlitzGet auto-key (no Nuances) - sync
            int callsAutoNoNuSync = 0;
            string AutoNoNuSync() { callsAutoNoNuSync++; return "v-auto"; }
            var g1 = cache.BlitzGet(AutoNoNuSync);
            var g2 = cache.BlitzGet(AutoNoNuSync);
            Assert.AreEqual("v-auto", g1);
            Assert.AreEqual("v-auto", g2);
            Assert.AreEqual(1, callsAutoNoNuSync, "Auto-key sync without Nuances should execute once");

            // 2) BlitzGet explicit key (no Nuances) - async
            int callsKeyNoNuAsync = 0;
            async Task<string> KeyNoNuAsync()
            {
                callsKeyNoNuAsync++;
                return await Task.FromResult("v-async-no-nu");
            }
            var a1 = await cache.BlitzGet("k-async-no-nu", KeyNoNuAsync);
            var a2 = await cache.BlitzGet("k-async-no-nu", KeyNoNuAsync);
            Assert.AreEqual("v-async-no-nu", a1);
            Assert.AreEqual("v-async-no-nu", a2);
            Assert.AreEqual(1, callsKeyNoNuAsync, "Explicit key async without Nuances should execute once");

            // 3) BlitzGet explicit key (with Nuances) - sync
            int callsKeyWithNuSync = 0;
            string KeyWithNuSync(Nuances n)
            {
                callsKeyWithNuSync++;
                n.CacheRetention = TestConstants.StandardTimeoutMs;
                return "v-sync-nu";
            }
            var ns1 = cache.BlitzGet("k-sync-nu", KeyWithNuSync);
            var ns2 = cache.BlitzGet("k-sync-nu", KeyWithNuSync);
            Assert.AreEqual("v-sync-nu", ns1);
            Assert.AreEqual("v-sync-nu", ns2);
            Assert.AreEqual(1, callsKeyWithNuSync, "Explicit key sync with Nuances should execute once");

            // 4) BlitzUpdate sync: create and update
            cache.BlitzUpdate("u-sync", () => "u1", TestConstants.StandardTimeoutMs);
            var u1 = cache.BlitzGet("u-sync", () => "ignored");
            Assert.AreEqual("u1", u1, "Value should come from BlitzUpdate-created entry");

            cache.BlitzUpdate("u-sync", () => "u2", TestConstants.StandardTimeoutMs);
            var u2 = cache.BlitzGet("u-sync", () => "ignored2");
            Assert.AreEqual("u2", u2, "Value should reflect BlitzUpdate overwrite");

            // 5) BlitzUpdate async: create and update
            await cache.BlitzUpdate("u-async", async () => await Task.FromResult("a1"), TestConstants.StandardTimeoutMs);
            var ga1 = await cache.BlitzGet("u-async", async () => await Task.FromResult("ignored"));
            Assert.AreEqual("a1", ga1);

            await cache.BlitzUpdate("u-async", async () => await Task.FromResult("a2"), TestConstants.StandardTimeoutMs);
            var ga2 = await cache.BlitzGet("u-async", async () => await Task.FromResult("ignored2"));
            Assert.AreEqual("a2", ga2);

            // 6) Remove: ensure miss after removal
            var statsBeforeRemove = cache.Statistics!;
            long missBefore = statsBeforeRemove.MissCount;

            cache.Remove("u-sync");
            await TestDelays.WaitForEvictionCallbacks();

            var afterRemoveGet = cache.BlitzGet("u-sync", () => "recreated");
            Assert.AreEqual("recreated", afterRemoveGet, "Entry should be recreated after removal");

            var statsAfterRemove = cache.Statistics!;
            Assert.AreEqual(missBefore + 1, statsAfterRemove.MissCount, "Remove should lead to a miss on next get");

            // 7) GetSemaphoreCount
            Assert.GreaterOrEqual(cache.GetSemaphoreCount(), 0, "Semaphore count should be non-negative");

            cache.Dispose();
        }

        [Test]
        public void Statistics_IsNull_Then_NotNull_After_Initialize()
        {
            using var cache = new BlitzCacheInstance();
            Assert.IsNull(cache.Statistics, "Statistics should be null before InitializeStatistics for performance");
            cache.InitializeStatistics();
            Assert.IsNotNull(cache.Statistics, "Statistics should be available after InitializeStatistics");
        }

        [Test]
        public async Task BlitzGet_AutoKey_Async_NoNuances_TrackStatistics()
        {
            var cache = new BlitzCacheInstance();
            cache.InitializeStatistics();

            int calls = 0;
            async Task<string> Factory()
            {
                calls++;
                return await Task.FromResult("auto-async-no-nu");
            }

            var v1 = await cache.BlitzGet(Factory);
            var v2 = await cache.BlitzGet(Factory);

            Assert.AreEqual("auto-async-no-nu", v1);
            Assert.AreEqual("auto-async-no-nu", v2);
            Assert.AreEqual(1, calls, "Auto-key async without Nuances should execute once");

            var stats = cache.Statistics!;
            Assert.AreEqual(1, stats.MissCount);
            Assert.AreEqual(1, stats.HitCount);
            Assert.AreEqual(1, stats.EntryCount);

            cache.Dispose();
        }

        [Test]
        public async Task BlitzGet_AutoKey_Async_WithNuances_TrackStatistics()
        {
            var cache = new BlitzCacheInstance();
            cache.InitializeStatistics();

            int calls = 0;
            Task<string> Factory(Nuances n)
            {
                calls++;
                n.CacheRetention = TestConstants.VeryShortTimeoutMs;
                return Task.FromResult("auto-async-with-nu");
            }

            var v1 = await cache.BlitzGet(Factory);
            var v2 = await cache.BlitzGet(Factory);

            Assert.AreEqual("auto-async-with-nu", v1);
            Assert.AreEqual("auto-async-with-nu", v2);
            Assert.AreEqual(1, calls, "Auto-key async with Nuances should execute once");

            var stats = cache.Statistics!;
            Assert.AreEqual(1, stats.MissCount);
            Assert.AreEqual(1, stats.HitCount);
            Assert.AreEqual(1, stats.EntryCount);

            cache.Dispose();
        }
    }
}


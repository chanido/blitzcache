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
            Assert.That(stats.HitCount, Is.EqualTo(0), "Initial hit count should be zero");
            Assert.That(stats.MissCount, Is.EqualTo(0), "Initial miss count should be zero");
            Assert.That(stats.HitRatio, Is.EqualTo(0.0).Within(0.001), "Initial hit ratio should be zero");
            Assert.That(stats.EntryCount, Is.EqualTo(0), "Initial entry count should be zero");
            Assert.That(stats.EvictionCount, Is.EqualTo(0), "Initial eviction count should be zero");
            Assert.That(stats.ActiveSemaphoreCount, Is.EqualTo(0), "Initial semaphore count should be zero");
            Assert.That(stats.TotalOperations, Is.EqualTo(0), "Initial total operations should be zero");
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
            Assert.That(stats.HitCount, Is.EqualTo(hitCountBefore), "Should have no hits");
            Assert.That(stats.MissCount, Is.EqualTo(missCountBefore + 1), "Should have one miss");
            Assert.That(stats.HitRatio, Is.EqualTo(0.0).Within(0.001), "Initial hit ratio should be zero");
            Assert.That(stats.EntryCount, Is.EqualTo(entryCountBefore + 1), "Should have one cached entry");
            Assert.That(stats.TotalOperations, Is.EqualTo(totalOperationsBefore + 1), "Should have one total operation");
            Assert.That(callCount, Is.EqualTo(1), "Function should be called once");
            Assert.That(result, Is.EqualTo("test result"), "Should return correct result");
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
            Assert.That(stats.HitCount, Is.EqualTo(hitCountBefore + 1), "Should have one hit");
            Assert.That(stats.MissCount, Is.EqualTo(missCountBefore), "Should have one miss");
            Assert.That(stats.HitRatio, Is.EqualTo(0.5).Within(0.001), "Hit ratio should be 50%");
            Assert.That(stats.EntryCount, Is.EqualTo(entryCountBefore), "Should still have one cached entry");
            Assert.That(stats.TotalOperations, Is.EqualTo(totalOperationsBefore + 1), "Should have two total operations");
            Assert.That(callCount, Is.EqualTo(1), "Function should only be called once");
            Assert.That(result, Is.EqualTo("test result"), "Should return cached result");
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
            Assert.That(stats.HitCount, Is.EqualTo(hitCountBefore + 3), "Should have 3 hits");
            Assert.That(stats.MissCount, Is.EqualTo(missCountBefore + 2), "Should have 2 misses");
            Assert.That(stats.HitRatio, Is.EqualTo(0.6).Within(0.001), "Hit ratio should be 60% (3/5)");
            Assert.That(stats.EntryCount, Is.EqualTo(entryCountBefore + 2), "Should have 2 cached entries");
            Assert.That(stats.TotalOperations, Is.EqualTo(totalOperationsBefore + 5), "Should have 5 total operations");
            Assert.That(callCount, Is.EqualTo(2), "Function should be called twice");
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
            Assert.That(stats.HitCount, Is.EqualTo(hitCountAfterFirst + 1), "Should have one hit");
            Assert.That(stats.MissCount, Is.EqualTo(missCountBefore + 1), "Should have one miss");
            Assert.That(stats.HitRatio, Is.EqualTo(0.5).Within(0.001), "Hit ratio should be 50%");
            Assert.That(stats.EntryCount, Is.EqualTo(entryCountBefore + 1), "Should have one cached entry");
            Assert.That(stats.TotalOperations, Is.EqualTo(totalOperationsBefore + 2), "Should have two total operations");
            Assert.That(callCount, Is.EqualTo(1), "Function should only be called once");
            Assert.That(result1, Is.EqualTo("async result"));
            Assert.That(result2, Is.EqualTo("async result"));
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
            Assert.That(cache.Statistics.EvictionCount, Is.EqualTo(evictionCountBefore + 1), "Eviction count should increase by 1");
            Assert.That(cache.Statistics.EntryCount, Is.EqualTo(0), "Entry count should be zero after removal");
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
            Assert.That(cache.Statistics.EvictionCount, Is.EqualTo(evictionCountBefore), "Eviction count should not change");
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
            Assert.That(statsAfter.HitCount, Is.EqualTo(hitCountBefore), "Hit count should not change");
            Assert.That(statsAfter.MissCount, Is.EqualTo(missCountBefore), "Miss count should not change");
            Assert.That(statsAfter.TotalOperations, Is.EqualTo(totalOperationsBefore), "Total operations should not change");
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
            Assert.That(statsBefore.TotalOperations, Is.GreaterThan(0), "Should have operations before reset");

            // Act
            cache.Statistics.Reset();
            TestDelays.WaitForEvictionCallbacksSync();

            // Assert
            var statsAfter = cache.Statistics;
            Assert.That(statsAfter.HitCount, Is.EqualTo(0), "Hit count should be reset");
            Assert.That(statsAfter.MissCount, Is.EqualTo(0), "Miss count should be reset");
            Assert.That(statsAfter.EvictionCount, Is.EqualTo(0), "Eviction count should be reset");
            Assert.That(statsAfter.HitRatio, Is.EqualTo(0.0).Within(0.001), "Hit ratio should be reset");
            Assert.That(statsAfter.TotalOperations, Is.EqualTo(0), "Total operations should be reset");
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
            Assert.That(stats.TotalOperations, Is.EqualTo(totalOperationsBefore + totalOperations), "Should track all operations");
            Assert.That(stats.HitCount, Is.GreaterThan(hitCountBefore), "Should have some hits due to key repetition");
            Assert.That(stats.MissCount, Is.GreaterThan(missCountBefore), "Should have some misses");
            Assert.That(stats.TotalOperations, Is.EqualTo(stats.HitCount + stats.MissCount), "Hits + misses should equal total operations");
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
            Assert.That(statsAfter.ActiveSemaphoreCount, Is.GreaterThanOrEqualTo(initialSemaphoreCount), "Semaphore count should reflect active semaphores");
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
            Assert.That(stats.EvictionCount, Is.GreaterThan(evictionCountBefore), "Should have tracked automatic evictions");
            Assert.That(stats.EntryCount, Is.EqualTo(entryCountBefore + 1), "Should have 1 new entry after re-creation");
            Assert.That(stats.MissCount, Is.EqualTo(missCountBefore + 2), "Should have 2 misses (1 initial + 1 after expiration)");
            Assert.That(result, Is.EqualTo("new_value"), "Should return new value");
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
            Assert.That(finalStats.EntryCount, Is.EqualTo(entryCountBefore + 2), "Should have 2 entries (keep_alive + new auto_expire)");
            Assert.That(finalStats.EvictionCount, Is.GreaterThan(evictionCountBefore + 1), "Should have tracked both manual and automatic evictions");
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
            Assert.That(entryCountAfterAdding, Is.EqualTo(entryCountBefore + 3), "Should have 3 entries after adding");
            Assert.That(entryCountAfterRemoval, Is.EqualTo(entryCountBefore + 2), "Should have 2 entries after removal");
            Assert.That(evictionCountAfterRemoval, Is.EqualTo(evictionCountBefore + 1), "Should have 1 eviction after removal");
            Assert.That(cache.Statistics.EntryCount, Is.EqualTo(entryCountBefore + 3), "Should have 3 entries again after final addition");
            Assert.That(cache.Statistics.EvictionCount, Is.EqualTo(evictionCountBefore + 1), "Eviction count should remain at 1");
        }

        [Test]
        public void Statistics_WhenDisabled_ReturnsNull()
        {
            // Arrange
            var cacheWithoutStats = new BlitzCacheInstance();

            try
            {
                // Act & Assert
                Assert.That(cacheWithoutStats.Statistics, Is.Null, "Statistics should be null when disabled");
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
                Assert.That(result1, Is.EqualTo("result-1"), "First call should return computed result");
                Assert.That(result2, Is.EqualTo("result-1"), "Second call should return cached result");
                Assert.That(callCount, Is.EqualTo(1), "Function should only be called once (cached on second call)");
                Assert.That(cacheWithoutStats.Statistics, Is.Null, "Statistics should remain null");
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
                Assert.That(result1, Is.EqualTo("async-result-1"), "First async call should return computed result");
                Assert.That(result2, Is.EqualTo("async-result-1"), "Second async call should return cached result");
                Assert.That(callCount, Is.EqualTo(1), "Async function should only be called once (cached on second call)");
                Assert.That(cacheWithoutStats.Statistics, Is.Null, "Statistics should remain null");
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
                Assert.That(result1, Is.EqualTo("initial-value"), "Should get initial updated value");
                Assert.That(result2, Is.EqualTo("updated-value"), "Should get updated value after second update");
                Assert.That(cacheWithoutStats.Statistics, Is.Null, "Statistics should remain null");
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
            Assert.That(topSlowest, Is.Not.Null, "TopSlowestQueries should not be null");
            Assert.That(topSlowest.Count, Is.EqualTo(3), "Should only keep the top 3 slowest queries");
            // Should be sorted descending by duration (worst case)
            var durations = topSlowest.Select(q => q.WorstCaseMs).ToList();
            for (int i = 1; i < durations.Count; i++)
                Assert.That(durations[i - 1], Is.GreaterThanOrEqualTo(durations[i]), "TopSlowestQueries should be sorted descending");
            // The slowest queries should be q4, q2, q3 (40, 30, 20)
            var keys = topSlowest.Select(q => q.CacheKey).ToList();
            Assert.That(keys, Is.EquivalentTo(new[] { "q4", "q2", "q3" }), "Top slowest queries should be correct");
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
            Assert.That(topSlowest, Is.Not.Null, "TopSlowestQueries should not be null");
            Assert.That(topSlowest.Count, Is.EqualTo(2), "Should only keep the top 2 slowest queries");
            // q1 should now be the slowest
            Assert.That(topSlowest[0].CacheKey, Is.EqualTo("q1"), "q1 should be the slowest after update");
            Assert.That(topSlowest[0].WorstCaseMs, Is.GreaterThanOrEqualTo(topSlowest[1].WorstCaseMs), "Should be sorted descending");
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
            Assert.That(topSlowest == null || !topSlowest.Any(), "TopSlowestQueries should be null or empty when disabled");
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
            Assert.That(afterAdd, Is.GreaterThan(initialBytes), "Memory should increase after adding entries");

            // Remove one key
            cache.Remove("size_key1");
            TestDelays.WaitForEvictionCallbacksSync();

            var afterRemove = cache.Statistics.ApproximateMemoryBytes;
            Assert.That(afterRemove, Is.LessThan(afterAdd), "Memory should decrease after removing an entry");
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
            Assert.That(top.Count, Is.EqualTo(2));
            Assert.That(top[0].CacheKey, Is.EqualTo("k_large"));
            Assert.That(top[1].CacheKey, Is.EqualTo("k_medium"));

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

            Assert.That(s1, Is.EqualTo("sync-value"));
            Assert.That(s2, Is.EqualTo("sync-value"));
            Assert.That(syncCalls, Is.EqualTo(1), "Sync Nuances function should execute once with auto-key");

            var statsAfterSync = cache.Statistics;
            Assert.That(statsAfterSync, Is.Not.Null);
            Assert.That(statsAfterSync!.MissCount, Is.EqualTo(1), "One miss recorded for first sync call");
            Assert.That(statsAfterSync.HitCount, Is.EqualTo(1), "One hit recorded for second sync call");
            Assert.That(statsAfterSync.EntryCount, Is.EqualTo(1), "One entry present after sync calls");

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

            Assert.That(a1, Is.EqualTo("async-value"));
            Assert.That(a2, Is.EqualTo("async-value"));
            Assert.That(asyncCalls, Is.EqualTo(1), "Async Nuances function should execute once with explicit key");

            var statsAfterAsync = cache.Statistics;
            Assert.That(statsAfterSync, Is.Not.Null);
            // Totals include previous sync operations too
            Assert.That(statsAfterAsync!.MissCount, Is.EqualTo(2), "Two misses total (sync + async first calls)");
            Assert.That(statsAfterAsync.HitCount, Is.EqualTo(2), "Two hits total (sync + async second calls)");
            Assert.That(statsAfterAsync.EntryCount, Is.EqualTo(2), "Two distinct entries (sync and async) present");

            // Verify expiration interacts with stats for auto-key + Nuances
            await TestDelays.WaitForStandardExpiration();
            var s3 = cache.BlitzGet(SyncFunc); // Re-create after expiration
            await TestDelays.WaitForEvictionCallbacks();

            var statsAfterExpire = cache.Statistics;
            Assert.That(statsAfterSync, Is.Not.Null);
            Assert.That(statsAfterExpire!.EvictionCount, Is.GreaterThanOrEqualTo(1), "Automatic expiration should increment eviction count");
            Assert.That(statsAfterExpire.MissCount, Is.EqualTo(3), "Miss count should include re-creation after expiration");
            Assert.That(statsAfterExpire.EntryCount, Is.EqualTo(2), "Entry count remains at two after re-creation");

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
            Assert.That(g1, Is.EqualTo("v-auto"));
            Assert.That(g2, Is.EqualTo("v-auto"));
            Assert.That(callsAutoNoNuSync, Is.EqualTo(1), "Auto-key sync without Nuances should execute once");

            // 2) BlitzGet explicit key (no Nuances) - async
            int callsKeyNoNuAsync = 0;
            async Task<string> KeyNoNuAsync()
            {
                callsKeyNoNuAsync++;
                return await Task.FromResult("v-async-no-nu");
            }
            var a1 = await cache.BlitzGet("k-async-no-nu", KeyNoNuAsync);
            var a2 = await cache.BlitzGet("k-async-no-nu", KeyNoNuAsync);
            Assert.That(a1, Is.EqualTo("v-async-no-nu"));
            Assert.That(a2, Is.EqualTo("v-async-no-nu"));
            Assert.That(callsKeyNoNuAsync, Is.EqualTo(1), "Explicit key async without Nuances should execute once");

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
            Assert.That(ns1, Is.EqualTo("v-sync-nu"));
            Assert.That(ns2, Is.EqualTo("v-sync-nu"));
            Assert.That(callsKeyWithNuSync, Is.EqualTo(1), "Explicit key sync with Nuances should execute once");

            // 4) BlitzUpdate sync: create and update
            cache.BlitzUpdate("u-sync", () => "u1", TestConstants.StandardTimeoutMs);
            var u1 = cache.BlitzGet("u-sync", () => "ignored");
            Assert.That(u1, Is.EqualTo("u1"), "Value should come from BlitzUpdate-created entry");

            cache.BlitzUpdate("u-sync", () => "u2", TestConstants.StandardTimeoutMs);
            var u2 = cache.BlitzGet("u-sync", () => "ignored2");
            Assert.That(u2, Is.EqualTo("u2"), "Value should reflect BlitzUpdate overwrite");

            // 5) BlitzUpdate async: create and update
            await cache.BlitzUpdate("u-async", async () => await Task.FromResult("a1"), TestConstants.StandardTimeoutMs);
            var ga1 = await cache.BlitzGet("u-async", async () => await Task.FromResult("ignored"));
            Assert.That(ga1, Is.EqualTo("a1"));

            await cache.BlitzUpdate("u-async", async () => await Task.FromResult("a2"), TestConstants.StandardTimeoutMs);
            var ga2 = await cache.BlitzGet("u-async", async () => await Task.FromResult("ignored2"));
            Assert.That(ga2, Is.EqualTo("a2"));

            // 6) Remove: ensure miss after removal
            var statsBeforeRemove = cache.Statistics!;
            long missBefore = statsBeforeRemove.MissCount;

            cache.Remove("u-sync");
            await TestDelays.WaitForEvictionCallbacks();

            var afterRemoveGet = cache.BlitzGet("u-sync", () => "recreated");
            Assert.That(afterRemoveGet, Is.EqualTo("recreated"), "Entry should be recreated after removal");

            var statsAfterRemove = cache.Statistics!;
            Assert.That(statsAfterRemove.MissCount, Is.EqualTo(missBefore + 1), "Remove should lead to a miss on next get");

            // 7) GetSemaphoreCount
            Assert.That(cache.GetSemaphoreCount(), Is.GreaterThanOrEqualTo(0), "Semaphore count should be non-negative");

            cache.Dispose();
        }

        [Test]
        public void Statistics_IsNull_Then_NotNull_After_Initialize()
        {
            using var cache = new BlitzCacheInstance();
            Assert.That(cache.Statistics, Is.Null, "Statistics should be null before InitializeStatistics for performance");
            cache.InitializeStatistics();
            Assert.That(cache.Statistics, Is.Not.Null, "Statistics should be available after InitializeStatistics");
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

            Assert.That(v1, Is.EqualTo("auto-async-no-nu"));
            Assert.That(v2, Is.EqualTo("auto-async-no-nu"));
            Assert.That(calls, Is.EqualTo(1), "Auto-key async without Nuances should execute once");

            var stats = cache.Statistics!;
            Assert.That(stats.MissCount, Is.EqualTo(1));
            Assert.That(stats.HitCount, Is.EqualTo(1));
            Assert.That(stats.EntryCount, Is.EqualTo(1));

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

            Assert.That(v1, Is.EqualTo("auto-async-with-nu"));
            Assert.That(v2, Is.EqualTo("auto-async-with-nu"));
            Assert.That(calls, Is.EqualTo(1), "Auto-key async with Nuances should execute once");

            var stats = cache.Statistics!;
            Assert.That(stats.MissCount, Is.EqualTo(1));
            Assert.That(stats.HitCount, Is.EqualTo(1));
            Assert.That(stats.EntryCount, Is.EqualTo(1));

            cache.Dispose();
        }
    }
}


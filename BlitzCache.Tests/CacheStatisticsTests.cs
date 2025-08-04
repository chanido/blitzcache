using BlitzCacheCore;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Tests for cache statistics and monitoring functionality.
    /// Validates that BlitzCache correctly tracks hits, misses, and other performance metrics.
    /// </summary>
    [TestFixture]
    public class CacheStatisticsTests
    {
        private IBlitzCache cache;

        [SetUp]
        public void Setup()
        {
            cache = TestFactory.CreateWithStatistics();
        }

        [TearDown]
        public void TearDown()
        {
            cache?.Dispose();
        }

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
            var result = cache.BlitzGet("test_key", TestFunction, TestFactory.StandardTimeoutMs);
            TestFactory.WaitForEvictionCallbacksSync();

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
            cache.BlitzGet("test_key", TestFunction, TestFactory.StandardTimeoutMs);
            TestFactory.WaitForEvictionCallbacksSync();
            
            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var entryCountBefore = cache.Statistics.EntryCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;
            
            // Act - Second call (hit)
            var result = cache.BlitzGet("test_key", TestFunction, TestFactory.StandardTimeoutMs);
            TestFactory.WaitForEvictionCallbacksSync();

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(hitCountBefore + 1, stats.HitCount, "Should have one hit");
            Assert.AreEqual(missCountBefore, stats.MissCount, "Should have one miss");
            Assert.AreEqual(TestFactory.TestHitRatio, stats.HitRatio, TestFactory.HitRatioTolerance, "Hit ratio should be 50%");
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
            cache.BlitzGet("key1", () => TestFunction("key1"), TestFactory.StandardTimeoutMs); // Miss
            cache.BlitzGet("key2", () => TestFunction("key2"), TestFactory.StandardTimeoutMs); // Miss
            cache.BlitzGet("key1", () => TestFunction("key1"), TestFactory.StandardTimeoutMs); // Hit
            cache.BlitzGet("key2", () => TestFunction("key2"), TestFactory.StandardTimeoutMs); // Hit
            cache.BlitzGet("key1", () => TestFunction("key1"), TestFactory.StandardTimeoutMs); // Hit
            TestFactory.WaitForEvictionCallbacksSync();

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
                await TestFactory.MediumDelay();
                return "async result";
            }
            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var entryCountBefore = cache.Statistics.EntryCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;

            // Act
            var result1 = await cache.BlitzGet("async_key", TestFunctionAsync, TestFactory.StandardTimeoutMs); // Miss
            await TestFactory.WaitForEvictionCallbacks();
            
            var hitCountAfterFirst = cache.Statistics.HitCount;
            var missCountAfterFirst = cache.Statistics.MissCount;
            var totalOperationsAfterFirst = cache.Statistics.TotalOperations;
            
            var result2 = await cache.BlitzGet("async_key", TestFunctionAsync, TestFactory.StandardTimeoutMs); // Hit
            await TestFactory.WaitForEvictionCallbacks();

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(hitCountAfterFirst + 1, stats.HitCount, "Should have one hit");
            Assert.AreEqual(missCountBefore + 1, stats.MissCount, "Should have one miss");
            Assert.AreEqual(TestFactory.TestHitRatio, stats.HitRatio, TestFactory.HitRatioTolerance, "Hit ratio should be 50%");
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
            cache.BlitzGet("test_key", () => "test value", TestFactory.StandardTimeoutMs);
            var evictionCountBefore = cache.Statistics.EvictionCount;
    
            // Act
            cache.Remove("test_key");
            TestFactory.WaitForEvictionCallbacksSync();

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
            TestFactory.WaitForEvictionCallbacksSync();

            // Assert
            Assert.AreEqual(evictionCountBefore, cache.Statistics.EvictionCount, "Eviction count should not change");
        }

        [Test]
        public void Statistics_BlitzUpdate_DoesNotAffectHitMissCounters()
        {
            // Arrange
            cache.BlitzGet("test_key", () => "original", TestFactory.StandardTimeoutMs);
            TestFactory.WaitForEvictionCallbacksSync();
            
            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;

            // Act
            cache.BlitzUpdate("test_key", () => "updated", TestFactory.StandardTimeoutMs);
            TestFactory.WaitForEvictionCallbacksSync();

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
            cache.BlitzGet("key1", () => "value1", TestFactory.StandardTimeoutMs); // Miss
            cache.BlitzGet("key1", () => "value1", TestFactory.StandardTimeoutMs); // Hit
            cache.Remove("key1"); // Eviction
            TestFactory.WaitForEvictionCallbacksSync();

            // Verify we have some stats
            var statsBefore = cache.Statistics;
            Assert.Greater(statsBefore.TotalOperations, 0, "Should have operations before reset");

            // Act
            cache.Statistics.Reset();
            TestFactory.WaitForEvictionCallbacksSync();

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
            var tasks = new Task[TestFactory.SmallLoopCount];
            var totalOperations = TestFactory.ConcurrentOperationsCount;
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
                        cache.BlitzGet(key, () => $"value_{threadId}_{j}", TestFactory.StandardTimeoutMs);
                    }
                });
            }

            Task.WaitAll(tasks);
            TestFactory.WaitForEvictionCallbacksSync();

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
            cache.BlitzGet("key1", () => "value1", TestFactory.StandardTimeoutMs);
            cache.BlitzGet("key2", () => "value2", TestFactory.StandardTimeoutMs);
            TestFactory.WaitForEvictionCallbacksSync();

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
            cache.BlitzGet("expiring_key", () => "value1", TestFactory.StandardExpirationMs);
            await TestFactory.WaitForEvictionCallbacks();

            // Wait for automatic expiration
            await TestFactory.WaitForStandardExpiration();

            // Try to access the expired entry (this should trigger cleanup and create new entry)
            var result = cache.BlitzGet("expiring_key", () => "new_value", TestFactory.StandardTimeoutMs);
            await TestFactory.WaitForEvictionCallbacks();

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
            cache.BlitzGet("auto_expire", () => "value1", TestFactory.ShortExpirationMs);
            cache.BlitzGet("manual_remove", () => "value2", TestFactory.StandardTimeoutMs);
            cache.BlitzGet("keep_alive", () => "value3", TestFactory.StandardTimeoutMs);
            await TestFactory.WaitForEvictionCallbacks();

            // Manual removal
            cache.Remove("manual_remove");
            await TestFactory.WaitForEvictionCallbacks();

            // Wait for automatic expiration
            await TestFactory.WaitForShortExpirationShort();

            // Access expired key to trigger callback
            cache.BlitzGet("auto_expire", () => "new_value", TestFactory.StandardTimeoutMs);
            await TestFactory.WaitForEvictionCallbacks();

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
            cache.BlitzGet("key1", () => "value1", TestFactory.StandardTimeoutMs);
            cache.BlitzGet("key2", () => "value2", TestFactory.StandardTimeoutMs);
            cache.BlitzGet("key3", () => "value3", TestFactory.StandardTimeoutMs);
            TestFactory.WaitForEvictionCallbacksSync();

            var entryCountAfterAdding = cache.Statistics.EntryCount;

            // Remove one
            cache.Remove("key2");
            TestFactory.WaitForEvictionCallbacksSync();

            var entryCountAfterRemoval = cache.Statistics.EntryCount;
            var evictionCountAfterRemoval = cache.Statistics.EvictionCount;

            // Add another
            cache.BlitzGet("key4", () => "value4", TestFactory.StandardTimeoutMs);
            TestFactory.WaitForEvictionCallbacksSync();

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
            var cacheWithoutStats = new BlitzCache(enableStatistics: false);

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
            var cacheWithoutStats = new BlitzCache();
            var callCount = 0;
            string TestFunction()
            {
                callCount++;
                return $"result-{callCount}";
            }

            try
            {
                // Act
                var result1 = cacheWithoutStats.BlitzGet("test-key", TestFunction, TestFactory.StandardTimeoutMs);
                var result2 = cacheWithoutStats.BlitzGet("test-key", TestFunction, TestFactory.StandardTimeoutMs);

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
            var cacheWithoutStats = new BlitzCache();
            var callCount = 0;
            async Task<string> TestFunction()
            {
                callCount++;
                await TestFactory.MediumDelay();
                return $"async-result-{callCount}";
            }

            try
            {
                // Act
                var result1 = await cacheWithoutStats.BlitzGet("async-test-key", TestFunction, TestFactory.StandardTimeoutMs);
                var result2 = await cacheWithoutStats.BlitzGet("async-test-key", TestFunction, TestFactory.StandardTimeoutMs);

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
            var cacheWithoutStats = new BlitzCache();

            try
            {
                // Act
                cacheWithoutStats.BlitzUpdate("update-key", () => "initial-value", TestFactory.StandardTimeoutMs);
                var result1 = cacheWithoutStats.BlitzGet("update-key", () => "fallback-value", TestFactory.StandardTimeoutMs);
                
                cacheWithoutStats.BlitzUpdate("update-key", () => "updated-value", TestFactory.StandardTimeoutMs);
                var result2 = cacheWithoutStats.BlitzGet("update-key", () => "fallback-value", TestFactory.StandardTimeoutMs);

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
    }
}


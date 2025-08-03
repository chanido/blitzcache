using BlitzCacheCore;
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
            cache = new BlitzCache(useGlobalCache: false);
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
            Assert.AreEqual(0, stats.CurrentEntryCount, "Initial entry count should be zero");
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

            // Act
            var result = cache.BlitzGet("test_key", TestFunction, 30000);

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(0, stats.HitCount, "Should have no hits");
            Assert.AreEqual(1, stats.MissCount, "Should have one miss");
            Assert.AreEqual(0.0, stats.HitRatio, 0.001, "Hit ratio should be zero with only misses");
            Assert.AreEqual(1, stats.CurrentEntryCount, "Should have one cached entry");
            Assert.AreEqual(1, stats.TotalOperations, "Should have one total operation");
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
            cache.BlitzGet("test_key", TestFunction, 30000);
            
            // Act - Second call (hit)
            var result = cache.BlitzGet("test_key", TestFunction, 30000);

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(1, stats.HitCount, "Should have one hit");
            Assert.AreEqual(1, stats.MissCount, "Should have one miss");
            Assert.AreEqual(0.5, stats.HitRatio, 0.001, "Hit ratio should be 50%");
            Assert.AreEqual(1, stats.CurrentEntryCount, "Should still have one cached entry");
            Assert.AreEqual(2, stats.TotalOperations, "Should have two total operations");
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

            // Act - Create multiple cache entries and hits
            cache.BlitzGet("key1", () => TestFunction("key1"), 30000); // Miss
            cache.BlitzGet("key2", () => TestFunction("key2"), 30000); // Miss
            cache.BlitzGet("key1", () => TestFunction("key1"), 30000); // Hit
            cache.BlitzGet("key2", () => TestFunction("key2"), 30000); // Hit
            cache.BlitzGet("key1", () => TestFunction("key1"), 30000); // Hit

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(3, stats.HitCount, "Should have 3 hits");
            Assert.AreEqual(2, stats.MissCount, "Should have 2 misses");
            Assert.AreEqual(0.6, stats.HitRatio, 0.001, "Hit ratio should be 60% (3/5)");
            Assert.AreEqual(2, stats.CurrentEntryCount, "Should have 2 cached entries");
            Assert.AreEqual(5, stats.TotalOperations, "Should have 5 total operations");
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
                await Task.Delay(10);
                return "async result";
            }

            // Act
            var result1 = await cache.BlitzGet("async_key", TestFunctionAsync, 30000); // Miss
            var result2 = await cache.BlitzGet("async_key", TestFunctionAsync, 30000); // Hit

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(1, stats.HitCount, "Should have one hit");
            Assert.AreEqual(1, stats.MissCount, "Should have one miss");
            Assert.AreEqual(0.5, stats.HitRatio, 0.001, "Hit ratio should be 50%");
            Assert.AreEqual(1, stats.CurrentEntryCount, "Should have one cached entry");
            Assert.AreEqual(2, stats.TotalOperations, "Should have two total operations");
            Assert.AreEqual(1, callCount, "Function should only be called once");
            Assert.AreEqual("async result", result1);
            Assert.AreEqual("async result", result2);
        }

        [Test]
        public void Statistics_RemoveOperation_IncrementsEvictionCount()
        {
            // Arrange
            cache.BlitzGet("test_key", () => "test value", 30000);
            var statsBefore = cache.Statistics;
            var evictionCountBefore = statsBefore.EvictionCount;

            // Act
            cache.Remove("test_key");

            // Assert
            var statsAfter = cache.Statistics;
            Assert.AreEqual(evictionCountBefore + 1, statsAfter.EvictionCount, "Eviction count should increase by 1");
            Assert.AreEqual(0, statsAfter.CurrentEntryCount, "Entry count should be zero after removal");
        }

        [Test]
        public void Statistics_RemoveNonExistentKey_DoesNotIncrementEvictionCount()
        {
            // Arrange
            var statsBefore = cache.Statistics;

            // Act
            cache.Remove("non_existent_key");

            // Assert
            var statsAfter = cache.Statistics;
            Assert.AreEqual(statsBefore.EvictionCount, statsAfter.EvictionCount, "Eviction count should not change");
        }

        [Test]
        public void Statistics_BlitzUpdate_DoesNotAffectHitMissCounters()
        {
            // Arrange
            cache.BlitzGet("test_key", () => "original", 30000);
            var statsBefore = cache.Statistics;

            // Act
            cache.BlitzUpdate("test_key", () => "updated", 30000);

            // Assert
            var statsAfter = cache.Statistics;
            Assert.AreEqual(statsBefore.HitCount, statsAfter.HitCount, "Hit count should not change");
            Assert.AreEqual(statsBefore.MissCount, statsAfter.MissCount, "Miss count should not change");
            Assert.AreEqual(statsBefore.TotalOperations, statsAfter.TotalOperations, "Total operations should not change");
        }

        [Test]
        public void Statistics_Reset_ClearsAllCounters()
        {
            // Arrange - Generate some statistics
            cache.BlitzGet("key1", () => "value1", 30000); // Miss
            cache.BlitzGet("key1", () => "value1", 30000); // Hit
            cache.Remove("key1"); // Eviction

            // Verify we have some stats
            var statsBefore = cache.Statistics;
            Assert.Greater(statsBefore.TotalOperations, 0, "Should have operations before reset");

            // Act
            cache.Statistics.Reset();

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
            var tasks = new Task[10];
            var totalOperations = 100;

            // Act - Multiple threads performing cache operations
            for (int i = 0; i < tasks.Length; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < totalOperations / tasks.Length; j++)
                    {
                        var key = $"thread_{threadId}_key_{j % 5}"; // Some keys will repeat (hits)
                        cache.BlitzGet(key, () => $"value_{threadId}_{j}", 30000);
                    }
                });
            }

            Task.WaitAll(tasks);

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(totalOperations, stats.TotalOperations, "Should track all operations");
            Assert.Greater(stats.HitCount, 0, "Should have some hits due to key repetition");
            Assert.Greater(stats.MissCount, 0, "Should have some misses");
            Assert.AreEqual(stats.HitCount + stats.MissCount, stats.TotalOperations, "Hits + misses should equal total operations");
        }

        [Test]
        public void Statistics_ActiveSemaphoreCount_ReflectsCurrentState()
        {
            // Arrange
            var initialCount = cache.Statistics.ActiveSemaphoreCount;

            // Act - Create some cache entries
            cache.BlitzGet("key1", () => "value1", 30000);
            cache.BlitzGet("key2", () => "value2", 30000);

            // Assert
            var statsAfter = cache.Statistics;
            Assert.GreaterOrEqual(statsAfter.ActiveSemaphoreCount, initialCount, "Semaphore count should reflect active semaphores");
        }
    }
}

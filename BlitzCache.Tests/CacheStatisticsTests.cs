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
            var result = cache.BlitzGet("test_key", TestFunction, 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
            cache.BlitzGet("test_key", TestFunction, 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute
            
            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var entryCountBefore = cache.Statistics.EntryCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;
            
            // Act - Second call (hit)
            var result = cache.BlitzGet("test_key", TestFunction, 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
            cache.BlitzGet("key1", () => TestFunction("key1"), 30000); // Miss
            cache.BlitzGet("key2", () => TestFunction("key2"), 30000); // Miss
            cache.BlitzGet("key1", () => TestFunction("key1"), 30000); // Hit
            cache.BlitzGet("key2", () => TestFunction("key2"), 30000); // Hit
            cache.BlitzGet("key1", () => TestFunction("key1"), 30000); // Hit
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
                await Task.Delay(10);
                return "async result";
            }
            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var entryCountBefore = cache.Statistics.EntryCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;

            // Act
            var result1 = await cache.BlitzGet("async_key", TestFunctionAsync, 30000); // Miss
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute
            
            var hitCountAfterFirst = cache.Statistics.HitCount;
            var missCountAfterFirst = cache.Statistics.MissCount;
            var totalOperationsAfterFirst = cache.Statistics.TotalOperations;
            
            var result2 = await cache.BlitzGet("async_key", TestFunctionAsync, 30000); // Hit
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
            cache.BlitzGet("test_key", () => "test value", 30000);
            var evictionCountBefore = cache.Statistics.EvictionCount;
    
            // Act
            cache.Remove("test_key");
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

            // Assert
            Assert.AreEqual(evictionCountBefore, cache.Statistics.EvictionCount, "Eviction count should not change");
        }

        [Test]
        public void Statistics_BlitzUpdate_DoesNotAffectHitMissCounters()
        {
            // Arrange
            cache.BlitzGet("test_key", () => "original", 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute
            
            var hitCountBefore = cache.Statistics.HitCount;
            var missCountBefore = cache.Statistics.MissCount;
            var totalOperationsBefore = cache.Statistics.TotalOperations;

            // Act
            cache.BlitzUpdate("test_key", () => "updated", 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
            cache.BlitzGet("key1", () => "value1", 30000); // Miss
            cache.BlitzGet("key1", () => "value1", 30000); // Hit
            cache.Remove("key1"); // Eviction
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

            // Verify we have some stats
            var statsBefore = cache.Statistics;
            Assert.Greater(statsBefore.TotalOperations, 0, "Should have operations before reset");

            // Act
            cache.Statistics.Reset();
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
                        cache.BlitzGet(key, () => $"value_{threadId}_{j}", 30000);
                    }
                });
            }

            Task.WaitAll(tasks);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
            cache.BlitzGet("key1", () => "value1", 30000);
            cache.BlitzGet("key2", () => "value2", 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
            var shortExpirationMs = 200;

            // Act - Add cache entry with short expiration
            cache.BlitzGet("expiring_key", () => "value1", shortExpirationMs);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

            // Wait for automatic expiration
            await Task.Delay(shortExpirationMs + 100);

            // Try to access the expired entry (this should trigger cleanup and create new entry)
            var result = cache.BlitzGet("expiring_key", () => "new_value", 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
            var shortExpirationMs = 100;

            // Act - Mix of automatic and manual evictions
            cache.BlitzGet("auto_expire", () => "value1", shortExpirationMs);
            cache.BlitzGet("manual_remove", () => "value2", 30000);
            cache.BlitzGet("keep_alive", () => "value3", 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

            // Manual removal
            cache.Remove("manual_remove");
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

            // Wait for automatic expiration
            await Task.Delay(shortExpirationMs + 50);

            // Access expired key to trigger callback
            cache.BlitzGet("auto_expire", () => "new_value", 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

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
            cache.BlitzGet("key1", () => "value1", 30000);
            cache.BlitzGet("key2", () => "value2", 30000);
            cache.BlitzGet("key3", () => "value3", 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

            var entryCountAfterAdding = cache.Statistics.EntryCount;

            // Remove one
            cache.Remove("key2");
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

            var entryCountAfterRemoval = cache.Statistics.EntryCount;
            var evictionCountAfterRemoval = cache.Statistics.EvictionCount;

            // Add another
            cache.BlitzGet("key4", () => "value4", 30000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute

            // Assert
            Assert.AreEqual(entryCountBefore + 3, entryCountAfterAdding, "Should have 3 entries after adding");
            Assert.AreEqual(entryCountBefore + 2, entryCountAfterRemoval, "Should have 2 entries after removal");
            Assert.AreEqual(evictionCountBefore + 1, evictionCountAfterRemoval, "Should have 1 eviction after removal");
            Assert.AreEqual(entryCountBefore + 3, cache.Statistics.EntryCount, "Should have 3 entries again after final addition");
            Assert.AreEqual(evictionCountBefore + 1, cache.Statistics.EvictionCount, "Eviction count should remain at 1");
        }
    }
}

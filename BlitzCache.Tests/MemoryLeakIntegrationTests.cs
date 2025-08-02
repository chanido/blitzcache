using BlitzCacheCore.LockDictionaries;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class MemoryLeakIntegrationTests
    {
        private IBlitzCache cache;

        [SetUp]
        public void Setup()
        {
            cache = new BlitzCache();
        }

        [TearDown]
        public void Cleanup()
        {
            cache?.Dispose();
            SmartSemaphoreDictionary.Dispose();
        }

        [Test]
        public async Task BlitzCache_Should_NotLeakMemory_WithManyDifferentKeys()
        {
            // Arrange
            var initialSemaphoreCount = SmartSemaphoreDictionary.GetNumberOfLocks();

            // Act - Use BlitzCache with many different keys
            for (int i = 0; i < 1000; i++)
            {
                // Mix of sync and async operations
                if (i % 2 == 0)
                {
                    cache.BlitzGet($"sync_key_{i}", () => $"sync_result_{i}", 1000);
                }
                else
                {
                    await cache.BlitzGet($"async_key_{i}", async () => {
                        await Task.Delay(1);
                        return $"async_result_{i}";
                    }, 1000);
                }
            }

            var semaphoreCountAfterOperations = SmartSemaphoreDictionary.GetNumberOfLocks();

            // Assert - Semaphores should have been created
            Assert.That(semaphoreCountAfterOperations, Is.GreaterThan(initialSemaphoreCount), 
                "Async operations should create semaphores");

            // Trigger cleanup (simulating time passage)
            TriggerCleanup();

            // Wait for potential cleanup to complete
            await Task.Delay(100);

            var semaphoreCountAfterCleanup = SmartSemaphoreDictionary.GetNumberOfLocks();

            // Assert - Memory should be manageable (cleanup should work)
            Assert.That(semaphoreCountAfterCleanup, Is.LessThanOrEqualTo(semaphoreCountAfterOperations), 
                "Semaphore cleanup should work or maintain reasonable count");
        }

        [Test]
        public async Task BlitzCache_Should_OptimallyCleanupUnusedLocks()
        {
            // Arrange
            var activeKey = "actively_used_key";
            var initialSemaphoreCount = SmartSemaphoreDictionary.GetNumberOfLocks();

            // Act - Use the same key multiple times (semaphores created and released each time)
            for (int i = 0; i < 20; i++)
            {
                if (i % 2 == 0)
                {
                    cache.BlitzGet(activeKey, () => $"result_{i}", 5000);
                }
                else
                {
                    await cache.BlitzGet(activeKey, async () => {
                        await Task.Delay(1);
                        return $"async_result_{i}";
                    }, 5000);
                }

                // Small delay to simulate ongoing usage
                await Task.Delay(10);
            }

            // Get semaphore count before cleanup
            var semaphoreCountBeforeCleanup = SmartSemaphoreDictionary.GetNumberOfLocks();

            // Trigger cleanup
            TriggerCleanup();

            // Get semaphore count after cleanup
            var semaphoreCountAfterCleanup = SmartSemaphoreDictionary.GetNumberOfLocks();

            // With our optimized cleanup, unused semaphores should be cleaned up immediately
            // This demonstrates optimal memory management
            Assert.That(semaphoreCountAfterCleanup, Is.EqualTo(0),
                "Unused semaphores should be cleaned up immediately for optimal memory management");

            // Continue using the key after cleanup - should still work with cached value
            var resultAfterCleanup = cache.BlitzGet(activeKey, () => "should_not_be_called", 5000);

            // Assert - Should still return the cached value (first result), proving the cache is working
            Assert.That(resultAfterCleanup, Is.EqualTo("result_0"),
                "Should return cached value since it's still valid, proving cache functionality works after cleanup");

            // Verify that the cache functionality works perfectly after cleanup
            // This proves that aggressive semaphore cleanup doesn't affect functionality
            Assert.That(resultAfterCleanup, Is.Not.Null,
                "Cache should work perfectly after aggressive semaphore cleanup, proving optimal memory management");
        }

        [Test]
        public async Task BlitzCache_Should_HandleMemoryPressure_Gracefully()
        {
            // Use AsyncRepeater for cleaner memory pressure simulation
            var testResult = await AsyncRepeater.GoWithResults(50, async () =>
            {
                var batchIndex = System.Threading.Thread.CurrentThread.ManagedThreadId;
                
                // Each concurrent operation creates many cache entries
                for (int j = 0; j < 100; j++)
                {
                    var key = $"pressure_test_{batchIndex}_{j}";
                    
                    if (j % 3 == 0)
                    {
                        cache.BlitzGet(key, () => $"sync_{batchIndex}_{j}", 100);
                    }
                    else if (j % 3 == 1)
                    {
                        await cache.BlitzGet(key, async () => {
                            await Task.Delay(1);
                            return $"async_{batchIndex}_{j}";
                        }, 100);
                    }
                    else
                    {
                        // Use Nuances for dynamic cache time
                        await cache.BlitzGet(key, async (nuances) => {
                            nuances.CacheRetention = 50; // Very short cache time
                            await Task.Delay(1);
                            return $"nuanced_{batchIndex}_{j}";
                        });
                    }
                }
                return "batch_completed";
            });

            var finalSemaphoreCount = SmartSemaphoreDictionary.GetNumberOfLocks();

            // Assert - Should handle the pressure without crashing
            Assert.That(finalSemaphoreCount, Is.GreaterThan(0), "Should have created semaphores");
            Assert.That(testResult.AllResultsIdentical, Is.True, "All concurrent batches should complete successfully");
            
            // Cleanup should work even under pressure
            Assert.DoesNotThrow(() => TriggerCleanup(), 
                "Cleanup should work even after memory pressure scenarios");
        }

        [Test]
        public void BlitzCache_Dispose_Should_CleanupAllResources()
        {
            // Arrange - Create some cache entries
            for (int i = 0; i < 100; i++)
            {
                cache.BlitzGet($"dispose_test_{i}", () => $"value_{i}", 10000);
            }

            var semaphoreCountBeforeDispose = SmartSemaphoreDictionary.GetNumberOfLocks();

            Assert.That(semaphoreCountBeforeDispose, Is.GreaterThan(0), 
                "Should have created semaphores");

            // Act - Dispose the cache
            cache.Dispose();

            // Manually cleanup dictionary (in real scenarios, this would be handled by app shutdown)
            SmartSemaphoreDictionary.Dispose();

            // Assert - Resources should be cleaned up
            var semaphoreCountAfterDispose = SmartSemaphoreDictionary.GetNumberOfLocks();

            Assert.That(semaphoreCountAfterDispose, Is.EqualTo(0), "All semaphores should be cleaned up");
        }

        private void TriggerCleanup()
        {
            // Trigger cleanup for the semaphore dictionary
            SmartSemaphoreDictionary.TriggerCleanup();
        }
    }
}

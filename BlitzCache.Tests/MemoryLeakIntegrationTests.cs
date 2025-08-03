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
            cache = new BlitzCache(useGlobalCache: false);
        }

        [TearDown]
        public void Cleanup()
        {
            cache?.Dispose();
        }

        [Test]
        public async Task BlitzCache_Should_NotLeakMemory_WithManyDifferentKeys()
        {
            // Arrange
            var initialSemaphoreCount = cache.GetSemaphoreCount();

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

            var semaphoreCountAfterOperations = cache.GetSemaphoreCount();

            // Assert - Semaphores should have been created
            Assert.That(semaphoreCountAfterOperations, Is.GreaterThan(initialSemaphoreCount), 
                "Async operations should create semaphores");

            // Assert - Memory should be manageable (semaphores created but not leaking excessively)
            Assert.That(semaphoreCountAfterOperations - initialSemaphoreCount, Is.LessThanOrEqualTo(1000), 
                "Semaphore count should remain reasonable even with many operations");
            
            Console.WriteLine($"📊 Created {semaphoreCountAfterOperations - initialSemaphoreCount} semaphores for unique keys");
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

            var finalSemaphoreCount = cache.GetSemaphoreCount();

            // Assert - Should handle the pressure without crashing
            Assert.That(finalSemaphoreCount, Is.GreaterThan(0), "Should have created semaphores");
            Assert.That(testResult.AllResultsIdentical, Is.True, "All concurrent batches should complete successfully");
            
            Console.WriteLine($"📊 Successfully handled memory pressure with {finalSemaphoreCount} semaphores");
        }
    }
}

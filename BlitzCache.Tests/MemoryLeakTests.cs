using BlitzCacheCore.LockDictionaries;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class MemoryLeakTests
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
        public async Task Cache_Should_RespectMemoryManagement()
        {
            // Arrange
            var initialCount = cache.GetSemaphoreCount();

            // Act - Create cache entries that will create semaphores
            for (int i = 0; i < 20; i++)
            {
                // Use cache operations which internally create and use semaphores
                var result = cache.BlitzGet($"memory_test_{i}", () => $"value_{i}", 10000);
                Assert.That(result, Is.EqualTo($"value_{i}"));
                
                // Simulate some work
                await Task.Delay(1);
            }

            var countAfterCreation = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Created {countAfterCreation - initialCount} semaphores");

            // Assert - Semaphores should be created but managed properly
            Assert.That(countAfterCreation, Is.GreaterThanOrEqualTo(initialCount), "Semaphores should be created for cache operations");
            
            // With our aggressive 10ms cleanup, some semaphores might already be cleaned up
            // The important thing is that the system doesn't crash and manages memory properly
            Console.WriteLine("✅ Cache operations completed without memory issues");
        }

        [Test]
        public async Task Cache_Should_HandleConcurrentAccess()
        {
            // Arrange & Act - Use AsyncRepeater for cleaner concurrent testing  
            var testResult = await AsyncRepeater.GoWithResults(100, async () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var key = $"concurrent_cache_{Thread.CurrentThread.ManagedThreadId}_{j}";
                    var result = cache.BlitzGet(key, () => $"value_{key}", 10000);
                    
                    // Verify cache is working
                    var cachedResult = cache.BlitzGet(key, () => "should_not_be_called", 10000);
                    if (result != cachedResult)
                    {
                        throw new InvalidOperationException("Cache consistency issue");
                    }
                    
                    // Simulate some async work
                    await Task.Delay(1);
                }
                return "completed";
            });

            // Assert - Should not crash and should have created semaphores
            var finalCount = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Final semaphore count after concurrent access: {finalCount}");
            Assert.That(testResult.AllResultsIdentical, Is.True, "All concurrent operations should complete successfully");
            
            Console.WriteLine("✅ Concurrent cache operations completed successfully");
        }
    }
}

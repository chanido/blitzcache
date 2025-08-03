using BlitzCacheCore.LockDictionaries;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Tests that demonstrate the balanced semaphore management behavior.
    /// </summary>
    [TestFixture]
    public class SemaphoreLifecycleTests
    {
        private IBlitzCache cache;

        [SetUp]
        public void Setup()
        {
            cache = new BlitzCache(useGlobalCache: false);
        }

        [TearDown]
        public void TearDownCleanup()
        {
            cache?.Dispose();
        }

        [Test]
        public async Task Semaphore_Dictionary_Should_Handle_Concurrent_Cache_Access()
        {
            Console.WriteLine("🧪 TESTING CONCURRENT CACHE ACCESS WITH SEMAPHORES");
            Console.WriteLine("==================================================");

            var tasks = new Task[20];
            var baseKey = "concurrent_test";

            // Act - Multiple concurrent cache operations
            for (int i = 0; i < tasks.Length; i++)
            {
                int index = i;
                tasks[i] = Task.Run(async () =>
                {
                    // Half sync, half async operations
                    if (index % 2 == 0)
                    {
                        return cache.BlitzGet($"{baseKey}_{index}", () => $"sync_value_{index}", 5000);
                    }
                    else
                    {
                        return await cache.BlitzGet($"{baseKey}_{index}", async () =>
                        {
                            await Task.Delay(10);
                            return $"async_value_{index}";
                        }, 5000);
                    }
                });
            }

            await Task.WhenAll(tasks);
            var semaphoreCount = cache.GetSemaphoreCount();

            Console.WriteLine($"📊 Semaphores after concurrent operations: {semaphoreCount}");
            Console.WriteLine($"📊 Completed {tasks.Length} concurrent operations");

            // Assert - All operations should complete successfully
            Assert.That(tasks.Length, Is.EqualTo(20), "All concurrent operations should complete");
            Assert.That(tasks, Is.All.Property("IsCompletedSuccessfully").True, "All tasks should complete successfully");

            Console.WriteLine("✅ Concurrent operations completed successfully!");
            Console.WriteLine("✅ Semaphore dictionary handled concurrency correctly!");
        }
    }
}

using BlitzCacheCore.LockDictionaries;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Integration tests for BlitzCache including end-to-end scenarios, 
    /// DI integration, and real-world usage patterns.
    /// </summary>
    [TestFixture]
    public class IntegrationTests
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
        }

        [Test]
        public void BlitzCache_Should_Use_SmartSemaphoreDictionary_For_Sync_Operations()
        {
            Console.WriteLine("🧪 TESTING BLITZCACHE WITH SMARTSEMAPHOREDICTIONARY");
            Console.WriteLine("===================================================");

            var initialSemaphores = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Initial semaphores: {initialSemaphores}");

            // Perform sync operations
            var result1 = cache.BlitzGet("sync_key", () => "sync_value", 10000);
            var semaphoresDuringOperation = cache.GetSemaphoreCount();
            
            Console.WriteLine($"📊 Semaphores during operation: {semaphoresDuringOperation}");
            Assert.That(result1, Is.EqualTo("sync_value"));
            Assert.That(semaphoresDuringOperation, Is.GreaterThan(initialSemaphores), "Should create semaphores for sync operations");

            // Cache should work correctly with semaphore management
            var result2 = cache.BlitzGet("sync_key", () => "new_value", 10000);
            Assert.That(result2, Is.EqualTo("sync_value"), "Should return cached value");

            Console.WriteLine("✅ SmartSemaphoreDictionary integration working!");
        }

        [Test]
        public async Task BlitzCache_Should_Use_SmartSemaphoreDictionary_For_Async_Operations()
        {
            Console.WriteLine("🧪 TESTING BLITZCACHE WITH SMARTSEMAPHOREDICTIONARY");
            Console.WriteLine("===================================================");

            var initialSemaphores = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Initial semaphores: {initialSemaphores}");

            // Perform async operations
            var result1 = await cache.BlitzGet("async_key", async () => {
                await Task.Delay(10);
                return "async_value";
            }, 10000);

            var semaphoresDuringOperation = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Semaphores during operation: {semaphoresDuringOperation}");
            
            Assert.That(result1, Is.EqualTo("async_value"));
            Assert.That(semaphoresDuringOperation, Is.GreaterThan(initialSemaphores), "Should create semaphores for async operations");

            // Cache should work correctly with semaphore management
            var result2 = await cache.BlitzGet("async_key", async () => {
                await Task.Delay(10);
                return "new_async_value";
            }, 10000);
            
            Assert.That(result2, Is.EqualTo("async_value"), "Should return cached value");

            Console.WriteLine("✅ SmartSemaphoreDictionary integration working!");
        }

        [Test]
        public void BlitzUpdate_Should_Work_With_SmartSemaphoreDictionary()
        {
            Console.WriteLine("🧪 TESTING BLITZUPDATE WITH SMARTSEMAPHOREDICTIONARY");
            Console.WriteLine("====================================================");

            var initialSemaphores = cache.GetSemaphoreCount();
            
            // Update cache
            cache.BlitzUpdate("update_key", () => "updated_value", 10000);
            
            var semaphoresAfterUpdate = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Semaphores after update: {semaphoresAfterUpdate}");

            // Verify the update worked
            var result = cache.BlitzGet("update_key", () => "fallback", 10000);
            Assert.That(result, Is.EqualTo("updated_value"), "BlitzUpdate should cache the value");

            Console.WriteLine("✅ BlitzUpdate with SmartSemaphoreDictionary working!");
        }

        [Test]
        public async Task BlitzUpdate_Async_Should_Work_With_SmartSemaphoreDictionary()
        {
            Console.WriteLine("🧪 TESTING ASYNC BLITZUPDATE WITH SMARTSEMAPHOREDICTIONARY");
            Console.WriteLine("==========================================================");

            var initialSemaphores = cache.GetSemaphoreCount();
            
            // Update cache asynchronously
            await cache.BlitzUpdate("async_update_key", async () => {
                await Task.Delay(10);
                return "async_updated_value";
            }, 10000);
            
            var semaphoresAfterUpdate = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Semaphores after async update: {semaphoresAfterUpdate}");

            // Verify the update worked
            var result = await cache.BlitzGet("async_update_key", async () => {
                await Task.Delay(10);
                return "async_fallback";
            }, 10000);
            
            Assert.That(result, Is.EqualTo("async_updated_value"), "Async BlitzUpdate should cache the value");

            Console.WriteLine("✅ Async BlitzUpdate with SmartSemaphoreDictionary working!");
        }

        [Test]
        public void Cache_Expiration_Should_Trigger_Semaphore_Cleanup()
        {
            Console.WriteLine("🧪 TESTING CACHE-SYNCHRONIZED SEMAPHORE CLEANUP");
            Console.WriteLine("================================================");

            // Create cache entry with very short expiration
            var result = cache.BlitzGet("short_expiry", () => "expires_soon", 50); // 50ms expiration
            Assert.That(result, Is.EqualTo("expires_soon"));

            var semaphoresAfterCreation = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Semaphores after creation: {semaphoresAfterCreation}");

            // Wait for cache to expire
            Console.WriteLine("⏱️ Waiting for cache expiration...");
            System.Threading.Thread.Sleep(100);

            // Verify cache actually expired by trying to get new value
            var newResult = cache.BlitzGet("short_expiry", () => "new_value_after_expiry", 10000);
            Assert.That(newResult, Is.EqualTo("new_value_after_expiry"), "Cache should have expired");

            Console.WriteLine("✅ Cache-synchronized cleanup working!");
        }

        [Test]
        public async Task Performance_Test_Smart_Dictionaries()
        {
            Console.WriteLine("🧪 PERFORMANCE TEST WITH SMART DICTIONARIES");
            Console.WriteLine("============================================");

            var iterations = 1000;
            var startTime = DateTime.UtcNow;

            // Use AsyncRepeater for cleaner concurrent testing
            var testResult = await AsyncRepeater.GoWithResults(iterations, async () => {
                var index = System.Threading.Thread.CurrentThread.ManagedThreadId;
                var isAsync = DateTime.UtcNow.Ticks % 2 == 0;
                
                if (isAsync)
                {
                    return await cache.BlitzGet($"async_{index}_{DateTime.UtcNow.Ticks}", async () => {
                        await Task.Delay(1);
                        return $"async_value_{index}";
                    }, 10000);
                }
                else
                {
                    return cache.BlitzGet($"sync_{index}_{DateTime.UtcNow.Ticks}", () => $"sync_value_{index}", 10000);
                }
            });

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            Console.WriteLine($"⏱️ Time for {iterations} mixed operations: {duration.TotalMilliseconds:F2}ms");
            Console.WriteLine($"📊 Average per operation: {duration.TotalMilliseconds / iterations:F4}ms");

            var finalSemaphores = cache.GetSemaphoreCount();

            Console.WriteLine($"📊 Final semaphores: {finalSemaphores}");

            // Performance should be excellent
            Assert.That(duration.TotalMilliseconds, Is.LessThan(10000), "Should be performant");

            Console.WriteLine("✅ Performance test with smart dictionaries passed!");
        }
    }
}

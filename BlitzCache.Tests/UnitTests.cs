using BlitzCacheCore.Extensions;
using BlitzCacheCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    public class UnitTests
    {
        private const int numberOfTests = 5000;
        private IBlitzCache cache;
        private ServiceProvider serviceProvider;

        [OneTimeSetUp]
        public void BeforeAll()
        {
            serviceProvider = new ServiceCollection()
                .AddBlitzCache()
                //.AddBlitzCache(30000) you can also specify the default timespan of the cache in milliseconds
                .BuildServiceProvider();

            cache = serviceProvider.GetService<IBlitzCache>();

            //Alternatively you can create a new instance of the BlitzCache directly without dependency injection
            //cache = new BlitzCache();
            //cache = new BlitzCache(30000);
        }

        [Test]
        public async Task ParallelAccessToAsyncMethod()
        {
            var slowClass = new SlowClassAsync();

            await AsyncRepeater.Go(numberOfTests, () => cache.BlitzGet(slowClass.ProcessQuickly));

            Assert.AreEqual(1, slowClass.Counter);
        }

        [Test]
        public async Task DifferentKeysWillCallTheAsyncMethodAgain()
        {
            var slowClass = new SlowClassAsync();

            var key1 = Guid.NewGuid().ToString();
            await AsyncRepeater.Go(numberOfTests, () => cache.BlitzGet(key1, slowClass.ProcessQuickly));

            var key2 = Guid.NewGuid().ToString();
            await AsyncRepeater.Go(numberOfTests, () => cache.BlitzGet(key2, slowClass.ProcessQuickly));

            Assert.AreEqual(2, slowClass.Counter);
        }

        [Test]
        public void ParallelAccessToSyncMethod()
        {
            var slowClass = new SlowClass();

            Parallel.For(0, numberOfTests, (i) =>
            {
                cache.BlitzGet(slowClass.ProcessQuickly);
            });


            Assert.AreEqual(1, slowClass.Counter);
        }

        [Test]
        public void DifferentKeysWillCallTheSyncMethodAgain()
        {
            var slowClass = new SlowClass();

            var key1 = Guid.NewGuid().ToString();
            Parallel.For(0, numberOfTests, (i) => { cache.BlitzGet(key1, slowClass.ProcessQuickly); });
            var key2 = Guid.NewGuid().ToString();
            Parallel.For(0, numberOfTests, (i) => { cache.BlitzGet(key2, slowClass.ProcessQuickly); });

            Assert.AreEqual(2, slowClass.Counter);
        }

        [Test]
        public void VariableTimespan()
        {
            var slowClass = new SlowClass();

            static string GetKey(int i) => i == 0 ? "Zero" : i % 2 == 0 ? "Even" : "Odd";

            bool? GetValueWithDifferentCacheRetention(Nuances n, int i)
            {
                bool? result = null;
                try { result = slowClass.FailIfZeroTrueIfEven(i); }
                catch { }

                switch (result)
                {
                    case null: n.CacheRetention = 1000; break; //Zero
                    case true: n.CacheRetention = 2000; break; //Even
                    case false: n.CacheRetention = 3000; break;//Odd
                }

                return result;
            }

            void WaitAndCheck(int milliseconds, int calls)
            {
                slowClass.ResetCounter();
                Thread.Sleep(milliseconds);
                Parallel.For(0, numberOfTests, (i) =>
                {
                    cache.BlitzGet(GetKey(i), (n) => GetValueWithDifferentCacheRetention(n, i));
                });

                Assert.AreEqual(calls, slowClass.Counter);
            }

            void CleanCache()
            {
                cache.Remove("Zero");
                cache.Remove("Even");
                cache.Remove("Odd");
            }

            WaitAndCheck(0, 3); //The first time we will call three times

            WaitAndCheck(500, 0); //If we wait only 500 everything should be cached

            WaitAndCheck(1100, 1); //If we wait 1100 only Zero should be recalculated

            CleanCache();

            WaitAndCheck(0, 3); //The first time we will call three times

            WaitAndCheck(2100, 2); //If we wait 2100 Zero and Even should be recalculated

            WaitAndCheck(1000, 2); //If we wait 1000 more Odd should be recalculated

            WaitAndCheck(3100, 3); //If we wait 3100 more everything should be recalculated

        }

        [Test]
        public void MultipleBlitzCacheInstances_ShouldUseGlobalCacheByDefault()
        {
            // Arrange
            var cache1 = new BlitzCache(60000, useGlobalCache: true);
            var cache2 = new BlitzCache(60000, useGlobalCache: true);

            // Act - Store value in cache1
            var result1 = cache1.BlitzGet("shared_key", () => "shared_value", 10000);
            var result2 = cache2.BlitzGet("shared_key", () => "different_value", 10000);

            // Assert - Both should return the same cached value
            Assert.AreEqual("shared_value", result1);
            Assert.AreEqual("shared_value", result2, "Should get cached value from global cache");

            // Cleanup
            cache1.Dispose();
            cache2.Dispose();
        }

        [Test]
        public void IndependentBlitzCacheInstances_ShouldHaveSeparateCaches()
        {
            // Arrange
            var cache1 = new BlitzCache(60000, useGlobalCache: false);
            var cache2 = new BlitzCache(60000, useGlobalCache: false);

            // Act - Store different values with same key in each cache
            var result1 = cache1.BlitzGet("same_key", () => "value_from_cache1", 10000);
            var result2 = cache2.BlitzGet("same_key", () => "value_from_cache2", 10000);

            // Assert - Each cache should have its own value
            Assert.AreEqual("value_from_cache1", result1);
            Assert.AreEqual("value_from_cache2", result2);

            // Cleanup
            cache1.Dispose();
            cache2.Dispose();
        }

        [Test]
        public async Task AsyncBlitzUpdate_ShouldReturnTaskAndCacheValue()
        {
            // Arrange
            var testCache = new BlitzCache(60000, useGlobalCache: false);

            // Act - Use AsyncRepeater to test async BlitzUpdate
            var updateTask = testCache.BlitzUpdate("async_key", async () => {
                await Task.Delay(10);
                return "async_value";
            }, 10000);

            // Assert - Should return Task and complete successfully
            Assert.IsInstanceOf<Task>(updateTask, "BlitzUpdate should return Task");
            await updateTask; // Should complete without error

            // Verify the value was cached using AsyncRepeater
            var testResult = await AsyncRepeater.GoWithResults(5, () => testCache.BlitzGet("async_key", () => Task.FromResult("fallback"), 10000));
            
            Assert.IsTrue(testResult.AllResultsIdentical, "All calls should get same cached value");
            Assert.AreEqual("async_value", testResult.FirstResult, "Should return cached async value");

            // Cleanup
            testCache.Dispose();
        }

        [Test]
        public void BlitzCacheConstructor_ShouldValidateParameters()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BlitzCache(null, 60000));
        }

        [Test]
        public void DisposingGlobalCacheInstance_ShouldNotDisposeGlobalCache()
        {
            // Arrange
            var cache1 = new BlitzCache(60000, useGlobalCache: true);
            var cache2 = new BlitzCache(60000, useGlobalCache: true);

            // Store value in global cache
            cache1.BlitzGet("global_key", () => "global_value", 10000);

            // Act - Dispose cache1 (should not dispose global cache)
            cache1.Dispose();

            // Assert - cache2 should still work and have the cached value
            var result = cache2.BlitzGet("global_key", () => "different_value", 10000);
            Assert.AreEqual("global_value", result, "Global cache should still be accessible");

            // Cleanup
            cache2.Dispose();
        }

        [Test]
        public void CustomMemoryCache_ShouldWorkCorrectly()
        {
            // Arrange
            var customMemoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var testCache = new BlitzCache(customMemoryCache, 60000);

            // Act
            var result1 = testCache.BlitzGet("custom_key", () => "custom_value", 10000);
            var result2 = testCache.BlitzGet("custom_key", () => "different_value", 10000);

            // Assert
            Assert.AreEqual("custom_value", result1);
            Assert.AreEqual("custom_value", result2, "Should get cached value");

            // Cleanup
            testCache.Dispose();
            customMemoryCache.Dispose();
        }

        [Test]
        public void DisposingIndependentCache_ShouldNotAffectOtherCaches()
        {
            // Arrange
            var cache1 = new BlitzCache(60000, useGlobalCache: false);
            var cache2 = new BlitzCache(60000, useGlobalCache: false);

            // Store values in both caches
            cache1.BlitzGet("test_key", () => "value1", 10000);
            cache2.BlitzGet("test_key", () => "value2", 10000);

            // Act - Dispose cache1
            cache1.Dispose();

            // Assert - cache2 should still work
            var result = cache2.BlitzGet("test_key", () => "new_value", 10000);
            Assert.AreEqual("value2", result, "Cache2 should still have its cached value");

            // Should be able to store new values in cache2
            var newResult = cache2.BlitzGet("new_key", () => "new_value", 10000);
            Assert.AreEqual("new_value", newResult);

            // Cleanup
            cache2.Dispose();
        }

        [Test]
        public async Task MultipleIndependentCaches_ShouldWorkUnderPressure()
        {
            // Arrange
            var cache1 = new BlitzCache(60000, useGlobalCache: false);
            var cache2 = new BlitzCache(60000, useGlobalCache: false);
            var cache3 = new BlitzCache(60000, useGlobalCache: false);

            // Act - Use AsyncRepeater for concurrent load testing
            var tasks = new Task[]
            {
                AsyncRepeater.Go(50, () => cache1.BlitzGet("load_test", () => Task.FromResult("cache1_value"), 5000)),
                AsyncRepeater.Go(50, () => cache2.BlitzGet("load_test", () => Task.FromResult("cache2_value"), 5000)),
                AsyncRepeater.Go(50, () => cache3.BlitzGet("load_test", () => Task.FromResult("cache3_value"), 5000))
            };

            await Task.WhenAll(tasks);

            // Assert - Verify each cache has its own values
            Assert.AreEqual("cache1_value", await cache1.BlitzGet("load_test", () => Task.FromResult("fallback"), 5000));
            Assert.AreEqual("cache2_value", await cache2.BlitzGet("load_test", () => Task.FromResult("fallback"), 5000));
            Assert.AreEqual("cache3_value", await cache3.BlitzGet("load_test", () => Task.FromResult("fallback"), 5000));

            // Cleanup
            cache1.Dispose();
            cache2.Dispose();
            cache3.Dispose();
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            cache.Dispose();
            serviceProvider.Dispose();
        }
    }
}
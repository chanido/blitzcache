using BlitzCacheCore.Extensions;
using BlitzCacheCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Core functionality tests for BlitzCache focusing on basic caching operations,
    /// cache expiration, key isolation, and BlitzUpdate operations.
    /// </summary>
    public class CoreFunctionalityTests
    {
        private const int numberOfTests = 5000;
        private IBlitzCache cache;
        private ServiceProvider serviceProvider;

        [OneTimeSetUp]
        public void BeforeAll()
        {
            serviceProvider = new ServiceCollection()
                .AddBlitzCache()
                .BuildServiceProvider();

            cache = serviceProvider.GetService<IBlitzCache>();
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
        public void VariableTimespan_UsingNuances()
        {
            var slowClass = new SlowClass();

            const int zeroRetention = 50;
            const int evenRetention = 100;
            const int oddRetention = 150;
            const int delta = 25;

            static string GetKey(int i) => i == 0 ? "Zero" : i % 2 == 0 ? "Even" : "Odd";

            bool? GetValueWithDifferentCacheRetention(Nuances n, int i)
            {
                bool? result = null;
                try { result = slowClass.FailIfZeroTrueIfEven(i); }
                catch { }

                switch (result)
                {
                    case null: n.CacheRetention = zeroRetention; break; //Zero
                    case true: n.CacheRetention = evenRetention; break; //Even
                    case false: n.CacheRetention = oddRetention; break; //Odd
                }

                return result;
            }

            void WaitAndCheck(int milliseconds, int calls, string message)
            {
                slowClass.ResetCounter();
                Thread.Sleep(milliseconds);
                Parallel.For(0, numberOfTests, (i) =>
                {
                    cache.BlitzGet(GetKey(i), (n) => GetValueWithDifferentCacheRetention(n, i));
                });

                Assert.AreEqual(calls, slowClass.Counter, $"Failed: {message}");
            }

            void CleanCache()
            {
                cache.Remove("Zero");
                cache.Remove("Even");
                cache.Remove("Odd");
            }

            CleanCache();
            WaitAndCheck(0, 3, "First time we will call three times");
            WaitAndCheck(zeroRetention - delta, 0, "If we wait less than zeroRetention everything should be cached");

            CleanCache();
            WaitAndCheck(0, 3, "First time we will call three times");
            WaitAndCheck( zeroRetention + delta, 1, "If we wait after zeroRetention only Zero should be recalculated");

            CleanCache();
            WaitAndCheck(0, 3, "First time we will call three times");
            WaitAndCheck(evenRetention + delta, 2, "If we wait evenRetention Zero and Even should be recalculated");

            CleanCache();
            WaitAndCheck(0, 3, "First time we will call three times");
            WaitAndCheck(oddRetention + delta, 3, "If we wait oddRetention Zero, Even and Odd should be recalculated");
        }

        [Test]
        public async Task AsyncBlitzUpdate_ShouldReturnTaskAndCacheValue()
        {
            // Arrange
            var testCache = TestFactory.CreateBasic();

            // Act - Use AsyncRepeater to test async BlitzUpdate
            var updateTask = testCache.BlitzUpdate("async_update_key", async () => {
                await TestFactory.WaitForEvictionCallbacks();
                return "async_update_value";
            }, TestFactory.StandardTimeoutMs);

            // Assert - Should return Task and complete successfully
            Assert.IsInstanceOf<Task>(updateTask, "BlitzUpdate should return Task");
            await updateTask; // Should complete without error

            // Verify the value was cached using AsyncRepeater
            var testResult = await AsyncRepeater.GoWithResults(5, () => testCache.BlitzGet("async_update_key", () => Task.FromResult("fallback"), TestFactory.StandardTimeoutMs));
            
            Assert.IsTrue(testResult.AllResultsIdentical, "All calls should get same cached value");
            Assert.AreEqual("async_update_value", testResult.FirstResult, "Should return cached async value");

            // Cleanup
            testCache.Dispose();
        }

        [Test]
        public void CacheKeyIsolation_SameFunctionDifferentKeys()
        {
            // Arrange
            var counter = 0;
            string TestFunction()
            {
                Interlocked.Increment(ref counter);
                return $"result_{counter}";
            }

            // Act
            var result1 = cache.BlitzGet("isolation_key1", TestFunction, TestFactory.StandardTimeoutMs);
            var result2 = cache.BlitzGet("isolation_key2", TestFunction, TestFactory.StandardTimeoutMs);
            var result1_cached = cache.BlitzGet("isolation_key1", TestFunction, TestFactory.StandardTimeoutMs);
            var result2_cached = cache.BlitzGet("isolation_key2", TestFunction, TestFactory.StandardTimeoutMs);

            // Assert
            Assert.AreEqual("result_1", result1, "First key should get first result");
            Assert.AreEqual("result_2", result2, "Second key should get second result");
            Assert.AreEqual("result_1", result1_cached, "First key should return cached result");
            Assert.AreEqual("result_2", result2_cached, "Second key should return cached result");
            Assert.AreEqual(2, counter, "Function should only be called twice");

            // Cleanup
            cache.Remove("isolation_key1");
            cache.Remove("isolation_key2");
        }

        [Test]
        public async Task CacheExpiration_ShouldRecalculateAfterTimeout()
        {
            // Arrange
            var testCache = TestFactory.CreateBasic();
            var counter = 0;
            string TestFunction()
            {
                Interlocked.Increment(ref counter);
                return $"result_{counter}";
            }

            // Act
            var result1 = testCache.BlitzGet("expiration_key", TestFunction, TestFactory.ShortTimeoutMs);
            await TestFactory.WaitForShortExpiration(); // Wait for expiration
            var result2 = testCache.BlitzGet("expiration_key", TestFunction, TestFactory.ShortTimeoutMs);

            // Assert
            Assert.AreEqual("result_1", result1, "First call should get first result");
            Assert.AreEqual("result_2", result2, "Second call after expiration should get new result");
            Assert.AreEqual(2, counter, "Function should be called twice due to expiration");

            // Cleanup
            testCache.Dispose();
        }

        [Test]
        public void RemoveOperation_ShouldClearCachedValue()
        {
            // Arrange
            var counter = 0;
            string TestFunction()
            {
                Interlocked.Increment(ref counter);
                return $"result_{counter}";
            }

            // Act
            var result1 = cache.BlitzGet("remove_key", TestFunction, TestFactory.StandardTimeoutMs);
            cache.Remove("remove_key");
            var result2 = cache.BlitzGet("remove_key", TestFunction, TestFactory.StandardTimeoutMs);

            // Assert
            Assert.AreEqual("result_1", result1, "First call should get first result");
            Assert.AreEqual("result_2", result2, "Second call after remove should get new result");
            Assert.AreEqual(2, counter, "Function should be called twice due to removal");

            // Cleanup
            cache.Remove("remove_key");
        }

        [Test]
        public async Task AsyncCacheOperations_ShouldWorkCorrectly()
        {
            // Arrange
            var testCache = TestFactory.CreateBasic();
            var counter = 0;

            async Task<string> AsyncTestFunction()
            {
                Interlocked.Increment(ref counter);
                await TestFactory.WaitForEvictionCallbacks();
                return $"async_result_{counter}";
            }

            // Act
            var result1 = await testCache.BlitzGet("async_core_key", AsyncTestFunction, TestFactory.StandardTimeoutMs);
            var result2 = await testCache.BlitzGet("async_core_key", AsyncTestFunction, TestFactory.StandardTimeoutMs);

            // Assert
            Assert.AreEqual("async_result_1", result1, "First async call should get first result");
            Assert.AreEqual("async_result_1", result2, "Second async call should get cached result");
            Assert.AreEqual(1, counter, "Async function should only be called once");

            // Cleanup
            testCache.Dispose();
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            cache.Dispose();
            serviceProvider.Dispose();
        }
    }
}

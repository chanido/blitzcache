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

        [SetUp]
        public void BeforeAll()
        {
            serviceProvider = new ServiceCollection()
                .AddBlitzCache()
                .BuildServiceProvider();

            cache = serviceProvider.GetService<IBlitzCache>();
        }

        [TearDown]
        public void AfterAll()
        {
            BlitzCache.ClearGlobalForTesting();
            serviceProvider.Dispose();
        }

        [Test]
        public async Task ParallelAccessToAsyncMethod()
        {
            var slowClass = new SlowClassAsync();

            await AsyncRepeater.Go(numberOfTests, () => cache.BlitzGet(slowClass.ProcessQuickly));

            Assert.That(slowClass.Counter, Is.EqualTo(1));
        }

        [Test]
        public async Task DifferentKeysWillCallTheAsyncMethodAgain()
        {
            var slowClass = new SlowClassAsync();

            var key1 = Guid.NewGuid().ToString();
            await AsyncRepeater.Go(numberOfTests, () => cache.BlitzGet(key1, slowClass.ProcessQuickly));

            var key2 = Guid.NewGuid().ToString();
            await AsyncRepeater.Go(numberOfTests, () => cache.BlitzGet(key2, slowClass.ProcessQuickly));

            Assert.That(slowClass.Counter, Is.EqualTo(2));
        }

        [Test]
        public void ParallelAccessToSyncMethod()
        {
            var slowClass = new SlowClass();

            Parallel.For(0, numberOfTests, (i) =>
            {
                cache.BlitzGet(slowClass.ProcessQuickly);
            });

            Assert.That(slowClass.Counter, Is.EqualTo(1));
        }

        [Test]
        public void DifferentKeysWillCallTheSyncMethodAgain()
        {
            var slowClass = new SlowClass();

            var key1 = Guid.NewGuid().ToString();
            Parallel.For(0, numberOfTests, (i) => { cache.BlitzGet(key1, slowClass.ProcessQuickly); });
            var key2 = Guid.NewGuid().ToString();
            Parallel.For(0, numberOfTests, (i) => { cache.BlitzGet(key2, slowClass.ProcessQuickly); });

            Assert.That(slowClass.Counter, Is.EqualTo(2));
        }

        [Test]
        public void VariableTimespan_UsingNuances()
        {
            var slowClass = new SlowClass();

            const int zeroRetention = 50;
            const int evenRetention = 100;
            const int oddRetention = 150;
            const int delta = 25;

            static string GetKey(int i)
            {
                return i == 0 ? "Zero" : i % 2 == 0 ? "Even" : "Odd";
            }

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

                Assert.That(slowClass.Counter, Is.EqualTo(calls), $"Failed: {message}");
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
            WaitAndCheck(zeroRetention + delta, 1, "If we wait after zeroRetention only Zero should be recalculated");

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
            // Act - Use AsyncRepeater to test async BlitzUpdate
            var updateTask = cache.BlitzUpdate("async_update_key", async () =>
            {
                await TestDelays.WaitForEvictionCallbacks();
                return "async_update_value";
            }, TestConstants.StandardTimeoutMs);

            // Assert - Should return Task and complete successfully
            Assert.That(updateTask, Is.InstanceOf<Task>(), "BlitzUpdate should return Task");
            await updateTask; // Should complete without error

            // Verify the value was cached using AsyncRepeater
            var testResult = await AsyncRepeater.GoWithResults(5, () => cache.BlitzGet("async_update_key", () => Task.FromResult("fallback"), TestConstants.StandardTimeoutMs));

            Assert.That(testResult.AllResultsIdentical, Is.True, "All calls should get same cached value");
            Assert.That(testResult.FirstResult, Is.EqualTo("async_update_value"), "Should return cached async value");
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
            var result1 = cache.BlitzGet("isolation_key1", TestFunction, TestConstants.StandardTimeoutMs);
            var result2 = cache.BlitzGet("isolation_key2", TestFunction, TestConstants.StandardTimeoutMs);
            var result1_cached = cache.BlitzGet("isolation_key1", TestFunction, TestConstants.StandardTimeoutMs);
            var result2_cached = cache.BlitzGet("isolation_key2", TestFunction, TestConstants.StandardTimeoutMs);

            // Assert
            Assert.That(result1, Is.EqualTo("result_1"), "First key should get first result");
            Assert.That(result2, Is.EqualTo("result_2"), "Second key should get second result");
            Assert.That(result1_cached, Is.EqualTo("result_1"), "First key should return cached result");
            Assert.That(result2_cached, Is.EqualTo("result_2"), "Second key should return cached result");
            Assert.That(counter, Is.EqualTo(2), "Function should only be called twice");

            // Cleanup
            cache.Remove("isolation_key1");
            cache.Remove("isolation_key2");
        }

        [Test]
        public async Task CacheExpiration_ShouldRecalculateAfterTimeout()
        {
            // Arrange
            var counter = 0;
            string TestFunction()
            {
                Interlocked.Increment(ref counter);
                return $"result_{counter}";
            }

            // Act
            var result1 = cache.BlitzGet("expiration_key", TestFunction, TestConstants.StandardTimeoutMs);
            await TestDelays.WaitForStandardExpiration(); // Wait for expiration
            var result2 = cache.BlitzGet("expiration_key", TestFunction, TestConstants.StandardTimeoutMs);

            // Assert
            Assert.That(result1, Is.EqualTo("result_1"), "First call should get first result");
            Assert.That(result2, Is.EqualTo("result_2"), "Second call after expiration should get new result");
            Assert.That(counter, Is.EqualTo(2), "Function should be called twice due to expiration");
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
            var result1 = cache.BlitzGet("remove_key", TestFunction, TestConstants.StandardTimeoutMs);
            cache.Remove("remove_key");
            var result2 = cache.BlitzGet("remove_key", TestFunction, TestConstants.StandardTimeoutMs);

            // Assert
            Assert.That(result1, Is.EqualTo("result_1"), "First call should get first result");
            Assert.That(result2, Is.EqualTo("result_2"), "Second call after remove should get new result");
            Assert.That(counter, Is.EqualTo(2), "Function should be called twice due to removal");

            // Cleanup
            cache.Remove("remove_key");
        }

        [Test]
        public async Task AsyncCacheOperations_ShouldWorkCorrectly()
        {
            // Arrange
            var counter = 0;

            async Task<string> AsyncTestFunction()
            {
                Interlocked.Increment(ref counter);
                await TestDelays.WaitForEvictionCallbacks();
                return $"async_result_{counter}";
            }

            // Act
            var result1 = await cache.BlitzGet("async_core_key", AsyncTestFunction, TestConstants.StandardTimeoutMs);
            var result2 = await cache.BlitzGet("async_core_key", AsyncTestFunction, TestConstants.StandardTimeoutMs);

            // Assert
            Assert.That(result1, Is.EqualTo("async_result_1"), "First async call should get first result");
            Assert.That(result2, Is.EqualTo("async_result_1"), "Second async call should get cached result");
            Assert.That(counter, Is.EqualTo(1), "Async function should only be called once");
        }
    }
}

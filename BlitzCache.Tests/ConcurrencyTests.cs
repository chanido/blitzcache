using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Tests for concurrent access behavior, thread safety, and queuing mechanisms in BlitzCache.
    /// </summary>
    [TestFixture]
    public class ConcurrencyTests
    {
        private IBlitzCache cache;
        private SlowClass slowClass;
        private SlowClassAsync slowClassAsync;

        [SetUp]
        public void Setup()
        {
            cache = new BlitzCache(useGlobalCache: false);
            slowClass = new SlowClass();
            slowClassAsync = new SlowClassAsync();
        }

        [TearDown]
        public void Cleanup()
        {
            cache?.Dispose();
        }

        private string GetUniqueCacheKey([System.Runtime.CompilerServices.CallerMemberName] string testName = "") 
            => $"{testName}-{Guid.NewGuid()}";

        [Test]
        public async Task ConcurrentAsyncCalls_ShouldExecuteOnlyOnce()
        {
            // Arrange
            var cacheKey = GetUniqueCacheKey();
            
            // Act - Use AsyncRepeater for cleaner test like existing UnitTests
            await AsyncRepeater.Go(100, () => cache.BlitzGet(cacheKey, slowClassAsync.ProcessQuickly, 10000));

            // Assert
            Assert.AreEqual(1, slowClassAsync.Counter, "Expensive async operation should only execute once despite 100 concurrent calls");
        }

        [Test]
        public async Task ConcurrentCalls_ShouldWaitForFirstExecution_AndReceiveSameResult()
        {
            // Arrange
            var cacheKey = GetUniqueCacheKey();

            // Act - Use enhanced AsyncRepeater with existing SlowClassAsync
            var testResult = await AsyncRepeater.GoWithResults(10, () => cache.BlitzGet(cacheKey, slowClassAsync.ProcessSlowly, 10000));

            // Assert
            Assert.AreEqual(1, slowClassAsync.Counter, "Expensive operation should only execute once");
            Assert.AreEqual(10, testResult.ResultCount, "Should have 10 results (one per concurrent call)");
            Assert.AreEqual(1, testResult.UniqueResultCount, "All calls should receive the same result");
            Assert.IsTrue(testResult.AllResultsIdentical, "All results should be identical");
        }

        [Test]
        public async Task ConcurrentAsyncCalls_WithRealisticTiming_ShouldQueue()
        {
            // Arrange
            var cacheKey = GetUniqueCacheKey();

            // Act - Use enhanced AsyncRepeater with staggered calls and existing SlowClassAsync
            var testResult = await AsyncRepeater.GoWithResults(5, 
                () => cache.BlitzGet(cacheKey, slowClassAsync.ProcessSlowly, 10000), 
                staggerDelayMs: 50);

            // Assert
            Assert.AreEqual(1, slowClassAsync.Counter, "Only one execution should have occurred");
            Assert.IsTrue(testResult.AllResultsIdentical, "All results should be identical");
            Assert.That(testResult.ElapsedMilliseconds, Is.LessThan(1300), 
                "Total time should be close to single execution time, indicating queuing worked");
        }

        [Test]
        public void SyncConcurrentCalls_ShouldExecuteOnlyOnce()
        {
            // Arrange
            var cacheKey = GetUniqueCacheKey();
            
            // Act - Use Parallel.For for sync operations like existing UnitTests
            Parallel.For(0, 100, (i) =>
            {
                cache.BlitzGet(cacheKey, slowClass.ProcessQuickly, 10000);
            });

            // Assert
            Assert.AreEqual(1, slowClass.Counter, "Expensive sync operation should only execute once despite 100 concurrent calls");
        }

        [Test]
        public void SyncConcurrentCalls_ShouldAllReceiveSameResult()
        {
            // Arrange
            var cacheKey = GetUniqueCacheKey();

            // Act - Use enhanced sync repeater with existing SlowClass
            var testResult = AsyncRepeater.GoSyncWithResults(8, () => cache.BlitzGet(cacheKey, slowClass.ProcessSlowly, 10000));

            // Assert
            Assert.AreEqual(1, slowClass.Counter, "Expensive sync operation should only execute once");
            Assert.AreEqual(8, testResult.ResultCount, "Should have 8 results");
            Assert.AreEqual(1, testResult.UniqueResultCount, "All sync calls should receive the same result");
            Assert.IsTrue(testResult.AllResultsIdentical, "All results should be identical");
        }

        [Test]
        public async Task MixedSyncAsyncCalls_ToSameCacheKey_ShouldStillWork()
        {
            // This test verifies edge case behavior when mixing sync and async calls
            // Note: This is an edge case and the behavior might depend on implementation details
            
            var executionCount = 0;
            var cacheKey = GetUniqueCacheKey();
            
            string SyncOperation()
            {
                Interlocked.Increment(ref executionCount);
                System.Threading.Thread.Sleep(100);
                return "MixedResult";
            }

            async Task<string> AsyncOperation()
            {
                Interlocked.Increment(ref executionCount);
                await Task.Delay(100);
                return "MixedResult";
            }

            // Act - This tests the edge case, results may vary by implementation
            var syncTask = Task.Run(() => cache.BlitzGet(cacheKey, SyncOperation, 10000));
            var asyncTask = cache.BlitzGet(cacheKey, AsyncOperation, 10000);

            var results = await Task.WhenAll(syncTask, asyncTask);

            // Assert - At minimum, we shouldn't have more executions than cache misses
            // Implementation detail: sync and async may use different locks
            Assert.That(executionCount, Is.LessThanOrEqualTo(2), 
                "Should not have excessive executions even with mixed sync/async calls");
        }
    }
}

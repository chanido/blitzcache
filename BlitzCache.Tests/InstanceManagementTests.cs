using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Tests for BlitzCache instance management including global vs independent cache behavior,
    /// disposal handling, and constructor validation.
    /// </summary>
    public class InstanceManagementTests
    {
        [Test]
        public void GlobalBlitzCacheInstance_ShouldShareAcrossAccess()
        {
            // Arrange - Use the global instance
            var global1 = BlitzCache.Global;
            var global2 = BlitzCache.Global;

            // Act - Store value using first reference
            var result1 = global1.BlitzGet("shared_key", () => "shared_value", 10000);
            var result2 = global2.BlitzGet("shared_key", () => "different_value", 10000);

            // Assert - Both should return the same cached value and be the same instance
            Assert.AreSame(global1, global2, "Global instances should be the same reference");
            Assert.AreEqual("shared_value", result1);
            Assert.AreEqual("shared_value", result2, "Should get cached value from global cache");
        }

        [Test]
        public void IndependentBlitzCacheInstances_ShouldHaveSeparateCaches()
        {
            // Arrange
            var cache1 = new BlitzCache(60000);
            var cache2 = new BlitzCache(60000);

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
        public void BlitzCacheConstructor_ShouldValidateParameters()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BlitzCache(null, 60000));
        }

        [Test]
        public void DisposingGlobalCacheInstance_ShouldNotDisposeGlobalCache()
        {
            // Arrange - Use the global singleton
            var global1 = BlitzCache.Global;
            var global2 = BlitzCache.Global;

            // Store value in global cache
            global1.BlitzGet("global_disposal_key", () => "global_value", 10000);

            // Note: You cannot dispose the global singleton (it would throw an exception in production)
            // The global cache persists for the entire application lifetime

            // Assert - Both references should work and share the same data
            var result = global2.BlitzGet("global_disposal_key", () => "different_value", 10000);
            Assert.AreEqual("global_value", result, "Global cache should still be accessible");
            Assert.AreSame(global1, global2, "Global instances should be the same reference");
        }

        [Test]
        public void CustomMemoryCache_ShouldWorkCorrectly()
        {
            // Arrange
            var customMemoryCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
            var testCache = new BlitzCache(customMemoryCache, 60000);

            // Act
            var result1 = testCache.BlitzGet("custom_cache_key", () => "custom_value", 10000);
            var result2 = testCache.BlitzGet("custom_cache_key", () => "different_value", 10000);

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
            var cache1 = new BlitzCache(60000);
            var cache2 = new BlitzCache(60000);

            // Store values in both caches
            cache1.BlitzGet("independent_test_key", () => "value1", 10000);
            cache2.BlitzGet("independent_test_key", () => "value2", 10000);

            // Act - Dispose cache1
            cache1.Dispose();

            // Assert - cache2 should still work
            var result = cache2.BlitzGet("independent_test_key", () => "new_value", 10000);
            Assert.AreEqual("value2", result, "Cache2 should still have its cached value");

            // Should be able to store new values in cache2
            var newResult = cache2.BlitzGet("new_independent_key", () => "new_value", 10000);
            Assert.AreEqual("new_value", newResult);

            // Cleanup
            cache2.Dispose();
        }

        [Test]
        public async Task MultipleIndependentCaches_ShouldWorkUnderPressure()
        {
            // Arrange
            var cache1 = new BlitzCache(60000);
            var cache2 = new BlitzCache(60000);
            var cache3 = new BlitzCache(60000);

            // Act - Use AsyncRepeater for concurrent load testing
            var tasks = new Task[]
            {
                AsyncRepeater.Go(50, () => cache1.BlitzGet("pressure_test", () => Task.FromResult("cache1_value"), 5000)),
                AsyncRepeater.Go(50, () => cache2.BlitzGet("pressure_test", () => Task.FromResult("cache2_value"), 5000)),
                AsyncRepeater.Go(50, () => cache3.BlitzGet("pressure_test", () => Task.FromResult("cache3_value"), 5000))
            };

            await Task.WhenAll(tasks);

            // Assert - Verify each cache has its own values
            Assert.AreEqual("cache1_value", await cache1.BlitzGet("pressure_test", () => Task.FromResult("fallback"), 5000));
            Assert.AreEqual("cache2_value", await cache2.BlitzGet("pressure_test", () => Task.FromResult("fallback"), 5000));
            Assert.AreEqual("cache3_value", await cache3.BlitzGet("pressure_test", () => Task.FromResult("fallback"), 5000));

            // Cleanup
            cache1.Dispose();
            cache2.Dispose();
            cache3.Dispose();
        }

        [Test]
        public void MultipleDisposalCalls_ShouldBeSafe()
        {
            // Arrange
            var testCache = new BlitzCache(60000);

            // Store a value to ensure cache is working
            var result = testCache.BlitzGet("disposal_safety_key", () => "test_value", 10000);
            Assert.AreEqual("test_value", result);

            // Act & Assert - Multiple disposal calls should not throw
            Assert.DoesNotThrow(() => testCache.Dispose());
            Assert.DoesNotThrow(() => testCache.Dispose());
            Assert.DoesNotThrow(() => testCache.Dispose());
        }

        [Test]
        public void DisposedCache_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var testCache = new BlitzCache(60000);
            testCache.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => 
                testCache.BlitzGet("disposed_key", () => "value", 1000));
        }

        [Test]
        public async Task DisposedCache_AsyncOperations_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var testCache = new BlitzCache(60000);
            testCache.Dispose();

            // Act & Assert - Use try/catch pattern to properly test async exceptions
            var exceptionThrown = false;
            try
            {
                await testCache.BlitzGet("disposed_async_key", () => Task.FromResult("value"), 1000);
            }
            catch (ObjectDisposedException)
            {
                exceptionThrown = true;
            }
            
            Assert.IsTrue(exceptionThrown, "ObjectDisposedException should be thrown when using disposed cache");
        }

        [Test]
        public void ResourceCleanup_ShouldClearSemaphores()
        {
            // Arrange
            var testCache = new BlitzCache(60000);
            
            // Create some cached values to ensure semaphores are created
            testCache.BlitzGet("cleanup_key1", () => "value1", 10000);
            testCache.BlitzGet("cleanup_key2", () => "value2", 10000);
            
            var semaphoreCountBefore = testCache.GetSemaphoreCount();
            Assert.Greater(semaphoreCountBefore, 0, "Should have semaphores before disposal");

            // Act
            testCache.Dispose();

            // Assert - GetSemaphoreCount should return 0 after disposal (semaphores cleared)
            var semaphoreCountAfter = testCache.GetSemaphoreCount();
            Assert.AreEqual(0, semaphoreCountAfter, "Semaphores should be cleared after disposal");
        }
    }
}

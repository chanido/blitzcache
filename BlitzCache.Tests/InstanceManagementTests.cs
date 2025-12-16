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
            var global1 = new BlitzCache();
            var global2 = new BlitzCache();

            // Act - Store value using first reference
            var result1 = global1.BlitzGet("shared_key", () => "shared_value", TestConstants.StandardTimeoutMs);
            var result2 = global2.BlitzGet("shared_key", () => "different_value", TestConstants.StandardTimeoutMs);

            // Assert - Both should return the same cached value and be the same instance
            Assert.That(global1.GetInternalInstance(), Is.SameAs(global2.GetInternalInstance()), "Global instances should be the same reference");
            Assert.That(result1, Is.EqualTo("shared_value"));
            Assert.That(result2, Is.EqualTo("shared_value"), "Should get cached value from global cache");

            // Global cache persists for the entire application lifetime
            // But for testing purposes, we will dispose it
            BlitzCache.ClearGlobalForTesting();
        }

        [Test]
        public void IndependentBlitzCacheInstances_ShouldHaveSeparateCaches()
        {
            // Arrange
            var cache1 = TestFactory.CreateBlitzCacheInstance();
            var cache2 = TestFactory.CreateBlitzCacheInstance();

            // Act - Store different values with same key in each cache
            var result1 = cache1.BlitzGet("same_key", () => "value_from_cache1", TestConstants.StandardTimeoutMs);
            var result2 = cache2.BlitzGet("same_key", () => "value_from_cache2", TestConstants.StandardTimeoutMs);

            // Assert - Each cache should have its own value
            Assert.That(result1, Is.EqualTo("value_from_cache1"));
            Assert.That(result2, Is.EqualTo("value_from_cache2"));

            // Cleanup
            cache1.Dispose();
            cache2.Dispose();
        }

        [Test]
        public void BlitzCacheConstructor_ShouldValidateParameters()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new BlitzCache(-1000));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BlitzCache(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BlitzCacheInstance(-1000));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BlitzCacheInstance(0));
        }

        [Test]
        public void DisposingIndependentCache_ShouldNotAffectOtherCaches()
        {
            // Arrange
            var cache1 = TestFactory.CreateBlitzCacheInstance();
            var cache2 = TestFactory.CreateBlitzCacheInstance();

            // Store values in both caches
            cache1.BlitzGet("independent_test_key", () => "value1", TestConstants.StandardTimeoutMs);
            cache2.BlitzGet("independent_test_key", () => "value2", TestConstants.StandardTimeoutMs);

            // Act - Dispose cache1
            cache1.Dispose();

            // Assert - cache2 should still work
            var result = cache2.BlitzGet("independent_test_key", () => "new_value", TestConstants.StandardTimeoutMs);
            Assert.That(result, Is.EqualTo("value2"), "Cache2 should still have its cached value");

            // Should be able to store new values in cache2
            var newResult = cache2.BlitzGet("new_independent_key", () => "new_value", TestConstants.StandardTimeoutMs);
            Assert.That(newResult, Is.EqualTo("new_value"));

            // Cleanup
            cache2.Dispose();
        }

        [Test]
        public async Task MultipleIndependentCaches_ShouldWorkUnderPressure()
        {
            // Arrange
            var cache1 = TestFactory.CreateBlitzCacheInstance();
            var cache2 = TestFactory.CreateBlitzCacheInstance();
            var cache3 = TestFactory.CreateBlitzCacheInstance();

            // Act - Use AsyncRepeater for concurrent load testing
            var tasks = new Task[]
            {
                AsyncRepeater.Go(50, () => cache1.BlitzGet("pressure_test", () => Task.FromResult("cache1_value"))),
                AsyncRepeater.Go(50, () => cache2.BlitzGet("pressure_test", () => Task.FromResult("cache2_value"))),
                AsyncRepeater.Go(50, () => cache3.BlitzGet("pressure_test", () => Task.FromResult("cache3_value")))
            };

            await Task.WhenAll(tasks);

            // Assert - Verify each cache has its own values
            Assert.That(await cache1.BlitzGet("pressure_test", () => Task.FromResult("fallback")), Is.EqualTo("cache1_value"));
            Assert.That(await cache2.BlitzGet("pressure_test", () => Task.FromResult("fallback")), Is.EqualTo("cache2_value"));
            Assert.That(await cache3.BlitzGet("pressure_test", () => Task.FromResult("fallback")), Is.EqualTo("cache3_value"));

            // Cleanup
            cache1.Dispose();
            cache2.Dispose();
            cache3.Dispose();
        }

        [Test]
        public void MultipleDisposalCalls_ShouldBeSafe()
        {
            // Arrange
            var testCache = TestFactory.CreateBlitzCacheInstance();

            // Store a value to ensure cache is working
            var result = testCache.BlitzGet("disposal_safety_key", () => "test_value");
            Assert.That(result, Is.EqualTo("test_value"));

            // Act & Assert - Multiple disposal calls should not throw
            Assert.DoesNotThrow(() => testCache.Dispose());
            Assert.DoesNotThrow(() => testCache.Dispose());
            Assert.DoesNotThrow(() => testCache.Dispose());
        }

        [Test]
        public void DisposedCache_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var testCache = TestFactory.CreateBlitzCacheInstance();
            testCache.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => testCache.BlitzGet("disposed_key", () => "value"));
        }

        [Test]
        public async Task DisposedCache_AsyncOperations_ShouldThrowObjectDisposedException()
        {
            // Arrange
            var testCache = TestFactory.CreateBlitzCacheInstance();
            testCache.Dispose();

            // Act & Assert - Use try/catch pattern to properly test async exceptions
            var exceptionThrown = false;
            try
            {
                await testCache.BlitzGet("disposed_async_key", () => Task.FromResult("value"));
            }
            catch (ObjectDisposedException)
            {
                exceptionThrown = true;
            }

            Assert.That(exceptionThrown, Is.True, "ObjectDisposedException should be thrown when using disposed cache");
        }

        [Test]
        public void ResourceCleanup_ShouldClearSemaphores()
        {
            // Arrange
            var testCache = TestFactory.CreateBlitzCacheInstance();

            // Create some cached values to ensure semaphores are created
            testCache.BlitzGet("cleanup_key1", () => "value1");
            testCache.BlitzGet("cleanup_key2", () => "value2");

            var semaphoreCountBefore = testCache.GetSemaphoreCount();
            Assert.That(semaphoreCountBefore, Is.GreaterThan(0), "Should have semaphores before disposal");

            // Act
            testCache.Dispose();

            // Assert - GetSemaphoreCount should return 0 after disposal (semaphores cleared)
            var semaphoreCountAfter = testCache.GetSemaphoreCount();
            Assert.That(semaphoreCountAfter, Is.EqualTo(0), "Semaphores should be cleared after disposal");
        }
    }
}

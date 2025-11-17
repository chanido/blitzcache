using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Tests for NullBlitzCacheForTesting utility class.
    /// Validates that NullBlitzCacheForTesting behaves correctly for testing scenarios
    /// where cache behavior should be bypassed.
    /// </summary>
    [TestFixture]
    public class NullBlitzCacheForTestingTests
    {
        [Test]
        public void NullCache_AlwaysExecutesFunction_NeverCaches()
        {
            // Arrange
            var cache = new NullBlitzCacheForTesting();
            var callCount = 0;
            string TestFunction()
            {
                callCount++;
                return $"result_{callCount}";
            }

            // Act
            var result1 = cache.BlitzGet("key", TestFunction, 1000);
            var result2 = cache.BlitzGet("key", TestFunction, 1000);
            var result3 = cache.BlitzGet("key", TestFunction, 1000);

            // Assert
            Assert.AreEqual(3, callCount, "NullCache should execute function every time");
            Assert.AreEqual("result_1", result1);
            Assert.AreEqual("result_2", result2);
            Assert.AreEqual("result_3", result3);
        }

        [Test]
        public async Task NullCache_AlwaysExecutesAsyncFunction_NeverCaches()
        {
            // Arrange
            var cache = new NullBlitzCacheForTesting();
            var callCount = 0;
            async Task<string> TestFunctionAsync()
            {
                callCount++;
                await Task.Delay(1);
                return $"async_result_{callCount}";
            }

            // Act
            var result1 = await cache.BlitzGet("async_key", TestFunctionAsync, 1000);
            var result2 = await cache.BlitzGet("async_key", TestFunctionAsync, 1000);
            var result3 = await cache.BlitzGet("async_key", TestFunctionAsync, 1000);

            // Assert
            Assert.AreEqual(3, callCount, "NullCache should execute async function every time");
            Assert.AreEqual("async_result_1", result1);
            Assert.AreEqual("async_result_2", result2);
            Assert.AreEqual("async_result_3", result3);
        }

        [Test]
        public void NullCache_WithDifferentKeys_ExecutesFunctionForEach()
        {
            // Arrange
            var cache = new NullBlitzCacheForTesting();
            var callCount = 0;
            string TestFunction()
            {
                callCount++;
                return $"result_{callCount}";
            }

            // Act
            var result1 = cache.BlitzGet("key1", TestFunction, 1000);
            var result2 = cache.BlitzGet("key2", TestFunction, 1000);
            var result3 = cache.BlitzGet("key3", TestFunction, 1000);

            // Assert
            Assert.AreEqual(3, callCount, "NullCache should execute function for each key");
            Assert.AreEqual("result_1", result1);
            Assert.AreEqual("result_2", result2);
            Assert.AreEqual("result_3", result3);
        }

        [Test]
        public void NullCache_Statistics_ReturnsNullByDefault()
        {
            // Arrange
            var cache = new NullBlitzCacheForTesting();

            // Act & Assert
            Assert.IsNull(cache.Statistics, "NullCache should return null statistics by default");
        }

        [Test]
        public void NullCache_AfterInitializeStatistics_ReturnsNullStatistics()
        {
            // Arrange
            IBlitzCache cache = new NullBlitzCacheForTesting();

            // Act
            cache.InitializeStatistics();

            // Assert
            Assert.IsNotNull(cache.Statistics, "After InitializeStatistics, should return NullCacheStatistics instance");
            Assert.AreEqual(0, cache.Statistics.HitCount);
            Assert.AreEqual(0, cache.Statistics.MissCount);
            Assert.AreEqual(0, cache.Statistics.TotalOperations);
            Assert.AreEqual(0.0, cache.Statistics.HitRatio);
        }

        [Test]
        public void NullCache_Statistics_AlwaysReturnsZero_EvenAfterOperations()
        {
            // Arrange
            IBlitzCache cache = new NullBlitzCacheForTesting();
            cache.InitializeStatistics();

            // Act
            cache.BlitzGet("key1", () => "value1", 1000);
            cache.BlitzGet("key1", () => "value2", 1000);
            cache.BlitzGet("key2", () => "value3", 1000);

            // Assert - Statistics remain zero because NullCache doesn't track
            Assert.AreEqual(0, cache.Statistics.HitCount, "NullCacheStatistics always returns 0");
            Assert.AreEqual(0, cache.Statistics.MissCount, "NullCacheStatistics always returns 0");
            Assert.AreEqual(0, cache.Statistics.TotalOperations, "NullCacheStatistics always returns 0");
            Assert.AreEqual(0, cache.Statistics.EntryCount, "NullCacheStatistics always returns 0");
        }

        [Test]
        public void NullCache_BlitzUpdate_DoesNothing()
        {
            // Arrange
            var cache = new NullBlitzCacheForTesting();
            var callCount = 0;
            string UpdateFunction()
            {
                callCount++;
                return "updated";
            }

            // Act
            cache.BlitzUpdate("key", UpdateFunction, 1000);

            // Assert
            Assert.AreEqual(0, callCount, "BlitzUpdate on NullCache should not execute the function");
        }

        [Test]
        public async Task NullCache_BlitzUpdateAsync_DoesNothing()
        {
            // Arrange
            var cache = new NullBlitzCacheForTesting();
            var callCount = 0;
            async Task<string> UpdateFunctionAsync()
            {
                callCount++;
                await Task.Delay(1);
                return "updated";
            }

            // Act
            await cache.BlitzUpdate("key", UpdateFunctionAsync, 1000);

            // Assert
            Assert.AreEqual(0, callCount, "BlitzUpdate async on NullCache should not execute the function");
        }

        [Test]
        public void NullCache_Remove_DoesNothing()
        {
            // Arrange
            var cache = new NullBlitzCacheForTesting();

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => cache.Remove("any_key"));
        }

        [Test]
        public void NullCache_GetSemaphoreCount_ReturnsZero()
        {
            // Arrange
            var cache = new NullBlitzCacheForTesting();

            // Act
            var count = cache.GetSemaphoreCount();

            // Assert
            Assert.AreEqual(0, count, "NullCache should always return 0 semaphores");
        }

        [Test]
        public void NullCache_Dispose_DoesNotThrow()
        {
            // Arrange
            var cache = new NullBlitzCacheForTesting();

            // Act & Assert
            Assert.DoesNotThrow(() => cache.Dispose());
        }

        [Test]
        public void NullCache_WithAutomaticCacheKey_ExecutesEveryTime()
        {
            // Arrange
            var cache = new NullBlitzCacheForTesting();
            var callCount = 0;
            string TestFunction()
            {
                callCount++;
                return $"result_{callCount}";
            }

            // Act - Using automatic cache key (CallerMemberName)
            var result1 = cache.BlitzGet(TestFunction, 1000);
            var result2 = cache.BlitzGet(TestFunction, 1000);
            var result3 = cache.BlitzGet(TestFunction, 1000);

            // Assert
            Assert.AreEqual(3, callCount, "NullCache should execute function every time even with auto keys");
            Assert.AreEqual("result_1", result1);
            Assert.AreEqual("result_2", result2);
            Assert.AreEqual("result_3", result3);
        }
    }
}

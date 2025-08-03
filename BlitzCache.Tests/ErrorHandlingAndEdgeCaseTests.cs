using BlitzCacheCore;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class ErrorHandlingAndEdgeCaseTests
    {
        private IBlitzCache cache;

        [SetUp]
        public void Setup()
        {
            cache = new BlitzCache(useGlobalCache: false);
        }

        [TearDown]
        public void TearDown()
        {
            cache?.Dispose();
        }

        #region GetSemaphoreCount Tests
        
        [Test]
        public void GetSemaphoreCount_WithNewCache_ShouldReturnZero()
        {
            // Act
            var count = cache.GetSemaphoreCount();
            
            // Assert
            Assert.That(count, Is.EqualTo(0), "New cache should have zero semaphores");
        }

        [Test]
        public void GetSemaphoreCount_AfterCacheOperations_ShouldReturnAccurateCount()
        {
            // Arrange
            var initialCount = cache.GetSemaphoreCount();
            
            // Act - Create cache entries that will create semaphores
            cache.BlitzGet("key1", () => "value1", 10000);
            cache.BlitzGet("key2", () => "value2", 10000);
            cache.BlitzGet("key3", () => "value3", 10000);
            
            var finalCount = cache.GetSemaphoreCount();
            
            // Assert
            Assert.That(finalCount, Is.GreaterThan(initialCount), "Should have created semaphores");
            Assert.That(finalCount, Is.GreaterThanOrEqualTo(3), "Should have at least 3 semaphores for 3 keys");
        }

        [Test]
        public void GetSemaphoreCount_AfterDisposal_ShouldReturnZero()
        {
            // Arrange
            cache.BlitzGet("key1", () => "value1", 10000);
            Assert.That(cache.GetSemaphoreCount(), Is.GreaterThan(0), "Should have semaphores before disposal");
            
            // Act
            cache.Dispose();
            
            // Assert
            var count = cache.GetSemaphoreCount();
            Assert.That(count, Is.EqualTo(0), "Disposed cache should have zero semaphores");
        }

        #endregion

        #region Null Parameter Tests

        [Test]
        public void BlitzGet_WithNullFunction_ShouldThrowNullReferenceException()
        {
            // Act & Assert - BlitzCache doesn't validate null functions, throws NullReferenceException
            Assert.Throws<NullReferenceException>(() => 
                cache.BlitzGet("key", (Func<string>)null, 10000));
        }

        [Test]
        public async Task BlitzGet_WithNullAsyncFunction_ShouldThrowNullReferenceException()
        {
            // Act & Assert - BlitzCache doesn't validate null functions, throws NullReferenceException
            var exceptionThrown = false;
            try
            {
                await cache.BlitzGet("key", (Func<Task<string>>)null, 10000);
            }
            catch (NullReferenceException)
            {
                exceptionThrown = true;
            }
            
            Assert.IsTrue(exceptionThrown, "NullReferenceException should be thrown with null async function");
        }

        [Test]
        public void BlitzGet_WithNullNuancesFunction_ShouldThrowNullReferenceException()
        {
            // Act & Assert - BlitzCache doesn't validate null functions, throws NullReferenceException
            Assert.Throws<NullReferenceException>(() => 
                cache.BlitzGet("key", (Func<Nuances, string>)null, 10000));
        }

        [Test]
        public void BlitzUpdate_WithNullFunction_ShouldThrowNullReferenceException()
        {
            // Act & Assert - BlitzCache doesn't validate null functions, throws NullReferenceException
            Assert.Throws<NullReferenceException>(() => 
                cache.BlitzUpdate("key", (Func<string>)null, 10000));
        }

        #endregion

        #region Disposed Cache Tests

        [Test]
        public void BlitzGet_WithDisposedCache_ShouldThrowObjectDisposedException()
        {
            // Arrange
            cache.Dispose();
            
            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => 
                cache.BlitzGet("key", () => "value", 10000));
        }

        [Test]
        public void BlitzUpdate_WithDisposedCache_ShouldThrowObjectDisposedException()
        {
            // Arrange
            cache.Dispose();
            
            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => 
                cache.BlitzUpdate("key", () => "value", 10000));
        }

        [Test]
        public void Remove_WithDisposedCache_ShouldThrowObjectDisposedException()
        {
            // Arrange
            cache.Dispose();
            
            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => 
                cache.Remove("key"));
        }

        [Test]
        public void GetSemaphoreCount_WithDisposedCache_ShouldNotThrow()
        {
            // Arrange
            cache.Dispose();
            
            // Act & Assert - GetSemaphoreCount should be safe to call on disposed cache
            Assert.DoesNotThrow(() => cache.GetSemaphoreCount());
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void BlitzGet_WithEmptyKey_ShouldHandleGracefully()
        {
            // Act & Assert - Should not throw, empty key should be treated as valid
            Assert.DoesNotThrow(() => 
                cache.BlitzGet("", () => "value", 10000));
        }

        [Test]
        public void BlitzGet_WithNullKey_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                cache.BlitzGet((string)null, () => "value", 10000));
        }

        [Test]
        public void BlitzGet_WithZeroTimeout_ShouldNotCache()
        {
            // Arrange
            var callCount = 0;
            Func<string> function = () => { callCount++; return $"value_{callCount}"; };
            
            // Act - Call with zero timeout twice
            var result1 = cache.BlitzGet("key", function, 0);
            var result2 = cache.BlitzGet("key", function, 0);
            
            // Assert - Should call function twice since nothing is cached
            Assert.That(callCount, Is.EqualTo(2), "Function should be called twice with zero timeout");
            Assert.That(result1, Is.Not.EqualTo(result2), "Results should be different when not cached");
        }

        [Test]
        public void BlitzGet_WithNegativeTimeout_ShouldWork()
        {
            // Act & Assert - BlitzCache accepts negative timeouts without throwing
            Assert.DoesNotThrow(() => 
                cache.BlitzGet("key", () => "value", -1000));
        }

        [Test]
        public void BlitzUpdate_WithNegativeTimeout_ShouldWork()
        {
            // Act & Assert - BlitzCache accepts negative timeouts without throwing
            Assert.DoesNotThrow(() => 
                cache.BlitzUpdate("key", () => "value", -1000));
        }

        [Test]
        public void Remove_WithNonexistentKey_ShouldNotThrow()
        {
            // Act & Assert - Should handle gracefully
            Assert.DoesNotThrow(() => cache.Remove("nonexistent_key"));
        }

        [Test]
        public void Remove_WithNullKey_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => cache.Remove(null));
        }

        [Test]
        public void BlitzGet_WithExceptionInFunction_ShouldNotCache()
        {
            // Arrange
            var callCount = 0;
            Func<string> throwingFunction = () => 
            {
                callCount++;
                throw new InvalidOperationException("Test exception");
            };
            
            // Act & Assert - First call should throw
            Assert.Throws<InvalidOperationException>(() => 
                cache.BlitzGet("key", throwingFunction, 10000));
            
            // Second call should also throw (not cached)
            Assert.Throws<InvalidOperationException>(() => 
                cache.BlitzGet("key", throwingFunction, 10000));
            
            Assert.That(callCount, Is.EqualTo(2), "Exception should prevent caching");
        }

        #endregion

        #region Multiple Disposal Safety Tests

        [Test]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Act & Assert - Multiple disposal calls should be safe
            Assert.DoesNotThrow(() => cache.Dispose());
            Assert.DoesNotThrow(() => cache.Dispose());
            Assert.DoesNotThrow(() => cache.Dispose());
        }

        #endregion
    }
}

using BlitzCacheCore;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Examples
{
    /// <summary>
    /// Basic usage examples for BlitzCache demonstrating core functionality.
    /// These tests serve as documentation and examples for new users.
    /// </summary>
    [TestFixture]
    public class BasicUsageExamples
    {
        private IBlitzCache cache;

        [SetUp]
        public void Setup()
        {
            // Create a new cache instance for each test
            cache = new BlitzCache(useGlobalCache: false);
        }

        [TearDown]
        public void TearDown()
        {
            cache?.Dispose();
        }

        /// <summary>
        /// Example 1: Basic synchronous caching
        /// Shows how to cache the result of an expensive operation
        /// </summary>
        [Test]
        public void Example1_BasicSyncCaching()
        {
            // Simulate an expensive operation (database call, API request, computation, etc.)
            var callCount = 0;
            string ExpensiveOperation()
            {
                callCount++;
                System.Threading.Thread.Sleep(100); // Simulate work
                return $"Computed result #{callCount}";
            }

            // First call - BlitzCache will execute the function and cache the result
            var result1 = cache.BlitzGet("my_key", ExpensiveOperation, 30000); // Cache for 30 seconds
            
            // Second call - BlitzCache returns cached value instantly, no function execution!
            var result2 = cache.BlitzGet("my_key", ExpensiveOperation, 30000);
            
            // Verify caching worked - both results are identical, function only called once
            Assert.AreEqual("Computed result #1", result1);
            Assert.AreEqual("Computed result #1", result2); // Same cached result
            Assert.AreEqual(1, callCount, "Function should only be called once");
        }

        /// <summary>
        /// Example 2: Basic asynchronous caching
        /// Shows how to cache async operations
        /// </summary>
        [Test]
        public async Task Example2_BasicAsyncCaching()
        {
            // Simulate an expensive async operation (HTTP calls, database queries, etc.)
            var callCount = 0;
            async Task<string> ExpensiveAsyncOperation()
            {
                callCount++;
                await Task.Delay(100); // Simulate async work
                return $"Async result #{callCount}";
            }

            // BlitzCache works seamlessly with async/await - just use await!
            var result1 = await cache.BlitzGet("async_key", ExpensiveAsyncOperation, 30000);
            
            // Second call returns cached result, no async operation needed
            var result2 = await cache.BlitzGet("async_key", ExpensiveAsyncOperation, 30000);
            
            // Verify async caching worked perfectly
            Assert.AreEqual("Async result #1", result1);
            Assert.AreEqual("Async result #1", result2);
            Assert.AreEqual(1, callCount, "Async function should only be called once");
        }

        /// <summary>
        /// Example 3: Using different cache keys
        /// Shows how different keys maintain separate cached values
        /// </summary>
        [Test]
        public void Example3_DifferentCacheKeys()
        {
            string GetUserData(string userId)
            {
                return $"User data for {userId}";
            }

            // Different cache keys = different cached values
            // Perfect for user-specific data, API endpoints, etc.
            var user1Data = cache.BlitzGet("user_123", () => GetUserData("123"), 30000);
            var user2Data = cache.BlitzGet("user_456", () => GetUserData("456"), 30000);
            
            // Each key maintains its own separate cached value
            Assert.AreEqual("User data for 123", user1Data);
            Assert.AreEqual("User data for 456", user2Data);
            Assert.AreNotEqual(user1Data, user2Data);
        }

        /// <summary>
        /// Example 4: Cache expiration
        /// Shows how cached values expire after the specified time
        /// </summary>
        [Test]
        public void Example4_CacheExpiration()
        {
            var callCount = 0;
            string GetTimestamp()
            {
                callCount++;
                return DateTime.Now.ToString("HH:mm:ss.fff");
            }

            // Cache with very short expiration (100ms) - perfect for testing
            var result1 = cache.BlitzGet("timestamp", GetTimestamp, 100);
            
            // Immediate second call - returns cached value (no function execution)
            var result2 = cache.BlitzGet("timestamp", GetTimestamp, 100);
            Assert.AreEqual(result1, result2, "Should return cached value immediately");
            
            // Wait for cache to expire
            System.Threading.Thread.Sleep(150);
            
            // After expiration - BlitzCache automatically calls function again
            var result3 = cache.BlitzGet("timestamp", GetTimestamp, 100);
            
            Assert.AreNotEqual(result1, result3, "Should return new value after expiration");
            Assert.AreEqual(2, callCount, "Function should be called twice due to expiration");
        }

        /// <summary>
        /// Example 5: Manual cache removal
        /// Shows how to manually remove cached values
        /// </summary>
        [Test]
        public void Example5_ManualCacheRemoval()
        {
            var callCount = 0;
            string GetData()
            {
                callCount++;
                return $"Data #{callCount}";
            }

            // Cache some data normally
            var result1 = cache.BlitzGet("removable_key", GetData, 30000);
            Assert.AreEqual("Data #1", result1);
            
            // Verify it's cached (no function call)
            var result2 = cache.BlitzGet("removable_key", GetData, 30000);
            Assert.AreEqual("Data #1", result2);
            Assert.AreEqual(1, callCount, "Should still be only one call");
            
            // Manually remove from cache - useful for cache invalidation
            cache.Remove("removable_key");
            
            // Next call executes function again (cache was cleared)
            var result3 = cache.BlitzGet("removable_key", GetData, 30000);
            Assert.AreEqual("Data #2", result3);
            Assert.AreEqual(2, callCount, "Function should be called again after removal");
        }

        /// <summary>
        /// Example 6: Using BlitzUpdate to pre-populate cache
        /// Shows how to update cache values without retrieving them
        /// </summary>
        [Test]
        public void Example6_BlitzUpdate()
        {
            // Pre-populate cache with BlitzUpdate - great for cache warming!
            cache.BlitzUpdate("preloaded_key", () => "Preloaded value", 30000);
            
            // When we request it, the value is already there - zero wait time!
            var callCount = 0;
            string FallbackFunction()
            {
                callCount++;
                return "This should not be called";
            }
            
            var result = cache.BlitzGet("preloaded_key", FallbackFunction, 30000);
            
            Assert.AreEqual("Preloaded value", result);
            Assert.AreEqual(0, callCount, "Fallback function should not be called");
        }

        /// <summary>
        /// Example 7: Working with different data types
        /// Shows caching various types of data
        /// </summary>
        [Test]
        public void Example7_DifferentDataTypes()
        {
            // BlitzCache works with ANY data type - no configuration needed!
            
            // Cache a number
            var number = cache.BlitzGet("number_key", () => 42, 30000);
            Assert.AreEqual(42, number);
            
            // Cache a complex object (automatically serialized)
            var person = cache.BlitzGet("person_key", () => new { Name = "John", Age = 30 }, 30000);
            Assert.AreEqual("John", person.Name);
            Assert.AreEqual(30, person.Age);
            
            // Cache collections - arrays, lists, etc.
            var list = cache.BlitzGet("list_key", () => new[] { "a", "b", "c" }, 30000);
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual("a", list[0]);
        }

        /// <summary>
        /// Example 8: Dependency Injection usage
        /// Shows how to use BlitzCache with dependency injection
        /// </summary>
        [Test]
        public void Example8_DependencyInjection()
        {
            // BlitzCache integrates perfectly with Dependency Injection!
            // Setup is incredibly simple - just one line in your DI container:
            
            // In your Startup.cs or Program.cs:
            // services.AddBlitzCache(); // Uses default 60-second timeout
            // or customize:
            // services.AddBlitzCache(30000); // Specify default timeout in milliseconds
            
            // Then inject IBlitzCache anywhere you need caching:
            // public class UserService
            // {
            //     private readonly IBlitzCache _cache;
            //     
            //     public UserService(IBlitzCache cache) => _cache = cache;
            //     
            //     public async Task<User> GetUserAsync(int userId)
            //     {
            //         // One line adds caching to any method!
            //         return await _cache.BlitzGet($"user_{userId}", 
            //             () => _database.GetUserAsync(userId), 
            //             TimeSpan.FromMinutes(5).TotalMilliseconds);
            //     }
            // }
            
            // Simulation for this test
            IBlitzCache injectedCache = new BlitzCache();
            
            string SimulateServiceMethod(string key)
            {
                return injectedCache.BlitzGet(key, () => $"Service data for {key}", 30000);
            }
            
            var result = SimulateServiceMethod("test");
            Assert.AreEqual("Service data for test", result);
            
            injectedCache.Dispose();
        }
    }
}

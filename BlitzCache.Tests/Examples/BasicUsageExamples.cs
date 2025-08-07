
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Examples
{
    /// <summary>
    /// Basic usage examples for BlitzCache demonstrating how simple and effective caching can be.
    /// These examples show how BlitzCache "just works" for common caching scenarios.
    /// Perfect for developers who want to see how easy it is to add caching to their applications.
    /// </summary>
    [TestFixture]
    public class BasicUsageExamples
    {
        private IBlitzCacheInstance cache;

        [SetUp]
        public void Setup() => cache = new BlitzCacheInstance();

        [TearDown]
        public void TearDown() => cache?.Dispose();

        /// <summary>
        /// Example 1: Basic synchronous caching - it's this simple!
        /// Shows how to cache the result of any expensive operation with just one line
        /// </summary>
        [Test]
        public void Example1_BasicSyncCaching()
        {
            var callCount = 0;
            string ExpensiveOperation()
            {
                callCount++;
                TestDelays.ShortDelay(); // Simulate expensive work
                return $"Computed result #{callCount}";
            }

            // First call - BlitzCache executes function and caches the result
            var result1 = cache.BlitzGet("my_key", ExpensiveOperation, TestConstants.StandardTimeoutMs);

            // Second call - BlitzCache returns cached value instantly!
            var result2 = cache.BlitzGet("my_key", ExpensiveOperation, TestConstants.StandardTimeoutMs);

            // Verify caching worked - same result, function only called once
            Assert.AreEqual("Computed result #1", result1);
            Assert.AreEqual("Computed result #1", result2);
            Assert.AreEqual(1, callCount, "Function should only be called once");
        }

        /// <summary>
        /// Example 2: Async operations work seamlessly
        /// Shows how BlitzCache works perfectly with async/await - no special setup needed!
        /// </summary>
        [Test]
        public async Task Example2_BasicAsyncCaching()
        {
            var callCount = 0;
            async Task<string> ExpensiveAsyncOperation()
            {
                callCount++;
                await TestDelays.ShortDelay(); // Simulate async work
                return $"Async result #{callCount}";
            }

            // BlitzCache works seamlessly with async - just add await!
            var result1 = await cache.BlitzGet("async_key", ExpensiveAsyncOperation, TestConstants.StandardTimeoutMs);
            var result2 = await cache.BlitzGet("async_key", ExpensiveAsyncOperation, TestConstants.StandardTimeoutMs);

            Assert.AreEqual("Async result #1", result1);
            Assert.AreEqual("Async result #1", result2);
            Assert.AreEqual(1, callCount, "Async function should only be called once");
        }

        /// <summary>
        /// Example 3: Different keys = different cached values
        /// Perfect for user-specific data, API endpoints, or any scenario-based caching
        /// </summary>
        [Test]
        public void Example3_DifferentCacheKeys()
        {
            string GetUserData(string userId)
            {
                return $"User data for {userId}";
            }

            // Different cache keys maintain completely separate cached values
            var user1Data = cache.BlitzGet("user_123", () => GetUserData("123"), TestConstants.StandardTimeoutMs);
            var user2Data = cache.BlitzGet("user_456", () => GetUserData("456"), TestConstants.StandardTimeoutMs);

            Assert.AreEqual("User data for 123", user1Data);
            Assert.AreEqual("User data for 456", user2Data);
            Assert.AreNotEqual(user1Data, user2Data);
        }

        /// <summary>
        /// Example 4: Cache expiration happens automatically
        /// BlitzCache automatically refreshes expired values - you don't need to worry about it!
        /// </summary>
        [Test]
        public async Task Example4_CacheExpiration()
        {
            var callCount = 0;
            string GetTimestamp()
            {
                callCount++;
                return DateTime.Now.ToString("HH:mm:ss.fff");
            }

            // Cache with short expiration for demo purposes
            var result1 = cache.BlitzGet("timestamp", GetTimestamp, TestConstants.StandardTimeoutMs);
            var result2 = cache.BlitzGet("timestamp", GetTimestamp, TestConstants.StandardTimeoutMs);
            Assert.AreEqual(result1, result2, "Should return cached value immediately");

            // Wait for cache to expire
            await TestDelays.WaitForStandardExpiration();

            // BlitzCache automatically calls function again after expiration
            var result3 = cache.BlitzGet("timestamp", GetTimestamp, TestConstants.StandardTimeoutMs);

            Assert.AreNotEqual(result1, result3, "Should return new value after expiration");
            Assert.AreEqual(2, callCount, "Function should be called twice due to expiration");
        }

        /// <summary>
        /// Example 5: Manual cache control when you need it
        /// Sometimes you need to invalidate cache manually - BlitzCache makes it simple
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
            var result1 = cache.BlitzGet("removable_key", GetData, TestConstants.StandardTimeoutMs);
            var result2 = cache.BlitzGet("removable_key", GetData, TestConstants.StandardTimeoutMs);
            Assert.AreEqual("Data #1", result1);
            Assert.AreEqual("Data #1", result2);
            Assert.AreEqual(1, callCount, "Should still be only one call");

            // Manually remove from cache when you need fresh data
            cache.Remove("removable_key");

            // Next call executes function again with fresh data
            var result3 = cache.BlitzGet("removable_key", GetData, TestConstants.StandardTimeoutMs);
            Assert.AreEqual("Data #2", result3);
            Assert.AreEqual(2, callCount, "Function should be called again after removal");
        }

        /// <summary>
        /// Example 6: Pre-populate cache for instant access
        /// Use BlitzUpdate to warm up your cache - great for application startup!
        /// </summary>
        [Test]
        public void Example6_BlitzUpdate()
        {
            // Pre-populate cache with BlitzUpdate - perfect for cache warming!
            cache.BlitzUpdate("preloaded_key", () => "Preloaded value", TestConstants.StandardTimeoutMs);

            // When requested, the value is already there - zero wait time!
            var callCount = 0;
            string FallbackFunction()
            {
                callCount++;
                return "This should not be called";
            }

            var result = cache.BlitzGet("preloaded_key", FallbackFunction, TestConstants.StandardTimeoutMs);

            Assert.AreEqual("Preloaded value", result);
            Assert.AreEqual(0, callCount, "Fallback function should not be called");
        }

        /// <summary>
        /// Example 7: Works with any data type automatically
        /// BlitzCache handles any .NET object - numbers, strings, classes, collections, everything!
        /// </summary>
        [Test]
        public void Example7_AnyDataType()
        {
            // Cache a number - works perfectly
            var number = cache.BlitzGet("number_key", () => 42, TestConstants.StandardTimeoutMs);
            Assert.AreEqual(42, number);

            // Cache a complex object - automatic serialization
            var person = cache.BlitzGet("person_key", () => new { Name = "John", Age = 30 }, TestConstants.StandardTimeoutMs);
            Assert.AreEqual("John", person.Name);
            Assert.AreEqual(30, person.Age);

            // Cache collections - arrays, lists, whatever you need
            var list = cache.BlitzGet("list_key", () => new[] { "a", "b", "c" }, TestConstants.StandardTimeoutMs);
            Assert.AreEqual(3, list.Length);
            Assert.AreEqual("a", list[0]);
        }

        /// <summary>
        /// Example 8: Global cache for application-wide caching
        /// Use BlitzCache.Global for simple application-wide caching with zero setup
        /// </summary>
        [Test]
        public void Example8_GlobalCache()
        {
            var cache = new BlitzCache();
            var callCount = 0;
            string LoadData()
            {
                callCount++;
                return $"Global data #{callCount}";
            }

            // Access the global cache directly - perfect for simple scenarios
            var result1 = cache.BlitzGet("global_key", LoadData, TestConstants.StandardTimeoutMs);
            var result2 = cache.BlitzGet("global_key", LoadData, TestConstants.StandardTimeoutMs);

            Assert.AreEqual("Global data #1", result1);
            Assert.AreEqual("Global data #1", result2);
            Assert.AreEqual(1, callCount, "Function should only be called once");

            // Clean up for this demo
            cache.Remove("global_key");
        }

        /// <summary>
        /// Example 9: Automatic cache keys from method context
        /// BlitzCache can automatically generate cache keys from your method name and file!
        /// </summary>
        [Test]
        public void Example9_AutomaticCacheKeys()
        {
            var callCount = 0;
            string GetData()
            {
                callCount++;
                return $"Method data #{callCount}";
            }

            // No cache key needed - BlitzCache uses method name and file path automatically!
            var result1 = cache.BlitzGet(GetData, TestConstants.StandardTimeoutMs);
            var result2 = cache.BlitzGet(GetData, TestConstants.StandardTimeoutMs);

            Assert.AreEqual("Method data #1", result1);
            Assert.AreEqual("Method data #1", result2);
            Assert.AreEqual(1, callCount, "Function should only be called once with automatic key");
        }

        /// <summary>
        /// Example 10: Real-world example - caching expensive database calls
        /// See how BlitzCache transforms slow operations into lightning-fast responses
        /// </summary>
        [Test]
        public void Example10_RealWorldExample()
        {
            var databaseCallCount = 0;
            string SimulateExpensiveDatabaseCall(int userId)
            {
                databaseCallCount++;
                TestDelays.ShortDelay(); // Simulate database latency
                return $"User profile for ID: {userId} (DB call #{databaseCallCount})";
            }

            // Simulate multiple requests for the same user data (common in web apps)
            var profile1 = cache.BlitzGet("user_profile_123", () => SimulateExpensiveDatabaseCall(123), TestConstants.StandardTimeoutMs);
            var profile2 = cache.BlitzGet("user_profile_123", () => SimulateExpensiveDatabaseCall(123), TestConstants.StandardTimeoutMs);
            var profile3 = cache.BlitzGet("user_profile_123", () => SimulateExpensiveDatabaseCall(123), TestConstants.StandardTimeoutMs);

            // All requests return the same cached data
            Assert.AreEqual("User profile for ID: 123 (DB call #1)", profile1);
            Assert.AreEqual("User profile for ID: 123 (DB call #1)", profile2);
            Assert.AreEqual("User profile for ID: 123 (DB call #1)", profile3);

            // Database was only called once despite 3 requests - huge performance gain!
            Assert.AreEqual(1, databaseCallCount, "Database should only be called once thanks to caching!");

            // Different user = different cache entry
            var otherProfile = cache.BlitzGet("user_profile_456", () => SimulateExpensiveDatabaseCall(456), TestConstants.StandardTimeoutMs);
            Assert.AreEqual("User profile for ID: 456 (DB call #2)", otherProfile);
            Assert.AreEqual(2, databaseCallCount, "Different user should trigger new database call");
        }

        /// <summary>
        /// Example 11: Accessing Top Slowest Queries statistics
        /// Demonstrates how to retrieve and inspect the slowest cache operations using BlitzCache statistics (v2.0.2+)
        /// </summary>
        [Test]
        public async Task Example11_TopSlowestQueries()
        {
            cache.InitializeStatistics();
            // Simulate several cache operations with varying delays
            for (int i = 0; i < 3; i++)
            {
                await cache.BlitzGet($"slow_key_{i}", async () =>
                {
                    await Task.Delay(TestConstants.EvictionCallbackWaitMs + i * 20); // 30ms, 50ms, 70ms
                    return $"Value {i}";
                });
            }

            // Access the TopSlowestQueries statistics
            var stats = cache.Statistics;
            Assert.IsNotNull(stats, "Statistics should not be null");
            Assert.IsNotNull(stats.TopSlowestQueries, "TopSlowestQueries should not be null");
            Assert.IsTrue(stats.TopSlowestQueries.Count() > 0, "TopSlowestQueries should contain entries");

            // Optionally, check that the slowest query is the one with the highest delay
            var slowest = stats.TopSlowestQueries.First();
            StringAssert.Contains("slow_key_2", slowest.ToString());
        }
    }
}


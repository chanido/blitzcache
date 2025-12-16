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
            Assert.That(result1, Is.EqualTo("Computed result #1"));
            Assert.That(result2, Is.EqualTo("Computed result #1"));
            Assert.That(callCount, Is.EqualTo(1), "Function should only be called once");
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

            Assert.That(result1, Is.EqualTo("Async result #1"));
            Assert.That(result2, Is.EqualTo("Async result #1"));
            Assert.That(callCount, Is.EqualTo(1), "Async function should only be called once");
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

            Assert.That(user1Data, Is.EqualTo("User data for 123"));
            Assert.That(user2Data, Is.EqualTo("User data for 456"));
            Assert.That(user1Data, Is.Not.EqualTo(user2Data));
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
            var result1 = cache.BlitzGet("timestamp", GetTimestamp, TestConstants.VeryShortTimeoutMs);
            var result2 = cache.BlitzGet("timestamp", GetTimestamp, TestConstants.VeryShortTimeoutMs);
            Assert.That(result2, Is.EqualTo(result1), "Should return cached value immediately");

            // Wait for cache to expire
            await TestDelays.WaitForStandardExpiration();

            // BlitzCache automatically calls function again after expiration
            var result3 = cache.BlitzGet("timestamp", GetTimestamp, TestConstants.VeryShortTimeoutMs);

            Assert.That(result3, Is.Not.EqualTo(result1), "Should return new value after expiration");
            Assert.That(callCount, Is.EqualTo(2), "Function should be called twice due to expiration");
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
            Assert.That(result1, Is.EqualTo("Data #1"));
            Assert.That(result2, Is.EqualTo("Data #1"));
            Assert.That(callCount, Is.EqualTo(1), "Should still be only one call");

            // Manually remove from cache when you need fresh data
            cache.Remove("removable_key");

            // Next call executes function again with fresh data
            var result3 = cache.BlitzGet("removable_key", GetData, TestConstants.StandardTimeoutMs);
            Assert.That(result3, Is.EqualTo("Data #2"));
            Assert.That(callCount, Is.EqualTo(2), "Function should be called again after removal");
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

            Assert.That(result, Is.EqualTo("Preloaded value"));
            Assert.That(callCount, Is.EqualTo(0), "Fallback function should not be called");
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
            Assert.That(number, Is.EqualTo(42));

            // Cache a complex object - automatic serialization
            var person = cache.BlitzGet("person_key", () => new { Name = "John", Age = 30 }, TestConstants.StandardTimeoutMs);
            Assert.That(person.Name, Is.EqualTo("John"));
            Assert.That(person.Age, Is.EqualTo(30));

            // Cache collections - arrays, lists, whatever you need
            var list = cache.BlitzGet("list_key", () => new[] { "a", "b", "c" }, TestConstants.StandardTimeoutMs);
            Assert.That(list.Length, Is.EqualTo(3));
            Assert.That(list[0], Is.EqualTo("a"));
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

            Assert.That(result1, Is.EqualTo("Global data #1"));
            Assert.That(result2, Is.EqualTo("Global data #1"));
            Assert.That(callCount, Is.EqualTo(1), "Function should only be called once");

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

            Assert.That(result1, Is.EqualTo("Method data #1"));
            Assert.That(result2, Is.EqualTo("Method data #1"));
            Assert.That(callCount, Is.EqualTo(1), "Function should only be called once with automatic key");
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
            Assert.That(profile1, Is.EqualTo("User profile for ID: 123 (DB call #1)"));
            Assert.That(profile2, Is.EqualTo("User profile for ID: 123 (DB call #1)"));
            Assert.That(profile3, Is.EqualTo("User profile for ID: 123 (DB call #1)"));

            // Database was only called once despite 3 requests - huge performance gain!
            Assert.That(databaseCallCount, Is.EqualTo(1), "Database should only be called once thanks to caching!");

            // Different user = different cache entry
            var otherProfile = cache.BlitzGet("user_profile_456", () => SimulateExpensiveDatabaseCall(456), TestConstants.StandardTimeoutMs);
            Assert.That(otherProfile, Is.EqualTo("User profile for ID: 456 (DB call #2)"));
            Assert.That(databaseCallCount, Is.EqualTo(2), "Different user should trigger new database call");
        }

        /// <summary>
        /// Example 11: Thundering Herd / Cache Stampede Prevention
        /// DEMONSTRATES THE CORE PROBLEM BLITZCACHE SOLVES:
        /// Without BlitzCache: 100 concurrent requests = 100 database executions
        /// With BlitzCache: 100 concurrent requests = 1 database execution, 99 wait for result
        /// This is the "cache stampede" or "thundering herd" problem that BlitzCache automatically prevents.
        /// </summary>
        [Test]
        public async Task Example11_ThunderingHerdPrevention()
        {
            var databaseCallCount = 0;
            var executionTimes = new System.Collections.Generic.List<DateTime>();

            async Task<string> ExpensiveDatabaseQuery()
            {
                // Record when this executes
                lock (executionTimes)
                {
                    executionTimes.Add(DateTime.Now);
                }
                
                System.Threading.Interlocked.Increment(ref databaseCallCount);
                await Task.Delay(100); // Simulate slow database query (100ms)
                return $"Expensive database result (execution #{databaseCallCount})";
            }

            Console.WriteLine("=== Thundering Herd / Cache Stampede Prevention Demo ===");
            Console.WriteLine("Simulating 100 concurrent requests for the same data...");
            Console.WriteLine();

            // THE PROBLEM SCENARIO:
            // Imagine 100 users hit your API at the exact same moment requesting the same data
            // Without proper protection, all 100 would execute your expensive database query
            // This is called "cache stampede", "thundering herd", or "dog-pile effect"

            var concurrentRequestCount = 100; // 100 concurrent requests
            var tasks = new Task<string>[concurrentRequestCount];

            var startTime = DateTime.Now;

            // Launch 100 concurrent requests for the same cache key
            for (int i = 0; i < concurrentRequestCount; i++)
            {
                var requestNumber = i + 1;
                tasks[i] = Task.Run(async () =>
                {
                    // Each request tries to get the same data
                    // WITHOUT BlitzCache: All 100 would hit the database
                    // WITH BlitzCache: Only 1 hits the database, 99 wait and share the result
                    var result = await cache.BlitzGet("shared_expensive_data", ExpensiveDatabaseQuery, TestConstants.StandardTimeoutMs);
                    return result;
                });
            }

            // Wait for all concurrent requests to complete
            var results = await Task.WhenAll(tasks);
            var endTime = DateTime.Now;
            var totalTime = (endTime - startTime).TotalMilliseconds;

            Console.WriteLine($"✅ All {concurrentRequestCount} concurrent requests completed in {totalTime:F0}ms");
            Console.WriteLine();
            Console.WriteLine("=== BlitzCache Thundering Herd Protection Results ===");
            Console.WriteLine($"Total concurrent requests: {concurrentRequestCount}");
            Console.WriteLine($"Actual database executions: {databaseCallCount}");
            Console.WriteLine($"Database queries prevented: {concurrentRequestCount - databaseCallCount}");
            Console.WriteLine($"Efficiency gain: {((concurrentRequestCount - databaseCallCount) / (double)concurrentRequestCount * 100):F1}% reduction in database load");
            Console.WriteLine();

            // VERIFICATION: BlitzCache ensures only ONE execution despite concurrent requests
            Assert.That(databaseCallCount, Is.EqualTo(1), 
                $"THUNDERING HERD PREVENTION: Only 1 database call should execute despite {concurrentRequestCount} concurrent requests!");
            
            Assert.That(executionTimes.Count, Is.EqualTo(1), 
                "Only one execution timestamp should exist");

            // All requests should return the exact same result
            Assert.That(results.All(r => r == results[0]), Is.True, 
                "All concurrent requests should receive identical results");
            
            Assert.That(results[0], Is.EqualTo("Expensive database result (execution #1)"),
                "Result should be from the single execution");

            Console.WriteLine("✨ BlitzCache successfully prevented cache stampede!");
            Console.WriteLine($"   Instead of {concurrentRequestCount} expensive database queries,");
            Console.WriteLine("   only 1 executed while the other 99 waited and shared the result.");
            Console.WriteLine();
            Console.WriteLine("💡 This is what BlitzCache does automatically:");
            Console.WriteLine("   - Prevents duplicate execution under concurrent load");
            Console.WriteLine("   - Protects your database/API from being overwhelmed");
            Console.WriteLine("   - Eliminates race conditions in cache population");
            Console.WriteLine("   - Zero configuration, completely thread-safe");
            Console.WriteLine();
            Console.WriteLine("🎯 Without BlitzCache, you'd need 15+ lines of SemaphoreSlim");
            Console.WriteLine("   boilerplate PER CACHED OPERATION to achieve this protection.");
            Console.WriteLine("   With BlitzCache: Just use BlitzGet() and it works automatically!");
        }

        /// <summary>
        /// Example 12: Accessing Top Slowest Queries statistics
        /// Demonstrates how to retrieve and inspect the slowest cache operations using BlitzCache statistics (v2.0.2+)
        /// </summary>
        [Test]
        public async Task Example12_TopSlowestQueries()
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
            Assert.That(stats, Is.Not.Null, "Statistics should not be null");
            Assert.That(stats.TopSlowestQueries, Is.Not.Null, "TopSlowestQueries should not be null");
            Assert.That(stats.TopSlowestQueries.Count() > 0, Is.True, "TopSlowestQueries should contain entries");

            // Optionally, check that the slowest query is the one with the highest delay
            var slowest = stats.TopSlowestQueries.First();
            Assert.That(slowest.ToString(), Does.Contain("slow_key_2"));
        }
    }
}


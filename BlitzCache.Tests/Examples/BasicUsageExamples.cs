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
            cache = new BlitzCache(enableStatistics: true);
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
            
            // === OPTION 1: Global Shared Cache (Recommended for most scenarios) ===
            // In your Startup.cs or Program.cs:
            // services.AddBlitzCache(); // Uses global singleton with default 60-second timeout
            // services.AddBlitzCache(30000); // Customize default timeout
            // services.AddBlitzCache(60000, enableStatistics: true); // Enable performance monitoring
            
            // === OPTION 2: Dedicated Cache Instance ===
            // For scenarios where you need isolated caching:
            // services.AddBlitzCacheInstance(); // Creates a dedicated instance
            // services.AddBlitzCacheInstance(45000, enableStatistics: true); // With custom settings
            
            // === Usage in Your Services ===
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
            
            // === Direct Global Access (Alternative Pattern) ===
            // You can also access the global cache directly without DI:
            // var globalResult = BlitzCache.Global.BlitzGet("global_key", () => "Global Data", 30000);
            
            // Simulation for this test
            IBlitzCache injectedCache = new BlitzCache();
            
            string SimulateServiceMethod(string key)
            {
                return injectedCache.BlitzGet(key, () => $"Service data for {key}", 30000);
            }
            
            var result = SimulateServiceMethod("test");
            Assert.AreEqual("Service data for test", result);
            
            // Test global access pattern
            var globalResult = BlitzCache.Global.BlitzGet("global_example", () => "Global cached data", 30000);
            Assert.AreEqual("Global cached data", globalResult);
            
            injectedCache.Dispose();
        }

        /// <summary>
        /// Example 9: Simple Dependency Injection with Defaults
        /// Shows the most basic DI setup - just add one line to get started!
        /// </summary>
        [Test]
        public void Example9_SimpleDependencyInjection()
        {
            // === Quick Start Guide ===
            // Add this ONE line to your DI container and you're ready to go:
            // services.AddBlitzCache();
            
            // That's it! BlitzCache will:
            // ✓ Use the global singleton (shared across your entire app)
            // ✓ Default to 60-second cache timeout
            // ✓ Be available as IBlitzCache in all your services
            
            // === In your service classes ===
            // public class ProductService
            // {
            //     private readonly IBlitzCache _cache;
            //     
            //     public ProductService(IBlitzCache cache) => _cache = cache;
            //     
            //     public Product GetProduct(int id) =>
            //         _cache.BlitzGet($"product_{id}", () => LoadFromDatabase(id));
            //         // Uses default 60-second timeout - perfect for most scenarios!
            // }
            
            // Demo: Simulate the injected cache behavior
            var simulatedDICache = BlitzCache.Global; // This is what DI would inject
            
            var callCount = 0;
            string LoadExpensiveData()
            {
                callCount++;
                return $"Loaded data (call #{callCount})";
            }
            
            // First call - loads and caches data
            var result1 = simulatedDICache.BlitzGet("simple_key", LoadExpensiveData);
            
            // Second call - returns cached data instantly
            var result2 = simulatedDICache.BlitzGet("simple_key", LoadExpensiveData);
            
            Assert.AreEqual("Loaded data (call #1)", result1);
            Assert.AreEqual("Loaded data (call #1)", result2); // Same cached result
            Assert.AreEqual(1, callCount, "Function should only be called once due to caching");
            
            // Note: Global cache persists across tests, so we clean up for this demo
            simulatedDICache.Remove("simple_key");
        }

        /// <summary>
        /// Example 10: Cache Statistics and Monitoring
        /// Shows how to monitor cache performance and effectiveness
        /// </summary>
        [Test]
        public void Example10_CacheStatisticsAndMonitoring()
        {
            // Simulate a data service with expensive operations
            var databaseCallCount = 0;
            string GetUserProfile(int userId)
            {
                databaseCallCount++; // Track actual database calls
                System.Threading.Thread.Sleep(50); // Simulate database latency
                return $"User Profile for ID: {userId}";
            }

            // Perform various cache operations
            Console.WriteLine("=== Cache Statistics Example ===");
            
            // Capture initial statistics
            var initialHitCount = cache.Statistics.HitCount;
            var initialMissCount = cache.Statistics.MissCount;
            var initialTotalOperations = cache.Statistics.TotalOperations;
            var initialEntryCount = cache.Statistics.EntryCount;
            var initialEvictionCount = cache.Statistics.EvictionCount;
            
            Console.WriteLine($"Initial state: {initialTotalOperations} operations, {cache.Statistics.HitRatio:P1} hit ratio");

            // First access - will be a cache miss
            var profile1 = cache.BlitzGet("user_123", () => GetUserProfile(123), 60000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute
            Console.WriteLine($"First access result: {profile1}");
            
            var statsAfterFirst = cache.Statistics;
            
            // Second access to same key - will be a cache hit
            var hitCountBeforeSecond = cache.Statistics.HitCount;
            var totalOperationsBeforeSecond = cache.Statistics.TotalOperations;
            
            var profile2 = cache.BlitzGet("user_123", () => GetUserProfile(123), 60000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute
            Console.WriteLine($"Second access result: {profile2}");
            
            var statsAfterSecond = cache.Statistics;
            
            // Access different key - will be another miss
            var missCountBeforeThird = cache.Statistics.MissCount;
            var entryCountBeforeThird = cache.Statistics.EntryCount;
            
            var profile3 = cache.BlitzGet("user_456", () => GetUserProfile(456), 60000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute
            Console.WriteLine($"Different key result: {profile3}");
            
            var statsAfterThird = cache.Statistics;
            
            // Another hit on first key
            var hitCountBeforeFourth = cache.Statistics.HitCount;
            
            var profile4 = cache.BlitzGet("user_123", () => GetUserProfile(123), 60000);
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute
            Console.WriteLine($"Third access to first key: {profile4}");

            // Check final statistics
            var finalStats = cache.Statistics;
            Console.WriteLine("\n=== Final Statistics ===");
            Console.WriteLine($"Total Operations: {finalStats.TotalOperations}");
            Console.WriteLine($"Cache Hits: {finalStats.HitCount}");
            Console.WriteLine($"Cache Misses: {finalStats.MissCount}");
            Console.WriteLine($"Hit Ratio: {finalStats.HitRatio:P1}");
            Console.WriteLine($"Current Cached Entries: {finalStats.EntryCount}");
            Console.WriteLine($"Active Semaphores: {finalStats.ActiveSemaphoreCount}");
            Console.WriteLine($"Database Calls Made: {databaseCallCount}");

            // Verify cache effectiveness using final statistics
            Assert.AreEqual(4, finalStats.TotalOperations, "Should have 4 total operations");
            Assert.AreEqual(2, finalStats.HitCount, "Should have 2 cache hits");
            Assert.AreEqual(2, finalStats.MissCount, "Should have 2 cache misses");
            Assert.AreEqual(0.5, finalStats.HitRatio, 0.001, "Hit ratio should be 50%");
            Assert.AreEqual(2, finalStats.EntryCount, "Should have 2 cached entries");
            Assert.AreEqual(2, databaseCallCount, "Database should only be called twice (cache working!)");

            // Demonstrate manual cache eviction tracking
            var evictionCountBeforeRemoval = cache.Statistics.EvictionCount;
            var entryCountBeforeRemoval = cache.Statistics.EntryCount;
            
            cache.Remove("user_123");
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute
            
            var statsAfterRemoval = cache.Statistics;
            Console.WriteLine($"\nAfter removing user_123: {statsAfterRemoval.EvictionCount} evictions, {statsAfterRemoval.EntryCount} entries");
            
            Assert.AreEqual(evictionCountBeforeRemoval + 1, statsAfterRemoval.EvictionCount, "Should have 1 eviction");
            Assert.AreEqual(entryCountBeforeRemoval - 1, statsAfterRemoval.EntryCount, "Should have 1 less entry");

            // Reset statistics for monitoring specific time periods
            cache.Statistics.Reset();
            System.Threading.Thread.Sleep(10); // Ensure eviction callback has time to execute
            
            var resetStats = cache.Statistics;
            Console.WriteLine($"After reset: {resetStats.TotalOperations} operations, {resetStats.HitRatio:P1} hit ratio");
            
            Assert.AreEqual(0, resetStats.TotalOperations, "Operations should be reset to zero");
            Assert.AreEqual(0.0, resetStats.HitRatio, 0.001, "Hit ratio should be reset to zero");
        }
    }
}

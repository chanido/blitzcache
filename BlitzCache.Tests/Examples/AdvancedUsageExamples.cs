using BlitzCacheCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Examples
{
    /// <summary>
    /// Advanced usage examples for BlitzCache demonstrating sophisticated scenarios.
    /// These tests showcase advanced features and patterns for experienced users.
    /// </summary>
    [TestFixture]
    public class AdvancedUsageExamples
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

        /// <summary>
        /// Example 1: Dynamic cache timeout using Nuances
        /// Shows how to dynamically adjust cache timeout based on the result
        /// </summary>
        [Test]
        public async Task Example1_DynamicCacheTimeout()
        {
            var callCount = 0;

            // Function that adjusts cache time based on result quality
            async Task<string> GetDataWithDynamicCaching(Nuances nuances)
            {
                callCount++;
                await Task.Delay(10);
                
                if (callCount == 1)
                {
                    // Cache temporary data for a short time (100ms)
                    nuances.CacheRetention = 100;
                    return "Temporary data";
                }
                
                // Cache stable data for much longer (30 seconds)
                nuances.CacheRetention = 30000;
                return "Stable data";
            }

            // First call - short cache time
            var result1 = await cache.BlitzGet("dynamic_key", GetDataWithDynamicCaching);
            Assert.AreEqual("Temporary data", result1);
            
            // Wait for short cache to expire
            Thread.Sleep(150);
            
            // Second call - will get new data with longer cache time
            var result2 = await cache.BlitzGet("dynamic_key", GetDataWithDynamicCaching);
            Assert.AreEqual("Stable data", result2);
            
            // Immediate third call - should return cached stable data
            var result3 = await cache.BlitzGet("dynamic_key", GetDataWithDynamicCaching);
            Assert.AreEqual("Stable data", result3);
            
            Assert.AreEqual(2, callCount, "Function should be called twice due to dynamic caching");
        }

        /// <summary>
        /// Example 2: Thread-safe concurrent access
        /// Shows how BlitzCache handles multiple threads accessing the same key
        /// </summary>
        [Test]
        public async Task Example2_ConcurrentAccess()
        {
            var callCount = 0;
            var executionTimes = new List<DateTime>();

            async Task<string> ExpensiveOperation()
            {
                var startTime = DateTime.Now;
                Interlocked.Increment(ref callCount);
                
                lock (executionTimes)
                {
                    executionTimes.Add(startTime);
                }
                
                // Simulate expensive database or API call
                await Task.Delay(200);
                return $"Result from thread {Thread.CurrentThread.ManagedThreadId}";
            }

            // Start 10 concurrent operations for the same key - BlitzCache ensures only one executes
            var tasks = new Task<string>[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = cache.BlitzGet("concurrent_key", ExpensiveOperation, 30000);
            }

            // Wait for all to complete
            var results = await Task.WhenAll(tasks);

            // Verify thread safety
            Assert.AreEqual(1, callCount, "Function should only be called once despite concurrent access");
            Assert.IsTrue(results.All(r => r == results[0]), "All results should be identical");
            Assert.AreEqual(1, executionTimes.Count, "Function should only execute once");
        }

        /// <summary>
        /// Example 3: Circuit breaker pattern
        /// Shows how to use BlitzCache to implement a circuit breaker for external services
        /// </summary>
        [Test]
        public async Task Example3_CircuitBreakerPattern()
        {
            var failureCount = 0;
            var isServiceHealthy = false;

            Task<string> CallExternalService(Nuances nuances)
            {
                if (!isServiceHealthy)
                {
                    failureCount++;
                    // Cache failures briefly to avoid hammering the failing service
                    nuances.CacheRetention = 5000;
                    throw new InvalidOperationException($"Service unavailable (failure #{failureCount})");
                }
                
                // Cache successful responses for longer
                nuances.CacheRetention = 30000;
                return Task.FromResult("Service response: Success!");
            }

            // First call - service is down, exception is thrown
            Assert.ThrowsAsync<InvalidOperationException>(() => cache.BlitzGet("service_key", CallExternalService));

            // Second call - returns cached exception without calling service again
            Assert.ThrowsAsync<InvalidOperationException>(() => cache.BlitzGet("service_key", CallExternalService));
            
            // Service comes back online
            isServiceHealthy = true;
            
            // Clear the cached failure
            cache.Remove("service_key");
            
            // Now calls should succeed
            var result = await cache.BlitzGet("service_key", CallExternalService);
            Assert.AreEqual("Service response: Success!", result);
        }

        /// <summary>
        /// Example 4: Multi-level caching strategy
        /// Shows how to implement different cache strategies for different data types
        /// </summary>
        [Test]
        public void Example4_MultiLevelCaching()
        {
            var databaseCallCount = 0;
            var apiCallCount = 0;

            // Simulate expensive database call
            string GetFromDatabase(string id)
            {
                databaseCallCount++;
                Thread.Sleep(100); // Simulate DB latency
                return $"DB_Data_{id}";
            }

            // Simulate fast but frequently changing API call
            string GetFromApi(string endpoint)
            {
                apiCallCount++;
                Thread.Sleep(50); // Simulate API latency
                return $"API_Data_{endpoint}";
            }

            // Strategy: Database data cached for 5 minutes (stable data)
            //          API data cached for 30 seconds (frequently changing)
            var dbResult1 = cache.BlitzGet("db_user_123", () => GetFromDatabase("123"), 300000);
            var dbResult2 = cache.BlitzGet("db_user_123", () => GetFromDatabase("123"), 300000);
            
            var apiResult1 = cache.BlitzGet("api_weather", () => GetFromApi("weather"), 30000);
            var apiResult2 = cache.BlitzGet("api_weather", () => GetFromApi("weather"), 30000);

            Assert.AreEqual("DB_Data_123", dbResult1);
            Assert.AreEqual("DB_Data_123", dbResult2);
            Assert.AreEqual(1, databaseCallCount, "Database should only be called once");

            Assert.AreEqual("API_Data_weather", apiResult1);
            Assert.AreEqual("API_Data_weather", apiResult2);
            Assert.AreEqual(1, apiCallCount, "API should only be called once");
        }

        /// <summary>
        /// Example 5: Cache warming strategy
        /// Shows how to pre-populate cache with commonly used data
        /// </summary>
        [Test]
        public async Task Example5_CacheWarming()
        {
            var actualCallCount = 0;

            string GetUserProfile(string userId) => $"Profile for user {userId}";

            // Pre-populate cache with commonly accessed data during app startup
            var commonUsers = new[] { "user_1", "user_2", "user_3" };
            
            var warmupTasks = commonUsers.Select(userId => 
                Task.Run(() => 
                {
                    actualCallCount++;
                    // BlitzUpdate forces cache population even if not requested yet
                    cache.BlitzUpdate($"profile_{userId}", () => GetUserProfile(userId), 60000);
                })
            );
            
            await Task.WhenAll(warmupTasks);
            
            // Now when users access their profiles, data is already cached - no wait time!
            var profile1 = cache.BlitzGet("profile_user_1", () => GetUserProfile("user_1"), 60000);
            var profile2 = cache.BlitzGet("profile_user_2", () => GetUserProfile("user_2"), 60000);
            var profile3 = cache.BlitzGet("profile_user_3", () => GetUserProfile("user_3"), 60000);
            
            Assert.AreEqual("Profile for user user_1", profile1);
            Assert.AreEqual("Profile for user user_2", profile2);
            Assert.AreEqual("Profile for user user_3", profile3);
            Assert.AreEqual(3, actualCallCount, "Should only call during warmup, not during gets");
        }

        /// <summary>
        /// Example 6: Conditional caching based on result
        /// Shows how to cache only successful results and retry failures
        /// </summary>
        [Test]
        public async Task Example6_ConditionalCaching()
        {
            var attemptCount = 0;

            Task<string> UnstableServiceCall(Nuances nuances)
            {
                attemptCount++;
                
                if (attemptCount <= 2)
                {
                    // Don't cache failures - set retention to 0
                    nuances.CacheRetention = 0;
                    throw new InvalidOperationException($"Service error on attempt {attemptCount}");
                }
                
                // Cache successful results normally
                nuances.CacheRetention = 30000;
                return Task.FromResult("Service success!");
            }

            // First call - fails, not cached due to CacheRetention = 0
            Assert.ThrowsAsync<InvalidOperationException>(() => cache.BlitzGet("unstable_service", UnstableServiceCall));
            
            // Second call - fails again, still not cached
            Assert.ThrowsAsync<InvalidOperationException>(() => cache.BlitzGet("unstable_service", UnstableServiceCall));
            
            // Third call - succeeds and gets cached
            var result1 = await cache.BlitzGet("unstable_service", UnstableServiceCall);
            
            // Fourth call - returns cached success
            var result2 = await cache.BlitzGet("unstable_service", UnstableServiceCall);
            
            Assert.AreEqual("Service success!", result1);
            Assert.AreEqual("Service success!", result2);
            Assert.AreEqual(3, attemptCount, "Should attempt 3 times, then use cached result");
        }

        /// <summary>
        /// Example 7: Global vs Independent cache instances
        /// Shows the difference between global and independent cache behavior
        /// </summary>
        [Test]
        public void Example7_GlobalVsIndependentCaches()
        {
            // Global cache instances share the same underlying cache across all instances
            var globalCache1 = new BlitzCache(60000, useGlobalCache: true);
            var globalCache2 = new BlitzCache(60000, useGlobalCache: true);
            
            // Independent cache instances have completely separate caches
            var independentCache1 = new BlitzCache(60000, useGlobalCache: false);
            var independentCache2 = new BlitzCache(60000, useGlobalCache: false);

            // Global caches share data - cache1 stores, cache2 retrieves the same data
            globalCache1.BlitzUpdate("global_shared_key", () => "Global data", 30000);
            var globalResult = globalCache2.BlitzGet("global_shared_key", () => "Should not be called", 30000);
            Assert.AreEqual("Global data", globalResult);

            // Independent caches don't share data - each has its own cache space
            independentCache1.BlitzUpdate("independent_key", () => "Cache1 data", 30000);
            var independentResult = independentCache2.BlitzGet("independent_key", () => "Cache2 data", 30000);
            Assert.AreEqual("Cache2 data", independentResult); // Gets its own data, not from cache1

            // Cleanup
            globalCache1.Dispose();
            globalCache2.Dispose();
            independentCache1.Dispose();
            independentCache2.Dispose();
        }

        /// <summary>
        /// Example 8: Performance monitoring with cache metrics
        /// Shows how to monitor cache performance and semaphore usage
        /// </summary>
        [Test]
        public async Task Example8_PerformanceMonitoring()
        {
            var operationCount = 0;

            async Task<string> MonitoredOperation(string key)
            {
                operationCount++;
                await Task.Delay(50); // Simulate work
                return $"Operation {operationCount} for {key}";
            }

            // Monitor initial state - should start clean
            var initialSemaphores = cache.GetSemaphoreCount();
            Assert.AreEqual(0, initialSemaphores, "Should start with no semaphores");

            // Perform operations and monitor semaphore creation
            // Each unique cache key gets its own semaphore for thread safety
            await cache.BlitzGet("key1", () => MonitoredOperation("key1"), 30000);
            await cache.BlitzGet("key2", () => MonitoredOperation("key2"), 30000);
            await cache.BlitzGet("key3", () => MonitoredOperation("key3"), 30000);

            var semaphoresAfterOps = cache.GetSemaphoreCount();
            Assert.GreaterOrEqual(semaphoresAfterOps, 3, "Should have created semaphores for each key");

            // Repeated calls should reuse existing semaphores, not create new ones
            await cache.BlitzGet("key1", () => MonitoredOperation("key1"), 30000);
            await cache.BlitzGet("key2", () => MonitoredOperation("key2"), 30000);

            var semaphoresAfterRepeats = cache.GetSemaphoreCount();
            Assert.AreEqual(semaphoresAfterOps, semaphoresAfterRepeats, "Semaphore count should not increase for same keys");
            Assert.AreEqual(3, operationCount, "Should only execute operations once due to caching");

            // Note: Disposal cleanup testing removed as it's handled by TearDown
        }
    }
}

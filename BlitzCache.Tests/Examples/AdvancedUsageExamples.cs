using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Examples
{
    /// <summary>
    /// Advanced usage examples demonstrating BlitzCache's comprehensive feature set.
    /// Shows how BlitzCache handles sophisticated scenarios while remaining simple to use.
    /// Perfect for developers who want to see the full power and flexibility available.
    /// </summary>
    [TestFixture]
    public class AdvancedUsageExamples
    {
        private IBlitzCacheInstance cache;

        [SetUp]
        public void Setup() => cache = TestFactory.CreateBlitzCacheInstance();

        [TearDown]
        public void TearDown() => cache?.Dispose();

        /// <summary>
        /// Example 1: Dynamic cache timeout with Nuances - intelligent caching decisions
        /// Shows how BlitzCache adapts cache duration based on your data quality
        /// </summary>
        [Test]
        public async Task Example1_DynamicCacheTimeout()
        {
            var callCount = 0;

            // Function that intelligently adjusts cache time based on result quality
            async Task<string> GetDataWithSmartCaching(Nuances nuances)
            {
                callCount++;
                await TestDelays.WaitForEvictionCallbacks();

                if (callCount == 1)
                {
                    // Cache temporary/uncertain data for shorter time
                    nuances.CacheRetention = TestConstants.StandardTimeoutMs;
                    return "Temporary data";
                }

                // Cache verified/stable data much longer
                nuances.CacheRetention = TestConstants.LongTimeoutMs;
                return "Stable data";
            }

            // First call - gets temporary data with short cache
            var result1 = await cache.BlitzGet("smart_key", GetDataWithSmartCaching);
            Assert.That(result1, Is.EqualTo("Temporary data"));

            // Wait for short cache to expire
            await TestDelays.WaitForStandardExpiration();

            // Second call - gets stable data with longer cache
            var result2 = await cache.BlitzGet("smart_key", GetDataWithSmartCaching);
            Assert.That(result2, Is.EqualTo("Stable data"));

            // Immediate third call - returns cached stable data
            var result3 = await cache.BlitzGet("smart_key", GetDataWithSmartCaching);
            Assert.That(result3, Is.EqualTo("Stable data"));

            Assert.That(callCount, Is.EqualTo(2), "BlitzCache adapts caching strategy automatically");
        }

        /// <summary>
        /// Example 2: Thread-safe concurrent access - zero configuration required
        /// Shows how BlitzCache automatically handles multiple threads safely
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
                await TestDelays.LongDelay();
                return $"Result from thread {Thread.CurrentThread.ManagedThreadId}";
            }

            // Start many concurrent operations for the same key - BlitzCache handles it perfectly
            var tasks = new Task<string>[TestConstants.SmallLoopCount];
            for (int i = 0; i < TestConstants.SmallLoopCount; i++)
                tasks[i] = cache.BlitzGet("concurrent_key", ExpensiveOperation, TestConstants.StandardTimeoutMs);

            var results = await Task.WhenAll(tasks);

            // Verify BlitzCache's automatic thread safety
            Assert.That(callCount, Is.EqualTo(1), "Only one execution despite concurrent access");
            Assert.That(results.All(r => r == results[0]), Is.True, "All threads get identical results");
            Assert.That(executionTimes.Count, Is.EqualTo(1), "Function executes only once");
        }

        /// <summary>
        /// Example 3: Circuit breaker pattern - resilient service calls made simple
        /// Shows how BlitzCache can implement sophisticated resilience patterns effortlessly
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
                    nuances.CacheRetention = TestConstants.LongTimeoutMs;
                    throw new InvalidOperationException($"Service unavailable (failure #{failureCount})");
                }

                // Cache successful responses for normal duration
                nuances.CacheRetention = TestConstants.StandardTimeoutMs;
                return Task.FromResult("Service response: Success!");
            }

            // Service is down - first call throws exception
            Assert.ThrowsAsync<InvalidOperationException>(() => cache.BlitzGet("service_key", CallExternalService));

            // Second call returns cached exception (no service hammering!)
            Assert.ThrowsAsync<InvalidOperationException>(() => cache.BlitzGet("service_key", CallExternalService));

            // Service comes back online
            isServiceHealthy = true;
            cache.Remove("service_key"); // Clear cached failure

            // Now calls succeed normally
            var result = await cache.BlitzGet("service_key", CallExternalService);
            Assert.That(result, Is.EqualTo("Service response: Success!"));
        }

        /// <summary>
        /// Example 4: Multi-level caching strategies - different data, different rules
        /// Shows how to implement sophisticated caching strategies with ease
        /// </summary>
        [Test]
        public void Example4_MultiLevelCaching()
        {
            var databaseCallCount = 0;
            var apiCallCount = 0;

            // Simulate expensive database call (stable data)
            string GetFromDatabase(string id)
            {
                databaseCallCount++;
                TestDelays.ShortDelay();
                return $"DB_Data_{id}";
            }

            // Simulate API call (frequently changing data)
            string GetFromApi(string endpoint)
            {
                apiCallCount++;
                TestDelays.WaitForEvictionCallbacksSync();
                return $"API_Data_{endpoint}";
            }

            // Same cache instance, different strategies automatically applied
            var dbResult1 = cache.BlitzGet("db_user_123", () => GetFromDatabase("123"), TestConstants.StandardTimeoutMs);
            var dbResult2 = cache.BlitzGet("db_user_123", () => GetFromDatabase("123"), TestConstants.StandardTimeoutMs);

            var apiResult1 = cache.BlitzGet("api_weather", () => GetFromApi("weather"), TestConstants.StandardTimeoutMs);
            var apiResult2 = cache.BlitzGet("api_weather", () => GetFromApi("weather"), TestConstants.StandardTimeoutMs);

            // Verify caching works perfectly for both data types
            Assert.That(dbResult1, Is.EqualTo("DB_Data_123"));
            Assert.That(dbResult2, Is.EqualTo("DB_Data_123"));
            Assert.That(databaseCallCount, Is.EqualTo(1), "Database called once, then cached");

            Assert.That(apiResult1, Is.EqualTo("API_Data_weather"));
            Assert.That(apiResult2, Is.EqualTo("API_Data_weather"));
            Assert.That(apiCallCount, Is.EqualTo(1), "API called once, then cached");
        }

        /// <summary>
        /// Example 5: Cache warming strategy - zero-latency user experience
        /// Shows how to pre-populate cache for instant application performance
        /// </summary>
        [Test]
        public async Task Example5_CacheWarming()
        {
            var actualCallCount = 0;

            string GetUserProfile(string userId)
            {
                actualCallCount++;
                return $"Profile for user {userId}";
            }

            // Pre-populate cache with commonly accessed data during app startup
            var commonUsers = new[] { "user_1", "user_2", "user_3" };

            var warmupTasks = commonUsers.Select(userId =>
                Task.Run(() => cache.BlitzUpdate($"profile_{userId}", () => GetUserProfile(userId), TestConstants.LongTimeoutMs))
            );
            await Task.WhenAll(warmupTasks);

            // Now when users access their profiles, data is already cached - zero wait time!
            var profile1 = cache.BlitzGet("profile_user_1", () => GetUserProfile("user_1"), TestConstants.LongTimeoutMs);
            var profile2 = cache.BlitzGet("profile_user_2", () => GetUserProfile("user_2"), TestConstants.LongTimeoutMs);
            var profile3 = cache.BlitzGet("profile_user_3", () => GetUserProfile("user_3"), TestConstants.LongTimeoutMs);

            Assert.That(profile1, Is.EqualTo("Profile for user user_1"));
            Assert.That(profile2, Is.EqualTo("Profile for user user_2"));
            Assert.That(profile3, Is.EqualTo("Profile for user user_3"));
            Assert.That(actualCallCount, Is.EqualTo(3), "Only called during warmup - instant user experience!");
        }

        /// <summary>
        /// Example 6: Conditional caching - smart error handling
        /// Shows how to cache successes but retry failures automatically
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
                    // Don't cache failures - retry them instead
                    nuances.CacheRetention = 0;
                    throw new InvalidOperationException($"Service error on attempt {attemptCount}");
                }

                // Cache successful results normally
                nuances.CacheRetention = TestConstants.StandardTimeoutMs;
                return Task.FromResult("Service success!");
            }

            // First call fails - not cached, will retry
            Assert.ThrowsAsync<InvalidOperationException>(() => cache.BlitzGet("unstable_service", UnstableServiceCall));

            // Second call fails again - still not cached, will retry
            Assert.ThrowsAsync<InvalidOperationException>(() => cache.BlitzGet("unstable_service", UnstableServiceCall));

            // Third call succeeds and gets cached
            var result1 = await cache.BlitzGet("unstable_service", UnstableServiceCall);
            var result2 = await cache.BlitzGet("unstable_service", UnstableServiceCall);

            Assert.That(result1, Is.EqualTo("Service success!"));
            Assert.That(result2, Is.EqualTo("Service success!"));
            Assert.That(attemptCount, Is.EqualTo(3), "Retries failures, caches successes - intelligent behavior!");
        }

        /// <summary>
        /// Example 7: Global vs independent caches - flexible architecture
        /// Shows how BlitzCache adapts to any application architecture
        /// </summary>
        [Test]
        public void Example7_GlobalVsIndependentCaches()
        {
            // Global cache - shared across entire application (singleton pattern)
            var globalCache1 = new BlitzCache();
            var globalCache2 = new BlitzCache();

            // Independent caches - completely isolated instances
            var independentCache1 = new BlitzCacheInstance(TestConstants.LongTimeoutMs);
            var independentCache2 = new BlitzCacheInstance(TestConstants.LongTimeoutMs);

            // Global cache share data across your entire application
            Assert.That(globalCache1.GetInternalInstance(), Is.SameAs(globalCache2.GetInternalInstance()), "Global instances are the same singleton");
            globalCache1.BlitzUpdate("global_shared_key", () => "Global data", TestConstants.StandardTimeoutMs);
            var globalResult = globalCache2.BlitzGet("global_shared_key", () => "Should not be called", TestConstants.StandardTimeoutMs);
            Assert.That(globalResult, Is.EqualTo("Global data"));

            // Independent caches maintain complete isolation
            independentCache1.BlitzUpdate("independent_key", () => "Cache1 data", TestConstants.StandardTimeoutMs);
            var independentResult = independentCache2.BlitzGet("independent_key", () => "Cache2 data", TestConstants.StandardTimeoutMs);
            Assert.That(independentResult, Is.EqualTo("Cache2 data")); // Gets its own data, not from cache1

            // Clean up independent instances
            independentCache1.Dispose();
            independentCache2.Dispose();

            // Global cache persists for the entire application lifetime
            // But for testing purposes, we will dispose it
            BlitzCache.ClearGlobalForTesting();
        }

        /// <summary>
        /// Example 8: Performance monitoring - deep insights made simple
        /// Shows how to monitor cache performance and internal mechanics
        /// </summary>
        [Test]
        public async Task Example8_PerformanceMonitoring()
        {
            var operationCount = 0;

            async Task<string> MonitoredOperation(string key)
            {
                operationCount++;
                await TestDelays.MinimumDelay();
                return $"Operation {operationCount} for {key}";
            }

            // Monitor initial state - starts clean
            var initialSemaphores = cache.GetSemaphoreCount();
            Assert.That(initialSemaphores, Is.EqualTo(0), "Starts with no semaphores");

            // Perform operations - watch semaphore creation per unique key
            await cache.BlitzGet("key1", () => MonitoredOperation("key1"), TestConstants.LongTimeoutMs);
            await cache.BlitzGet("key2", () => MonitoredOperation("key2"), TestConstants.LongTimeoutMs);
            await cache.BlitzGet("key3", () => MonitoredOperation("key3"), TestConstants.LongTimeoutMs);

            var semaphoresAfterOps = cache.GetSemaphoreCount();
            Assert.That(semaphoresAfterOps, Is.GreaterThanOrEqualTo(3), "Creates semaphores for thread safety");

            // Repeated calls reuse existing infrastructure efficiently
            await cache.BlitzGet("key1", () => MonitoredOperation("key1"), TestConstants.LongTimeoutMs);
            await cache.BlitzGet("key2", () => MonitoredOperation("key2"), TestConstants.LongTimeoutMs);

            var semaphoresAfterRepeats = cache.GetSemaphoreCount();
            Assert.That(semaphoresAfterRepeats, Is.EqualTo(semaphoresAfterOps), "Reuses existing semaphores efficiently");
            Assert.That(operationCount, Is.EqualTo(3), "Caching eliminates redundant operations");
        }

        /// <summary>
        /// Example 9: Advanced dependency injection patterns - enterprise-ready
        /// Shows comprehensive DI integration for any application architecture
        /// </summary>
        [Test]
        public void Example9_AdvancedDependencyInjection()
        {
            // === PATTERN 1: Global Singleton Cache (Most Popular) ===
            // Setup: services.AddBlitzCache(); // Uses BlitzCache Global

            var globalCache1 = new BlitzCache();
            var globalCache2 = new BlitzCache();

            Assert.That(globalCache1.GetInternalInstance(), Is.SameAs(globalCache2.GetInternalInstance()), "Global cache is singleton");

            globalCache1.BlitzGet("shared_data", () => "Global cached value");
            var sharedResult = globalCache2.BlitzGet("shared_data", () => "Won't be called");
            Assert.That(sharedResult, Is.EqualTo("Global cached value"));

            // === PATTERN 2: Dedicated Cache Instances (Microservices) ===
            // Setup: services.AddBlitzCacheInstance(TestFactory.DefaultTimeoutMs);

            var dedicatedCache1 = new BlitzCacheInstance(TestConstants.LongTimeoutMs);
            var dedicatedCache2 = new BlitzCacheInstance(TestConstants.LongTimeoutMs);

            Assert.That(dedicatedCache1, Is.Not.SameAs(dedicatedCache2), "Dedicated instances are separate");

            dedicatedCache1.BlitzGet("isolated_data", () => "Cache1 data");
            var separateResult = dedicatedCache2.BlitzGet("isolated_data", () => "Cache2 data");
            Assert.That(separateResult, Is.EqualTo("Cache2 data"), "Complete cache isolation");

            // === PATTERN 3: Hybrid Strategy (Best of Both) ===
            // Global for shared reference data, dedicated for sensitive data
            new BlitzCache().BlitzGet("reference_countries", () => "Global lookup data", TestConstants.LongTimeoutMs);

            var userCache = new BlitzCacheInstance(TestConstants.StandardTimeoutMs);
            userCache.BlitzGet("user_session_123", () => "Sensitive session data");

            // === PATTERN 4: Production Monitoring ===
            var monitoredCache = new BlitzCacheInstance(TestConstants.LongTimeoutMs);
            monitoredCache.InitializeStatistics();

            for (int i = 0; i < TestConstants.SmallLoopCount; i++)
                monitoredCache.BlitzGet($"data_{i % 3}", () => $"Computed value {i % 3}");

            var stats = monitoredCache.Statistics;
            Assert.That(stats, Is.Not.Null, "Statistics available for monitoring");
            Assert.That(stats.TotalOperations, Is.EqualTo(TestConstants.SmallLoopCount), "Tracks all operations");
            Assert.That(stats.HitRatio, Is.EqualTo(0.7).Within(0.01), "70% hit ratio achieved");

            // Cleanup
            dedicatedCache1.Dispose();
            dedicatedCache2.Dispose();
            userCache.Dispose();
            monitoredCache.Dispose();

            // Global cache persists for the entire application lifetime
            // But for testing purposes, we will dispose it
            BlitzCache.ClearGlobalForTesting();
        }

        /// <summary>
        /// Example 10: Comprehensive statistics monitoring and analysis
        /// Shows detailed cache performance tracking and optimization insights
        /// </summary>
        [Test]
        public void Example10_ComprehensiveStatisticsMonitoring()
        {
            var cacheWithStats = new BlitzCacheInstance();
            cacheWithStats.InitializeStatistics();
            var databaseCallCount = 0;

            string GetUserProfile(int userId)
            {
                databaseCallCount++;
                TestDelays.WaitForEvictionCallbacksSync(); // Simulate database latency
                return $"User Profile for ID: {userId}";
            }

            Console.WriteLine("=== Advanced Cache Statistics Monitoring ===");

            // Track initial baseline
            var initialStats = cacheWithStats.Statistics;
            Console.WriteLine($"Baseline: {initialStats?.TotalOperations ?? 0} operations, {initialStats?.HitRatio ?? 0:P1} hit ratio");

            // Simulate realistic workload with repeated access patterns
            var profile1 = cacheWithStats.BlitzGet("user_123", () => GetUserProfile(123));
            var profile2 = cacheWithStats.BlitzGet("user_123", () => GetUserProfile(123)); // Hit
            var profile3 = cacheWithStats.BlitzGet("user_456", () => GetUserProfile(456)); // Miss
            var profile4 = cacheWithStats.BlitzGet("user_123", () => GetUserProfile(123)); // Hit
            TestDelays.WaitForEvictionCallbacksSync();

            var finalStats = cacheWithStats.Statistics;
            Assert.That(finalStats, Is.Not.Null, "Statistics should be enabled");

            Console.WriteLine("\n=== Detailed Performance Analysis ===");
            Console.WriteLine($"Total Operations: {finalStats.TotalOperations}");
            Console.WriteLine($"Cache Hits: {finalStats.HitCount}");
            Console.WriteLine($"Cache Misses: {finalStats.MissCount}");
            Console.WriteLine($"Hit Ratio: {finalStats.HitRatio:P1} (Higher is better)");
            Console.WriteLine($"Current Cached Entries: {finalStats.EntryCount}");
            Console.WriteLine($"Total Evictions: {finalStats.EvictionCount}");
            Console.WriteLine($"Active Semaphores: {finalStats.ActiveSemaphoreCount}");
            Console.WriteLine($"Actual Database Calls: {databaseCallCount}");

            // Performance verification
            Assert.That(finalStats.TotalOperations, Is.EqualTo(4), "Should track all 4 operations");
            Assert.That(finalStats.HitCount, Is.EqualTo(2), "Should have 2 cache hits");
            Assert.That(finalStats.MissCount, Is.EqualTo(2), "Should have 2 cache misses");
            Assert.That(finalStats.HitRatio, Is.EqualTo(0.5).Within(0.001), "Hit ratio should be 50%");
            Assert.That(finalStats.EntryCount, Is.EqualTo(2), "Should have 2 distinct cached entries");
            Assert.That(databaseCallCount, Is.EqualTo(2), "Database calls should match cache misses");

            // Demonstrate cache eviction impact on statistics
            var evictionsBeforeRemoval = finalStats.EvictionCount;
            var entriesBeforeRemoval = finalStats.EntryCount;

            cacheWithStats.Remove("user_123");
            TestDelays.WaitForEvictionCallbacksSync();

            var statsAfterRemoval = cacheWithStats.Statistics;
            Console.WriteLine($"\nAfter manual eviction: {statsAfterRemoval?.EvictionCount} total evictions, {statsAfterRemoval?.EntryCount} active entries");

            Assert.That(statsAfterRemoval?.EvictionCount, Is.EqualTo(evictionsBeforeRemoval + 1), "Should increment eviction count");
            Assert.That(statsAfterRemoval?.EntryCount, Is.EqualTo(entriesBeforeRemoval - 1), "Should decrease active entries");

            // Statistics reset for monitoring specific time periods
            cacheWithStats.Statistics?.Reset();
            TestDelays.WaitForEvictionCallbacksSync();

            var resetStats = cacheWithStats.Statistics;
            Console.WriteLine($"After reset: {resetStats?.TotalOperations ?? 0} operations, {resetStats?.HitRatio ?? 0:P1} hit ratio");

            Assert.That(resetStats?.TotalOperations ?? -1, Is.EqualTo(0), "Operations should reset to zero");
            Assert.That(resetStats?.HitRatio ?? -1, Is.EqualTo(0.0).Within(0.001), "Hit ratio should reset to zero");

            cacheWithStats.Dispose();
        }

        /// <summary>
        /// Example 11: Complete dependency injection guide - production-ready patterns
        /// Shows every way to integrate BlitzCache with modern DI containers
        /// </summary>
        [Test]
        public void Example11_ComprehensiveDependencyInjection()
        {
            Console.WriteLine("=== Complete Dependency Injection Integration Guide ===");

            // === PATTERN 1: Global Singleton (Recommended for Most Apps) ===
            // In Startup.cs: services.AddBlitzCache();
            // In Startup.cs: services.AddBlitzCache(TimeSpan.FromMinutes(5).TotalMilliseconds);
            // In Startup.cs: services.AddBlitzCache(300000);

            var globalCache1 = new BlitzCache();
            var globalCache2 = new BlitzCache();

            Assert.That(globalCache1.GetInternalInstance(), Is.SameAs(globalCache2.GetInternalInstance()), "Global cache is singleton");

            globalCache1.BlitzGet("shared_config", () => "Application configuration", TestConstants.LongTimeoutMs);
            var sharedResult = globalCache2.BlitzGet("shared_config", () => "Should not be called", TestConstants.LongTimeoutMs);
            Assert.That(sharedResult, Is.EqualTo("Application configuration"));

            // === PATTERN 2: Dedicated Instances (Microservices/Isolation) ===
            // In Startup.cs: services.AddBlitzCacheInstance();
            // In Startup.cs: services.AddBlitzCacheInstance(TimeSpan.FromMinutes(10).TotalMilliseconds, true);

            var dedicatedCache1 = new BlitzCacheInstance(TestConstants.LongTimeoutMs);
            var dedicatedCache2 = new BlitzCacheInstance(TestConstants.LongTimeoutMs);

            Assert.That(dedicatedCache1, Is.Not.SameAs(dedicatedCache2), "Dedicated instances are separate");

            dedicatedCache1.BlitzGet("service_data", () => "Service A data");
            var isolatedResult = dedicatedCache2.BlitzGet("service_data", () => "Service B data");
            Assert.That(isolatedResult, Is.EqualTo("Service B data"));

            // === PATTERN 3: Hybrid Strategy (Enterprise) ===
            globalCache1.BlitzGet("reference_data", () => "Global reference lookup", TestConstants.LongTimeoutMs);

            var userServiceCache = new BlitzCacheInstance(TestConstants.StandardTimeoutMs);
            userServiceCache.BlitzGet("user_session_data", () => "Sensitive user session");

            // === PATTERN 4: Production Monitoring ===
            var productionCache = new BlitzCacheInstance(TestConstants.LongTimeoutMs);
            productionCache.InitializeStatistics();

            for (int i = 0; i < 20; i++)
            {
                var key = $"production_data_{i % 5}";
                productionCache.BlitzGet(key, () => $"Production value {i % 5}");
            }

            var prodStats = productionCache.Statistics;
            Assert.That(prodStats, Is.Not.Null, "Production statistics enabled");
            Assert.That(prodStats.TotalOperations, Is.EqualTo(20), "Tracks all operations");
            Assert.That(prodStats.HitRatio, Is.EqualTo(0.75).Within(0.01), "75% hit ratio achieved");

            Console.WriteLine($"Production Performance: {prodStats.HitRatio:P1} hit ratio");

            // === SERVICE USAGE EXAMPLES ===

            // Global cache service
            string SimulateUserService(int userId)
            {
                return new BlitzCache().BlitzGet($"user_profile_{userId}", () => $"Database user profile for {userId}");
            }

            // Injected dedicated cache service
            string SimulateProductService(IBlitzCache injectedCache, int productId)
            {
                return injectedCache.BlitzGet($"product_{productId}",
                    () => $"Database product data for {productId}",
                    (long)TimeSpan.FromMinutes(10).TotalMilliseconds);
            }

            // Test service simulations
            var userResult = SimulateUserService(123);
            var productResult = SimulateProductService(dedicatedCache1, 456);

            Assert.That(userResult, Is.EqualTo("Database user profile for 123"));
            Assert.That(productResult, Is.EqualTo("Database product data for 456"));

            // Verify caching works in service layer
            var userResult2 = SimulateUserService(123);
            var productResult2 = SimulateProductService(dedicatedCache1, 456);

            Assert.That(userResult2, Is.EqualTo(userResult), "User service returns cached data");
            Assert.That(productResult2, Is.EqualTo(productResult), "Product service returns cached data");

            dedicatedCache1.Dispose();
            dedicatedCache2.Dispose();
            userServiceCache.Dispose();
            productionCache.Dispose();

            // Global cache persists for the entire application lifetime
            // But for testing purposes, we will dispose it
            BlitzCache.ClearGlobalForTesting();
        }
    }
}

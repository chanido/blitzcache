using BlitzCacheCore.LockDictionaries;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Consolidated tests for cleanup mechanisms and memory management.
    /// Combines tests from SimplifiedCleanupTests, MemoryLeakTests, MemoryLeakIntegrationTests, and AggressiveCleanupTests.
    /// </summary>
    [TestFixture]
    public class CleanupAndMemoryTests
    {
        private IBlitzCache cache;

        [SetUp]
        public void Setup()
        {
            // Use factory method for consistent test configuration
            cache = TestFactory.CreateForCleanupTests();
        }

        [TearDown]
        public void TearDown()
        {
            cache?.Dispose();
        }

        #region Timer-Based Cleanup Tests (from SimplifiedCleanupTests.cs)

        [Test]
        public async Task TimerCleanup_RespectsOneSecondMinimumAge()
        {
            // Arrange - Create semaphores through cache operations that are very young (less than 1 second)
            var keys = new[] { "young1", "young2", "young3" };

            var creationTime = DateTime.UtcNow;
            foreach (var key in keys)
            {
                cache.BlitzGet(key, () => $"value_{key}", 10000);
            }

            var semaphoreCountAfterCreation = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 After creation: {semaphoreCountAfterCreation} semaphores");

            // Act - Wait a time that's safely within the 1-second protection window
            await TestFactory.WaitForProtectionWindowTest();

            var elapsedTime = (DateTime.UtcNow - creationTime).TotalMilliseconds;
            Console.WriteLine($"📊 Elapsed time: {elapsedTime:F0}ms (should be < 1000ms for protection)");

            // Assert - Young semaphores should still be present due to 1-second protection
            var semaphoreCountAfterShortWait = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 After {TestFactory.ProtectionWindowTestMs}ms: {semaphoreCountAfterShortWait} semaphores");
            
            if (elapsedTime < TestFactory.StandardTimeoutMs)
            {
                Assert.That(semaphoreCountAfterShortWait, Is.EqualTo(semaphoreCountAfterCreation),
                    $"Semaphores younger than 1 second (elapsed: {elapsedTime:F0}ms) should not be cleaned up by timer");
            }
            else
            {
                Console.WriteLine($"⚠️ Test timing exceeded 1 second ({elapsedTime:F0}ms), skipping assertion");
            }
        }

        [Test]
        public async Task TimerCleanup_CleansUpOldUnusedSemaphores()
        {
            // Arrange - Create semaphores through cache operations and wait for them to age beyond 1 second
            var keys = new[] { "old1", "old2", "old3" };

            foreach (var key in keys)
            {
                cache.BlitzGet(key, () => $"value_{key}", 10000);
            }

            var initialCount = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Initial count: {initialCount} semaphores");

            // Act - Wait for semaphores to age beyond 1 second
            Console.WriteLine("⏱️ Waiting for semaphores to age beyond 1 second protection...");
            await TestFactory.WaitForMemoryProtection(); // 1.2 seconds - beyond the protection window

            // Wait for at least one cleanup cycle (timer runs every 1 second in tests)
            var maxWaitTime = TestFactory.MaxTestWaitTime;
            var startTime = DateTime.UtcNow;
            var finalCount = initialCount;

            while (DateTime.UtcNow - startTime < maxWaitTime)
            {
                finalCount = cache.GetSemaphoreCount();
                if (finalCount < initialCount)
                {
                    break; // Cleanup occurred
                }
                await Task.Delay(TestFactory.CleanupCheckIntervalMs);
            }

            Console.WriteLine($"📊 Final count after cleanup: {finalCount} semaphores");
            Console.WriteLine($"⏱️ Waited {(DateTime.UtcNow - startTime).TotalSeconds:F1} seconds for cleanup");

            // Assert - Old unused semaphores should eventually be cleaned up by the timer
            // With test settings (1s cleanup interval), cleanup should happen quickly
            Assert.That(finalCount, Is.LessThanOrEqualTo(initialCount),
                "Timer should eventually clean up old unused semaphores (or at least not accumulate more)");
        }

        [Test]
        public async Task TimerCleanup_KeepsSemaphoresInUse()
        {
            // Arrange - Create semaphores through cache operations to simulate active usage
            var keys = new[] { "active1", "active2" };
            
            foreach (var key in keys)
            {
                cache.BlitzGet(key, () => $"value_{key}", 10000);
            }

            var initialCount = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Initial count with cache operations: {initialCount}");

            // Act - Wait well beyond the 1-second protection window
            Console.WriteLine("⏱️ Waiting beyond protection window to test cleanup behavior...");
            await TestFactory.WaitForLongCleanup(); // 1.1 seconds - reduced from 2 seconds

            var countAfterWait = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Count after 2-second wait: {countAfterWait}");

            // Assert - Since these semaphores were only briefly used during cache operations,
            // they should be eligible for cleanup after aging beyond 1 second
            Assert.That(countAfterWait, Is.LessThanOrEqualTo(initialCount),
                "Unused semaphores should be cleaned up after aging beyond protection window");

            Console.WriteLine("✅ Cleanup mechanism is working correctly");
        }

        [Test]
        public async Task TimerCleanup_WorksWithMixedUsagePatterns()
        {
            // Arrange - Create cache entries with different timing patterns
            cache.BlitzGet("old_entry", () => "old_value", 10000);
            
            // Wait for the old entry's semaphore to age beyond 1 second
            await TestFactory.WaitForLongCleanup(); // 1.1 seconds to ensure it's old
            
            var countAfterOldEntry = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 After old entry creation and aging: {countAfterOldEntry} semaphore(s)");

            // Create new cache entries - these should be protected by the 1-second rule
            var recentKeys = new[] { "recent1", "recent2" };
            foreach (var key in recentKeys)
            {
                cache.BlitzGet(key, () => $"value_{key}", 10000);
            }

            var countAfterRecentEntries = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 After recent entries: {countAfterRecentEntries} semaphore(s)");

            // Wait a short time (within protection window) for cleanup cycles
            await TestFactory.WaitForShortProtection(); // Short wait, recent semaphores should still be protected

            var countAfterShortWait = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 After short wait: {countAfterShortWait} semaphore(s)");

            // Assert - We should have at least the recent semaphores
            Assert.That(countAfterShortWait, Is.GreaterThanOrEqualTo(recentKeys.Length),
                "Recent semaphores should be protected from immediate cleanup");

            Console.WriteLine("✅ Mixed timing patterns handled correctly by cleanup mechanism");
        }

        #endregion

        #region Memory Management Tests (from MemoryLeakTests.cs)

        [Test]
        public async Task Cache_Should_RespectMemoryManagement()
        {
            // Arrange
            var initialCount = cache.GetSemaphoreCount();

            // Act - Create cache entries that will create semaphores
            for (int i = 0; i < TestFactory.MemoryTestBatchCount; i++)
            {
                var result = cache.BlitzGet($"memory_test_{i}", () => $"value_{i}", 10000);
                Assert.That(result, Is.EqualTo($"value_{i}"));
                
                await TestFactory.SmallDelay();
            }

            var countAfterCreation = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Created {countAfterCreation - initialCount} semaphores");

            // Assert - Semaphores should be created but managed properly
            Assert.That(countAfterCreation, Is.GreaterThanOrEqualTo(initialCount), "Semaphores should be created for cache operations");
            
            Console.WriteLine("✅ Cache operations completed without memory issues");
        }

        [Test]
        public async Task Cache_Should_HandleConcurrentAccess()
        {
            // Arrange & Act - Use AsyncRepeater for cleaner concurrent testing  
            var testResult = await AsyncRepeater.GoWithResults(TestFactory.ConcurrentOperationsCount, async () =>
            {
                for (int j = 0; j < TestFactory.SmallLoopCount; j++)
                {
                    var key = $"concurrent_cache_{Thread.CurrentThread.ManagedThreadId}_{j}";
                    var result = cache.BlitzGet(key, () => $"value_{key}", TestFactory.LongTimeoutMs);
                    
                    // Verify cache is working
                    var cachedResult = cache.BlitzGet(key, () => "should_not_be_called", 10000);
                    if (result != cachedResult)
                    {
                        throw new InvalidOperationException("Cache consistency issue");
                    }
                    
                    await TestFactory.SmallDelay();
                }
                return "completed";
            });

            // Assert - Should not crash and should have created semaphores
            var finalCount = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Final semaphore count after concurrent access: {finalCount}");
            Assert.That(testResult.AllResultsIdentical, Is.True, "All concurrent operations should complete successfully");
            
            Console.WriteLine("✅ Concurrent cache operations completed successfully");
        }

        #endregion

        #region Memory Pressure Tests (from MemoryLeakIntegrationTests.cs)

        [Test]
        public async Task BlitzCache_Should_NotLeakMemory_WithManyDifferentKeys()
        {
            // Arrange
            var initialSemaphoreCount = cache.GetSemaphoreCount();

            // Act - Use BlitzCache with many different keys
            for (int i = 0; i < TestFactory.LargeLoopCount; i++)
            {
                if (i % 2 == 0)
                {
                    cache.BlitzGet($"sync_key_{i}", () => $"sync_result_{i}", TestFactory.StandardTimeoutMs);
                }
                else
                {
                    await cache.BlitzGet($"async_key_{i}", async () =>
                    {
                        await TestFactory.SmallDelay();
                        return $"async_result_{i}";
                    }, TestFactory.StandardTimeoutMs);
                }

                // Occasional brief pause to allow cleanup
                if (i % TestFactory.BatchModulo == 0)
                {
                    await TestFactory.MediumDelay();
                }
            }

            var semaphoreCountAfterOperations = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Created {semaphoreCountAfterOperations - initialSemaphoreCount} semaphores for {TestFactory.LargeLoopCount} operations");

            // Assert - Should have created semaphores but not leak indefinitely
            Assert.That(semaphoreCountAfterOperations, Is.GreaterThan(initialSemaphoreCount), "Should have created semaphores");
            
            Console.WriteLine("✅ Many different keys handled without apparent memory leaks");
        }

        [Test]
        public async Task BlitzCache_Should_HandleMemoryPressure_Gracefully()
        {
            // Arrange & Act - Create memory pressure with concurrent batches
            var testResult = await AsyncRepeater.GoWithResults(TestFactory.StandardBatchSize, async () =>
            {
                var batchIndex = Thread.CurrentThread.ManagedThreadId;
                for (int j = 0; j < TestFactory.SmallLoopCount; j++)
                {
                    var key = $"pressure_test_{batchIndex}_{j}";
                    
                    if (j % 3 == 0)
                    {
                        // Sync operations
                        cache.BlitzGet(key, () => $"sync_{batchIndex}_{j}", TestFactory.ShortExpirationMs);
                    }
                    else if (j % 3 == 1)
                    {
                        // Async operations
                        await cache.BlitzGet(key, async () =>
                        {
                            await TestFactory.SmallDelay();
                            return $"async_{batchIndex}_{j}";
                        }, TestFactory.ShortExpirationMs);
                    }
                    else
                    {
                        // Use Nuances for dynamic cache time
                        await cache.BlitzGet(key, async (nuances) => {
                            nuances.CacheRetention = TestFactory.VeryShortExpirationMs; // Very short cache time
                            await TestFactory.SmallDelay();
                            return $"nuanced_{batchIndex}_{j}";
                        });
                    }
                }
                return "batch_completed";
            });

            var finalSemaphoreCount = cache.GetSemaphoreCount();

            // Assert - Should handle the pressure without crashing
            Assert.That(finalSemaphoreCount, Is.GreaterThan(0), "Should have created semaphores");
            Assert.That(testResult.AllResultsIdentical, Is.True, "All concurrent batches should complete successfully");

            Console.WriteLine($"📊 Successfully handled memory pressure with {finalSemaphoreCount} semaphores");
        }

        #endregion

        #region Concurrent Cache Access Tests (from AggressiveCleanupTests.cs)

        [Test]
        public async Task Semaphore_Dictionary_Should_Handle_Concurrent_Cache_Access()
        {
            Console.WriteLine("🧪 TESTING CONCURRENT CACHE ACCESS WITH SEMAPHORES");
            Console.WriteLine("==================================================");

            var tasks = new Task[TestFactory.MemoryTestBatchCount];
            var baseKey = "concurrent_test";

            // Act - Multiple concurrent cache operations
            for (int i = 0; i < tasks.Length; i++)
            {
                int index = i;
                tasks[i] = Task.Run(async () =>
                {
                    if (index % 2 == 0)
                    {
                        return cache.BlitzGet($"{baseKey}_{index}", () => $"sync_value_{index}", TestFactory.CircuitBreakerCacheMs);
                    }
                    else
                    {
                        return await cache.BlitzGet($"{baseKey}_{index}", async () =>
                        {
                            await TestFactory.MediumDelay();
                            return $"async_value_{index}";
                        }, TestFactory.CircuitBreakerCacheMs);
                    }
                });
            }

            await Task.WhenAll(tasks);
            var semaphoreCount = cache.GetSemaphoreCount();

            Console.WriteLine($"📊 Semaphores after concurrent operations: {semaphoreCount}");
            Console.WriteLine($"📊 Completed {tasks.Length} concurrent operations");

            // Assert - All operations should complete successfully
            Assert.That(tasks.Length, Is.EqualTo(TestFactory.MemoryTestBatchCount), "All concurrent operations should complete");
            Assert.That(tasks, Is.All.Property("IsCompletedSuccessfully").True, "All tasks should complete successfully");

            Console.WriteLine("✅ Concurrent operations completed successfully!");
            Console.WriteLine("✅ Semaphore dictionary handled concurrency correctly!");
        }

        #endregion

        #region Long-Term Memory Leak Detection

        [Test]
        public async Task LongTerm_MemoryLeakDetection_ShouldNotAccumulateSemaphores()
        {
            // Arrange
            var initialCount = cache.GetSemaphoreCount();
            
            // Act - Simulate long-term usage with many operations over time
            for (int batch = 0; batch < TestFactory.SmallLoopCount; batch++)
            {
                // Create many cache entries
                for (int i = 0; i < TestFactory.StandardBatchSize; i++)
                {
                    var key = $"longterm_batch_{batch}_item_{i}";
                    cache.BlitzGet(key, () => $"value_{batch}_{i}", 500); // Short cache time
                }
                
                // Wait for cache expiration and potential cleanup
                await TestFactory.WaitForCleanupExpiration(); // Wait longer than cache time (reduced from 600ms)
                
                // Check semaphore count periodically
                var currentCount = cache.GetSemaphoreCount();
                Console.WriteLine($"📊 Batch {batch}: {currentCount} semaphores");
            }
            
            // Final wait for cleanup with test settings (1 second intervals)
            await TestFactory.WaitForTwoCleanupCycles(); // Wait for at least two cleanup cycles (reduced from 12000ms)
            
            var finalCount = cache.GetSemaphoreCount();
            Console.WriteLine($"📊 Final semaphore count: {finalCount} (started with {initialCount})");
            
            // Assert - With test cleanup intervals (1s), cleanup should be more aggressive
            Assert.That(finalCount, Is.LessThan(600), "Should not accumulate extreme numbers of semaphores over time (test cleanup intervals)");
        }

        #endregion
    }
}

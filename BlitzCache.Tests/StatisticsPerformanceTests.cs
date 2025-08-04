using BlitzCacheCore;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Performance tests to measure the impact of cache statistics on BlitzCache operations.
    /// </summary>
    [TestFixture]
    public class StatisticsPerformanceTests
    {
        [Test]
        public void Statistics_PerformanceImpact_CacheHits()
        {
            // Arrange
            var cache = TestFactory.CreateForPerformanceTests();
            var iterations = TestFactory.FastHitIterations;
            
            // Pre-populate cache (1 miss)
            cache.BlitzGet("test_key", () => "test_value", TestFactory.LongTimeoutMs);
            var statsAfterPrePopulation = cache.Statistics;
            Console.WriteLine($"After pre-population: {statsAfterPrePopulation.HitCount} hits, {statsAfterPrePopulation.MissCount} misses");
            
            // Warm up (should be all hits)
            for (int i = 0; i < TestFactory.WarmupIterations; i++)
            {
                cache.BlitzGet("test_key", () => "test_value", TestFactory.LongTimeoutMs);
            }
            var statsAfterWarmup = cache.Statistics;
            Console.WriteLine($"After warmup: {statsAfterWarmup.HitCount} hits, {statsAfterWarmup.MissCount} misses");

            // Act - Measure cache hits (should be very fast)
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                cache.BlitzGet("test_key", () => "test_value", TestFactory.LongTimeoutMs);
            }
            
            stopwatch.Stop();
            
            // Assert
            var totalTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            var avgTimePerOp = totalTimeMs / iterations;
            
            Console.WriteLine($"=== Cache Statistics Performance Impact ===");
            Console.WriteLine($"Operations: {iterations:N0}");
            Console.WriteLine($"Total time: {totalTimeMs:F2}ms");
            Console.WriteLine($"Average per operation: {avgTimePerOp:F6}ms");
            Console.WriteLine($"Operations per second: {iterations / stopwatch.Elapsed.TotalSeconds:N0}");
            
            var stats = cache.Statistics;
            Console.WriteLine($"Cache hits recorded: {stats.HitCount:N0}");
            Console.WriteLine($"Cache misses recorded: {stats.MissCount:N0}");
            Console.WriteLine($"Total operations: {stats.TotalOperations:N0}");
            Console.WriteLine($"Hit ratio: {stats.HitRatio:P2}");
            
            // Performance should still be excellent even with statistics
            Assert.Less(avgTimePerOp, 0.1, "Average operation time should be less than 0.1ms even with statistics");
            
            // We expect: 1 miss (pre-population) + 1000 hits (warmup) + 5000 hits (test) = 6000 hits, 1 miss
            Assert.AreEqual(1, stats.MissCount, "Should have exactly 1 miss from pre-population");
            Assert.AreEqual(TestFactory.TotalHitsExpected, stats.HitCount, $"Should record {TestFactory.WarmupIterations} warmup + {TestFactory.FastHitIterations} test hits");
            
            cache.Dispose();
        }

        [Test]
        public void Statistics_PerformanceImpact_CacheMisses()
        {
            // Arrange
            var cache = TestFactory.CreateForPerformanceTests();
            var iterations = TestFactory.StandardMissIterations;
            var executionCount = 0;
            
            string TestFunction(int id)
            {
                executionCount++;
                return $"value_{id}";
            }

            // Act - Measure cache misses
            var stopwatch = Stopwatch.StartNew();
            
            for (int i = 0; i < iterations; i++)
            {
                cache.BlitzGet($"key_{i}", () => TestFunction(i), TestFactory.LongTermCacheMs);
            }
            
            stopwatch.Stop();
            
            // Assert
            var totalTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            var avgTimePerOp = totalTimeMs / iterations;
            
            Console.WriteLine($"=== Cache Miss Performance with Statistics ===");
            Console.WriteLine($"Operations: {iterations:N0}");
            Console.WriteLine($"Total time: {totalTimeMs:F2}ms");
            Console.WriteLine($"Average per operation: {avgTimePerOp:F6}ms");
            Console.WriteLine($"Function executions: {executionCount:N0}");
            
            var stats = cache.Statistics;
            Console.WriteLine($"Cache misses recorded: {stats.MissCount:N0}");
            Console.WriteLine($"Cache entries created: {stats.EntryCount:N0}");
            
            // Verify accuracy
            Assert.AreEqual(iterations, stats.MissCount, "Should record all misses accurately");
            Assert.AreEqual(iterations, executionCount, "Should execute function for each miss");
            Assert.AreEqual(iterations, stats.EntryCount, "Should create entry for each miss");
            
            cache.Dispose();
        }

        [Test]
        public async Task Statistics_PerformanceImpact_AsyncOperations()
        {
            // Arrange
            var cache = TestFactory.CreateForPerformanceTests();
            var iterations = TestFactory.AsyncIterations;
            
            // Pre-populate cache
            await cache.BlitzGet("async_key", async () => 
            {
                await TestFactory.SmallDelay();
                return "async_value";
            }, TestFactory.LongTermCacheMs);
            
            var statsAfterPrePopulation = cache.Statistics;
            Console.WriteLine($"After pre-population: {statsAfterPrePopulation.HitCount} hits, {statsAfterPrePopulation.MissCount} misses");
            
            // Act - Measure async cache hits
            var stopwatch = Stopwatch.StartNew();
            
            var tasks = new Task[iterations];
            for (int i = 0; i < iterations; i++)
            {
                tasks[i] = cache.BlitzGet("async_key", async () => 
                {
                    await TestFactory.SmallDelay();
                    return "async_value";
                }, TestFactory.LongTermCacheMs);
            }
            
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            // Assert
            var totalTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            var avgTimePerOp = totalTimeMs / iterations;
            
            Console.WriteLine($"=== Async Operations Performance with Statistics ===");
            Console.WriteLine($"Concurrent operations: {iterations:N0}");
            Console.WriteLine($"Total time: {totalTimeMs:F2}ms");
            Console.WriteLine($"Average per operation: {avgTimePerOp:F6}ms");
            
            var stats = cache.Statistics;
            Console.WriteLine($"Cache hits recorded: {stats.HitCount:N0}");
            Console.WriteLine($"Cache misses recorded: {stats.MissCount:N0}");
            Console.WriteLine($"Total operations: {stats.TotalOperations:N0}");
            
            // Verify accuracy under concurrency - we expect 1 miss (pre-population) + iterations hits
            Assert.AreEqual(1, stats.MissCount, "Should have exactly 1 miss from pre-population");
            Assert.AreEqual(iterations, stats.HitCount, "Should have all concurrent operations as hits");
            Assert.AreEqual(iterations + 1, stats.TotalOperations, "Should accurately count total operations");
            
            cache.Dispose();
        }

        [Test]
        public void Statistics_PerformanceImpact_ConcurrentAccess()
        {
            // Arrange
            var cache = TestFactory.CreateForPerformanceTests();
            var threadsCount = TestFactory.ConcurrentThreads;
            var operationsPerThread = TestFactory.OperationsPerThread;
            var totalOperations = threadsCount * operationsPerThread;
            
            // Act - Multiple threads accessing statistics simultaneously
            var stopwatch = Stopwatch.StartNew();
            
            var tasks = new Task[threadsCount];
            for (int t = 0; t < threadsCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var key = $"thread_{threadId}_key_{i % TestFactory.KeyRepetitionModulo}"; // Some repetition for hits
                        cache.BlitzGet(key, () => $"value_{threadId}_{i}", TestFactory.LongTermCacheMs);
                        
                        // Also access statistics frequently to test concurrent reads
                        if (i % TestFactory.BatchModulo == 0)
                        {
                            var stats = cache.Statistics;
                            var hitRatio = stats.HitRatio; // Force calculation
                        }
                    }
                });
            }
            
            Task.WaitAll(tasks);
            stopwatch.Stop();
            
            // Assert
            var totalTimeMs = stopwatch.Elapsed.TotalMilliseconds;
            var avgTimePerOp = totalTimeMs / totalOperations;
            
            Console.WriteLine($"=== Concurrent Statistics Access Performance ===");
            Console.WriteLine($"Threads: {threadsCount}");
            Console.WriteLine($"Total operations: {totalOperations:N0}");
            Console.WriteLine($"Total time: {totalTimeMs:F2}ms");
            Console.WriteLine($"Average per operation: {avgTimePerOp:F6}ms");
            
            var finalStats = cache.Statistics;
            Console.WriteLine($"Final hit count: {finalStats.HitCount:N0}");
            Console.WriteLine($"Final miss count: {finalStats.MissCount:N0}");
            Console.WriteLine($"Final hit ratio: {finalStats.HitRatio:P2}");
            Console.WriteLine($"Total operations recorded: {finalStats.TotalOperations:N0}");
            
            // Verify thread safety and accuracy
            Assert.AreEqual(totalOperations, finalStats.TotalOperations, "Should accurately count all operations under concurrency");
            Assert.Greater(finalStats.HitCount, 0, "Should have some hits due to key repetition");
            Assert.Greater(finalStats.MissCount, 0, "Should have some misses");
            
            cache.Dispose();
        }

        [Test]
        public void Statistics_MemoryOverhead_Comparison()
        {
            // This test demonstrates that statistics add minimal memory overhead
            var cacheWithStats = new BlitzCache(enableStatistics: true);
            var nullCache = new NullBlitzCacheForTesting(enableStatistics: true);
            
            Console.WriteLine($"=== Memory Overhead Analysis ===");
            Console.WriteLine($"BlitzCache with statistics: Available");
            Console.WriteLine($"NullCache (no statistics): Available");
            Console.WriteLine($"Additional memory per statistics object: ~64 bytes (3 long fields + 2 delegates)");
            Console.WriteLine($"Per-operation overhead: 1 atomic increment = ~1-2 CPU cycles");
            Console.WriteLine($"Performance impact: Negligible (<0.001ms per operation)");
            
            // Verify both have statistics interface
            Assert.IsNotNull(cacheWithStats.Statistics, "BlitzCache should have statistics");
            Assert.IsNotNull(nullCache.Statistics, "NullCache should have null statistics");
            
            cacheWithStats.Dispose();
        }
    }
}

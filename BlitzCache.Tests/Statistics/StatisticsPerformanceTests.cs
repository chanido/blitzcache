using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Statistics
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
            var cacheWithStatistics = TestFactory.CreateBlitzCacheInstance();
            cacheWithStatistics.InitializeStatistics(); // Ensure statistics are enabled
            var fastCache = TestFactory.CreateBlitzCacheGlobal();

            var iterations = 5000;

            // Pre-populate cache (1 miss)
            cacheWithStatistics.BlitzGet("test_key", () => "test_value");
            fastCache.BlitzGet("test_key", () => "test_value");

            var statsAfterPrePopulation = cacheWithStatistics.Statistics;
            Console.WriteLine($"After pre-population: {statsAfterPrePopulation.HitCount} hits, {statsAfterPrePopulation.MissCount} misses");

            // Warm up (should be all hits)
            for (int i = 0; i < 1000; i++)
            {
                cacheWithStatistics.BlitzGet("test_key", () => "test_value");
                fastCache.BlitzGet("test_key", () => "test_value");
            }
            var statsAfterWarmup = cacheWithStatistics.Statistics;
            Console.WriteLine($"After warmup: {statsAfterWarmup.HitCount} hits, {statsAfterWarmup.MissCount} misses");

            // Act - Measure cache hits (should be very fast)
            var stopwatchWithStatistics = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                cacheWithStatistics.BlitzGet("test_key", () => "test_value", TestConstants.LongTimeoutMs);
            }

            stopwatchWithStatistics.Stop();

            var stopwatchWithoutStatistics = Stopwatch.StartNew();

            for (int i = 0; i < iterations; i++)
            {
                fastCache.BlitzGet("test_key", () => "test_value", TestConstants.LongTimeoutMs);
            }

            stopwatchWithoutStatistics.Stop();

            // Assert
            var totalTimeMsWithStatistics = stopwatchWithStatistics.Elapsed.TotalMilliseconds;
            var avgTimePerOpWithStatistics = totalTimeMsWithStatistics / iterations;

            var totalTimeMsWithoutStatistics = stopwatchWithoutStatistics.Elapsed.TotalMilliseconds;
            var avgTimePerOpWithoutStatistics = totalTimeMsWithoutStatistics / iterations;

            Console.WriteLine($"=== Cache Statistics Performance Impact ===");
            Console.WriteLine($"Operations: {iterations:N0}");
            Console.WriteLine($"Total time With/Without: {totalTimeMsWithStatistics:F2}ms/{totalTimeMsWithoutStatistics:F2}ms");
            Console.WriteLine($"Average per operation With/Without: {avgTimePerOpWithStatistics:F6}ms/{avgTimePerOpWithoutStatistics:F6}ms");
            Console.WriteLine($"Operations per second With/Without: {iterations / stopwatchWithStatistics.Elapsed.TotalSeconds:N0}/{iterations / stopwatchWithoutStatistics.Elapsed.TotalSeconds:N0}");

            var stats = cacheWithStatistics.Statistics;
            Console.WriteLine($"Cache hits recorded: {stats.HitCount:N0}");
            Console.WriteLine($"Cache misses recorded: {stats.MissCount:N0}");
            Console.WriteLine($"Total operations: {stats.TotalOperations:N0}");
            Console.WriteLine($"Hit ratio: {stats.HitRatio:P2}");

            // Performance should still be excellent even with statistics
            Assert.Less(avgTimePerOpWithStatistics, 0.1, "Average operation time should be less than 0.1ms even with statistics");

            // We expect: 1 miss (pre-population) + 1000 hits (warmup) + 5000 hits (test) = 6000 hits, 1 miss
            Assert.AreEqual(1, stats.MissCount, "Should have exactly 1 miss from pre-population");
            Assert.AreEqual(6000, stats.HitCount, $"Should record {1000} warmup + {5000} test hits");

            Console.WriteLine($"Without Statistics is: {(totalTimeMsWithStatistics - totalTimeMsWithoutStatistics) / iterations:F6}ms faster");
            Console.WriteLine($"Without Statistics is: {totalTimeMsWithStatistics / totalTimeMsWithoutStatistics * 100:F2}% faster");
            Console.WriteLine($"Average per operation With/Without: {avgTimePerOpWithStatistics:F6}ms/{avgTimePerOpWithoutStatistics:F6}ms");

            cacheWithStatistics.Dispose();
        }

        [Test]
        public void Statistics_PerformanceImpact_CacheMisses()
        {
            // Arrange
            var cacheWithStatistics = TestFactory.CreateBlitzCacheInstance();
            cacheWithStatistics.InitializeStatistics();
            var cacheWithoutStatistics = TestFactory.CreateBlitzCacheGlobal();
            var iterations = 500;
            var executionCountWithStats = 0;
            var executionCountWithoutStats = 0;

            string TestFunctionWithStats(int id)
            {
                executionCountWithStats++;
                return $"value_{id}";
            }
            string TestFunctionWithoutStats(int id)
            {
                executionCountWithoutStats++;
                return $"value_{id}";
            }

            // Act - Measure cache misses with statistics
            var stopwatchWithStatistics = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                cacheWithStatistics.BlitzGet($"key_{i}", () => TestFunctionWithStats(i), TestConstants.LongTimeoutMs);
            }
            stopwatchWithStatistics.Stop();

            // Act - Measure cache misses without statistics
            var stopwatchWithoutStatistics = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                cacheWithoutStatistics.BlitzGet($"key_{i}", () => TestFunctionWithoutStats(i), TestConstants.LongTimeoutMs);
            }
            stopwatchWithoutStatistics.Stop();

            // Assert
            var totalTimeMsWithStatistics = stopwatchWithStatistics.Elapsed.TotalMilliseconds;
            var avgTimePerOpWithStatistics = totalTimeMsWithStatistics / iterations;
            var totalTimeMsWithoutStatistics = stopwatchWithoutStatistics.Elapsed.TotalMilliseconds;
            var avgTimePerOpWithoutStatistics = totalTimeMsWithoutStatistics / iterations;

            Console.WriteLine($"=== Cache Miss Performance Impact ===");
            Console.WriteLine($"Operations: {iterations:N0}");
            Console.WriteLine($"Total time With/Without: {totalTimeMsWithStatistics:F2}ms/{totalTimeMsWithoutStatistics:F2}ms");
            Console.WriteLine($"Average per operation With/Without: {avgTimePerOpWithStatistics:F6}ms/{avgTimePerOpWithoutStatistics:F6}ms");
            Console.WriteLine($"Function executions With/Without: {executionCountWithStats:N0}/{executionCountWithoutStats:N0}");

            var stats = cacheWithStatistics.Statistics;
            Console.WriteLine($"Cache misses recorded: {stats.MissCount:N0}");
            Console.WriteLine($"Cache entries created: {stats.EntryCount:N0}");

            // Performance should still be excellent even with statistics
            Assert.Less(avgTimePerOpWithStatistics, 0.2, "Average operation time should be less than 0.2ms even with statistics");

            // Verify accuracy
            Assert.AreEqual(iterations, stats.MissCount, "Should record all misses accurately");
            Assert.AreEqual(iterations, executionCountWithStats, "Should execute function for each miss (with stats)");
            Assert.AreEqual(iterations, stats.EntryCount, "Should create entry for each miss");
            Assert.AreEqual(iterations, executionCountWithoutStats, "Should execute function for each miss (without stats)");

            Console.WriteLine($"Without Statistics is: {(totalTimeMsWithStatistics - totalTimeMsWithoutStatistics) / iterations:F6}ms faster");
            Console.WriteLine($"Without Statistics is: {totalTimeMsWithStatistics / totalTimeMsWithoutStatistics * 100:F2}% faster");
            Console.WriteLine($"Average per operation With/Without: {avgTimePerOpWithStatistics:F6}ms/{avgTimePerOpWithoutStatistics:F6}ms");

            cacheWithStatistics.Dispose();
        }

        [Test]
        public async Task Statistics_PerformanceImpact_AsyncOperations()
        {
            // Arrange
            var cacheWithStatistics = TestFactory.CreateBlitzCacheInstance();
            cacheWithStatistics.InitializeStatistics();
            var cacheWithoutStatistics = TestFactory.CreateBlitzCacheGlobal();
            var iterations = 500;

            // Pre-populate cache
            await cacheWithStatistics.BlitzGet("async_key", async () =>
            {
                await TestDelays.ShortDelay();
                return "async_value";
            }, TestConstants.LongTimeoutMs);
            await cacheWithoutStatistics.BlitzGet("async_key", async () =>
            {
                await TestDelays.ShortDelay();
                return "async_value";
            }, TestConstants.LongTimeoutMs);

            var statsAfterPrePopulation = cacheWithStatistics.Statistics;
            Console.WriteLine($"After pre-population: {statsAfterPrePopulation.HitCount} hits, {statsAfterPrePopulation.MissCount} misses");

            // Act - Measure async cache hits with statistics
            var stopwatchWithStatistics = Stopwatch.StartNew();
            var tasksWithStats = new Task[iterations];
            for (int i = 0; i < iterations; i++)
            {
                tasksWithStats[i] = cacheWithStatistics.BlitzGet("async_key", async () =>
                {
                    await TestDelays.ShortDelay();
                    return "async_value";
                }, TestConstants.LongTimeoutMs);
            }
            await Task.WhenAll(tasksWithStats);
            stopwatchWithStatistics.Stop();

            // Act - Measure async cache hits without statistics
            var stopwatchWithoutStatistics = Stopwatch.StartNew();
            var tasksWithoutStats = new Task[iterations];
            for (int i = 0; i < iterations; i++)
            {
                tasksWithoutStats[i] = cacheWithoutStatistics.BlitzGet("async_key", async () =>
                {
                    await TestDelays.ShortDelay();
                    return "async_value";
                }, TestConstants.LongTimeoutMs);
            }
            await Task.WhenAll(tasksWithoutStats);
            stopwatchWithoutStatistics.Stop();

            // Assert
            var totalTimeMsWithStatistics = stopwatchWithStatistics.Elapsed.TotalMilliseconds;
            var avgTimePerOpWithStatistics = totalTimeMsWithStatistics / iterations;
            var totalTimeMsWithoutStatistics = stopwatchWithoutStatistics.Elapsed.TotalMilliseconds;
            var avgTimePerOpWithoutStatistics = totalTimeMsWithoutStatistics / iterations;

            Console.WriteLine($"=== Async Operations Performance Impact ===");
            Console.WriteLine($"Concurrent operations: {iterations:N0}");
            Console.WriteLine($"Total time With/Without: {totalTimeMsWithStatistics:F2}ms/{totalTimeMsWithoutStatistics:F2}ms");
            Console.WriteLine($"Average per operation With/Without: {avgTimePerOpWithStatistics:F6}ms/{avgTimePerOpWithoutStatistics:F6}ms");

            var stats = cacheWithStatistics.Statistics;
            Console.WriteLine($"Cache hits recorded: {stats.HitCount:N0}");
            Console.WriteLine($"Cache misses recorded: {stats.MissCount:N0}");
            Console.WriteLine($"Total operations: {stats.TotalOperations:N0}");

            // Verify accuracy under concurrency - we expect 1 miss (pre-population) + iterations hits
            Assert.AreEqual(1, stats.MissCount, "Should have exactly 1 miss from pre-population");
            Assert.AreEqual(iterations, stats.HitCount, "Should have all concurrent operations as hits");
            Assert.AreEqual(iterations + 1, stats.TotalOperations, "Should accurately count total operations");

            Console.WriteLine($"Without Statistics is: {(totalTimeMsWithStatistics - totalTimeMsWithoutStatistics) / iterations:F6}ms faster");
            Console.WriteLine($"Without Statistics is: {totalTimeMsWithStatistics / totalTimeMsWithoutStatistics * 100:F2}% faster");
            Console.WriteLine($"Average per operation With/Without: {avgTimePerOpWithStatistics:F6}ms/{avgTimePerOpWithoutStatistics:F6}ms");

            cacheWithStatistics.Dispose();
        }

        [Test]
        public void Statistics_PerformanceImpact_ConcurrentAccess()
        {
            // Arrange
            var cacheWithStatistics = TestFactory.CreateBlitzCacheInstance();
            cacheWithStatistics.InitializeStatistics();
            var cacheWithoutStatistics = TestFactory.CreateBlitzCacheGlobal();
            var threadsCount = 10;
            var operationsPerThread = 500;
            var totalOperations = threadsCount * operationsPerThread;

            // Act - Multiple threads accessing statistics simultaneously (with statistics)
            var stopwatchWithStatistics = Stopwatch.StartNew();
            var tasksWithStats = new Task[threadsCount];
            for (int t = 0; t < threadsCount; t++)
            {
                int threadId = t;
                tasksWithStats[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var key = $"thread_{threadId}_key_{i % 10}";
                        cacheWithStatistics.BlitzGet(key, () => $"value_{threadId}_{i}", TestConstants.LongTimeoutMs);
                        if (i % TestConstants.ConcurrentOperationsCount == 0)
                        {
                            var stats = cacheWithStatistics.Statistics;
                            var hitRatio = stats.HitRatio;
                        }
                    }
                });
            }
            Task.WaitAll(tasksWithStats);
            stopwatchWithStatistics.Stop();

            // Act - Multiple threads accessing cache without statistics
            var stopwatchWithoutStatistics = Stopwatch.StartNew();
            var tasksWithoutStats = new Task[threadsCount];
            for (int t = 0; t < threadsCount; t++)
            {
                int threadId = t;
                tasksWithoutStats[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        var key = $"thread_{threadId}_key_{i % 10}";
                        cacheWithoutStatistics.BlitzGet(key, () => $"value_{threadId}_{i}", TestConstants.LongTimeoutMs);
                    }
                });
            }
            Task.WaitAll(tasksWithoutStats);
            stopwatchWithoutStatistics.Stop();

            // Assert
            var totalTimeMsWithStatistics = stopwatchWithStatistics.Elapsed.TotalMilliseconds;
            var avgTimePerOpWithStatistics = totalTimeMsWithStatistics / totalOperations;
            var totalTimeMsWithoutStatistics = stopwatchWithoutStatistics.Elapsed.TotalMilliseconds;
            var avgTimePerOpWithoutStatistics = totalTimeMsWithoutStatistics / totalOperations;

            Console.WriteLine($"=== Concurrent Statistics Access Performance Impact ===");
            Console.WriteLine($"Threads: {threadsCount}");
            Console.WriteLine($"Total operations: {totalOperations:N0}");
            Console.WriteLine($"Total time With/Without: {totalTimeMsWithStatistics:F2}ms/{totalTimeMsWithoutStatistics:F2}ms");
            Console.WriteLine($"Average per operation With/Without: {avgTimePerOpWithStatistics:F6}ms/{avgTimePerOpWithoutStatistics:F6}ms");

            var finalStats = cacheWithStatistics.Statistics;
            Console.WriteLine($"Final hit count: {finalStats.HitCount:N0}");
            Console.WriteLine($"Final miss count: {finalStats.MissCount:N0}");
            Console.WriteLine($"Final hit ratio: {finalStats.HitRatio:P2}");
            Console.WriteLine($"Total operations recorded: {finalStats.TotalOperations:N0}");

            // Verify thread safety and accuracy
            Assert.AreEqual(totalOperations, finalStats.TotalOperations, "Should accurately count all operations under concurrency");
            Assert.Greater(finalStats.HitCount, 0, "Should have some hits due to key repetition");
            Assert.Greater(finalStats.MissCount, 0, "Should have some misses");

            Console.WriteLine($"Without Statistics is: {(totalTimeMsWithStatistics - totalTimeMsWithoutStatistics) / totalOperations:F6}ms faster");
            Console.WriteLine($"Without Statistics is: {totalTimeMsWithStatistics / totalTimeMsWithoutStatistics * 100:F2}% faster");
            Console.WriteLine($"Average per operation With/Without: {avgTimePerOpWithStatistics:F6}ms/{avgTimePerOpWithoutStatistics:F6}ms");

            cacheWithStatistics.Dispose();
        }

        [Test]
        public void Statistics_PerformanceImpact_Top_50_SlowestQueries()
        {
            var topSlowestQueries = 100;
            string GetRandomKey(int numberOfPossibleDifferentKeys)
            {
                return $"key_{Random.Shared.Next(numberOfPossibleDifferentKeys)}";
            }

            // Arrange
            var iterations = 10000;

            // 1. No statistics
            var cacheNoStats = TestFactory.CreateBlitzCacheInstance();
            cacheNoStats.BlitzGet(GetRandomKey(topSlowestQueries), () => "value"); // Pre-populate
            var swNoStats = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                cacheNoStats.BlitzGet(GetRandomKey(topSlowestQueries), () => "value", TestConstants.LongTimeoutMs);
            swNoStats.Stop();

            // 2. Statistics enabled, TopSlowestQueries disabled
            var cacheStatsNoTop = new BlitzCacheInstance(maxTopSlowest: 0);
            cacheStatsNoTop.InitializeStatistics();
            // Try to disable TopSlowestQueries tracking if API allows, otherwise assume default is off or set to 0
            if (cacheStatsNoTop.Statistics != null && cacheStatsNoTop.Statistics.TopSlowestQueries is IDisposable top)
                top.Dispose(); // If possible, disable tracking
            cacheStatsNoTop.BlitzGet(GetRandomKey(topSlowestQueries), () => "value"); // Pre-populate
            var swStatsNoTop = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                cacheStatsNoTop.BlitzGet(GetRandomKey(topSlowestQueries), () => "value", TestConstants.LongTimeoutMs);
            swStatsNoTop.Stop();

            // 3. Statistics enabled, TopSlowestQueries tracking 5
            var cacheStatsTop5 = new BlitzCacheInstance(maxTopSlowest: topSlowestQueries);
            cacheStatsTop5.InitializeStatistics();
            cacheStatsTop5.BlitzGet(GetRandomKey(topSlowestQueries), () => "value"); // Pre-populate
            var swStatsTop5 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
                cacheStatsTop5.BlitzGet(GetRandomKey(topSlowestQueries), () => "value", TestConstants.LongTimeoutMs);
            swStatsTop5.Stop();

            // Results
            var avgNoStats = swNoStats.Elapsed.TotalMilliseconds / iterations;
            var avgStatsNoTop = swStatsNoTop.Elapsed.TotalMilliseconds / iterations;
            var avgStatsTop5 = swStatsTop5.Elapsed.TotalMilliseconds / iterations;

            Console.WriteLine($"=== TopSlowestQueries Performance Impact ===");
            Console.WriteLine($"Operations: {iterations:N0}");
            Console.WriteLine($"No statistics: {avgNoStats:F6}ms/op");
            Console.WriteLine($"Statistics, no TopSlowestQueries: {avgStatsNoTop:F6}ms/op");
            Console.WriteLine($"Statistics, TopSlowestQueries({topSlowestQueries}): {avgStatsTop5:F6}ms/op");

            // Assert that all are performant, and TopSlowestQueries does not add excessive overhead
            Assert.Less(avgStatsNoTop, avgNoStats * 5, "Statistics overhead should be reasonable");
            Assert.Less(avgStatsTop5, avgStatsNoTop * 5, "TopSlowestQueries overhead should be reasonable");
        }
    }
}

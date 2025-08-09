using BlitzCacheCore.Statistics;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Statistics
{
    [TestFixture]
    public class TopSlowestQueriesTests
    {
        [Test]
        public void AddAndGetTop_BasicFunctionality_WorksCorrectly()
        {
            var topN = 3;
            var top = new TopNTracker<SlowQuery>(topN, (key, ms) => new SlowQuery(key, ms));
            top.AddOrUpdate("A", 100);
            top.AddOrUpdate("B", 200);
            top.AddOrUpdate("C", 150);

            var results = top.Get().ToList();

            Assert.AreEqual(topN, results.Count);
            Assert.AreEqual("B", results.ElementAt(0).CacheKey);
            Assert.AreEqual("C", results.ElementAt(1).CacheKey);
            Assert.AreEqual("A", results.ElementAt(2).CacheKey);
        }

        [Test]
        public void Add_UpdatesExistingQuery_UpdatesStats()
        {
            var top = new TopNTracker<SlowQuery>(5, (key, ms) => new SlowQuery(key, ms));
            top.AddOrUpdate("B", 150);
            top.AddOrUpdate("A", 100);
            top.AddOrUpdate("A", 200);

            var results = top.Get().ToList();

            Assert.AreEqual(2, results.Count);
            var aStats = results.First();
            Assert.IsNotNull(aStats);
            Assert.AreEqual("A", aStats.CacheKey);
            Assert.AreEqual(200, aStats.WorstCaseMs);
            Assert.AreEqual(100, aStats.BestCaseMs);
            Assert.AreEqual(150, aStats.AverageMs);
            Assert.AreEqual(2, aStats.Occurrences);
        }

        [Test]
        public void Clear_EmptiesTheCollection()
        {
            var top = new TopNTracker<SlowQuery>(2, (key, ms) => new SlowQuery(key, ms));
            top.AddOrUpdate("A", 100);
            top.AddOrUpdate("B", 200);

            top.Clear();
            var results = top.Get().ToList();

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Add_MoreThanMaxSize_OnlyKeepsTopN()
        {
            var top = new TopNTracker<SlowQuery>(2, (key, ms) => new SlowQuery(key, ms));
            top.AddOrUpdate("A", 100);
            top.AddOrUpdate("B", 200);
            top.AddOrUpdate("C", 300);

            var results = top.Get().ToList();

            Assert.AreEqual(2, results.Count);
            // Should contain the two slowest
            Assert.IsTrue(results.First().CacheKey == "C");
            Assert.IsTrue(results.Last().CacheKey == "B");
        }

        [Test]
        public void Concurrent_AddAndUpdate_NoExceptionsAndCorrectness()
        {
            var topN = 5;
            var top = new TopNTracker<SlowQuery>(topN, (key, ms) => new SlowQuery(key, ms));
            int threads = 10;
            int perThread = 1000;
            var keys = new[] { "A", "B", "C", "D", "E", "F", "G" };
            var tasks = new List<Task>();

            for (int t = 0; t < threads; t++)
            {
                tasks.Add(Task.Run(() =>
                {
                    var rand = new Random(Thread.CurrentThread.ManagedThreadId + Environment.TickCount);
                    for (int i = 0; i < perThread; i++)
                    {
                        var key = keys[rand.Next(keys.Length)];
                        var duration = rand.Next(50, 1000);
                        top.AddOrUpdate(key, duration);
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());

            var results = top.Get().ToList();
            Assert.AreEqual(topN, results.Count);
            // Should not throw and should contain only valid keys
            foreach (var entry in results)
            {
                Assert.IsTrue(keys.Any(k => entry.CacheKey.Contains(k)) || string.IsNullOrWhiteSpace(entry.CacheKey));
            }
        }
    }
}

using BlitzCacheCore.Statistics;
using BlitzCacheCore.Statistics.Speed;
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

            Assert.That(results.Count, Is.EqualTo(topN));
            Assert.That(results.ElementAt(0).CacheKey, Is.EqualTo("B"));
            Assert.That(results.ElementAt(1).CacheKey, Is.EqualTo("C"));
            Assert.That(results.ElementAt(2).CacheKey, Is.EqualTo("A"));
        }

        [Test]
        public void Add_UpdatesExistingQuery_UpdatesStats()
        {
            var top = new TopNTracker<SlowQuery>(5, (key, ms) => new SlowQuery(key, ms));
            top.AddOrUpdate("B", 150);
            top.AddOrUpdate("A", 100);
            top.AddOrUpdate("A", 200);

            var results = top.Get().ToList();

            Assert.That(results.Count, Is.EqualTo(2));
            var aStats = results.First();
            Assert.That(aStats, Is.Not.Null);
            Assert.That(aStats.CacheKey, Is.EqualTo("A"));
            Assert.That(aStats.WorstCaseMs, Is.EqualTo(200));
            Assert.That(aStats.BestCaseMs, Is.EqualTo(100));
            Assert.That(aStats.AverageMs, Is.EqualTo(150));
            Assert.That(aStats.Occurrences, Is.EqualTo(2));
        }

        [Test]
        public void Clear_EmptiesTheCollection()
        {
            var top = new TopNTracker<SlowQuery>(2, (key, ms) => new SlowQuery(key, ms));
            top.AddOrUpdate("A", 100);
            top.AddOrUpdate("B", 200);

            top.Clear();
            var results = top.Get().ToList();

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public void Add_MoreThanMaxSize_OnlyKeepsTopN()
        {
            var top = new TopNTracker<SlowQuery>(2, (key, ms) => new SlowQuery(key, ms));
            top.AddOrUpdate("A", 100);
            top.AddOrUpdate("B", 200);
            top.AddOrUpdate("C", 300);

            var results = top.Get().ToList();

            Assert.That(results.Count, Is.EqualTo(2));
            // Should contain the two slowest
            Assert.That(results.First().CacheKey == "C", Is.True);
            Assert.That(results.Last().CacheKey == "B", Is.True);
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
            Assert.That(results.Count, Is.EqualTo(topN));
            // Should not throw and should contain only valid keys
            foreach (var entry in results)
            {
                Assert.That(keys.Any(k => entry.CacheKey.Contains(k)) || string.IsNullOrWhiteSpace(entry.CacheKey), Is.True);
            }
        }
    }
}

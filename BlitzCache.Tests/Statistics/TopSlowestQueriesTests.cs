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
            var topSlowest = new TopSlowestQueries(topN);
            topSlowest.Add("A", 100);
            topSlowest.Add("B", 200);
            topSlowest.Add("C", 150);

            var top = topSlowest.Get().ToList();

            Assert.AreEqual(topN, top.Count);
            Assert.AreEqual("B", top.ElementAt(0).CacheKey);
            Assert.AreEqual("C", top.ElementAt(1).CacheKey);
            Assert.AreEqual("A", top.ElementAt(2).CacheKey);
        }

        [Test]
        public void Add_UpdatesExistingQuery_UpdatesStats()
        {
            var topSlowest = new TopSlowestQueries(5);
            topSlowest.Add("B", 150);
            topSlowest.Add("A", 100);
            topSlowest.Add("A", 200);

            var top = topSlowest.Get().ToList();

            Assert.AreEqual(2, top.Count);
            var aStats = top.First();
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
            var topSlowest = new TopSlowestQueries(2);
            topSlowest.Add("A", 100);
            topSlowest.Add("B", 200);

            topSlowest.Clear();
            var top = topSlowest.Get().ToList();

            Assert.AreEqual(0, top.Count);
        }

        [Test]
        public void Add_MoreThanMaxSize_OnlyKeepsTopN()
        {
            var topSlowest = new TopSlowestQueries(2);
            topSlowest.Add("A", 100);
            topSlowest.Add("B", 200);
            topSlowest.Add("C", 300);

            var top = topSlowest.Get().ToList();

            Assert.AreEqual(2, top.Count);
            // Should contain the two slowest
            Assert.IsTrue(top.First().CacheKey == "C");
            Assert.IsTrue(top.Last().CacheKey == "B");
        }

        [Test]
        public void Concurrent_AddAndUpdate_NoExceptionsAndCorrectness()
        {
            var topN = 5;
            var topSlowest = new TopSlowestQueries(topN);
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
                        topSlowest.Add(key, duration);
                    }
                }));
            }
            Task.WaitAll(tasks.ToArray());

            var top = topSlowest.Get().ToList();
            Assert.AreEqual(topN, top.Count);
            // Should not throw and should contain only valid keys
            foreach (var entry in top)
            {
                Assert.IsTrue(keys.Any(k => entry.CacheKey.Contains(k)) || string.IsNullOrWhiteSpace(entry.CacheKey));
            }
        }
    }
}

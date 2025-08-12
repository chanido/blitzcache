using BlitzCacheCore.Statistics;
using BlitzCacheCore.Statistics.Speed;
using NUnit.Framework;

namespace BlitzCacheCore.Tests.Statistics
{
    [TestFixture]
    public class SlowQueryTests
    {

        [Test]
        public void Constructor_InitializesPropertiesCorrectly()
        {
            var sq = new SlowQuery("key1", 100);
            Assert.AreEqual("key1", sq.CacheKey);
            Assert.AreEqual(100, sq.WorstCaseMs);
            Assert.AreEqual(100, sq.BestCaseMs);
            Assert.AreEqual(100, sq.AverageMs);
            Assert.AreEqual(1, sq.Occurrences);
        }

        [Test]
        public void Update_UpdatesWorstBestAverageAndOccurrences()
        {
            var sq1 = new SlowQuery("key1", 100);

            sq1.Update(200);

            Assert.AreEqual(200, sq1.WorstCaseMs);
            Assert.AreEqual(100, sq1.BestCaseMs);
            Assert.AreEqual(150, sq1.AverageMs);
            Assert.AreEqual(2, sq1.Occurrences);
        }

        [Test]
        public void AreEquals_And_GetHashCode_BasedOnCacheKey()
        {
            var sq1 = new SlowQuery("key1", 100);
            var sq2 = new SlowQuery("key1", 200);
            var sq3 = new SlowQuery("key2", 100);
            Assert.True(sq1.Equals(sq2));
            Assert.False(sq1.Equals(sq3));
            Assert.AreEqual(sq1.GetHashCode(), sq2.GetHashCode());
            Assert.AreNotEqual(sq1.GetHashCode(), sq3.GetHashCode());
        }

        [Test]
        public void ToString_ContainsAllKeyStats()
        {
            var sq = new SlowQuery("key1", 100);
            var str = sq.ToString();
            Assert.True(str.Contains("key1"));
            Assert.True(str.Contains("Worse: 100ms"));
            Assert.True(str.Contains("Best: 100ms"));
            Assert.True(str.Contains("Avg: 100"));
            Assert.True(str.Contains("Occurrences: 1"));
        }

        [Test]
        public void IsFaster_WorkAsExpected()
        {
            var sq1 = new SlowQuery("key1", 100);

            Assert.False(sq1.IsFasterThan(99));
            Assert.False(sq1.IsFasterThan(100));
            Assert.True(sq1.IsFasterThan(200));
        }

        [Test]
        public void IsIComparable()
        {
            var sq1 = new SlowQuery("key1", 100);
            var sq2 = new SlowQuery("key2", 200);
            var sq3 = new SlowQuery("key3", 100);

            Assert.AreEqual(-1, sq1.CompareTo(sq2));
            Assert.AreEqual(0, sq1.CompareTo(sq3));
            Assert.AreEqual(1, sq2.CompareTo(sq1));
        }
    }
}

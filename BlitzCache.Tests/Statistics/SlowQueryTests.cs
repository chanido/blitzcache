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
            Assert.That(sq.CacheKey, Is.EqualTo("key1"));
            Assert.That(sq.WorstCaseMs, Is.EqualTo(100));
            Assert.That(sq.BestCaseMs, Is.EqualTo(100));
            Assert.That(sq.AverageMs, Is.EqualTo(100));
            Assert.That(sq.Occurrences, Is.EqualTo(1));
        }

        [Test]
        public void Update_UpdatesWorstBestAverageAndOccurrences()
        {
            var sq1 = new SlowQuery("key1", 100);

            sq1.Update(200);

            Assert.That(sq1.WorstCaseMs, Is.EqualTo(200));
            Assert.That(sq1.BestCaseMs, Is.EqualTo(100));
            Assert.That(sq1.AverageMs, Is.EqualTo(150));
            Assert.That(sq1.Occurrences, Is.EqualTo(2));
        }

        [Test]
        public void AreEquals_And_GetHashCode_BasedOnCacheKey()
        {
            var sq1 = new SlowQuery("key1", 100);
            var sq2 = new SlowQuery("key1", 200);
            var sq3 = new SlowQuery("key2", 100);
            Assert.That(sq1.Equals(sq2), Is.True);
            Assert.That(sq1.Equals(sq3), Is.False);
            Assert.That(sq1.GetHashCode(), Is.EqualTo(sq2.GetHashCode()));
            Assert.That(sq1.GetHashCode(), Is.Not.EqualTo(sq3.GetHashCode()));
        }

        [Test]
        public void ToString_ContainsAllKeyStats()
        {
            var sq = new SlowQuery("key1", 100);
            var str = sq.ToString();
            Assert.That(str.Contains("key1"), Is.True);
            Assert.That(str.Contains("Worse: 100ms"), Is.True);
            Assert.That(str.Contains("Best: 100ms"), Is.True);
            Assert.That(str.Contains("Avg: 100"), Is.True);
            Assert.That(str.Contains("Occurrences: 1"), Is.True);
        }

        [Test]
        public void IsFaster_WorkAsExpected()
        {
            var sq1 = new SlowQuery("key1", 100);

            Assert.That(sq1.IsFasterThan(99), Is.False);
            Assert.That(sq1.IsFasterThan(100), Is.False);
            Assert.That(sq1.IsFasterThan(200), Is.True);
        }

        [Test]
        public void IsIComparable()
        {
            var sq1 = new SlowQuery("key1", 100);
            var sq2 = new SlowQuery("key2", 200);
            var sq3 = new SlowQuery("key3", 100);

            Assert.That(sq1.CompareTo(sq2), Is.EqualTo(-1));
            Assert.That(sq1.CompareTo(sq3), Is.EqualTo(0));
            Assert.That(sq2.CompareTo(sq1), Is.EqualTo(1));
        }
    }
}

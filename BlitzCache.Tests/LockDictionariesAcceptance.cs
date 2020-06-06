using BlitzCache.LockDictionaries;
using NUnit.Framework;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace BlitzCache.Tests
{
    public class LockDictionariesAcceptance
    {
        private const int numberOfTests = 50000;

        [Test]
        public void SemaphoreDictionaryPerformance()
        {
            var start = DateTime.Now;
            Parallel.For(0, numberOfTests, (i) =>
            {
                SemaphoreDictionary.Get(Guid.NewGuid().ToString());
            });

            var elapsed = (DateTime.Now - start).TotalMilliseconds;

            Assert.AreEqual(numberOfTests, SemaphoreDictionary.GetNumberOfLocks());
        }

        [Test]
        public void LockDictionaryPerformance()
        {
            var start = DateTime.Now;
            Parallel.For(0, numberOfTests, (i) =>
            {
                LockDictionary.Get(Guid.NewGuid().ToString());
            });

            var elapsed = (DateTime.Now - start).TotalMilliseconds;


            Assert.AreEqual(numberOfTests, LockDictionary.GetNumberOfLocks());
        }
    }
}

using BlitzCacheCore.Statistics;
using System;

namespace BlitzCacheCore.Tests.Helpers
{
    /// <summary>
    /// Test implementation of ICacheStatistics that throws exceptions to test error handling.
    /// </summary>
    public class FaultyTestCacheStatistics : ICacheStatistics
    {
        private int accessCount = 0;

        public long HitCount
        {
            get
            {
                var count = ++accessCount;
                if (count <= 2) // Throw exception on first two accesses
                    throw new InvalidOperationException("Test exception for error handling");
                return 10;
            }
        }

        public long MissCount => 5;
        public double HitRatio => 0.667;
        public long EntryCount => 8;
        public long EvictionCount => 2;
        public int ActiveSemaphoreCount => 3;
        public long TotalOperations => 15;
        public void Reset() { }
    }
}
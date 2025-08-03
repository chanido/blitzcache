using BlitzCacheCore;
using NUnit.Framework;
using System;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class DebugStatisticsTest
    {
        [Test]
        public void Debug_EvictionCount()
        {
            var cache = new BlitzCache(useGlobalCache: false);
            
            Console.WriteLine("=== Debug Eviction Count ===");
            
            // Initial state
            var initial = cache.Statistics;
            Console.WriteLine($"Initial eviction count: {initial.EvictionCount}");
            Console.WriteLine($"Initial entry count: {initial.EntryCount}");
            
            // Add an entry
            cache.BlitzGet("test_key", () => "test value", 30000);
            var afterAdd = cache.Statistics;
            Console.WriteLine($"After add eviction count: {afterAdd.EvictionCount}");
            Console.WriteLine($"After add entry count: {afterAdd.EntryCount}");
            
            // Remove the entry
            cache.Remove("test_key");
            var afterRemove = cache.Statistics;
            Console.WriteLine($"After remove eviction count: {afterRemove.EvictionCount}");
            Console.WriteLine($"After remove entry count: {afterRemove.EntryCount}");
            
            cache.Dispose();
        }
    }
}

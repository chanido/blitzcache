using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    /// <summary>
    /// Tests to verify that cache statistics accurately track automatic cache expirations.
    /// </summary>
    [TestFixture]
    public class CacheExpirationStatisticsTests
    {
        [Test]
        public async Task Statistics_AutomaticExpiration_TracksEvictionCorrectly()
        {
            // Arrange
            var cache = TestHelpers.CreateBlitzCacheInstanceWithStatistics();
            var testId = Guid.NewGuid().ToString("N")[..8]; // Unique test ID

            // Act - Add cache entries with short expiration (one at a time)
            cache.BlitzGet($"key1_{testId}", () => "value1", TestHelpers.StandardTimeoutMs);
            var statsAfterFirst = cache.Statistics;
            Console.WriteLine($"After first creation: {statsAfterFirst.EntryCount} entries, {statsAfterFirst.EvictionCount} evictions");

            // Verify first entry
            Assert.AreEqual(1, statsAfterFirst.EntryCount, "Should have 1 cached entry");
            Assert.AreEqual(0, statsAfterFirst.EvictionCount, "Should have no evictions after first creation");

            // Wait for automatic expiration
            await TestHelpers.WaitForStandardExpiration(); // Wait longer than expiration

            // Try to access the expired entry (this should trigger cleanup and create new entry)
            var result1 = cache.BlitzGet($"key1_{testId}", () => "new_value1", TestHelpers.StandardTimeoutMs);

            // Give some time for the eviction callbacks to execute
            await TestHelpers.WaitForEvictionCallbacks();

            var statsAfterExpiration = cache.Statistics;
            Console.WriteLine($"After expiration: {statsAfterExpiration.EntryCount} entries, {statsAfterExpiration.EvictionCount} evictions");
            Console.WriteLine($"Total operations: {statsAfterExpiration.TotalOperations}, Hits: {statsAfterExpiration.HitCount}, Misses: {statsAfterExpiration.MissCount}");

            // Assert - Verify that evictions were tracked
            Assert.Greater(statsAfterExpiration.EvictionCount, 0, "Should have tracked automatic evictions");
            Assert.AreEqual(1, statsAfterExpiration.EntryCount, "Should have 1 new entry after re-creation");

            // The second BlitzGet call should be a miss since the original entry expired
            Assert.AreEqual(2, statsAfterExpiration.MissCount, "Should have 2 misses (1 initial + 1 after expiration)");

            // Verify the new value was set
            Assert.AreEqual("new_value1", result1);

            cache.Dispose();
        }

        [Test]
        public async Task Statistics_ManualRemoval_DoesNotDoubleCountEvictions()
        {
            // Arrange
            var cache = TestHelpers.CreateBlitzCacheInstanceWithStatistics(); // Use instance cache to avoid interference
            var testId = Guid.NewGuid().ToString("N")[..8]; // Unique test ID

            // Get initial state to handle any pre-existing evictions
            var initialEvictionCount = cache.Statistics.EvictionCount;
            var initialEntryCount = cache.Statistics.EntryCount;
            Console.WriteLine($"Initial: {initialEvictionCount} evictions, {initialEntryCount} entries");

            // Act - Add and manually remove
            cache.BlitzGet($"test_key_{testId}", () => "test_value", TestHelpers.StandardTimeoutMs);
            var evictionCountAfterCreation = cache.Statistics.EvictionCount;

            cache.Remove($"test_key_{testId}");
            await TestHelpers.WaitForEvictionCallbacks();
            var evictionCountAfterRemoval = cache.Statistics.EvictionCount;
            var entryCountAfterRemoval = cache.Statistics.EntryCount;

            Console.WriteLine($"After creation: {evictionCountAfterCreation} evictions");
            Console.WriteLine($"After manual removal: {evictionCountAfterRemoval} evictions");

            // Calculate the actual evictions that happened during this test
            var evictionsDuringCreation = evictionCountAfterCreation - initialEvictionCount;
            var evictionsDuringRemoval = evictionCountAfterRemoval - evictionCountAfterCreation;

            // Assert - Manual removal should be counted exactly once, no evictions during creation
            Assert.AreEqual(0, evictionsDuringCreation, "Should have no evictions during creation");
            Assert.AreEqual(1, evictionsDuringRemoval, "Should have exactly 1 eviction during manual removal");
            Assert.AreEqual(0, entryCountAfterRemoval, "Should have 0 entries after removal");

            cache.Dispose();
        }

        [Test]
        public async Task Statistics_MixedEvictions_TracksAllCorrectly()
        {
            // Arrange
            var cache = TestHelpers.CreateBlitzCacheInstanceWithStatistics();

            // Act - Mix of automatic and manual evictions
            cache.BlitzGet("auto_expire", () => "value1", TestHelpers.StandardTimeoutMs);
            cache.BlitzGet("manual_remove", () => "value2", TestHelpers.StandardTimeoutMs);
            cache.BlitzGet("keep_alive", () => "value3", TestHelpers.StandardTimeoutMs);

            var statsAfterCreation = cache.Statistics;
            Console.WriteLine($"After creation: {statsAfterCreation.EntryCount} entries");

            // Manual removal
            cache.Remove("manual_remove");

            // Wait for automatic expiration
            await TestHelpers.WaitForStandardExpiration();

            // Access expired key to trigger callback
            cache.BlitzGet("auto_expire", () => "new_value", TestHelpers.StandardTimeoutMs);

            // Give time for callbacks
            await TestHelpers.WaitForEvictionCallbacks();

            var finalStats = cache.Statistics;
            Console.WriteLine($"Final: {finalStats.EntryCount} entries, {finalStats.EvictionCount} evictions");

            // Assert - Should track both types of evictions
            Assert.AreEqual(2, finalStats.EntryCount, "Should have 2 entries (keep_alive + new auto_expire)");
            Assert.Greater(finalStats.EvictionCount, 1, "Should have tracked both manual and automatic evictions");

            cache.Dispose();
        }

        [Test]
        public void Statistics_EntryCount_StaysAccurate()
        {
            // This test verifies that EntryCount reflects actual cache state
            var cache = TestHelpers.CreateBlitzCacheInstanceWithStatistics();

            // Add some entries
            cache.BlitzGet("key1", () => "value1", TestHelpers.StandardTimeoutMs);
            cache.BlitzGet("key2", () => "value2", TestHelpers.StandardTimeoutMs);
            cache.BlitzGet("key3", () => "value3", TestHelpers.StandardTimeoutMs);

            var entryCountAfter3Adds = cache.Statistics.EntryCount;
            Assert.AreEqual(3, entryCountAfter3Adds, "Should have 3 entries");

            // Remove one
            cache.Remove("key2");
            TestHelpers.WaitForEvictionCallbacksSync(); // Wait for eviction callback to complete

            var entryCountAfterRemoval = cache.Statistics.EntryCount;
            var evictionCountAfterRemoval = cache.Statistics.EvictionCount;
            Assert.AreEqual(2, entryCountAfterRemoval, "Should have 2 entries after removal");
            Assert.AreEqual(1, evictionCountAfterRemoval, "Should have 1 eviction");

            // Add another
            cache.BlitzGet("key4", () => "value4", TestHelpers.StandardTimeoutMs);

            var finalEntryCount = cache.Statistics.EntryCount;
            var finalEvictionCount = cache.Statistics.EvictionCount;
            Assert.AreEqual(3, finalEntryCount, "Should have 3 entries again");
            Assert.AreEqual(1, finalEvictionCount, "Eviction count should remain 1");

            cache.Dispose();
        }
    }
}

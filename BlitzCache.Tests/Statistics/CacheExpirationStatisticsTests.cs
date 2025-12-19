using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Statistics
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
            var cache = TestFactory.CreateBlitzCacheInstance();
            cache.InitializeStatistics();
            var testId = Guid.NewGuid().ToString("N")[..8]; // Unique test ID

            // Act - Add cache entries with short expiration (one at a time)
            cache.BlitzGet($"key1_{testId}", () => "value1", TestConstants.StandardTimeoutMs);
            var statsAfterFirst = cache.Statistics;
            Console.WriteLine($"After first creation: {statsAfterFirst.EntryCount} entries, {statsAfterFirst.EvictionCount} evictions");

            // Verify first entry
            Assert.That(statsAfterFirst.EntryCount, Is.EqualTo(1), "Should have 1 cached entry");
            Assert.That(statsAfterFirst.EvictionCount, Is.EqualTo(0), "Should have no evictions after first creation");

            // Wait for automatic expiration
            await TestDelays.WaitForStandardExpiration(); // Wait longer than expiration

            // Try to access the expired entry (this should trigger cleanup and create new entry)
            var result1 = cache.BlitzGet($"key1_{testId}", () => "new_value1", TestConstants.StandardTimeoutMs);

            // Give some time for the eviction callbacks to execute
            await TestDelays.WaitForStandardExpiration();

            var statsAfterExpiration = cache.Statistics;
            Console.WriteLine($"After expiration: {statsAfterExpiration.EntryCount} entries, {statsAfterExpiration.EvictionCount} evictions");
            Console.WriteLine($"Total operations: {statsAfterExpiration.TotalOperations}, Hits: {statsAfterExpiration.HitCount}, Misses: {statsAfterExpiration.MissCount}");

            // Assert - Verify that evictions were tracked
            Assert.That(statsAfterExpiration.EvictionCount, Is.GreaterThan(0), "Should have tracked automatic evictions");
            Assert.That(statsAfterExpiration.EntryCount, Is.EqualTo(1), "Should have 1 new entry after re-creation");

            // The second BlitzGet call should be a miss since the original entry expired
            Assert.That(statsAfterExpiration.MissCount, Is.EqualTo(2), "Should have 2 misses (1 initial + 1 after expiration)");

            // Verify the new value was set
            Assert.That(result1, Is.EqualTo("new_value1"));

            cache.Dispose();
        }

        [Test]
        public async Task Statistics_ManualRemoval_DoesNotDoubleCountEvictions()
        {
            // Arrange
            var cache = TestFactory.CreateBlitzCacheInstance(); // Use instance cache to avoid interference
            cache.InitializeStatistics();
            var testId = Guid.NewGuid().ToString("N")[..8]; // Unique test ID

            // Get initial state to handle any pre-existing evictions
            var initialEvictionCount = cache.Statistics.EvictionCount;
            var initialEntryCount = cache.Statistics.EntryCount;
            Console.WriteLine($"Initial: {initialEvictionCount} evictions, {initialEntryCount} entries");

            // Act - Add and manually remove
            cache.BlitzGet($"test_key_{testId}", () => "test_value", TestConstants.StandardTimeoutMs);
            var evictionCountAfterCreation = cache.Statistics.EvictionCount;

            cache.Remove($"test_key_{testId}");
            await TestDelays.WaitForEvictionCallbacks();
            var evictionCountAfterRemoval = cache.Statistics.EvictionCount;
            var entryCountAfterRemoval = cache.Statistics.EntryCount;

            Console.WriteLine($"After creation: {evictionCountAfterCreation} evictions");
            Console.WriteLine($"After manual removal: {evictionCountAfterRemoval} evictions");

            // Calculate the actual evictions that happened during this test
            var evictionsDuringCreation = evictionCountAfterCreation - initialEvictionCount;
            var evictionsDuringRemoval = evictionCountAfterRemoval - evictionCountAfterCreation;

            // Assert - Manual removal should be counted exactly once, no evictions during creation
            Assert.That(evictionsDuringCreation, Is.EqualTo(0), "Should have no evictions during creation");
            Assert.That(evictionsDuringRemoval, Is.EqualTo(1), "Should have exactly 1 eviction during manual removal");
            Assert.That(entryCountAfterRemoval, Is.EqualTo(0), "Should have 0 entries after removal");

            cache.Dispose();
        }

        [Test]
        public async Task Statistics_MixedEvictions_TracksAllCorrectly()
        {
            // Arrange
            var cache = TestFactory.CreateBlitzCacheInstance();
            cache.InitializeStatistics();

            // Act - Mix of automatic and manual evictions
            cache.BlitzGet("auto_expire", () => "value1", TestConstants.StandardTimeoutMs);
            cache.BlitzGet("manual_remove", () => "value2", TestConstants.StandardTimeoutMs);
            cache.BlitzGet("keep_alive", () => "value3", TestConstants.StandardTimeoutMs);

            var statsAfterCreation = cache.Statistics;
            Console.WriteLine($"After creation: {statsAfterCreation.EntryCount} entries");

            // Manual removal
            cache.Remove("manual_remove");

            // Wait for automatic expiration
            await TestDelays.WaitForStandardExpiration();

            // Access expired key to trigger callback
            cache.BlitzGet("auto_expire", () => "new_value", TestConstants.StandardTimeoutMs);

            // Give time for callbacks
            await TestDelays.WaitForEvictionCallbacks();

            var finalStats = cache.Statistics;
            Console.WriteLine($"Final: {finalStats.EntryCount} entries, {finalStats.EvictionCount} evictions");

            // Assert - Should track both types of evictions
            Assert.That(finalStats.EntryCount, Is.EqualTo(2), "Should have 2 entries (keep_alive + new auto_expire)");
            Assert.That(finalStats.EvictionCount, Is.GreaterThan(1), "Should have tracked both manual and automatic evictions");

            cache.Dispose();
        }

        [Test]
        public void Statistics_EntryCount_StaysAccurate()
        {
            // This test verifies that EntryCount reflects actual cache state
            var cache = TestFactory.CreateBlitzCacheInstance();
            cache.InitializeStatistics();

            // Add some entries
            cache.BlitzGet("key1", () => "value1", TestConstants.StandardTimeoutMs);
            cache.BlitzGet("key2", () => "value2", TestConstants.StandardTimeoutMs);
            cache.BlitzGet("key3", () => "value3", TestConstants.StandardTimeoutMs);

            var entryCountAfter3Adds = cache.Statistics.EntryCount;
            Assert.That(entryCountAfter3Adds, Is.EqualTo(3), "Should have 3 entries");

            // Remove one
            cache.Remove("key2");
            TestDelays.WaitForEvictionCallbacksSync(); // Wait for eviction callback to complete

            var entryCountAfterRemoval = cache.Statistics.EntryCount;
            var evictionCountAfterRemoval = cache.Statistics.EvictionCount;
            Assert.That(entryCountAfterRemoval, Is.EqualTo(2), "Should have 2 entries after removal");
            Assert.That(evictionCountAfterRemoval, Is.EqualTo(1), "Should have 1 eviction");

            // Add another
            cache.BlitzGet("key4", () => "value4", TestConstants.StandardTimeoutMs);

            var finalEntryCount = cache.Statistics.EntryCount;
            var finalEvictionCount = cache.Statistics.EvictionCount;
            Assert.That(finalEntryCount, Is.EqualTo(3), "Should have 3 entries again");
            Assert.That(finalEvictionCount, Is.EqualTo(1), "Eviction count should remain 1");

            cache.Dispose();
        }
    }
}

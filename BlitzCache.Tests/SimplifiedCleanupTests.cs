using System;
using System.Threading.Tasks;
using BlitzCacheCore.LockDictionaries;
using NUnit.Framework;

namespace BlitzCache.Tests
{
    [TestFixture]
    public class SimplifiedCleanupTests
    {
        [Test]
        public void SimplifiedCleanup_RemovesAllUnusedLocks()
        {
            // Arrange
            var keys = new[] { "test1", "test2", "test3" };

            // Act - Create and immediately release locks
            foreach (var key in keys)
            {
                using (var smartLock = SmartLockDictionary.GetSmartLock(key))
                {
                    // Lock is in use here
                }
                // Lock is now released and not in use
            }

            Console.WriteLine($"📊 Before cleanup: {SmartLockDictionary.GetNumberOfLocks()} locks");

            // Trigger cleanup
            SmartLockDictionary.TriggerCleanup();

            Console.WriteLine($"📊 After cleanup: {SmartLockDictionary.GetNumberOfLocks()} locks");

            // Assert - All locks should be cleaned up since they're not in use
            Assert.AreEqual(0, SmartLockDictionary.GetNumberOfLocks(), 
                "All unused locks should be cleaned up immediately");
        }

        [Test]
        public void SimplifiedCleanup_KeepsLocksInUse()
        {
            // Arrange - Create locks but keep them in use
            var smartLock1 = SmartLockDictionary.GetSmartLock("test1");
            var smartLock2 = SmartLockDictionary.GetSmartLock("test2");

            Console.WriteLine($"📊 Before cleanup: {SmartLockDictionary.GetNumberOfLocks()} locks");

            // Act - Trigger cleanup while locks are still in use
            SmartLockDictionary.TriggerCleanup();

            Console.WriteLine($"📊 After cleanup: {SmartLockDictionary.GetNumberOfLocks()} locks");

            // Assert - No locks should be cleaned up since they're all in use
            Assert.AreEqual(2, SmartLockDictionary.GetNumberOfLocks(), 
                "Locks in use should not be cleaned up");

            // Clean up
            smartLock1.Dispose();
            smartLock2.Dispose();

            // Now cleanup should work
            SmartLockDictionary.TriggerCleanup();
            Console.WriteLine($"📊 Final cleanup: {SmartLockDictionary.GetNumberOfLocks()} locks");
            
            Assert.AreEqual(0, SmartLockDictionary.GetNumberOfLocks(), 
                "All locks should be cleaned up after disposal");
        }
    }
}

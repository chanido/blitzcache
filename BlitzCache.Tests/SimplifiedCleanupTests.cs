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
        public void SimplifiedCleanup_RemovesAllUnusedSemaphores()
        {
            // Arrange
            var keys = new[] { "test1", "test2", "test3" };

            // Act - Create and immediately release semaphores
            foreach (var key in keys)
            {
                using (var smartSemaphore = SmartSemaphoreDictionary.GetSmartSemaphore(key))
                {
                    smartSemaphore.WaitAsync().GetAwaiter().GetResult();
                    // Semaphore is in use here
                    smartSemaphore.Release();
                }
                // Semaphore is now released and not in use
            }

            Console.WriteLine($"📊 Before cleanup: {SmartSemaphoreDictionary.GetNumberOfLocks()} semaphores");

            // Trigger cleanup
            SmartSemaphoreDictionary.TriggerCleanup();

            Console.WriteLine($"📊 After cleanup: {SmartSemaphoreDictionary.GetNumberOfLocks()} semaphores");

            // Assert - All semaphores should be cleaned up since they're not in use
            Assert.AreEqual(0, SmartSemaphoreDictionary.GetNumberOfLocks(), 
                "All unused semaphores should be cleaned up immediately");
        }

        [Test]
        public void SimplifiedCleanup_KeepsSemaphoresInUse()
        {
            // Arrange - Create semaphores but keep them in use
            var smartSemaphore1 = SmartSemaphoreDictionary.GetSmartSemaphore("test1");
            var smartSemaphore2 = SmartSemaphoreDictionary.GetSmartSemaphore("test2");

            // Acquire the semaphores
            smartSemaphore1.WaitAsync().GetAwaiter().GetResult();
            smartSemaphore2.WaitAsync().GetAwaiter().GetResult();

            Console.WriteLine($"📊 Before cleanup: {SmartSemaphoreDictionary.GetNumberOfLocks()} semaphores");

            // Act - Trigger cleanup while semaphores are still in use
            SmartSemaphoreDictionary.TriggerCleanup();

            Console.WriteLine($"📊 After cleanup: {SmartSemaphoreDictionary.GetNumberOfLocks()} semaphores");

            // Assert - No semaphores should be cleaned up since they're all in use
            Assert.AreEqual(2, SmartSemaphoreDictionary.GetNumberOfLocks(), 
                "Semaphores in use should not be cleaned up");

            // Clean up - release and dispose
            smartSemaphore1.Release();
            smartSemaphore2.Release();
            smartSemaphore1.Dispose();
            smartSemaphore2.Dispose();

            // Now cleanup should work
            SmartSemaphoreDictionary.TriggerCleanup();
            Console.WriteLine($"📊 Final cleanup: {SmartSemaphoreDictionary.GetNumberOfLocks()} semaphores");
            
            Assert.AreEqual(0, SmartSemaphoreDictionary.GetNumberOfLocks(), 
                "All semaphores should be cleaned up after disposal");
        }
    }
}

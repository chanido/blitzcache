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
                var semaphore = BlitzSemaphoreDictionary.GetSemaphore(key);
                using (var lockHandle = semaphore.AcquireAsync().GetAwaiter().GetResult())
                {
                    // Semaphore is in use here
                }
                // Semaphore is now released and not in use
            }

            Console.WriteLine($"📊 Before cleanup: {BlitzSemaphoreDictionary.GetNumberOfLocks()} semaphores");

            // Trigger cleanup
            BlitzSemaphoreDictionary.TriggerCleanup();

            Console.WriteLine($"📊 After cleanup: {BlitzSemaphoreDictionary.GetNumberOfLocks()} semaphores");

            // Assert - All semaphores should be cleaned up since they're not in use
            Assert.AreEqual(0, BlitzSemaphoreDictionary.GetNumberOfLocks(), 
                "All unused semaphores should be cleaned up immediately");
        }

        [Test]
        public void SimplifiedCleanup_KeepsSemaphoresInUse()
        {
            // Arrange - Create semaphores but keep them in use
            var semaphore1 = BlitzSemaphoreDictionary.GetSemaphore("test1");
            var semaphore2 = BlitzSemaphoreDictionary.GetSemaphore("test2");

            // Acquire the semaphores
            var lockHandle1 = semaphore1.AcquireAsync().GetAwaiter().GetResult();
            var lockHandle2 = semaphore2.AcquireAsync().GetAwaiter().GetResult();

            Console.WriteLine($"📊 Before cleanup: {BlitzSemaphoreDictionary.GetNumberOfLocks()} semaphores");

            // Act - Trigger cleanup while semaphores are still in use
            BlitzSemaphoreDictionary.TriggerCleanup();

            Console.WriteLine($"📊 After cleanup: {BlitzSemaphoreDictionary.GetNumberOfLocks()} semaphores");

            // Assert - No semaphores should be cleaned up since they're all in use
            Assert.AreEqual(2, BlitzSemaphoreDictionary.GetNumberOfLocks(), 
                "Semaphores in use should not be cleaned up");

            // Clean up - release the locks
            lockHandle1.Dispose();
            lockHandle2.Dispose();

            // Now cleanup should work
            BlitzSemaphoreDictionary.TriggerCleanup();
            Console.WriteLine($"📊 Final cleanup: {BlitzSemaphoreDictionary.GetNumberOfLocks()} semaphores");
            
            Assert.AreEqual(0, BlitzSemaphoreDictionary.GetNumberOfLocks(), 
                "All semaphores should be cleaned up after disposal");
        }
    }
}

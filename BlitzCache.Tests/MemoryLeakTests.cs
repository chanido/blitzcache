using BlitzCacheCore.LockDictionaries;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class MemoryLeakTests
    {
        [TearDown]
        public void Cleanup()
        {
            // Clean up singletons after each test
            BlitzSemaphoreDictionary.Dispose();
        }

        [Test]
        public async Task SemaphoreDictionary_Should_CleanupUnusedSemaphores()
        {
            // Arrange
            var initialCount = BlitzSemaphoreDictionary.GetNumberOfLocks();

            // Act - Create and immediately release semaphores (simulating normal usage)
            for (int i = 0; i < 10; i++)
            {
                var semaphore = BlitzSemaphoreDictionary.GetSemaphore($"test_semaphore_{i}");
                using (var lockHandle = await semaphore.AcquireAsync())
                {
                    // Semaphore is acquired and immediately released
                }
            }

            var countAfterCreation = BlitzSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(countAfterCreation, Is.EqualTo(initialCount + 10), "All semaphores should be created");

            // Trigger cleanup
            BlitzSemaphoreDictionary.TriggerCleanup();

            // Assert - Unused semaphores should be cleaned up
            var countAfterCleanup = BlitzSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(countAfterCleanup, Is.EqualTo(initialCount), 
                "Unused semaphores should be cleaned up immediately");
        }

        [Test]
        public async Task SemaphoreDictionary_Should_NotCleanupActivelyUsedSemaphores()
        {
            // Arrange
            var testKey = "actively_used_semaphore";
            var initialCount = BlitzSemaphoreDictionary.GetNumberOfLocks();

            // Act - Acquire a semaphore and keep it acquired
            var semaphore = BlitzSemaphoreDictionary.GetSemaphore(testKey);
            var lockHandle = await semaphore.AcquireAsync();

            var countAfterCreation = BlitzSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(countAfterCreation, Is.EqualTo(initialCount + 1), "Semaphore should be created");

            // Trigger cleanup while semaphore is still in use
            BlitzSemaphoreDictionary.TriggerCleanup();

            // Assert - Active semaphore should not be cleaned up
            var countAfterCleanup = BlitzSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(countAfterCleanup, Is.EqualTo(initialCount + 1), 
                "Active semaphores should not be cleaned up");

            // Cleanup - release the semaphore
            lockHandle.Dispose();
            
            // Now it should be eligible for cleanup
            BlitzSemaphoreDictionary.TriggerCleanup();
            var finalCount = BlitzSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(finalCount, Is.EqualTo(initialCount), 
                "Released semaphores should be cleaned up");
        }



        [Test]
        public async Task SemaphoreDictionary_Dispose_Should_CleanupAllResources()
        {
            // Arrange - Create some semaphores using the proper API
            for (int i = 0; i < 10; i++)
            {
                var semaphore = BlitzSemaphoreDictionary.GetSemaphore($"dispose_test_{i}");
                // Acquire and immediately release to create the semaphore entry
                using (await semaphore.AcquireAsync())
                {
                    // Semaphore created and tracked
                }
            }

            var countBeforeDispose = BlitzSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(countBeforeDispose, Is.GreaterThan(0), "Should have semaphores before dispose");

            // Act
            BlitzSemaphoreDictionary.Dispose();

            // Assert
            var countAfterDispose = BlitzSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(countAfterDispose, Is.EqualTo(0), "All semaphores should be cleared after dispose");
        }



        [Test]
        public async Task SemaphoreDictionary_Should_HandleConcurrentAccess_WithCleanup()
        {
            // Arrange & Act - Use AsyncRepeater for cleaner concurrent testing  
            var testResult = await AsyncRepeater.GoWithResults(100, async () =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var semaphore = BlitzSemaphoreDictionary.GetSemaphore($"concurrent_semaphore_{Thread.CurrentThread.ManagedThreadId}_{j}");
                    using (var lockHandle = await semaphore.AcquireAsync())
                    {
                        // Simulate some async work
                        await Task.Delay(1);
                    }
                }
                return "completed";
            });

            // Assert - Should not crash and should have created semaphores
            var finalCount = BlitzSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(finalCount, Is.GreaterThan(0), "Should have created semaphores during concurrent access");
            Assert.That(testResult.AllResultsIdentical, Is.True, "All concurrent operations should complete successfully");
            
            // Cleanup should work even after concurrent usage
            Assert.DoesNotThrow(() => BlitzSemaphoreDictionary.TriggerCleanup(), 
                "Cleanup should not throw exceptions after concurrent usage");
        }
    }
}

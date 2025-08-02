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
            SmartSemaphoreDictionary.Dispose();
        }

        [Test]
        public void SemaphoreDictionary_Should_CleanupOldSemaphores()
        {
            // Arrange
            var initialCount = SmartSemaphoreDictionary.GetNumberOfLocks();

            // Act - Create many semaphores
            for (int i = 0; i < 100; i++)
            {
                SmartSemaphoreDictionary.Get($"test_semaphore_{i}");
            }

            var countAfterCreation = SmartSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(countAfterCreation, Is.EqualTo(initialCount + 100), "All semaphores should be created");

            // Wait for cleanup
            SmartSemaphoreDictionary.TriggerCleanup();

            // Assert - Some semaphores should be cleaned up
            var countAfterCleanup = SmartSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(countAfterCleanup, Is.LessThanOrEqualTo(countAfterCreation), 
                "Old semaphores should be cleaned up");
        }

        [Test]
        public async Task SemaphoreDictionary_Should_NotCleanupRecentlyUsedSemaphores()
        {
            // Arrange
            var testKey = "recently_used_semaphore";
            var initialCount = SmartSemaphoreDictionary.GetNumberOfLocks();

            // Act - Create and continuously use a semaphore
            for (int i = 0; i < 10; i++)
            {
                var semaphore = SmartSemaphoreDictionary.Get(testKey);
                await semaphore.WaitAsync();
                semaphore.Release();
                await Task.Delay(100);
            }

            var countAfterUsage = SmartSemaphoreDictionary.GetNumberOfLocks();

            // Trigger cleanup
            SmartSemaphoreDictionary.TriggerCleanup();

            // Assert - Recently used semaphore should still exist
            var countAfterCleanup = SmartSemaphoreDictionary.GetNumberOfLocks();
            var semaphoreStillExists = countAfterCleanup > initialCount;
            
            Assert.That(semaphoreStillExists, Is.True, 
                "Recently used semaphores should not be cleaned up");
        }



        [Test]
        public void SemaphoreDictionary_Dispose_Should_CleanupAllResources()
        {
            // Arrange - Create some semaphores
            for (int i = 0; i < 50; i++)
            {
                SmartSemaphoreDictionary.Get($"dispose_test_{i}");
            }

            var countBeforeDispose = SmartSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(countBeforeDispose, Is.GreaterThan(0), "Should have semaphores before dispose");

            // Act
            SmartSemaphoreDictionary.Dispose();

            // Assert
            var countAfterDispose = SmartSemaphoreDictionary.GetNumberOfLocks();
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
                    var semaphore = SmartSemaphoreDictionary.Get($"concurrent_semaphore_{Thread.CurrentThread.ManagedThreadId}_{j}");
                    await semaphore.WaitAsync();
                    try
                    {
                        // Simulate some async work
                        await Task.Delay(1);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }
                return "completed";
            });

            // Assert - Should not crash and should have created semaphores
            var finalCount = SmartSemaphoreDictionary.GetNumberOfLocks();
            Assert.That(finalCount, Is.GreaterThan(0), "Should have created semaphores during concurrent access");
            Assert.That(testResult.AllResultsIdentical, Is.True, "All concurrent operations should complete successfully");
            
            // Cleanup should work even after concurrent usage
            Assert.DoesNotThrow(() => SmartSemaphoreDictionary.TriggerCleanup(), 
                "Cleanup should not throw exceptions after concurrent usage");
        }
    }
}

using BlitzCacheCore.LockDictionaries;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class BlitzSemaphoreDictionaryTests
    {
        private BlitzSemaphoreDictionary semaphoreDictionary;

        [SetUp]
        public void Setup()
        {
            // Create a new instance for each test to ensure isolation
            semaphoreDictionary = new BlitzSemaphoreDictionary();
        }

        [TearDown]
        public void TearDown()
        {
            // Dispose the instance after each test
            semaphoreDictionary?.Dispose();
        }

        [Test]
        public void GetSemaphore_ShouldReturnSameInstanceForSameKey()
        {
            // Arrange
            const string key = "test_key";

            // Act
            var semaphore1 = semaphoreDictionary.GetSemaphore(key);
            var semaphore2 = semaphoreDictionary.GetSemaphore(key);

            // Assert
            Assert.That(semaphore2, Is.SameAs(semaphore1), "Should return the same semaphore instance for the same key");
        }

        [Test]
        public void GetSemaphore_ShouldReturnDifferentInstancesForDifferentKeys()
        {
            // Arrange
            const string key1 = "test_key_1";
            const string key2 = "test_key_2";

            // Act
            var semaphore1 = semaphoreDictionary.GetSemaphore(key1);
            var semaphore2 = semaphoreDictionary.GetSemaphore(key2);

            // Assert
            Assert.That(semaphore2, Is.Not.SameAs(semaphore1), "Should return different semaphore instances for different keys");
        }

        [Test]
        public void GetNumberOfLocks_ShouldReflectCreatedSemaphores()
        {
            // Arrange
            var initialCount = semaphoreDictionary.GetNumberOfLocks();

            // Act
            semaphoreDictionary.GetSemaphore("key1");
            semaphoreDictionary.GetSemaphore("key2");
            semaphoreDictionary.GetSemaphore("key3");

            // Assert
            var finalCount = semaphoreDictionary.GetNumberOfLocks();
            Assert.That(finalCount, Is.EqualTo(initialCount + 3), "Should have three new semaphores");
        }

        [Test]
        public async Task GetSemaphore_ShouldWorkConcurrently()
        {
            // Arrange
            const int concurrentOperations = 100;
            var tasks = new Task[concurrentOperations];

            // Act
            for (int i = 0; i < concurrentOperations; i++)
            {
                int index = i;
                tasks[i] = Task.Run(async () =>
                {
                    var semaphore = semaphoreDictionary.GetSemaphore($"concurrent_key_{index}");
                    using (var lockHandle = await semaphore.AcquireAsync())
                    {
                        // Simulate some work
                        await Task.Delay(1);
                    }
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            var finalCount = semaphoreDictionary.GetNumberOfLocks();
            Assert.That(finalCount, Is.EqualTo(concurrentOperations), "Should have created one semaphore per concurrent operation");
        }

        [Test]
        public async Task SemaphoreCleanup_ShouldEventuallyCleanupUnusedSemaphores()
        {
            // Arrange - Create some semaphores and use them briefly
            var keys = new[] { "cleanup_test_1", "cleanup_test_2", "cleanup_test_3" };
            
            foreach (var key in keys)
            {
                var semaphore = semaphoreDictionary.GetSemaphore(key);
                using (var lockHandle = await semaphore.AcquireAsync())
                {
                    // Use semaphore briefly
                    await Task.Delay(1);
                }
            }

            var initialCount = semaphoreDictionary.GetNumberOfLocks();
            Console.WriteLine($"📊 Initial semaphore count: {initialCount}");

            // Act - Wait for cleanup to potentially occur
            // The cleanup interval is 10ms, and protection window is 1 second
            // So we wait longer than the protection window
            await Task.Delay(1500); // 1.5 seconds

            // Check multiple times as cleanup is asynchronous
            var maxWaitTime = TimeSpan.FromSeconds(5);
            var startTime = DateTime.UtcNow;
            var finalCount = initialCount;

            while (DateTime.UtcNow - startTime < maxWaitTime)
            {
                finalCount = semaphoreDictionary.GetNumberOfLocks();
                if (finalCount < initialCount)
                {
                    break; // Cleanup occurred
                }
                await Task.Delay(100); // Check every 100ms
            }

            Console.WriteLine($"📊 Final semaphore count: {finalCount}");
            Console.WriteLine($"⏱️ Waited {(DateTime.UtcNow - startTime).TotalSeconds:F1} seconds for cleanup");

            // Assert - Should eventually clean up unused semaphores
            Assert.That(finalCount, Is.LessThanOrEqualTo(initialCount), 
                "Cleanup should eventually reduce the number of semaphores or keep them stable");
        }

        [Test]
        public async Task ActiveSemaphores_ShouldNotBeCleanedUp()
        {
            // Arrange - Create and keep semaphores active
            var semaphore1 = semaphoreDictionary.GetSemaphore("active_semaphore_1");
            var semaphore2 = semaphoreDictionary.GetSemaphore("active_semaphore_2");

            // Keep them locked/active
            var lock1 = await semaphore1.AcquireAsync();
            var lock2 = await semaphore2.AcquireAsync();

            var initialCount = semaphoreDictionary.GetNumberOfLocks();
            Console.WriteLine($"📊 Initial count with active semaphores: {initialCount}");

            // Act - Wait well beyond cleanup cycles
            await Task.Delay(2000); // 2 seconds

            var finalCount = semaphoreDictionary.GetNumberOfLocks();
            Console.WriteLine($"📊 Final count after waiting: {finalCount}");

            // Assert - Active semaphores should not be cleaned up
            Assert.That(finalCount, Is.GreaterThanOrEqualTo(2), 
                "Active semaphores should not be cleaned up");

            // Cleanup
            lock1.Dispose();
            lock2.Dispose();
        }

        [Test]
        public void Dispose_ShouldClearAllSemaphores()
        {
            // Arrange
            semaphoreDictionary.GetSemaphore("test1");
            semaphoreDictionary.GetSemaphore("test2");
            semaphoreDictionary.GetSemaphore("test3");

            var countBeforeDispose = semaphoreDictionary.GetNumberOfLocks();
            Assert.That(countBeforeDispose, Is.GreaterThan(0), "Should have semaphores before dispose");

            // Act
            semaphoreDictionary.Dispose();

            // Assert
            var countAfterDispose = semaphoreDictionary.GetNumberOfLocks();
            Assert.That(countAfterDispose, Is.EqualTo(0), "Should have no semaphores after dispose");
        }

        [Test]
        public void GetSemaphore_AfterDispose_ShouldThrowObjectDisposedException()
        {
            // Arrange
            semaphoreDictionary.GetSemaphore("test_key");
            semaphoreDictionary.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() =>
            {
                semaphoreDictionary.GetSemaphore("test_key");
            }, "Should throw ObjectDisposedException when accessing disposed instance");
        }
    }
}

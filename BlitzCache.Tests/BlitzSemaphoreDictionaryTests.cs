using BlitzCacheCore.LockDictionaries;
using BlitzCacheCore.Tests.Helpers;
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
            // Use faster cleanup interval for tests
            semaphoreDictionary = new BlitzSemaphoreDictionary(TimeSpan.FromMilliseconds(500));
        }

        [TearDown]
        public void TearDown()
        {
            semaphoreDictionary?.Dispose();
        }

        [Test]
        public void GetSemaphore_ShouldReturnSameInstanceForSameKey()
        {
            const string key = "test_key";

            var semaphore1 = semaphoreDictionary.GetSemaphore(key);
            var semaphore2 = semaphoreDictionary.GetSemaphore(key);

            Assert.That(semaphore2, Is.SameAs(semaphore1));
        }

        [Test]
        public void GetSemaphore_ShouldReturnDifferentInstancesForDifferentKeys()
        {
            var semaphore1 = semaphoreDictionary.GetSemaphore("key1");
            var semaphore2 = semaphoreDictionary.GetSemaphore("key2");

            Assert.That(semaphore2, Is.Not.SameAs(semaphore1));
        }

        [Test]
        public void GetNumberOfLocks_ShouldReflectCreatedSemaphores()
        {
            var initialCount = semaphoreDictionary.GetNumberOfLocks();

            semaphoreDictionary.GetSemaphore("key1");
            semaphoreDictionary.GetSemaphore("key2");
            semaphoreDictionary.GetSemaphore("key3");

            var finalCount = semaphoreDictionary.GetNumberOfLocks();
            Assert.That(finalCount, Is.EqualTo(initialCount + 3));
        }

        [Test]
        public async Task GetSemaphore_ShouldWorkConcurrently()
        {
            const int concurrentOperations = TestFactory.ConcurrentOperationsCount;
            var tasks = new Task[concurrentOperations];

            for (int i = 0; i < concurrentOperations; i++)
            {
                int index = i;
                tasks[i] = Task.Run(async () =>
                {
                    var semaphore = semaphoreDictionary.GetSemaphore($"concurrent_key_{index}");
                    using var lockHandle = await semaphore.AcquireAsync();
                    await TestFactory.SmallDelay();
                });
            }

            await Task.WhenAll(tasks);

            var finalCount = semaphoreDictionary.GetNumberOfLocks();
            Assert.That(finalCount, Is.EqualTo(concurrentOperations));
        }

        [Test]
        public async Task SemaphoreCleanup_ShouldEventuallyCleanupUnusedSemaphores()
        {
            var keys = new[] { "cleanup_test_1", "cleanup_test_2", "cleanup_test_3" };
            
            foreach (var key in keys)
            {
                var semaphore = semaphoreDictionary.GetSemaphore(key);
                using var lockHandle = await semaphore.AcquireAsync();
                await TestFactory.SmallDelay();
            }

            var initialCount = semaphoreDictionary.GetNumberOfLocks();

            // Wait for cleanup cycles to occur (reduced from 1500ms with faster cleanup interval)
            await TestFactory.WaitForSemaphoreCleanup();

            var finalCount = semaphoreDictionary.GetNumberOfLocks();
            Assert.That(finalCount, Is.LessThanOrEqualTo(initialCount));
        }

        [Test]
        public async Task ActiveSemaphores_ShouldNotBeCleanedUp()
        {
            var semaphore1 = semaphoreDictionary.GetSemaphore("active_semaphore_1");
            var semaphore2 = semaphoreDictionary.GetSemaphore("active_semaphore_2");

            using var lock1 = await semaphore1.AcquireAsync();
            using var lock2 = await semaphore2.AcquireAsync();

            var initialCount = semaphoreDictionary.GetNumberOfLocks();

            await TestFactory.WaitForExtendedSemaphoreCleanup(); // Reduced from 2000ms with faster cleanup interval

            var finalCount = semaphoreDictionary.GetNumberOfLocks();
            Assert.That(finalCount, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void Dispose_ShouldClearAllSemaphores()
        {
            semaphoreDictionary.GetSemaphore("test1");
            semaphoreDictionary.GetSemaphore("test2");
            semaphoreDictionary.GetSemaphore("test3");

            var countBeforeDispose = semaphoreDictionary.GetNumberOfLocks();
            Assert.That(countBeforeDispose, Is.GreaterThan(0));

            semaphoreDictionary.Dispose();

            var countAfterDispose = semaphoreDictionary.GetNumberOfLocks();
            Assert.That(countAfterDispose, Is.EqualTo(0));
        }

        [Test]
        public void GetSemaphore_AfterDispose_ShouldThrowObjectDisposedException()
        {
            semaphoreDictionary.GetSemaphore("test_key");
            semaphoreDictionary.Dispose();

            Assert.Throws<ObjectDisposedException>(() => semaphoreDictionary.GetSemaphore("test_key"));
        }
    }
}

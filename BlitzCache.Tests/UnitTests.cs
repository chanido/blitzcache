using BlitzCacheCore.Extensions;
using BlitzCacheCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    public class UnitTests
    {
        private const int numberOfTests = 5000;
        private IBlitzCache cache;
        private ServiceProvider serviceProvider;

        [OneTimeSetUp]
        public void BeforeAll()
        {
            serviceProvider = new ServiceCollection()
                .AddBlitzCache()
                //.AddBlitzCache(30000) you can also specify the default timespan of the cache in milliseconds
                .BuildServiceProvider();

            cache = serviceProvider.GetService<IBlitzCache>();

            //Alternatively you can create a new instance of the BlitzCache directly without dependency injection
            //cache = new BlitzCache();
            //cache = new BlitzCache(30000);
        }

        [Test]
        public async Task ParallelAccessToAsyncMethod()
        {
            var slowClass = new SlowClassAsync();

            await AsyncRepeater.Go(numberOfTests, () => cache.BlitzGet(slowClass.ProcessQuickly));

            Assert.AreEqual(1, slowClass.Counter);
        }

        [Test]
        public async Task DifferentKeysWillCallTheAsyncMethodAgain()
        {
            var slowClass = new SlowClassAsync();

            var key1 = Guid.NewGuid().ToString();
            await AsyncRepeater.Go(numberOfTests, () => cache.BlitzGet(key1, slowClass.ProcessQuickly));

            var key2 = Guid.NewGuid().ToString();
            await AsyncRepeater.Go(numberOfTests, () => cache.BlitzGet(key2, slowClass.ProcessQuickly));

            Assert.AreEqual(2, slowClass.Counter);
        }

        [Test]
        public void ParallelAccessToSyncMethod()
        {
            var slowClass = new SlowClass();

            Parallel.For(0, numberOfTests, (i) =>
            {
                cache.BlitzGet(slowClass.ProcessQuickly);
            });


            Assert.AreEqual(1, slowClass.Counter);
        }

        [Test]
        public void DifferentKeysWillCallTheSyncMethodAgain()
        {
            var slowClass = new SlowClass();

            var key1 = Guid.NewGuid().ToString();
            Parallel.For(0, numberOfTests, (i) => { cache.BlitzGet(key1, slowClass.ProcessQuickly); });
            var key2 = Guid.NewGuid().ToString();
            Parallel.For(0, numberOfTests, (i) => { cache.BlitzGet(key2, slowClass.ProcessQuickly); });

            Assert.AreEqual(2, slowClass.Counter);
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            cache.Dispose();
            serviceProvider.Dispose();
        }
    }
}
using BlitzCacheCore.Extensions;
using BlitzCacheCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Threading;
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

        [Test]
        public void VariableTimespan()
        {
            var slowClass = new SlowClass();

            static string GetKey(int i) => i == 0 ? "Zero" : i % 2 == 0 ? "Even" : "Odd";

            bool? GetValueWithDifferentCacheRetention(Nuances n, int i)
            {
                bool? result = null;
                try { result = slowClass.FailIfZeroTrueIfEven(i); }
                catch { }

                switch (result)
                {
                    case null: n.CacheRetention = 1000; break; //Zero
                    case true: n.CacheRetention = 2000; break; //Even
                    case false: n.CacheRetention = 3000; break;//Odd
                }

                return result;
            }

            void WaitAndCheck(int milliseconds, int calls)
            {
                slowClass.ResetCounter();
                Thread.Sleep(milliseconds);
                Parallel.For(0, numberOfTests, (i) =>
                {
                    cache.BlitzGet(GetKey(i), (n) => GetValueWithDifferentCacheRetention(n, i));
                });

                Assert.AreEqual(calls, slowClass.Counter);
            }

            void CleanCache()
            {
                cache.Remove("Zero");
                cache.Remove("Even");
                cache.Remove("Odd");
            }

            WaitAndCheck(0, 3); //The first time we will call three times

            WaitAndCheck(500, 0); //If we wait only 500 everything should be cached

            WaitAndCheck(1100, 1); //If we wait 1100 only Zero should be recalculated

            CleanCache();

            WaitAndCheck(0, 3); //The first time we will call three times

            WaitAndCheck(2100, 2); //If we wait 2100 Zero and Even should be recalculated

            WaitAndCheck(1000, 2); //If we wait 1000 more Odd should be recalculated

            WaitAndCheck(3100, 3); //If we wait 3100 more everything should be recalculated

        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            cache.Dispose();
            serviceProvider.Dispose();
        }
    }
}
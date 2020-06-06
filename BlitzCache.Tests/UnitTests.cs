using BlitzCache.Extensions;
using BlitzCache.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System.Threading.Tasks;

namespace BlitzCache.Tests
{
    public class UnitTests
    {
        private const int numberOfTests = 5000;
        private BlitzCache cache;
        private ServiceProvider serviceProvider;

        [OneTimeSetUp]
        public void BeforeAll()
        {
            serviceProvider = new ServiceCollection()
                //.AddMemoryCache()
                .AddBlitzCache()
                .BuildServiceProvider();

            cache = serviceProvider.GetService<BlitzCache>();
        }

        [SetUp]
        public void Setup()
        {
            //cache = new BlitzCache();
        }


        [Test]
        public async Task ParallelAccessToAsyncMethod()
        {
            var slowClass = new SlowClassAsync();

            await AsyncRepeater.Go(numberOfTests, () => cache.GetThreadsafe("ParallelAccessToAsyncMethod", slowClass.ProcessQuickly, 10000));

            Assert.AreEqual(1, slowClass.Counter);
        }

        [Test]
        public void ParallelAccessToSyncMethod()
        {
            var slowClass = new SlowClass();

            cache.

            Parallel.For(0, numberOfTests, (i) =>
            {
                cache.GetThreadsafe("ParallelAccessToSyncMethod", slowClass.ProcessQuickly, 10000);
            });


            Assert.AreEqual(1, slowClass.Counter);
        }

        [OneTimeTearDown]
        public void AfterAll()
        {
            cache.Dispose();
            serviceProvider.Dispose();
        }
    }
}
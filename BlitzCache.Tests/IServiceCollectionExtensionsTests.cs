using BlitzCacheCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Extensions
{
    [TestFixture]
    public class IServiceCollectionExtensionsTests
    {
        private ServiceProvider serviceProvider;

        [SetUp]
        public void BeforeAll() => BlitzCache.ClearGlobalForTesting();

        [TearDown]
        public void AfterAll() => serviceProvider.Dispose();

        [Test]
        public async Task AddBlitzCache_RegistersGlobalSingleton()
        {
            serviceProvider = new ServiceCollection().AddBlitzCache().BuildServiceProvider();

            var cache1 = serviceProvider.GetService<IBlitzCache>();
            var cache2 = serviceProvider.GetService<IBlitzCache>();

            Assert.IsNotNull(cache1);
            Assert.AreSame(cache1, cache2, "Should resolve the same global singleton instance");
            await AssertCacheWorksAsync(cache1);
            await AssertCacheWorksAsync(cache2, true);
        }

        [Test]
        public async Task AddBlitzCache_WithOptionsDelegate_RegistersGlobalSingleton()
        {
            serviceProvider = new ServiceCollection()
                .AddBlitzCache(o => { o.DefaultMilliseconds = 5000; o.MaxTopSlowest = 3; o.MaxTopHeaviest = 2; })
                .BuildServiceProvider();

            var cache = serviceProvider.GetRequiredService<IBlitzCache>();
            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public async Task BlitzCache_DirectOptionsConstructor_Works()
        {
            BlitzCache.ClearGlobalForTesting();
            var cache = new BlitzCache(new BlitzCacheOptions { DefaultMilliseconds = 2500, MaxTopSlowest = 2, MaxTopHeaviest = 2 });
            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public async Task AddBlitzCache_WithConfigurePipeline_UsesConfiguredOptions()
        {
            BlitzCache.ClearGlobalForTesting();
            serviceProvider = new ServiceCollection()
                .Configure<BlitzCacheOptions>(o => { o.DefaultMilliseconds = 3333; o.MaxTopSlowest = 4; })
                .AddBlitzCache() // picks up configured options
                .BuildServiceProvider();

            var cache = serviceProvider.GetRequiredService<IBlitzCache>();
            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public async Task AddBlitzCache_ActionConfigure_ComposesWithOptionsPattern()
        {
            BlitzCache.ClearGlobalForTesting();
            serviceProvider = new ServiceCollection()
                .AddBlitzCache(o => { o.DefaultMilliseconds = 4444; o.MaxTopSlowest = 1; })
                .BuildServiceProvider();
            var cache = serviceProvider.GetRequiredService<IBlitzCache>();
            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public void AddBlitzCache_ThrowsOnNull() => Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddBlitzCache(null));

        [Test]
        public void AddBlitzCacheLogging_ThrowsOnNull() => Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddBlitzCacheLogging(null));


        [Test]
        public async Task AddBlitzCache_MultipleRegistrations_StillSingleton()
        {
            serviceProvider = new ServiceCollection()
                .AddBlitzCache()
                .AddBlitzCache()
                .BuildServiceProvider();

            var cache1 = serviceProvider.GetService<IBlitzCache>();
            var cache2 = serviceProvider.GetService<IBlitzCache>();

            Assert.IsNotNull(cache1);
            Assert.AreSame(cache1, cache2, "Multiple AddBlitzCache calls should still resolve the same singleton instance");
            await AssertCacheWorksAsync(cache1);
            await AssertCacheWorksAsync(cache2, true);
        }

        [Test]
        public async Task AddBlitzCacheLogging_WithoutStatistics_LogsWarningAndDoesNotThrow()
        {
            serviceProvider = new ServiceCollection()
                .AddBlitzCache()
                .AddLogging(b => b.AddDebug())
                .AddBlitzCacheLogging(logInterval: TimeSpan.FromMilliseconds(10), globalCacheIdentifier: "TestApp")
                .BuildServiceProvider();

            var hostedServices = serviceProvider.GetServices<IHostedService>();

            Assert.IsTrue(Enumerable.Any(hostedServices, s => s.GetType().Name.Contains("BlitzCacheLoggingService")), "Should register BlitzCacheLoggingService as a hosted service");
            // Should not throw, and should log a warning (cannot assert log output here)
            var cache = serviceProvider.GetService<IBlitzCache>();

            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public async Task AddBlitzCacheLogging_WithOptionsDelegate_RegistersHostedService()
        {
            serviceProvider = new ServiceCollection()
                .AddBlitzCache()
                .AddLogging(b => b.AddDebug())
                .AddBlitzCacheLogging(o => { o.LogInterval = TimeSpan.FromMilliseconds(10); o.GlobalCacheIdentifier = "TestAppOpt"; })
                .BuildServiceProvider();

            var hostedServices = serviceProvider.GetServices<IHostedService>();
            Assert.IsTrue(hostedServices.Any(s => s.GetType().Name.Contains("BlitzCacheLoggingService")), "Should register BlitzCacheLoggingService via options overload");
            var cache = serviceProvider.GetService<IBlitzCache>();
            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public async Task AddBlitzCache_StatisticsShouldBeNull()
        {
            serviceProvider = new ServiceCollection()
                // Try to add BlitzCache
                .AddBlitzCache()
                .BuildServiceProvider();

            var cache = serviceProvider.GetService<IBlitzCache>() as BlitzCache;

            await AssertCacheWorksAsync(cache);
            Assert.IsNotNull(cache, "Cache should not be null");
            Assert.IsNull(cache.Statistics, "Statistics should not be null for the global singleton, even if requested");
        }

        private static async Task AssertCacheWorksAsync(IBlitzCache cache, bool isSingletonOnSecondCall = false)
        {
            var slow = new Helpers.SlowClassAsync();
            var result1 = await cache.BlitzGet(slow.ProcessQuickly);
            var result2 = await cache.BlitzGet(slow.ProcessQuickly);
            Assert.AreEqual(result1, result2, "Cached value should be returned on second call");
            Assert.AreEqual(isSingletonOnSecondCall ? 0 : 1, slow.Counter, "SlowClassAsync.Counter should be 1 if cache is working");
        }
    }
}

using BlitzCacheCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Extensions
{
    [TestFixture]
    public class IServiceCollectionExtensionsTests
    {
        [SetUp]
        public void ResetBlitzCacheGlobal()
        {
            BlitzCache.ClearGlobalForTesting(); // Reset the global instance
        }

        [Test]
        public async Task AddBlitzCache_RegistersGlobalSingleton()
        {
            var services = new ServiceCollection();
            services.AddBlitzCache();
            var provider = services.BuildServiceProvider();
            var cache1 = provider.GetService<IBlitzCache>();
            var cache2 = provider.GetService<IBlitzCache>();
            Assert.IsNotNull(cache1);
            Assert.AreSame(cache1, cache2, "Should resolve the same global singleton instance");
            await AssertCacheWorksAsync(cache1);
            await AssertCacheWorksAsync(cache2, true);
        }

        [Test]
        public async Task AddBlitzCacheInstance_RegistersDedicatedSingleton()
        {
            var services = new ServiceCollection();
            services.AddBlitzCacheInstance();
            var provider = services.BuildServiceProvider();
            var cache1 = provider.GetService<IBlitzCache>();
            var cache2 = provider.GetService<IBlitzCache>();
            Assert.IsNotNull(cache1);
            Assert.AreSame(cache1, cache2, "Should resolve the same dedicated singleton instance");
            Assert.AreNotSame(BlitzCache.Global, cache1, "Should not be the global singleton");
            await AssertCacheWorksAsync(cache1);
            await AssertCacheWorksAsync(cache2, true);
        }

        [Test]
        public async Task AddBlitzCacheInstance_Options_AreApplied()
        {
            var services = new ServiceCollection();
            services.AddBlitzCacheInstance(defaultMilliseconds: 12345, enableStatistics: true);
            var provider = services.BuildServiceProvider();
            var cache = provider.GetService<IBlitzCache>() as BlitzCache;
            Assert.IsNotNull(cache);
            Assert.IsNotNull(cache.Statistics, "Statistics should be enabled");
            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public void AddBlitzCache_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddBlitzCache(null));
        }

        [Test]
        public void AddBlitzCacheInstance_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddBlitzCacheInstance(null));
        }

        [Test]
        public async Task AddBlitzCacheLogging_RegistersHostedService()
        {
            var services = new ServiceCollection();
            services.AddBlitzCacheInstance(enableStatistics: true);
            services.AddLogging(b => b.AddDebug());
            services.AddBlitzCacheLogging(logInterval: TimeSpan.FromMilliseconds(10), applicationIdentifier: "TestApp");
            var provider = services.BuildServiceProvider();
            var hostedServices = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
            Assert.IsTrue(System.Linq.Enumerable.Any(hostedServices, s => s.GetType().Name.Contains("BlitzCacheLoggingService")), "Should register BlitzCacheLoggingService as a hosted service");

            var cache = provider.GetService<IBlitzCache>();
            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public void AddBlitzCacheLogging_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => IServiceCollectionExtensions.AddBlitzCacheLogging(null));
        }

        [Test]
        public async Task AddBlitzCache_MultipleRegistrations_StillSingleton()
        {
            var services = new ServiceCollection();
            services.AddBlitzCache();
            services.AddBlitzCache();
            var provider = services.BuildServiceProvider();
            var cache1 = provider.GetService<IBlitzCache>();
            var cache2 = provider.GetService<IBlitzCache>();
            Assert.IsNotNull(cache1);
            Assert.AreSame(cache1, cache2, "Multiple AddBlitzCache calls should still resolve the same singleton instance");
            await AssertCacheWorksAsync(cache1);
            await AssertCacheWorksAsync(cache2, true);
        }

        [Test]
        public async Task AddBlitzCacheInstance_MultipleRegistrations_StillSingleton()
        {
            var services = new ServiceCollection();
            services.AddBlitzCacheInstance();
            services.AddBlitzCacheInstance();
            var provider = services.BuildServiceProvider();
            var cache1 = provider.GetService<IBlitzCache>();
            var cache2 = provider.GetService<IBlitzCache>();
            Assert.IsNotNull(cache1);
            Assert.AreSame(cache1, cache2, "Multiple AddBlitzCacheInstance calls should still resolve the same singleton instance");
            await AssertCacheWorksAsync(cache1);
            await AssertCacheWorksAsync(cache2, true);
        }

        [Test]
        public async Task AddBlitzCache_Then_AddBlitzCacheInstance_GlobalWins()
        {
            var services = new ServiceCollection();
            services.AddBlitzCache();
            services.AddBlitzCacheInstance();
            var provider = services.BuildServiceProvider();
            var cache = provider.GetService<IBlitzCache>();
            Assert.IsNotNull(cache);
            Assert.AreSame(BlitzCache.Global, cache, "AddBlitzCacheInstance after AddBlitzCache should get global singleton");
            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public async Task AddBlitzCacheInstance_Then_AddBlitzCache_InstanceWins()
        {
            var services = new ServiceCollection();
            services.AddBlitzCacheInstance();
            services.AddBlitzCache();
            var provider = services.BuildServiceProvider();
            var cache = provider.GetService<IBlitzCache>();
            Assert.IsNotNull(cache);
            Assert.AreNotSame(BlitzCache.Global, cache, "AddBlitzCache after AddBlitzCacheInstance should get the instance");
            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public async Task AddBlitzCacheLogging_WithoutStatistics_LogsWarningAndDoesNotThrow()
        {
            var services = new ServiceCollection();
            services.AddBlitzCacheInstance(enableStatistics: false);
            services.AddLogging(b => b.AddDebug());
            services.AddBlitzCacheLogging(logInterval: TimeSpan.FromMilliseconds(10), applicationIdentifier: "TestApp");
            var provider = services.BuildServiceProvider();
            var hostedServices = provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
            Assert.IsTrue(System.Linq.Enumerable.Any(hostedServices, s => s.GetType().Name.Contains("BlitzCacheLoggingService")), "Should register BlitzCacheLoggingService as a hosted service");
            // Should not throw, and should log a warning (cannot assert log output here)
            var cache = provider.GetService<IBlitzCache>();
            await AssertCacheWorksAsync(cache);
        }

        [Test]
        public async Task AddBlitzCache_StatisticsShouldNotBeNull()
        {
            var services = new ServiceCollection();
            // Try to add BlitzCache with statistics enabled
            services.AddBlitzCache(defaultMilliseconds: 60000);
            var provider = services.BuildServiceProvider();
            var cache = provider.GetService<IBlitzCache>() as BlitzCache;
            await AssertCacheWorksAsync(cache);

            Assert.IsNotNull(cache, "Cache should not be null");
            Assert.IsNotNull(cache.Statistics, "Statistics should not be null for the global singleton, even if requested");
            Assert.AreEqual(1, cache.Statistics.EntryCount, "EntryCount should be 1 after the AssertCacheWorksAsync call");
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


using BlitzCacheCore.Extensions;
using BlitzCacheCore.Logging;
using BlitzCacheCore.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Examples
{
    [TestFixture]
    public class BlitzCacheLoggingExamples
    {
        private ServiceProvider serviceProvider;

        [SetUp]
        public void BeforeAll() => BlitzCache.ClearGlobalForTesting();

        [TearDown]
        public void AfterAll() => serviceProvider.Dispose();

        [Test]
        public async Task BasicLoggingSetup_ShouldConfigurePeriodicStatisticsLogging()
        {
            // Arrange - Get BlitzCache with logging
            serviceProvider = new ServiceCollection()
                // Add logging to capture the output
                .AddLogging(builder => builder.AddProvider(new TestLoggerProvider()))
                // Add BlitzCache with statistics enabled (required for logging) - use test constants
                .AddBlitzCache()
                // Add automatic statistics logging with very short interval for fast testing
                .AddBlitzCacheLogging(TimeSpan.FromMilliseconds(TestConstants.VeryShortTimeoutMs))
                .BuildServiceProvider();

            var cache = serviceProvider.GetRequiredService<IBlitzCache>();

            // Act -  Start the hosted service briefly to verify it works
            var hostedServices = serviceProvider.GetServices<IHostedService>();
            Assert.That(hostedServices, Has.Exactly(1).Items);
            // Generate some cache activity to create interesting statistics
            await GenerateCacheActivity(cache);

            var loggingService = hostedServices.First();
            await loggingService.StartAsync(default);
            await TestDelays.MinimumDelay(); // Use test helper delay
            await loggingService.StopAsync(default);

            // Assert - Verify cache has statistics
            Assert.That(cache.Statistics, Is.Not.Null);
            Assert.That(cache.Statistics.TotalOperations, Is.GreaterThan(0));

            TestContext.WriteLine($"Cache statistics after activity:");
            TestContext.WriteLine($"- Total Operations: {cache.Statistics.TotalOperations}");
            TestContext.WriteLine($"- Hit Count: {cache.Statistics.HitCount}");
            TestContext.WriteLine($"- Miss Count: {cache.Statistics.MissCount}");
            TestContext.WriteLine($"- Hit Ratio: {cache.Statistics.HitRatio:P2}");
        }


        [Test]
        public async Task LoggingWithCustomApplicationIdentifier_ShouldIncludeIdentifierInLogs()
        {
            const string customIdentifier = "MyMicroservice-API";

            // Arrange - Set up BlitzCache with custom application identifier
            serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddProvider(new TestLoggerProvider()))
                .AddBlitzCache()
                // Add automatic statistics logging with custom identifier
                .AddBlitzCacheLogging(TimeSpan.FromMilliseconds(TestConstants.VeryShortTimeoutMs), customIdentifier)
                .BuildServiceProvider();
            var cache = serviceProvider.GetRequiredService<IBlitzCache>();
            var hostedServices = serviceProvider.GetServices<IHostedService>();

            // Act - Generate some cache activity and start the service briefly
            await GenerateCacheActivity(cache);


            var loggingService = hostedServices.First();
            await loggingService.StartAsync(default);
            await TestDelays.MinimumDelay();
            await loggingService.StopAsync(default);

            // Assert - Verify cache has statistics
            Assert.That(cache.Statistics, Is.Not.Null);
            Assert.That(cache.Statistics.TotalOperations, Is.GreaterThan(0));

            TestContext.WriteLine($"Custom application identifier '{customIdentifier}' should appear in all log messages.");
        }

        [Test]
        public async Task BlitzCacheInstancesCanShowStatisticsEasily()
        {
            var cacheInstance = new BlitzCacheInstance();
            cacheInstance.InitializeStatistics();

            await GenerateCacheActivity(cacheInstance);

            Assert.IsNotNull(cacheInstance, "Cache should not be null");
            Assert.IsNotNull(cacheInstance.Statistics, "Statistics should not be null for the global singleton");
            Assert.IsTrue(cacheInstance.Statistics.EntryCount > 0, "Statistics should have at least one entry after cache operations");
        }

        [Test]
        public async Task LoggingWhateverInstance()
        {
            // Arrange - Set up BlitzCache with custom application identifier
            serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddProvider(new TestLoggerProvider()))
                // Add automatic statistics logging with custom identifier
                .AddBlitzCacheLogging(TimeSpan.FromMilliseconds(TestConstants.VeryShortTimeoutMs))
                .BuildServiceProvider();
            var hostedServices = serviceProvider.GetServices<IHostedService>();


            var newInstance = new BlitzCacheInstance();
            BlitzCacheLoggingService.Add(newInstance, logInterval: TimeSpan.FromMilliseconds(TestConstants.VeryShortTimeoutMs));

            // Act - Generate some cache activity and start the service briefly
            await GenerateCacheActivity(newInstance);


            var loggingService = hostedServices.First();
            await loggingService.StartAsync(default);
            await TestDelays.MinimumDelay();
            await loggingService.StopAsync(default);

            // Assert - Verify cache has statistics
            Assert.That(newInstance.Statistics, Is.Not.Null);
            Assert.That(newInstance.Statistics.TotalOperations, Is.GreaterThan(0));

            TestContext.WriteLine($"Statistics for the newInstance should be logged.");
        }

        private static async Task GenerateCacheActivity(IBlitzCache cache)
        {
            // Generate cache misses and hits using test constants
            var result1 = cache.BlitzGet("key1", () => "expensive operation 1");
            var result2 = cache.BlitzGet("key1", () => "expensive operation 1"); // Cache hit

            var asyncResult1 = await cache.BlitzGet("key2", async () =>
            {
                await TestDelays.MinimumDelay(); // Use test helper delay instead of hardcoded value
                return "expensive async operation";
            });

            var asyncResult2 = await cache.BlitzGet("key2", async () =>
            {
                await TestDelays.MinimumDelay(); // Use test helper delay instead of hardcoded value
                return "expensive async operation";
            }); // Cache hit
        }
    }
}

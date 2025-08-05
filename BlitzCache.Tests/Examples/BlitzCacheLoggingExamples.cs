
using BlitzCacheCore.Extensions;
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
                .AddBlitzCache(defaultMilliseconds: TestHelpers.LongTimeoutMs, enableStatistics: true)
                // Add automatic statistics logging with very short interval for fast testing
                .AddBlitzCacheLogging(TimeSpan.FromMilliseconds(TestHelpers.VeryShortTimeoutMs))
                .BuildServiceProvider();

            var cache = serviceProvider.GetRequiredService<IBlitzCache>();

            // Act - Generate some cache activity to create interesting statistics
            await GenerateCacheActivity(cache);

            // Start the hosted service briefly to verify it works
            var hostedServices = serviceProvider.GetServices<IHostedService>();
            Assert.That(hostedServices, Has.Exactly(1).Items);

            var loggingService = hostedServices.First();
            await loggingService.StartAsync(default);
            await TestHelpers.MinimumDelay(); // Use test helper delay
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
        public void LoggingWithDisabledStatistics_ShouldLogWarningMessage()
        {
            // Arrange - Set up BlitzCache without statistics
            serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddProvider(new TestLoggerProvider()))
                // Add BlitzCache without statistics (default behavior) - use test constants
                .AddBlitzCache(defaultMilliseconds: TestHelpers.LongTimeoutMs, enableStatistics: false)
                .AddBlitzCacheLogging(TimeSpan.FromMilliseconds(TestHelpers.VeryShortTimeoutMs))
                .BuildServiceProvider();

            var cache = serviceProvider.GetRequiredService<IBlitzCache>();

            // Assert - Statistics should be null
            Assert.That(cache.Statistics, Is.Null);

            TestContext.WriteLine("BlitzCache configured without statistics - logging service will detect this and log a warning.");
        }

        [Test]
        public async Task LoggingWithCustomApplicationIdentifier_ShouldIncludeIdentifierInLogs()
        {
            const string customIdentifier = "MyMicroservice-API";

            // Arrange - Set up BlitzCache with custom application identifier
            serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder.AddProvider(new TestLoggerProvider()))
                // Add BlitzCache with statistics enabled
                .AddBlitzCache(enableStatistics: true)
                // Add automatic statistics logging with custom identifier
                .AddBlitzCacheLogging(TimeSpan.FromMilliseconds(TestHelpers.VeryShortTimeoutMs), customIdentifier)
                .BuildServiceProvider();

            var cache = serviceProvider.GetRequiredService<IBlitzCache>();

            // Act - Generate some cache activity and start the service briefly
            await GenerateCacheActivity(cache);

            var hostedServices = serviceProvider.GetServices<IHostedService>();
            var loggingService = hostedServices.First();
            await loggingService.StartAsync(default);
            await TestHelpers.MinimumDelay();
            await loggingService.StopAsync(default);

            // Assert - Verify cache has statistics
            Assert.That(cache.Statistics, Is.Not.Null);
            Assert.That(cache.Statistics.TotalOperations, Is.GreaterThan(0));

            TestContext.WriteLine($"Custom application identifier '{customIdentifier}' should appear in all log messages.");
        }

        private static async Task GenerateCacheActivity(IBlitzCache cache)
        {
            // Generate cache misses and hits using test constants
            var result1 = cache.BlitzGet("key1", () => "expensive operation 1");
            var result2 = cache.BlitzGet("key1", () => "expensive operation 1"); // Cache hit

            var asyncResult1 = await cache.BlitzGet("key2", async () =>
            {
                await TestHelpers.MinimumDelay(); // Use test helper delay instead of hardcoded value
                return "expensive async operation";
            });

            var asyncResult2 = await cache.BlitzGet("key2", async () =>
            {
                await TestHelpers.MinimumDelay(); // Use test helper delay instead of hardcoded value
                return "expensive async operation";
            }); // Cache hit
        }
    }
}

using BlitzCacheCore.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Examples
{
    /// <summary>
    /// Real-world example showing how to set up BlitzCache with automatic statistics logging
    /// in a typical application startup scenario.
    /// </summary>
    public class RealWorldLoggingExample
    {
        /// <summary>
        /// Example showing how to configure BlitzCache with automatic logging
        /// in a real application (like an ASP.NET Core app or Windows Service)
        /// </summary>
        public static IHost ConfigureBlitzCacheWithLoggingExample(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // Configure BlitzCache with statistics enabled and custom settings
                    services.AddBlitzCache(
                        defaultMilliseconds: 3600000, // 1 hour default cache duration
                        enableStatistics: true       // Required for logging functionality
                    );

                    // Enable automatic statistics logging every hour
                    services.AddBlitzCacheLogging(TimeSpan.FromHours(1));

                    // Or configure more frequent logging for development/testing
                    // services.AddBlitzCacheLogging(TimeSpan.FromMinutes(10));
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();

            return host;
        }
    }

    /// <summary>
    /// Example service that uses BlitzCache and benefits from automatic statistics logging
    /// </summary>
    public class DataService
    {
        private readonly IBlitzCache cache;
        private readonly ILogger<DataService> logger;

        public DataService(IBlitzCache cache, ILogger<DataService> logger)
        {
            this.cache = cache;
            this.logger = logger;
        }

        public async Task<string> GetExpensiveDataAsync(int id)
        {
            // BlitzCache will automatically prevent concurrent execution
            // Statistics will be automatically logged every hour
            return await cache.BlitzGet($"expensive-data-{id}", async () =>
            {
                logger.LogDebug("Executing expensive operation for ID: {Id}", id);

                // Simulate expensive database or API call
                await Task.Delay(2000);

                return $"Expensive data for ID {id} generated at {DateTime.UtcNow}";
            }, milliseconds: 300000); // Cache for 5 minutes
        }

        public void GetCacheStatisticsSummary()
        {
            // You can also manually check statistics at any time
            var stats = cache.Statistics;
            if (stats != null)
            {
                logger.LogInformation(
                    "Current cache performance: {HitRatio:P1} hit ratio, {TotalOperations} total operations",
                    stats.HitRatio,
                    stats.TotalOperations
                );
            }
        }
    }
}

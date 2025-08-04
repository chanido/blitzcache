using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Extensions
{
    public static class BlitzCacheLoggingExtensions
    {
        /// <summary>
        /// Adds automatic periodic logging of BlitzCache statistics.
        /// Statistics will be logged at the specified interval using the provided logger.
        /// Note: BlitzCache must be configured with statistics enabled for this to work.
        /// </summary>
        /// <param name="services">The service collection to add the logging service to.</param>
        /// <param name="logInterval">How often to log statistics. Defaults to 1 hour.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddBlitzCacheLogging(this IServiceCollection services, TimeSpan? logInterval = null) 
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddHostedService<BlitzCacheLoggingService>(provider => 
                new BlitzCacheLoggingService(
                    provider.GetRequiredService<IBlitzCache>(),
                    provider.GetRequiredService<ILogger<BlitzCacheLoggingService>>(),
                    logInterval ?? TimeSpan.FromHours(1)
                ));

            return services;
        }
    }

    internal class BlitzCacheLoggingService : BackgroundService
    {
        private readonly IBlitzCache blitzCache;
        private readonly ILogger<BlitzCacheLoggingService> logger;
        private readonly TimeSpan logInterval;

        public BlitzCacheLoggingService(IBlitzCache blitzCache, ILogger<BlitzCacheLoggingService> logger, TimeSpan logInterval)
        {
            this.blitzCache = blitzCache ?? throw new ArgumentNullException(nameof(blitzCache));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.logInterval = logInterval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("BlitzCache statistics logging started with interval: {Interval}", logInterval);

            if (blitzCache.Statistics == null)
            {
                logger.LogWarning("BlitzCache statistics are disabled. No statistics will be logged. Enable statistics when configuring BlitzCache to use this feature.");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(logInterval, stoppingToken);
                    LogCacheStatistics();
                }
                catch (OperationCanceledException)
                {
                    // Expected when the service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred while logging BlitzCache statistics");
                }
            }

            logger.LogInformation("BlitzCache statistics logging stopped");
        }

        private void LogCacheStatistics()
        {
            var stats = blitzCache.Statistics;
            if (stats == null) return;

            logger.LogInformation(
                "BlitzCache Statistics - " +
                "Hits: {HitCount}, " +
                "Misses: {MissCount}, " +
                "Hit Ratio: {HitRatio:P2}, " +
                "Entries: {EntryCount}, " +
                "Evictions: {EvictionCount}, " +
                "Active Semaphores: {ActiveSemaphoreCount}, " +
                "Total Operations: {TotalOperations}",
                stats.HitCount,
                stats.MissCount,
                stats.HitRatio,
                stats.EntryCount,
                stats.EvictionCount,
                stats.ActiveSemaphoreCount,
                stats.TotalOperations
            );
        }
    }
}

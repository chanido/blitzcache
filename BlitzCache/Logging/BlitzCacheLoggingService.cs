using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Logging
{
    internal class BlitzCacheLoggingService : BackgroundService
    {
        private readonly IBlitzCache blitzCache;
        private readonly ILogger<BlitzCacheLoggingService> logger;
        private readonly TimeSpan logInterval;
        private readonly string applicationIdentifier;

        public BlitzCacheLoggingService(IBlitzCache blitzCache, ILogger<BlitzCacheLoggingService> logger, TimeSpan logInterval, string applicationIdentifier = null)
        {
            this.blitzCache = blitzCache ?? throw new ArgumentNullException(nameof(blitzCache));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.logInterval = logInterval;
            this.applicationIdentifier = GetApplicationIdentifier(applicationIdentifier);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("BlitzCache statistics logging started for '{ApplicationIdentifier}' with interval: {Interval}",
                applicationIdentifier, logInterval);

            if (blitzCache.Statistics == null)
            {
                logger.LogWarning("BlitzCache statistics are disabled for '{ApplicationIdentifier}'. No statistics will be logged. Enable statistics when configuring BlitzCache to use this feature.",
                    applicationIdentifier);
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
                    logger.LogError(ex, "Error occurred while logging BlitzCache statistics for '{ApplicationIdentifier}'",
                        applicationIdentifier);
                }
            }

            logger.LogInformation("BlitzCache statistics logging stopped for '{ApplicationIdentifier}'", applicationIdentifier);
        }

        private void LogCacheStatistics()
        {
            var stats = blitzCache.Statistics;
            if (stats == null) return;

            logger.LogInformation(
                "[{ApplicationIdentifier}] BlitzCache Statistics - " +
                "Hits: {HitCount}, " +
                "Misses: {MissCount}, " +
                "Hit Ratio: {HitRatio:P2}, " +
                "Entries: {EntryCount}, " +
                "Evictions: {EvictionCount}, " +
                "Active Semaphores: {ActiveSemaphoreCount}, " +
                "Total Operations: {TotalOperations}",
                applicationIdentifier,
                stats.HitCount,
                stats.MissCount,
                stats.HitRatio,
                stats.EntryCount,
                stats.EvictionCount,
                stats.ActiveSemaphoreCount,
                stats.TotalOperations
            );
        }

        /// <summary>
        /// Gets the application identifier, using the provided value or auto-detecting from the running application.
        /// </summary>
        /// <param name="customIdentifier">Custom identifier provided by the user. If null or empty, auto-detection is used.</param>
        /// <returns>The application identifier to use in logs.</returns>
        private static string GetApplicationIdentifier(string customIdentifier)
        {
            if (!string.IsNullOrWhiteSpace(customIdentifier))
                return customIdentifier.Trim();

            // Try multiple methods to get a meaningful application name
            try
            {
                // Method 1: Try to get the entry assembly name (most reliable for applications)
                var entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null)
                {
                    var assemblyName = entryAssembly.GetName().Name;
                    if (!string.IsNullOrWhiteSpace(assemblyName) &&
                        !assemblyName.Equals("testhost", StringComparison.OrdinalIgnoreCase))
                    {
                        return assemblyName;
                    }
                }

                // Method 2: Try to get the process name
                var processName = Process.GetCurrentProcess().ProcessName;
                if (!string.IsNullOrWhiteSpace(processName) &&
                    !processName.Equals("dotnet", StringComparison.OrdinalIgnoreCase) &&
                    !processName.Equals("testhost", StringComparison.OrdinalIgnoreCase))
                {
                    return processName;
                }

                // Method 3: Try to get from the executable path
                var mainModule = Process.GetCurrentProcess().MainModule;
                if (mainModule?.FileName != null)
                {
                    var fileName = Path.GetFileNameWithoutExtension(mainModule.FileName);
                    if (!string.IsNullOrWhiteSpace(fileName) &&
                        !fileName.Equals("dotnet", StringComparison.OrdinalIgnoreCase) &&
                        !fileName.Equals("testhost", StringComparison.OrdinalIgnoreCase))
                    {
                        return fileName;
                    }
                }

                // Method 4: Fall back to calling assembly
                var callingAssembly = Assembly.GetCallingAssembly();
                if (callingAssembly != null)
                {
                    var assemblyName = callingAssembly.GetName().Name;
                    if (!string.IsNullOrWhiteSpace(assemblyName))
                    {
                        return assemblyName;
                    }
                }
            }
            catch
            {
                // If all detection methods fail, use a default identifier
            }

            return "Unknown-Application";
        }
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Logging
{
    public class BlitzCacheLoggingService : BackgroundService
    {
        private static readonly ConcurrentBag<BlitzLoggerInstance> blitzCacheInstances = new ConcurrentBag<BlitzLoggerInstance>();
        private readonly ILogger<BlitzCacheLoggingService> logger;
        private readonly TimeSpan logInterval;

        public BlitzCacheLoggingService(ILogger<BlitzCacheLoggingService> logger, TimeSpan? logInterval = null)
        {
            this.logger = logger ?? throw new ArgumentNullException("No compatible logger found");
            this.logInterval = logInterval ?? TimeSpan.FromMinutes(5);
        }

        internal BlitzCacheLoggingService(IBlitzCache cache, ILogger<BlitzCacheLoggingService> logger, string? identifier = null, TimeSpan? logInterval = null)
            : this(logger, logInterval)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache), "BlitzCache cannot be null");
            RegisterBlitzCacheInstance(cache, identifier, logInterval);
            cache.InitializeStatistics();
        }

        public static void RegisterBlitzCacheInstance(IBlitzCache instance, string? identifier = null, TimeSpan? logInterval = null)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance), "BlitzCacheInstance cannot be null");

            if (blitzCacheInstances.Any(bi => bi.Equals(instance))) return; // Prevent duplicate registrations

            blitzCacheInstances.Add(new BlitzLoggerInstance(instance, identifier, logInterval));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("BlitzCache statistics logging started with interval: {Interval}", logInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(logInterval, stoppingToken);
                    LogInstances();
                }
                catch (OperationCanceledException)
                {
                    // Expected when the service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Unexpected error occurred while logging BlitzCache statistics'");
                }
            }

            logger.LogInformation("BlitzCache statistics logging stopped'");
        }

        private void LogInstances()
        {
            foreach (var instance in blitzCacheInstances)
            {
                if (instance.NeedsLogging())
                {
                    instance.Log(logger);
                }
            }
        }

#if DEBUG
        internal static ConcurrentBag<BlitzLoggerInstance> GetInstances() => blitzCacheInstances;

        internal static void ClearForTesting() => blitzCacheInstances.Clear();
#endif
    }
}

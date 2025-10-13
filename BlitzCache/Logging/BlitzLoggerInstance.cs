using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;

namespace BlitzCacheCore.Logging
{
    internal class BlitzLoggerInstance
    {
        internal TimeSpan LogInterval { get; }
        internal string Identifier { get; }
        private readonly IBlitzCache instance;
        private DateTime lastLogTime = DateTime.MinValue;


        internal BlitzLoggerInstance(IBlitzCache instance, string? identifier = null, TimeSpan? logInterval = null)
        {
            this.instance = instance ?? throw new ArgumentNullException(nameof(instance), "BlitzCacheInstance cannot be null");
            LogInterval = logInterval ?? TimeSpan.FromHours(1);
            Identifier = GetApplicationIdentifier(identifier);
        }

        internal bool NeedsLogging() => DateTime.UtcNow - lastLogTime >= LogInterval;

        internal void Log(ILogger<BlitzCacheLoggingService> logger)
        {
            try
            {
                if (instance.Statistics is null) instance.InitializeStatistics();

                var stats = instance.Statistics!;

                // Build the log dynamically so we can omit sections entirely when disabled (<1) for performance clarity.
                var message = $"***[{Identifier}] BlitzCache Statistics***\n" +
                              $"Hits: {stats.HitCount}\n" +
                              $"Misses: {stats.MissCount}\n" +
                              $"Hit Ratio: {stats.HitRatio:P2}\n" +
                              $"Entries: {stats.EntryCount}\n" +
                              $"Evictions: {stats.EvictionCount}\n" +
                              $"Active Semaphores: {stats.ActiveSemaphoreCount}\n" +
                              $"Total Operations: {stats.TotalOperations}\n" +
                              $"Approx. Memory: {Formatters.FormatBytes(stats.ApproximateMemoryBytes)}";

                // Attempt to detect internal tracking flags (available when concrete type is CacheStatistics)
                var cacheStatsType = stats.GetType();
                bool heaviestEnabled = false;
                bool slowestEnabled = false;
                try
                {
                    var heaviestProp = cacheStatsType.GetProperty("HeaviestTrackingEnabled", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    var slowestProp = cacheStatsType.GetProperty("SlowestTrackingEnabled", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                    heaviestEnabled = heaviestProp?.GetValue(stats) as bool? == true;
                    slowestEnabled = slowestProp?.GetValue(stats) as bool? == true;
                }
                catch { }

                if (heaviestEnabled)
                {
                    var heaviest = stats.TopHeaviestEntries != null && stats.TopHeaviestEntries.Any()
                        ? string.Concat(stats.TopHeaviestEntries.Select(q => $"\n\t{q}"))
                        : "\n\t<none>";
                    message += $"\nTop Heaviest:{heaviest}";
                }
                if (slowestEnabled)
                {
                    var slowest = stats.TopSlowestQueries != null && stats.TopSlowestQueries.Any()
                        ? string.Concat(stats.TopSlowestQueries.Select(q => $"\n\t{q}"))
                        : "\n\t<none>";
                    message += $"\nTop Slowest Queries:{slowest}";
                }

                logger.LogInformation(message);

                lastLogTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while logging BlitzCache statistics for '{Identifier}'", Identifier);
            }
        }

        /// <summary>
        /// Gets the application identifier, using the provided value or auto-detecting from the running application.
        /// </summary>
        /// <param name="customIdentifier">Custom identifier provided by the user. If null or empty, auto-detection is used.</param>
        /// <returns>The application identifier to use in logs.</returns>
        private static string GetApplicationIdentifier(string? customIdentifier = null)
        {
            if (!string.IsNullOrWhiteSpace(customIdentifier)) return customIdentifier.Trim();

            // Try multiple methods to get a meaningful application name
            try
            {
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

                // Fall back to calling assembly
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
            { }

            return "Unknown-Application";
        }

        public override bool Equals(object? obj)
        {
            var cacheInstance = obj is IBlitzCache otherInstance ? otherInstance : obj is BlitzLoggerInstance other ? other.instance : null;

            return instance.Equals(cacheInstance);
        }

        public override int GetHashCode() => instance.GetHashCode();
    }
}

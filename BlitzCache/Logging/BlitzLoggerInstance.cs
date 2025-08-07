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

                logger.LogInformation(
                "***[{Identifier}] BlitzCache Statistics***\n" +
                "Hits: {HitCount}\n" +
                "Misses: {MissCount}\n" +
                "Hit Ratio: {HitRatio:P2}\n" +
                "Entries: {EntryCount}\n" +
                "Evictions: {EvictionCount}\n" +
                "Active Semaphores: {ActiveSemaphoreCount}\n" +
                "Total Operations: {TotalOperations}\n" +
                "Top Slowest Queries: {TopSlowestQueries}",
                Identifier,
                stats.HitCount,
                stats.MissCount,
                stats.HitRatio,
                stats.EntryCount,
                stats.EvictionCount,
                stats.ActiveSemaphoreCount,
                stats.TotalOperations,
                stats.TopSlowestQueries != null && stats.TopSlowestQueries.Count() > 0 ? string.Concat(stats.TopSlowestQueries.Select(q => $"\n\t{q}")) : "Not available"
            );

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

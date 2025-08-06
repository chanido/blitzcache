using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
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
                "[{Identifier}] BlitzCache Statistics - " +
                "Hits: {HitCount}, " +
                "Misses: {MissCount}, " +
                "Hit Ratio: {HitRatio:P2}, " +
                "Entries: {EntryCount}, " +
                "Evictions: {EvictionCount}, " +
                "Active Semaphores: {ActiveSemaphoreCount}, " +
                "Total Operations: {TotalOperations}",
                Identifier,
                stats.HitCount,
                stats.MissCount,
                stats.HitRatio,
                stats.EntryCount,
                stats.EvictionCount,
                stats.ActiveSemaphoreCount,
                stats.TotalOperations
            );

                lastLogTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while logging BlitzCache statistics for '{Identifier}'", Identifier);
            }
        }

        public override bool Equals(object? obj)
        {
            var cacheInstance = obj is IBlitzCache otherInstance ? otherInstance : obj is BlitzLoggerInstance other ? other.instance : null;

            return instance.Equals(cacheInstance);
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
            { }

            return "Unknown-Application";
        }
    }
}

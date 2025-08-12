using System;

namespace BlitzCacheCore.Logging
{
    /// <summary>
    /// Configuration options for BlitzCache statistics logging.
    /// Extend with new properties as logging capabilities evolve.
    /// </summary>
    public sealed class BlitzCacheLoggingOptions
    {
        /// <summary>
        /// Interval between statistics log outputs. Defaults to 1 hour.
        /// </summary>
        public TimeSpan LogInterval { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Optional identifier for the global cache in log output. If null will be auto detected.
        /// </summary>
        public string? GlobalCacheIdentifier { get; set; } = null;

        internal void Validate()
        {
            if (LogInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(LogInterval), "LogInterval must be positive");
        }
    }
}

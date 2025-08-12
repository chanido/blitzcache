using System;
using BlitzCacheCore.Capacity;
using BlitzCacheCore.Statistics.Memory;

namespace BlitzCacheCore
{
    /// <summary>
    /// Configuration options for the global BlitzCache singleton.
    /// New fields can be added here without breaking existing callers.
    /// </summary>
    public sealed class BlitzCacheOptions
    {
        public long DefaultMilliseconds { get; set; } = 60000;
        public TimeSpan? CleanupInterval { get; set; } = null;
        public int MaxTopSlowest { get; set; } = 5;
        public int MaxTopHeaviest { get; set; } = 5;
        public long? MaxCacheSizeBytes { get; set; } = null;
        public SizeComputationMode? SizeComputationMode { get; set; } = null;
        public CapacityEvictionStrategy EvictionStrategy { get; set; } = CapacityEvictionStrategy.SmallestFirst;
        public bool EnableStatistics { get; set; } = true; // currently always on for global; reserved for future toggling

        internal void Validate()
        {
            if (DefaultMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(DefaultMilliseconds));
            if (MaxTopSlowest < 0) throw new ArgumentOutOfRangeException(nameof(MaxTopSlowest));
            if (MaxTopHeaviest < 0) throw new ArgumentOutOfRangeException(nameof(MaxTopHeaviest));
            if (MaxCacheSizeBytes.HasValue && MaxCacheSizeBytes.Value < 0) throw new ArgumentOutOfRangeException(nameof(MaxCacheSizeBytes));
        }
    }
}

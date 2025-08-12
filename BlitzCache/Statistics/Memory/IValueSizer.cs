using System;

namespace BlitzCacheCore.Statistics.Memory
{
    /// <summary>
    /// Strategy to estimate the approximate size in bytes of a value to be cached.
    /// Implementations should be fast and allocation-friendly; the result is best-effort.
    /// </summary>
    public interface IValueSizer
    {
        long GetSizeBytes(object? value);
    }
}

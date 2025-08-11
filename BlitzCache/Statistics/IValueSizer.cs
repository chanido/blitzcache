using System;

namespace BlitzCacheCore.Statistics
{
    /// <summary>
    /// Strategy to estimate the approximate size in bytes of a value to be cached.
    /// Implementations should be fast and allocation-friendly; the result is best-effort.
    /// </summary>
    public interface IValueSizer
    {
        long GetSizeBytes(object? value);
    }

    /// <summary>
    /// A basic, zero-dependency, best-effort sizer for common .NET types.
    /// </summary>
    internal class ApproximateValueSizer : IValueSizer
    {
        private const long FallbackSizeBytes = 128; // conservative estimate for unknown types
        public long GetSizeBytes(object? value)
        {
            if (value is null) return 0;

            switch (value)
            {
                case string s:
                    // UTF-16: 2 bytes/char; add small object/header overhead approximation
                    return (s.Length * 2L) + 24;
                case byte[] b:
                    return b.LongLength + 24;
                case Array arr when arr.Length > 0 && arr.GetType().GetElementType() == typeof(int):
                    return (arr.Length * 4L) + 24;
                case Array arr2 when arr2.Length > 0 && arr2.GetType().GetElementType() == typeof(long):
                    return (arr2.Length * 8L) + 24;
                case Array arr3 when arr3.Length > 0 && arr3.GetType().GetElementType() == typeof(double):
                    return (arr3.Length * 8L) + 24;
                case Array arr4 when arr4.Length > 0 && arr4.GetType().GetElementType() == typeof(float):
                    return (arr4.Length * 4L) + 24;
                default:
                    // Fallback: rough estimate using string serialization length if available
                    // but avoid heavy serialization to keep it cheap; use type name overhead
                    return FallbackSizeBytes;
            }
        }
    }
}

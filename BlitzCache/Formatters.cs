namespace BlitzCacheCore
{
    internal static class Formatters
    {
        internal static string FormatDuration(long milliseconds)
        {
            if (milliseconds < 0) return "0ms"; // clamp negative
            if (milliseconds < 1000) return $"{milliseconds}ms";

            var ts = System.TimeSpan.FromMilliseconds(milliseconds);

            // Less than a minute: keep millisecond precision as S.mmm seconds
            if (ts.TotalSeconds < 60)
            {
                var totalWholeSeconds = (int)ts.TotalSeconds;
                var msRemainder = milliseconds % 1000;
                return $"{totalWholeSeconds}.{msRemainder:000}s";
            }

            // Less than a day: format as HH:MM:SS
            if (ts.TotalDays < 1)
            {
                return $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            }

            // One day or more: Dd HH:MM:SS
            return $"{ts.Days}d {ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
        }

        internal static string FormatBytes(long bytes)
        {
            if (bytes < 0) bytes = 0;
            const double K = 1024.0;
            if (bytes < K) return $"{bytes} bytes";
            var kb = bytes / K;
            if (kb < K) return $"{kb:0.##} KB"; // < 1 MB
            var mb = kb / K;
            if (mb < K) return $"{mb:0.##} MB"; // < 1 GB
            var gb = mb / K;
            if (gb < K) return $"{gb:0.##} GB"; // < 1 TB
            var tb = gb / K;
            return $"{tb:0.##} TB";
        }
    }
}

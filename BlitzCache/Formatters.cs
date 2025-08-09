namespace BlitzCacheCore
{
    internal static class Formatters
    {
        internal static string FormatDuration(long milliseconds)
        {
            if (milliseconds < 1000)
                return $"{milliseconds}ms";

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
            if (bytes < 1024) return $"{bytes} bytes";
            var kb = bytes / 1024.0;
            if (kb < 1024) return $"{kb:0.##} KB";
            var mb = kb / 1024.0;
            if (mb < 1024) return $"{mb:0.##} MB";
            var gb = mb / 1024.0;
            return $"{gb:0.##} GB";
        }
    }
}

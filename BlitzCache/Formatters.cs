namespace BlitzCacheCore
{
    internal static class Formatters
    {
        internal static string FormatDuration(long milliseconds)
        {
            if (milliseconds < 1000)
                return $"{milliseconds}ms";

            var totalSeconds = milliseconds / 1000;
            var msRemainder = milliseconds % 1000;
            if (milliseconds < 60_000)
                return $"{totalSeconds}:{msRemainder:000}"; // seconds:milliseconds

            var minutes = totalSeconds / 60;
            var secondsRemainder = totalSeconds % 60;
            return $"{minutes}:{secondsRemainder:00}"; // minutes:seconds
        }

        internal static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} bytes";
            var kb = bytes / 1024.0;
            if (kb < 1024) return $"{kb:0.##} KB";
            var mb = kb / 1024.0;
            return $"{mb:0.##} MB";
        }
    }
}

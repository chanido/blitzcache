namespace BlitzCacheCore.Statistics
{
    /// <summary>
    /// Represents a cache entry approximate size for reporting purposes.
    /// </summary>
    public class HeavyEntry
    {
        public string CacheKey { get; }
        public long SizeBytes { get; }

        public HeavyEntry(string cacheKey, long sizeBytes)
        {
            CacheKey = cacheKey;
            SizeBytes = sizeBytes;
        }

        public override string ToString()
        {
            var size = SizeBytes;
            if (size < 1024)
                return $"{CacheKey} - ~{size} bytes";

            var kb = size / 1024.0;
            if (kb < 1024)
                return $"{CacheKey} - ~{kb:0.##} KB";

            var mb = kb / 1024.0;
            return $"{CacheKey} - ~{mb:0.##} MB";
        }
    }
}

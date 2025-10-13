namespace BlitzCacheCore.Statistics.Speed
{
    /// <summary>
    /// Represents a cache entry approximate size for reporting purposes.
    /// </summary>
    public class HeavyEntry : IStatisticalEntry
    {
        public string CacheKey { get; }
        public long SizeBytes { get; private set; }

        public long Score => SizeBytes;

        public HeavyEntry(string cacheKey, long sizeBytes)
        {
            CacheKey = cacheKey;
            SizeBytes = sizeBytes;
        }

        void IStatisticalEntry.Update(long value)
        {
            SizeBytes = value;
        }

        public override string ToString()
        {
            return $"{CacheKey} - ~{Formatters.FormatBytes(SizeBytes)}";
        }
    }
}

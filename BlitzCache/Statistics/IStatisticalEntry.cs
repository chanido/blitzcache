namespace BlitzCacheCore.Statistics
{
    /// <summary>
    /// Represents a tracked statistical entry with a key and a sortable score.
    /// Implementations should update their internal state when new observations arrive.
    /// </summary>
    public interface IStatisticalEntry
    {
        string CacheKey { get; }
        long Score { get; }
        void Update(long value);
    }
}

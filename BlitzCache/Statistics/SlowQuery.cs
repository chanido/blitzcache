using System;

namespace BlitzCacheCore.Statistics
{
    public class SlowQuery : IComparable
    {
        public string CacheKey { get; private set; }
        public long WorstCaseMs { get; private set; }
        public long BestCaseMs { get; private set; }
        public long AverageMs { get; private set; }
        public long Occurrences { get; private set; }

        public SlowQuery(string cacheKey, long worstCaseMs)
        {
            CacheKey = cacheKey ?? throw new ArgumentNullException(nameof(cacheKey));
            WorstCaseMs = worstCaseMs;
            BestCaseMs = worstCaseMs;
            AverageMs = worstCaseMs;
            Occurrences = 1;
        }

        public SlowQuery Update(long currentExecution)
        {
            // Update the worst case, best case, and average based on the new slow query
            WorstCaseMs = Math.Max(currentExecution, WorstCaseMs);
            BestCaseMs = Math.Min(currentExecution, BestCaseMs);
            AverageMs = ((AverageMs * Occurrences) + currentExecution) / (Occurrences + 1);
            Occurrences++;

            return this;
        }

        public bool IsFasterThan(long durationMilliseconds) => WorstCaseMs < durationMilliseconds;

        public override bool Equals(object obj) => obj is SlowQuery other && CacheKey == other.CacheKey;
        public override int GetHashCode() => CacheKey.GetHashCode();
        public override string ToString() => $"{CacheKey} - Worse: {WorstCaseMs}ms | Best: {BestCaseMs}ms | Avg: {AverageMs} | Occurrences: {Occurrences}";
        public int CompareTo(object obj) => obj is SlowQuery other ? WorstCaseMs.CompareTo(other.WorstCaseMs) : throw new ArgumentException("Object is not a SlowQuery");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace BlitzCacheCore.Statistics
{
    /// <summary>
    /// Thread-safe, fixed-size collection for tracking the top N slowest queries.
    /// </summary>
    internal class TopSlowestQueries
    {
        private readonly int maxSize;
        private SlowQuery[] queries;
        private int index = 0;
        private readonly object sync = new object();

        public TopSlowestQueries(int maxSize)
        {
            this.maxSize = maxSize;
            queries = new SlowQuery[maxSize];
        }

        public void Add(string cacheKey, long durationMilliseconds)
        {
            lock (sync)
            {
                AddIfNecessary(cacheKey, durationMilliseconds);

                Sort();
            }
        }

        private void AddIfNecessary(string cacheKey, long durationMilliseconds)
        {
            var existing = Find(cacheKey);

            if (existing != null)
            {
                existing.Update(durationMilliseconds);
                return;
            }

            if (index < maxSize)
            {
                queries[index++] = new SlowQuery(cacheKey, durationMilliseconds);
                return;
            }

            if (queries[index - 1].IsFasterThan(durationMilliseconds))
            {
                queries[index - 1] = new SlowQuery(cacheKey, durationMilliseconds);
                return;
            }
        }

        private void Sort() => Array.Sort(queries, (a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            // Sort descending by WorstCaseMs
            return b.WorstCaseMs.CompareTo(a.WorstCaseMs);
        });

        public IEnumerable<SlowQuery> Get() => queries.Where(q => q != null);

        public void Clear()
        {
            lock (sync)
            {
                queries = new SlowQuery[maxSize];
            }
        }

        private SlowQuery? Find(string cacheKey)
        {
            for (int i = 0; i < index; i++)
            {
                if (queries[i].CacheKey == cacheKey)
                {
                    return queries[i];
                }
            }

            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace BlitzCacheCore.Statistics
{
    /// <summary>
    /// Thread-safe, generic Top-N tracker for entries implementing IStatisticalEntry.
    /// Maintains highest-Score items, trims on overflow, sorts on read.
    /// </summary>
    internal class TopNTracker<T> where T : IStatisticalEntry
    {
        private readonly int maxSize;
        private readonly object sync = new object();
        private readonly Dictionary<string, T> items = new Dictionary<string, T>();
        private readonly Func<string, long, T> factory;

        public TopNTracker(int maxSize, Func<string, long, T> factory)
        {
            if (maxSize < 1) throw new ArgumentOutOfRangeException(nameof(maxSize));
            this.maxSize = maxSize;
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public void AddOrUpdate(string key, long value)
        {
            lock (sync)
            {
                if (items.TryGetValue(key, out var entry))
                {
                    entry.Update(value);
                }
                else
                {
                    items[key] = factory(key, value);
                }

                TrimIfNeeded();
            }
        }

        public void Remove(string key)
        {
            lock (sync)
            {
                items.Remove(key);
            }
        }

        public IEnumerable<T> Get()
        {
            KeyValuePair<string, T>[] snapshot;
            lock (sync)
            {
                snapshot = items
                    .OrderByDescending(kv => kv.Value.Score)
                    .Take(maxSize)
                    .ToArray();
            }

            foreach (var kv in snapshot)
                yield return kv.Value;
        }

        public void Clear()
        {
            lock (sync)
            {
                items.Clear();
            }
        }

        private void TrimIfNeeded()
        {
            if (items.Count <= maxSize) return;
            foreach (var key in items
                .OrderBy(kv => kv.Value.Score)
                .Select(kv => kv.Key)
                .Take(items.Count - maxSize)
                .ToArray())
            {
                items.Remove(key);
            }
        }
    }
}

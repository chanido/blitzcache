using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BlitzCacheCore.Statistics
{
    /// <summary>
    /// Thread-safe, fixed-size tracker of heaviest entries by approximate size in bytes.
    /// </summary>
    internal class TopHeaviestEntries
    {
        private readonly int maxSize;
        private readonly object sync = new object();
        private readonly Dictionary<string, long> sizes = new Dictionary<string, long>();

        public TopHeaviestEntries(int maxSize)
        {
            if (maxSize < 1) throw new ArgumentOutOfRangeException(nameof(maxSize));
            this.maxSize = maxSize;
        }

        public void AddOrUpdate(string key, long sizeBytes)
        {
            lock (sync)
            {
                sizes[key] = sizeBytes;
                TrimIfNeeded();
            }
        }

        public void Remove(string key)
        {
            lock (sync)
            {
                sizes.Remove(key);
            }
        }

        public IEnumerable<HeavyEntry> Get()
        {
            // Snapshot sorted by size desc, take maxSize
            KeyValuePair<string, long>[] snapshot;
            lock (sync)
            {
                snapshot = sizes.OrderByDescending(kv => kv.Value).Take(maxSize).ToArray();
            }
            foreach (var kv in snapshot)
                yield return new HeavyEntry(kv.Key, kv.Value);
        }

        public void Clear()
        {
            lock (sync)
            {
                sizes.Clear();
            }
        }

        private void TrimIfNeeded()
        {
            if (sizes.Count <= maxSize) return;
            // Remove smallest entries until count == maxSize
            foreach (var key in sizes.OrderBy(kv => kv.Value).Select(kv => kv.Key).Take(Math.Max(0, sizes.Count - maxSize)).ToArray())
            {
                sizes.Remove(key);
            }
        }
    }
}

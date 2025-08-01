using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace BlitzCacheCore.LockDictionaries
{
    /// <summary>
    /// Enhanced cleanup manager that supports custom cleanup logic in addition to time-based cleanup.
    /// </summary>
    public class SmartCleanupManager<TKey, TValue> : IDisposable where TValue : ICleanupEntry
    {
        private readonly ConcurrentDictionary<TKey, TValue> dictionary;
        private readonly Timer cleanupTimer;
        private readonly TimeSpan maxIdleTime;
        private readonly TimeSpan cleanupInterval;
        private readonly Action<TValue> onEntryRemoved;
        private readonly Func<TValue, bool> customCleanupLogic;
        private bool disposed = false;

        /// <summary>
        /// Creates a new smart cleanup manager with custom cleanup logic.
        /// </summary>
        /// <param name="dictionary">The dictionary to manage cleanup for</param>
        /// <param name="maxIdleTime">Maximum time an entry can be idle before cleanup (default: 30 minutes)</param>
        /// <param name="cleanupInterval">How often to run cleanup (default: 5 minutes)</param>
        /// <param name="onEntryRemoved">Optional callback when an entry is removed during cleanup</param>
        /// <param name="customCleanupLogic">Custom logic to determine if an entry should be cleaned up</param>
        public SmartCleanupManager(
            ConcurrentDictionary<TKey, TValue> dictionary,
            TimeSpan? maxIdleTime = null,
            TimeSpan? cleanupInterval = null,
            Action<TValue> onEntryRemoved = null,
            Func<TValue, bool> customCleanupLogic = null)
        {
            this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            this.maxIdleTime = maxIdleTime ?? TimeSpan.FromMinutes(30);
            this.cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5);
            this.onEntryRemoved = onEntryRemoved;
            this.customCleanupLogic = customCleanupLogic;

            // Start the cleanup timer
            this.cleanupTimer = new Timer(PerformCleanup, null, this.cleanupInterval, this.cleanupInterval);
        }

        /// <summary>
        /// Performs cleanup of idle entries using both time-based and custom logic.
        /// </summary>
        private void PerformCleanup(object state)
        {
            if (disposed) return;

            try
            {
                var cutoffTime = DateTime.UtcNow.Subtract(maxIdleTime);
                var keysToRemove = dictionary
                    .Where(kvp => ShouldCleanupEntry(kvp.Value, cutoffTime))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    if (dictionary.TryRemove(key, out var removedValue))
                    {
                        // Notify that entry was removed (for cleanup like disposing semaphores)
                        onEntryRemoved?.Invoke(removedValue);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors to prevent timer from stopping
                // In a production scenario, you might want to log this
            }
        }

        /// <summary>
        /// Determines if an entry should be cleaned up using both time-based and custom logic.
        /// </summary>
        private bool ShouldCleanupEntry(TValue entry, DateTime cutoffTime)
        {
            // If custom logic is provided, use it first
            if (customCleanupLogic != null)
            {
                return customCleanupLogic(entry);
            }

            // Fallback to time-based cleanup
            return entry.LastAccessed < cutoffTime;
        }

        /// <summary>
        /// Manually triggers cleanup (useful for testing).
        /// </summary>
        public void TriggerCleanup()
        {
            PerformCleanup(null);
        }

        /// <summary>
        /// Disposes the cleanup manager and stops the cleanup timer.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                cleanupTimer?.Dispose();
                disposed = true;
            }
        }
    }
}

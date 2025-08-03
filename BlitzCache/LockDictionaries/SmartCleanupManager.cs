using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly TimeSpan cleanupInterval;
        private bool disposed = false;

        /// <summary>
        /// Creates a new smart cleanup manager with custom cleanup logic.
        /// </summary>
        /// <param name="dictionary">The dictionary to manage cleanup for</param>
        /// <param name="cleanupInterval">How often to run cleanup (default: 5 minutes)</param>
        public SmartCleanupManager(
            ConcurrentDictionary<TKey, TValue> dictionary,
            TimeSpan? cleanupInterval = null)
        {
            this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            this.cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5);

            // Start the cleanup timer
            cleanupTimer = new Timer(PerformCleanup, null, this.cleanupInterval, this.cleanupInterval);
        }

        /// <summary>
        /// Performs cleanup of idle entries using both time-based and custom logic.
        /// </summary>
        private void PerformCleanup(object state)
        {
            if (disposed) return;

            try
            {
                var keysToRemove = dictionary
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    if (dictionary.TryGetValue(key, out var entry) && entry.AttemptDispose())
                    {
                        dictionary.TryRemove(key, out _);
                    } // If we can't dispose, we just leave it in the dictionary for the next cleanup
                }
            }
            catch
            {
                // Ignore cleanup errors to prevent timer from stopping
                // In a production scenario, you might want to log this
            }
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

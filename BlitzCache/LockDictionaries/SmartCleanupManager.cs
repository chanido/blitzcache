using System;
using System.Collections.Concurrent;
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
            var interval = cleanupInterval ?? TimeSpan.FromMinutes(5);

            cleanupTimer = new Timer(PerformCleanup, null, interval, interval);
        }

        /// <summary>
        /// Performs cleanup of idle entries using both time-based and custom logic.
        /// </summary>
        private void PerformCleanup(object state)
        {
            if (disposed) return;

            try
            {
                foreach (var kvp in dictionary)
                {
                    if (kvp.Value.AttemptDispose())
                    {
                        dictionary.TryRemove(kvp.Key, out _);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors to prevent timer from stopping
            }
        }

        /// <summary>
        /// Disposes the cleanup manager and stops the cleanup timer.
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            
            disposed = true;
            cleanupTimer?.Dispose();
        }
    }
}

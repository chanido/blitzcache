using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.LockDictionaries
{
    /// <summary>
    /// A dictionary that manages BlitzSemaphore instances with automatic cleanup.
    /// Provides thread-safe access to named semaphores with intelligent memory management.
    /// </summary>
    public class BlitzSemaphoreDictionary : IDisposable
    {
        private readonly ConcurrentDictionary<string, BlitzSemaphore> semaphores = new ConcurrentDictionary<string, BlitzSemaphore>();
        private readonly SmartCleanupManager<string, BlitzSemaphore> cleanupManager;
        private bool disposed = false;

        public BlitzSemaphoreDictionary() =>
            cleanupManager = new SmartCleanupManager<string, BlitzSemaphore>(semaphores, cleanupInterval: TimeSpan.FromSeconds(10));

        /// <summary>
        /// Gets or creates a BlitzSemaphore for the specified key.
        /// </summary>
        /// <param name="key">The unique key to identify the semaphore</param>
        /// <returns>A BlitzSemaphore instance that can be used for synchronization</returns>
        public BlitzSemaphore GetSemaphore(string key)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BlitzSemaphoreDictionary));
            
            var semaphore = semaphores.GetOrAdd(key, _ => new BlitzSemaphore());
            semaphore.MarkAsAccessed();
            return semaphore;
        }

        /// <summary>
        /// Gets or creates a BlitzSemaphore for the specified key and synchronously acquires it.
        /// Returns an IDisposable that automatically releases the semaphore when disposed.
        /// </summary>
        /// <param name="key">The unique key to identify the semaphore</param>
        /// <returns>An IDisposable lock handle that releases the semaphore when disposed</returns>
        public IDisposable Wait(string key) => GetSemaphore(key).Acquire();

        /// <summary>
        /// Gets or creates a BlitzSemaphore for the specified key and asynchronously acquires it.
        /// Returns an IDisposable that automatically releases the semaphore when disposed.
        /// </summary>
        /// <param name="key">The unique key to identify the semaphore</param>
        /// <returns>An IDisposable lock handle that releases the semaphore when disposed</returns>
        public Task<IDisposable> WaitAsync(string key) => GetSemaphore(key).AcquireAsync();

        public int GetNumberOfLocks() => semaphores.Count;

        public void Dispose()
        {
            if (disposed) return;
            
            disposed = true;
            cleanupManager?.Dispose();

            foreach (var entry in semaphores.Values)
            {
                entry.Dispose();
            }

            semaphores?.Clear();
        }
    }
}

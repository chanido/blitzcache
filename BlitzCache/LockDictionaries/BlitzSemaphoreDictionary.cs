using System;
using System.Collections.Concurrent;
using System.Reflection;
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

        public BlitzSemaphoreDictionary()
        {
            // Only keep semaphores that are actively in use or recently used
            cleanupManager = new SmartCleanupManager<string, BlitzSemaphore>(semaphores, cleanupInterval: TimeSpan.FromSeconds(10));
        }

        /// <summary>
        /// Gets or creates a BlitzSemaphore for the specified key.
        /// </summary>
        /// <param name="key">The unique key to identify the semaphore</param>
        /// <returns>A BlitzSemaphore instance that can be used for synchronization</returns>
        public BlitzSemaphore GetSemaphore(string key)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BlitzSemaphoreDictionary));
            
            if (!semaphores.ContainsKey(key))
            {
                semaphores.TryAdd(key, new BlitzSemaphore());
            }

            if (semaphores.TryGetValue(key, out var entry))
            {
                entry.MarkAsAccessed();
                return entry;
            }

            // Fallback - should not happen
            var fallbackEntry = new BlitzSemaphore();
            semaphores.TryAdd(key, fallbackEntry);
            return fallbackEntry;
        }

        public int GetNumberOfLocks() => semaphores.Count;

        public void Dispose()
        {
            if (disposed) return;
            
            cleanupManager?.Dispose();

            // Dispose all semaphores
            foreach (var entry in semaphores.Values)
            {
                entry.Dispose();
            }

            semaphores?.Clear();
            disposed = true;
        }
    }
}

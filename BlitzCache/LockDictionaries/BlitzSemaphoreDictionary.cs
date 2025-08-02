using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.LockDictionaries
{
    /// <summary>
    /// A singleton dictionary that manages BlitzSemaphore instances with automatic cleanup.
    /// Provides thread-safe access to named semaphores with intelligent memory management.
    /// </summary>
    public class BlitzSemaphoreDictionary
    {
        private static BlitzSemaphoreDictionary singleton;
        private static readonly object dictionaryLock = new object();

        private readonly ConcurrentDictionary<string, BlitzSemaphore> semaphores = new ConcurrentDictionary<string, BlitzSemaphore>();
        private readonly SmartCleanupManager<string, BlitzSemaphore> cleanupManager;

        private BlitzSemaphoreDictionary()
        {
            // Ultra-aggressive cleanup - check every 10 seconds
            // Only keep semaphores that are actively in use or recently used
            cleanupManager = new SmartCleanupManager<string, BlitzSemaphore>(
                semaphores, 
                maxIdleTime: TimeSpan.FromSeconds(30), // Very short fallback timeout
                cleanupInterval: TimeSpan.FromSeconds(10),
                onEntryRemoved: entry => entry.Dispose(),
                customCleanupLogic: ShouldCleanupEntry);
        }

        private bool ShouldCleanupEntry(BlitzSemaphore entry) => !entry.IsInUse;

        public static BlitzSemaphoreDictionary GetInstance()
        {
            if (singleton == null)
                lock (dictionaryLock)
                    singleton ??= new BlitzSemaphoreDictionary();

            return singleton;
        }

        /// <summary>
        /// Gets or creates a BlitzSemaphore for the specified key.
        /// </summary>
        /// <param name="key">The unique key to identify the semaphore</param>
        /// <returns>A BlitzSemaphore instance that can be used for synchronization</returns>
        public static BlitzSemaphore GetSemaphore(string key)
        {
            var semaphoreDictionary = GetInstance();
            
            if (!semaphoreDictionary.semaphores.ContainsKey(key))
            {
                semaphoreDictionary.semaphores.TryAdd(key, new BlitzSemaphore());
            }

            if (semaphoreDictionary.semaphores.TryGetValue(key, out var entry))
            {
                return entry;
            }

            // Fallback - should not happen
            var fallbackEntry = new BlitzSemaphore();
            semaphoreDictionary.semaphores.TryAdd(key, fallbackEntry);
            return fallbackEntry;
        }

        public static int GetNumberOfLocks() => GetInstance().semaphores.Count;

        public static void TriggerCleanup() => GetInstance().cleanupManager.TriggerCleanup();

        public static void Dispose()
        {
            var instance = GetInstance();
            instance?.cleanupManager?.Dispose();
            
            // Dispose all semaphores
            foreach (var entry in instance.semaphores.Values)
            {
                entry.Dispose();
            }
            
            instance?.semaphores?.Clear();
            singleton = null;
        }
    }
}

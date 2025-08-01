using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.LockDictionaries
{
    public class SmartSemaphoreDictionary
    {
        private static SmartSemaphoreDictionary singleton;
        private static readonly object dictionaryLock = new object();

        private readonly ConcurrentDictionary<string, SemaphoreEntry> semaphores = new ConcurrentDictionary<string, SemaphoreEntry>();
        private readonly SmartCleanupManager<string, SemaphoreEntry> cleanupManager;

        private class SemaphoreEntry : ICleanupEntry
        {
            public SemaphoreSlim Semaphore { get; }
            public DateTime LastAccessed { get; set; }
            public bool IsInUse { get; set; }

            public SemaphoreEntry()
            {
                Semaphore = new SemaphoreSlim(1, 1);
                LastAccessed = DateTime.UtcNow;
                IsInUse = false;
            }

            public void UpdateAccess()
            {
                LastAccessed = DateTime.UtcNow;
            }

            public void MarkInUse()
            {
                IsInUse = true;
                LastAccessed = DateTime.UtcNow;
            }

            public void MarkNotInUse()
            {
                IsInUse = false;
                LastAccessed = DateTime.UtcNow;
            }

            public void Dispose()
            {
                Semaphore?.Dispose();
            }
        }

        private SmartSemaphoreDictionary()
        {
            // Ultra-aggressive cleanup - check every 10 seconds
            // Only keep semaphores that are actively in use or recently used
            cleanupManager = new SmartCleanupManager<string, SemaphoreEntry>(
                semaphores, 
                maxIdleTime: TimeSpan.FromSeconds(30), // Very short fallback timeout
                cleanupInterval: TimeSpan.FromSeconds(10),
                onEntryRemoved: entry => entry.Dispose(),
                customCleanupLogic: ShouldCleanupEntry);
        }

        private bool ShouldCleanupEntry(SemaphoreEntry entry)
        {
            // Simple rule: Only clean up semaphores that are not actively in use
            // Since semaphores are cheap to create on-demand, we don't need to keep unused ones
            return !entry.IsInUse;
        }

        public static SmartSemaphoreDictionary GetInstance()
        {
            if (singleton == null)
                lock (dictionaryLock)
                    singleton ??= new SmartSemaphoreDictionary();

            return singleton;
        }

        public static SmartSemaphore GetSmartSemaphore(string key)
        {
            var semaphoreDictionary = GetInstance();
            
            if (!semaphoreDictionary.semaphores.ContainsKey(key))
            {
                semaphoreDictionary.semaphores.TryAdd(key, new SemaphoreEntry());
            }

            if (semaphoreDictionary.semaphores.TryGetValue(key, out var entry))
            {
                entry.MarkInUse();
                return new SmartSemaphore(entry.Semaphore, key, ReleaseSemaphore);
            }

            // Fallback - should not happen
            var fallbackEntry = new SemaphoreEntry();
            semaphoreDictionary.semaphores.TryAdd(key, fallbackEntry);
            fallbackEntry.MarkInUse();
            return new SmartSemaphore(fallbackEntry.Semaphore, key, ReleaseSemaphore);
        }

        public static SemaphoreSlim Get(string key)
        {
            var semaphoreDictionary = GetInstance();
            
            if (!semaphoreDictionary.semaphores.ContainsKey(key))
            {
                semaphoreDictionary.semaphores.TryAdd(key, new SemaphoreEntry());
            }

            if (semaphoreDictionary.semaphores.TryGetValue(key, out var entry))
            {
                entry.MarkInUse();
                return entry.Semaphore;
            }

            // Fallback
            return new SemaphoreSlim(1, 1);
        }

        public static void ReleaseSemaphore(string key)
        {
            var semaphoreDictionary = GetInstance();
            if (semaphoreDictionary.semaphores.TryGetValue(key, out var entry))
            {
                entry.MarkNotInUse();
            }
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

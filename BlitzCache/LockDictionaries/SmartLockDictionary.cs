using System;
using System.Collections.Concurrent;
using System.Threading;

namespace BlitzCacheCore.LockDictionaries
{
    public class SmartLockDictionary
    {
        private static SmartLockDictionary singleton;
        private static readonly object dictionaryLock = new object();

        private readonly ConcurrentDictionary<string, LockEntry> locks = new ConcurrentDictionary<string, LockEntry>();
        private readonly SmartCleanupManager<string, LockEntry> cleanupManager;

        private class LockEntry : ICleanupEntry
        {
            public object LockObject { get; }
            public DateTime LastAccessed { get; set; }
            public bool IsInUse { get; set; }

            public LockEntry()
            {
                LockObject = new object();
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
        }

        private SmartLockDictionary()
        {
            // Ultra-aggressive cleanup - check every 10 seconds
            // Only keep locks that are actively in use or recently used
            cleanupManager = new SmartCleanupManager<string, LockEntry>(
                locks, 
                maxIdleTime: TimeSpan.FromSeconds(30), // Very short fallback timeout
                cleanupInterval: TimeSpan.FromSeconds(10),
                customCleanupLogic: ShouldCleanupEntry);
        }

        private bool ShouldCleanupEntry(LockEntry entry)
        {
            // Simple rule: Only clean up locks that are not actively in use
            // Since locks are cheap to create on-demand, we don't need to keep unused ones
            return !entry.IsInUse;
        }

        public static SmartLockDictionary GetInstance()
        {
            if (singleton == null)
                lock (dictionaryLock)
                    singleton ??= new SmartLockDictionary();

            return singleton;
        }

        public static SmartLock GetSmartLock(string key)
        {
            var locksDictionary = GetInstance();
            
            if (!locksDictionary.locks.ContainsKey(key))
            {
                locksDictionary.locks.TryAdd(key, new LockEntry());
            }

            if (locksDictionary.locks.TryGetValue(key, out var entry))
            {
                entry.MarkInUse();
                return new SmartLock(entry.LockObject, key, ReleaseLock);
            }

            // Fallback - should not happen
            var fallbackEntry = new LockEntry();
            locksDictionary.locks.TryAdd(key, fallbackEntry);
            fallbackEntry.MarkInUse();
            return new SmartLock(fallbackEntry.LockObject, key, ReleaseLock);
        }

        public static object Get(string key)
        {
            var locksDictionary = GetInstance();
            
            if (!locksDictionary.locks.ContainsKey(key))
            {
                locksDictionary.locks.TryAdd(key, new LockEntry());
            }

            if (locksDictionary.locks.TryGetValue(key, out var entry))
            {
                entry.MarkInUse();
                return entry.LockObject;
            }

            // Fallback
            return new object();
        }

        public static void ReleaseLock(string key)
        {
            var locksDictionary = GetInstance();
            if (locksDictionary.locks.TryGetValue(key, out var entry))
            {
                entry.MarkNotInUse();
            }
        }

        public static int GetNumberOfLocks() => GetInstance().locks.Count;

        public static void TriggerCleanup() => GetInstance().cleanupManager.TriggerCleanup();

        public static void Dispose()
        {
            singleton?.cleanupManager?.Dispose();
            singleton?.locks?.Clear();
            singleton = null;
        }
    }
}

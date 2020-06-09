using System.Collections.Generic;
using System.Threading;

namespace BlitzCacheCore.LockDictionaries
{
    public class SemaphoreDictionary
    {
        private static SemaphoreDictionary singleton;
        private static readonly object dictionaryLock = new object();

        private readonly Dictionary<string, SemaphoreSlim> locks;

        private SemaphoreDictionary()
        {
            locks = new Dictionary<string, SemaphoreSlim>();
        }

        private static SemaphoreDictionary GetInstance()
        {
            if (singleton == null)
                lock (dictionaryLock)
                    singleton ??= new SemaphoreDictionary();

            return singleton;
        }

        private void AddLockSafe(string key)
        {
            if (!HasKey(key)) locks.Add(key, new SemaphoreSlim(1, 1));
        }

        private bool HasKey(string key) => locks.ContainsKey(key);

        public static SemaphoreSlim Get(string key)
        {
            var semaphoreDictionary = GetInstance();

            if (!semaphoreDictionary.HasKey(key))
            {
                lock (dictionaryLock)
                    semaphoreDictionary.AddLockSafe(key);
            }

            return semaphoreDictionary.locks[key];
        }

        public static int GetNumberOfLocks() => GetInstance().locks.Count;
    }
}
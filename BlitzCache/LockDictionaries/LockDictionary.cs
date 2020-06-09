using System.Collections.Generic;

namespace BlitzCacheCore.LockDictionaries
{
    public class LockDictionary
    {
        private static LockDictionary singleton;
        private static readonly object dictionaryLock = new object();

        private readonly Dictionary<string, object> locks = new Dictionary<string, object>();

        private LockDictionary()
        {
            locks = new Dictionary<string, object>();
        }

        private static LockDictionary GetInstance()
        {
            if (singleton == null)
                lock (dictionaryLock)
                    singleton ??= new LockDictionary();

            return singleton;
        }

        private void AddLockSafe(string key)
        {
            if (!HasKey(key)) locks.Add(key, new object());
        }

        private bool HasKey(string key) => locks.ContainsKey(key);

        public static object Get(string key)
        {
            var locksDictionary = GetInstance();

            if (!locksDictionary.HasKey(key))
            {
                lock (dictionaryLock)
                    locksDictionary.AddLockSafe(key);
            }

            return locksDictionary.locks[key];
        }

        public static int GetNumberOfLocks() => GetInstance().locks.Count;
    }
}
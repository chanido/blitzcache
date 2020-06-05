using System.Collections.Generic;
using System.Threading;

namespace BlitzCache.LockDictionaries
{
    public static class SemaphoreDictionary
    {
        private static readonly object dictionaryLock = new object();
        private static readonly Dictionary<string, SemaphoreSlim> locks = new Dictionary<string, SemaphoreSlim>();

        public static SemaphoreSlim Get(string key)
        {
            if (!locks.ContainsKey(key))
            {
                lock (dictionaryLock)
                {
                    if (!locks.ContainsKey(key)) locks.Add(key, new SemaphoreSlim(1,1));
                }
            }

            return locks[key];
        }
    }
}
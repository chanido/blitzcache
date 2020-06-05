using System.Collections.Generic;

namespace BlitzCache.LockDictionaries
{
    public static class LockDictionary
    {
        private static readonly object dictionaryLock = new object();
        private static readonly Dictionary<string, object> locks = new Dictionary<string, object>();

        public static object Get(string key)
        {
            if (!locks.ContainsKey(key))
            {
                lock (dictionaryLock)
                {
                    if (!locks.ContainsKey(key)) locks.Add(key, new object());
                }
            }

            return locks[key];
        }
    }
}
using System;
using System.Threading;

namespace BlitzCacheCore.LockDictionaries
{
    /// <summary>
    /// A smart lock that automatically tracks when it's in use and notifies the dictionary when released.
    /// </summary>
    public class SmartLock : IDisposable
    {
        private readonly object lockObject;
        private readonly string key;
        private readonly Action<string> onRelease;
        private bool disposed = false;

        public SmartLock(object lockObject, string key, Action<string> onRelease)
        {
            this.lockObject = lockObject ?? throw new ArgumentNullException(nameof(lockObject));
            this.key = key ?? throw new ArgumentNullException(nameof(key));
            this.onRelease = onRelease;

            // Acquire the lock
            Monitor.Enter(this.lockObject);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    // Release the lock
                    Monitor.Exit(lockObject);
                }
                finally
                {
                    // Notify the dictionary that this lock is no longer in use
                    onRelease?.Invoke(key);
                    disposed = true;
                }
            }
        }
    }
}

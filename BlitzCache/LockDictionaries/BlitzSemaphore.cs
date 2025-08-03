using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.LockDictionaries
{
    /// <summary>
    /// A high-performance semaphore with automatic lifecycle management and cleanup tracking.
    /// </summary>
    public class BlitzSemaphore : ICleanupEntry, IDisposable
    {
        private readonly SemaphoreSlim semaphore;
        private int activeUsers = 0;
        public DateTime LastAccessed { get; private set; } = DateTime.UtcNow;
        public bool IsInUse => activeUsers > 0 || (DateTime.UtcNow - LastAccessed).TotalSeconds < 1;
        public bool IsDisposed { get; private set; }= false;
        private static readonly object blitzSemaphoreLock = new object();


        public BlitzSemaphore()
        {
            semaphore = new SemaphoreSlim(1, 1);
        }

        public void MarkAsAccessed() => LastAccessed = DateTime.UtcNow; // Update last accessed time

        private void IncreaseActiveUsers()
        {
            if (IsDisposed) return;

            lock (blitzSemaphoreLock)
            {
                if (IsDisposed) return; // Double-check after acquiring lock
                MarkAsAccessed(); // Update access time
                activeUsers++;
            }
        }

        private void DecreaseActiveUsers()
        {
            if (IsDisposed || activeUsers == 0) return;
            lock (blitzSemaphoreLock)
            {
                if (IsDisposed || activeUsers == 0) return; // Double-check after acquiring lock
                activeUsers--;
            }
        }

        /// <summary>
        /// Asynchronously acquire the semaphore. Returns an IDisposable that automatically releases when disposed.
        /// </summary>
        public async Task<IDisposable> AcquireAsync()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(BlitzSemaphore));

            IncreaseActiveUsers();
            await semaphore.WaitAsync();
            return new BlitzSemaphoreReleaser(this);
        }
        
        /// <summary>
        /// Synchronously acquire the semaphore. Returns an IDisposable that automatically releases when disposed.
        /// </summary>
        public IDisposable Acquire() => AcquireAsync().GetAwaiter().GetResult();

        private void ReleaseSemaphore()
        {
            if (IsDisposed) return;
            
            lock (blitzSemaphoreLock)
            {
                if (IsDisposed) return; // Double-check after acquiring lock
                semaphore.Release();
                DecreaseActiveUsers();
            }
        }

        public bool AttemptDispose()
        {
            if (IsDisposed) return true;
            if (IsInUse) return false;

            IsDisposed = true;
            semaphore?.Dispose();
            return true;
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                semaphore?.Dispose();
                IsDisposed = true;
            }
        }

        private class BlitzSemaphoreReleaser : IDisposable
        {
            private readonly BlitzSemaphore entry;
            private bool disposed = false;

            public BlitzSemaphoreReleaser(BlitzSemaphore entry) => this.entry = entry;

            public void Dispose()
            {
                if (!disposed)
                {
                    entry.ReleaseSemaphore();
                    disposed = true;
                }
            }
        }
    }
}

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
        private readonly object lockObject = new object();
        private int activeUsers = 0;
        public DateTime LastAccessed { get; private set; } = DateTime.UtcNow;
        public bool IsInUse => activeUsers > 0 || (DateTime.UtcNow - LastAccessed).TotalSeconds < 1;
        public bool IsDisposed { get; private set; } = false;


        public BlitzSemaphore() => semaphore = new SemaphoreSlim(1, 1);

        public void MarkAsAccessed() => LastAccessed = DateTime.UtcNow;

        private bool ExecuteIfNotDisposed(Action action)
        {
            if (IsDisposed) return false;
            
            lock (lockObject)
            {
                if (IsDisposed) return false;
                action();
                return true;
            }
        }

        private void IncreaseActiveUsers()
        {
            ExecuteIfNotDisposed(() =>
            {
                MarkAsAccessed();
                activeUsers++;
            });
        }

        private void DecreaseActiveUsers()
        {
            if (activeUsers == 0) return;
            ExecuteIfNotDisposed(() =>
            {
                if (activeUsers > 0) activeUsers--;
            });
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
            if (ExecuteIfNotDisposed(() => semaphore.Release()))
            {
                DecreaseActiveUsers();
            }
        }

        public bool AttemptDispose()
        {
            if (IsDisposed) return true;
            
            lock (lockObject)
            {
                if (IsDisposed) return true;
                if (IsInUse) return false;
                
                PerformDisposal();
                return true;
            }
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            
            lock (lockObject)
            {
                if (IsDisposed) return;
                PerformDisposal();
            }
        }

        private void PerformDisposal()
        {
            IsDisposed = true;
            semaphore?.Dispose();
        }

        private class BlitzSemaphoreReleaser : IDisposable
        {
            private readonly BlitzSemaphore entry;
            private bool disposed = false;

            public BlitzSemaphoreReleaser(BlitzSemaphore entry) => this.entry = entry;

            public void Dispose()
            {
                if (disposed) return;
                
                entry.ReleaseSemaphore();
                disposed = true;
            }
        }
    }
}

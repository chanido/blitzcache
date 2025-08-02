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
        private bool disposed = false;
        
        public DateTime LastAccessed { get; set; }
        public bool IsInUse => activeUsers > 0;

        public BlitzSemaphore()
        {
            semaphore = new SemaphoreSlim(1, 1);
            LastAccessed = DateTime.UtcNow;
        }

        public void UpdateAccess() => LastAccessed = DateTime.UtcNow;

        /// <summary>
        /// Asynchronously acquire the semaphore. Returns an IDisposable that automatically releases when disposed.
        /// </summary>
        public async Task<IDisposable> AcquireAsync()
        {
            if (disposed) throw new ObjectDisposedException(nameof(BlitzSemaphore));
            
            Interlocked.Increment(ref activeUsers);
            UpdateAccess();
            await semaphore.WaitAsync();
            return new SemaphoreReleaser(this);
        }

        /// <summary>
        /// Asynchronously acquire the semaphore with a timeout. Returns null if timeout expires.
        /// </summary>
        public async Task<IDisposable> AcquireAsync(int timeoutMs)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BlitzSemaphore));
            
            Interlocked.Increment(ref activeUsers);
            UpdateAccess();
            var acquired = await semaphore.WaitAsync(timeoutMs);
            if (acquired)
                return new SemaphoreReleaser(this);
            
            Interlocked.Decrement(ref activeUsers);
            return null;
        }

        /// <summary>
        /// Asynchronously acquire the semaphore with cancellation support.
        /// </summary>
        public async Task<IDisposable> AcquireAsync(CancellationToken cancellationToken)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BlitzSemaphore));
            
            Interlocked.Increment(ref activeUsers);
            UpdateAccess();
            await semaphore.WaitAsync(cancellationToken);
            return new SemaphoreReleaser(this);
        }

        private void ReleaseSemaphore()
        {
            if (disposed) return;
            
            semaphore.Release();
            Interlocked.Decrement(ref activeUsers);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                semaphore?.Dispose();
                disposed = true;
            }
        }

        private class SemaphoreReleaser : IDisposable
        {
            private readonly BlitzSemaphore entry;
            private bool disposed = false;

            public SemaphoreReleaser(BlitzSemaphore entry) => this.entry = entry;

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

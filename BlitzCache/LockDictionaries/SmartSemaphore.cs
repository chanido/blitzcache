using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.LockDictionaries
{
    /// <summary>
    /// A smart semaphore that automatically tracks when it's in use and notifies the dictionary when released.
    /// </summary>
    public class SmartSemaphore : IDisposable
    {
        private readonly SemaphoreSlim semaphore;
        private readonly string key;
        private readonly Action<string> onRelease;
        private bool disposed = false;
        private bool semaphoreAcquired = false;

        public SmartSemaphore(SemaphoreSlim semaphore, string key, Action<string> onRelease)
        {
            this.semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
            this.key = key ?? throw new ArgumentNullException(nameof(key));
            this.onRelease = onRelease;
        }

        /// <summary>
        /// Asynchronously acquire the semaphore.
        /// </summary>
        public async Task WaitAsync()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SmartSemaphore));

            await semaphore.WaitAsync();
            semaphoreAcquired = true;
        }

        /// <summary>
        /// Asynchronously acquire the semaphore with a timeout.
        /// </summary>
        public async Task<bool> WaitAsync(int timeoutMs)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SmartSemaphore));

            var acquired = await semaphore.WaitAsync(timeoutMs);
            if (acquired)
            {
                semaphoreAcquired = true;
            }
            return acquired;
        }

        /// <summary>
        /// Asynchronously acquire the semaphore with a cancellation token.
        /// </summary>
        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(SmartSemaphore));

            await semaphore.WaitAsync(cancellationToken);
            semaphoreAcquired = true;
        }

        /// <summary>
        /// Release the semaphore.
        /// </summary>
        public void Release()
        {
            if (disposed || !semaphoreAcquired)
                return;

            try
            {
                semaphore.Release();
                semaphoreAcquired = false;
            }
            finally
            {
                // Notify the dictionary that this semaphore is no longer in use
                onRelease?.Invoke(key);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                try
                {
                    // Release the semaphore if it was acquired
                    if (semaphoreAcquired)
                    {
                        Release();
                    }
                }
                finally
                {
                    disposed = true;
                }
            }
        }
    }
}

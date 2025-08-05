using BlitzCacheCore.Statistics;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#nullable enable

namespace BlitzCacheCore
{
    public class BlitzCache : IBlitzCache
    {
        private static IBlitzCacheInstance? globalInstance;

        private static readonly object globalLock = new object();

        /// <summary>
        /// Creates a new BlitzCache instance.
        /// </summary>
        /// <param name="defaultMilliseconds">Default cache duration in milliseconds</param>
        /// <param name="enableStatistics">Whether to enable statistics tracking (default: false for better performance)</param>
        /// <param name="cleanupInterval">Interval for automatic cleanup of unused semaphores (default: 10 seconds)</param>
        public BlitzCache(long? defaultMilliseconds = 60000, bool? enableStatistics = false, TimeSpan? cleanupInterval = null)
        {
            if (defaultMilliseconds < 1) throw new ArgumentOutOfRangeException(nameof(defaultMilliseconds), "Default milliseconds must be non-negative");

            if (globalInstance is null)
            {
                lock (globalLock)
                {
                    if (globalInstance is null)
                    {
                        globalInstance = new BlitzCacheInstance(defaultMilliseconds, enableStatistics, cleanupInterval);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current number of semaphores for testing and monitoring purposes.
        /// </summary>
        public int GetSemaphoreCount() => globalInstance!.GetSemaphoreCount();

        /// <summary>
        /// Gets cache performance statistics and monitoring information.
        /// Use these metrics to understand cache effectiveness and optimize cache configuration.
        /// Returns null if statistics are disabled for better performance.
        /// </summary>
        public ICacheStatistics? Statistics => globalInstance?.Statistics;


        public T BlitzGet<T>(Func<T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            globalInstance!.BlitzGet(callerMemberName + sourceFilePath, nuances => function(), milliseconds);

        public T BlitzGet<T>(Func<Nuances, T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            globalInstance!.BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null) => globalInstance!.BlitzGet(cacheKey, nuances => function(), milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds = null) => globalInstance!.BlitzGet(cacheKey, function, milliseconds);

        public Task<T> BlitzGet<T>(Func<Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            globalInstance!.BlitzGet(callerMemberName + sourceFilePath, nuances => function(), milliseconds);

        public Task<T> BlitzGet<T>(Func<Nuances, Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            globalInstance!.BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public Task<T> BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null) =>
            globalInstance!.BlitzGet(cacheKey, nuances => function(), milliseconds);

        public Task<T> BlitzGet<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds = null) =>
            globalInstance!.BlitzGet(cacheKey, function, milliseconds);

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds) => globalInstance!.BlitzUpdate<T>(cacheKey, function, milliseconds);

        public Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds) => globalInstance!.BlitzUpdate<T>(cacheKey, function, milliseconds);

        public void Remove(string cacheKey) => globalInstance!.Remove(cacheKey);

#if DEBUG
        internal IBlitzCacheInstance? GetInternalInstance() => globalInstance;

        internal static void ClearGlobalForTesting()
        {
            if (globalInstance is null) return;

            globalInstance.Dispose();
            globalInstance = null;
        }
#endif
    }
}

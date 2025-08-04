using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BlitzCacheCore.Statistics;

#nullable enable

namespace BlitzCacheCore
{
    public class NullBlitzCacheForTesting : IBlitzCache
    {
        private readonly ICacheStatistics? nullStatistics;

        public NullBlitzCacheForTesting(bool enableStatistics = false) 
        { 
            nullStatistics = enableStatistics ? new NullCacheStatistics() : null;
        }

        public ICacheStatistics? Statistics => nullStatistics;

        public T BlitzGet<T>(Func<T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);
        public T BlitzGet<T>(Func<Nuances, T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null) => function.Invoke();
        public T BlitzGet<T>(string cacheKey, Func<Nuances,T> function, long? milliseconds = null) => function.Invoke(new Nuances());

        public Task<T> BlitzGet<T>(Func<Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public Task<T> BlitzGet<T>(Func<Nuances, Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public async Task<T> BlitzGet<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds = null)
            => await function.Invoke(new Nuances());
        public async Task<T> BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null)
            => await function.Invoke();

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds) { }

        public Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds) => Task.CompletedTask;

        public void Remove(string cacheKey) { }

        public int GetSemaphoreCount() => 0;

        public void Dispose() { }

        protected virtual void Dispose(bool dispose) { }
    }

    /// <summary>
    /// Null implementation of cache statistics for testing purposes.
    /// Always returns zero for all metrics.
    /// </summary>
    internal class NullCacheStatistics : ICacheStatistics
    {
        public long HitCount => 0;
        public long MissCount => 0;
        public double HitRatio => 0.0;
        public long EntryCount => 0;
        public long EvictionCount => 0;
        public int ActiveSemaphoreCount => 0;
        public long TotalOperations => 0;
        public void Reset() { }
    }
}

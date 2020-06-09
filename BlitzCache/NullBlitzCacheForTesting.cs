using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlitzCacheCore
{
    public class NullBlitzCacheForTesting : IBlitzCache
    {
        public NullBlitzCacheForTesting() { }

        public T BlitzGet<T>(Func<T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public T BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null) => function.Invoke();

        public Task<T> BlitzGet<T>(Func<Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            BlitzGet(callerMemberName + sourceFilePath, function, milliseconds);

        public async Task<T> BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null)
            => await function.Invoke();

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds) { }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async void BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds) { }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously


        public void Remove(string cacheKey) { }

        public void Dispose() { }

        protected virtual void Dispose(bool dispose) { }
    }
}

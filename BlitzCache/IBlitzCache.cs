using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BlitzCacheCore
{
    public interface IBlitzCache
    {
        T BlitzGet<T>(Func<T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "");
        T BlitzGet<T>(Func<Nuances, T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "");
        Task<T> BlitzGet<T>(Func<Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "");
        Task<T> BlitzGet<T>(Func<Nuances, Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "");
        T BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null);
        T BlitzGet<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds = null);
        Task<T> BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null);
        Task<T> BlitzGet<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds = null);
        void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds);
        Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds);
        void Remove(string cacheKey);
        int GetSemaphoreCount();
        void Dispose();
    }
}
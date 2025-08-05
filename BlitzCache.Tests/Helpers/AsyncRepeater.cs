using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Helpers
{
    public static class AsyncRepeater
    {
        public static async Task Go<T>(int times, Func<Task<T>> function)
        {
            var tasks = new List<Task<T>>();

            for (int i = 0; i < times; i++)
                tasks.Add(function());

            await Task.WhenAll(tasks);

            var result = tasks[^1].Result;
        }

        /// <summary>
        /// Enhanced async repeater that returns all results and timing information
        /// </summary>
        public static async Task<ConcurrentTestResult<T>> GoWithResults<T>(int times, Func<Task<T>> function, int staggerDelayMs = 0)
        {
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<T>>();

            for (int i = 0; i < times; i++)
            {
                if (i > 0 && staggerDelayMs > 0)
                    await Task.Delay(staggerDelayMs);

                tasks.Add(function());
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            return new ConcurrentTestResult<T>
            {
                Results = results,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                UniqueResults = results.Distinct().ToArray(),
                AllResultsIdentical = results.Distinct().Count() == 1
            };
        }

        /// <summary>
        /// Enhanced sync repeater that returns all results
        /// </summary>
        public static ConcurrentTestResult<T> GoSyncWithResults<T>(int times, Func<T> function)
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new T[times];

            Parallel.For(0, times, i =>
            {
                results[i] = function();
            });

            stopwatch.Stop();

            return new ConcurrentTestResult<T>
            {
                Results = results,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                UniqueResults = results.Distinct().ToArray(),
                AllResultsIdentical = results.Distinct().Count() == 1
            };
        }
    }

    /// <summary>
    /// Result container for concurrent test operations
    /// </summary>
    public class ConcurrentTestResult<T>
    {
        public T[] Results { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public T[] UniqueResults { get; set; }
        public bool AllResultsIdentical { get; set; }

        public int ResultCount => Results?.Length ?? 0;
        public int UniqueResultCount => UniqueResults?.Length ?? 0;
        public T FirstResult => Results != null && Results.Length > 0 ? Results[0] : default(T);
    }
}

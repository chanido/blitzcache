using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlitzCache.Tests.Helpers
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
    }
}

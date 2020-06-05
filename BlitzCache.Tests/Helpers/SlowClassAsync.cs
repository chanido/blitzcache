using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlitzCache.Tests.Helpers
{
    public class SlowClassAsync
    {
        public int Counter { get; set; }

        public async Task<int> ProcessQuickly() => await Process(100);
        public async Task<int> ProcessSlowly() => await Process(1000);

        private async Task<int> Process(int milliseconds) 
        {
            await Task.Run(() => sleep(milliseconds));

            return ++Counter;
        }

        void sleep(int time) => System.Threading.Thread.Sleep(time);
    }
}

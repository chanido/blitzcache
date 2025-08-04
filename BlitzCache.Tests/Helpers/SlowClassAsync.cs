using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Helpers
{
    public class SlowClassAsync
    {
        private static readonly object locker = new object();
        public int Counter { get; set; }

        public async Task<int> ProcessQuickly() => await Process(10);
        public async Task<int> ProcessSlowly() => await Process(50);

        private async Task<int> Process(int milliseconds)
        {
            await Task.Run(() => System.Threading.Thread.Sleep(milliseconds));

            lock (locker)
                return ++Counter;
        }
    }
}

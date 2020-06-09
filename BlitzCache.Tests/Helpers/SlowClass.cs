namespace BlitzCacheCore.Tests.Helpers
{
    public class SlowClass
    {
        private static readonly object locker = new object();
        public int Counter { get; set; }

        public int ProcessQuickly() => Process(100);
        public int ProcessSlowly() => Process(1000);

        private int Process(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);

            lock (locker)
                return ++Counter;
        }
    }
}

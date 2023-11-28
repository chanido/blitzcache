using System;

namespace BlitzCacheCore.Tests.Helpers
{
    public class SlowClass
    {
        private static readonly object locker = new object();
        public int Counter { get; set; }

        public int ProcessQuickly() => Process(100);
        public int ProcessSlowly() => Process(1000);
        public bool FailIfZeroTrueIfEven(int number)
        {
            Process(0);

            if (number == 0) throw new Exception("Zero");

            return number % 2 == 0;
        }

        private int Process(int milliseconds)
        {
            System.Threading.Thread.Sleep(milliseconds);

            lock (locker)
                return ++Counter;
        }

        internal void ResetCounter()
        {
            lock (locker)
                Counter = 0;
        }
    }
}

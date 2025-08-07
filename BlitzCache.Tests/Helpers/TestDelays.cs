using BlitzCacheCore.LockDictionaries;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests.Helpers
{
    internal static class TestDelays
    {
        public static Task LongDelay() => Task.Delay(TestConstants.LongTimeoutMs);
        public static Task MinimumDelay() => Task.Delay(TestConstants.EvictionCallbackWaitMs);
        public static Task ShortDelay() => Task.Delay(TestConstants.VeryShortTimeoutMs);

        public static Task WaitForEvictionCallbacks() => Task.Delay(TestConstants.EvictionCallbackWaitMs);
        public static void WaitForEvictionCallbacksSync() => Thread.Sleep(TestConstants.EvictionCallbackWaitMs);

        public static Task WaitForSemaphoreExpiration() => Task.Delay(BlitzSemaphore.BlitzSemaphoreExpirationSeconds * 1500);
        public static Task WaitForStandardExpiration() => Task.Delay(TestConstants.StandardTimeoutMs + TestConstants.ExpirationBufferMs);
    }
}
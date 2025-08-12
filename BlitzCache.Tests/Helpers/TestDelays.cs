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

        /// <summary>
        /// Repeatedly waits (with StandardExpiration delay) until condition returns true or attempts exhausted.
        /// </summary>
        public static async Task<bool> WaitUntilAsync(System.Func<bool> condition, int maxAttempts = 3)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                if (condition()) return true;
                await WaitForStandardExpiration();
            }
            return condition();
        }

        /// <summary>
        /// Synchronous variant for tests that are not async. Blocks the thread between attempts.
        /// </summary>
        public static bool WaitUntil(System.Func<bool> condition, int maxAttempts = 3)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                if (condition()) return true;
                WaitForStandardExpiration().GetAwaiter().GetResult();
            }
            return condition();
        }
    }
}
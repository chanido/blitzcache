using System;
using System.Threading;
using System.Threading.Tasks;
using BlitzCacheCore.LockDictionaries;

namespace BlitzCacheCore.Tests.Helpers
{
    /// <summary>
    /// Factory class for creating commonly used test configurations and constants.
    /// Centralizes test timeouts and cache configurations for easier maintenance.
    /// </summary>
    public static class TestHelpers
    {
        #region Timeout Constants - Use These for New Tests

        public const int ConcurrentOperationsCount = 100;
        public const int SmallLoopCount = 10;
        public const int LargeLoopCount = 1000;

        public const int EvictionCallbackWaitMs = 1;

        public const int VeryShortTimeoutMs = EvictionCallbackWaitMs * 5;

        public const int StandardTimeoutMs = EvictionCallbackWaitMs * 25;

        public const int LongTimeoutMs = EvictionCallbackWaitMs * 100;

        public const int ExpirationBufferMs = VeryShortTimeoutMs;

        #endregion

        #region Factory Methods

        /// <summary>
        /// Creates a basic BlitzCache instance for general testing. RECOMMENDED
        /// </summary>
        public static IBlitzCache CreateBasic() =>
            new BlitzCache(defaultMilliseconds: LongTimeoutMs, enableStatistics: false,
                cleanupInterval: TimeSpan.FromMilliseconds(StandardTimeoutMs));

        /// <summary>
        /// Creates a BlitzCache instance with statistics enabled for testing. RECOMMENDED
        /// </summary>
        public static IBlitzCache CreateWithStatistics() =>
            new BlitzCache(defaultMilliseconds: LongTimeoutMs, enableStatistics: true,
                cleanupInterval: TimeSpan.FromMilliseconds(StandardTimeoutMs));

        #endregion

        #region Wait Methods

        public static Task WaitForEvictionCallbacks() => Task.Delay(EvictionCallbackWaitMs);
        public static void WaitForEvictionCallbacksSync() => Thread.Sleep(EvictionCallbackWaitMs);

        public static Task MinimumDelay() => Task.Delay(EvictionCallbackWaitMs);
        public static Task ShortDelay() => Task.Delay(VeryShortTimeoutMs);
        public static Task WaitForStandardExpiration() => Task.Delay(StandardTimeoutMs + ExpirationBufferMs);

        public static Task LongDelay() => Task.Delay(LongTimeoutMs);
        
        public static Task WaitForSemaphoreExpiration() => Task.Delay(BlitzSemaphore.BlitzSemaphoreExpirationSeconds * 1500);
                
        #endregion
    }
}

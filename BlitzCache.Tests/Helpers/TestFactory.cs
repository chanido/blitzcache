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
    public static class TestFactory
    {
        public const int ConcurrentOperationsCount = 100;
        public const int SmallLoopCount = 10;
        public const int LargeLoopCount = 1000;

        #region Core Timeout Constants - Use These for New Tests

        /// <summary>Small delay for eviction callback execution (1ms - reduced for faster tests)</summary>
        public const int EvictionCallbackWaitMs = 1;

        /// <summary>Very short timeout for quick expiration tests</summary>
        public const int VeryShortTimeoutMs = EvictionCallbackWaitMs * 5;

        /// <summary>Short timeout for quick tests</summary>
        public const int ShortTimeoutMs = EvictionCallbackWaitMs * 10;

        /// <summary>Standard timeout for most tests</summary>
        public const int StandardTimeoutMs = EvictionCallbackWaitMs * 50;

        /// <summary>Default timeout matching BlitzCache default</summary>
        public const int LongTimeoutMs = EvictionCallbackWaitMs * 100;

        public const int ExpirationBufferMs = VeryShortTimeoutMs;

        #endregion

        #region Core Factory Methods

        /// <summary>
        /// Creates a basic BlitzCache instance for general testing. RECOMMENDED
        /// </summary>
        public static IBlitzCache CreateBasic() =>
            new BlitzCache(defaultMilliseconds: LongTimeoutMs, enableStatistics: false,
                cleanupInterval: TimeSpan.FromMilliseconds(ShortTimeoutMs));

        /// <summary>
        /// Creates a BlitzCache instance with statistics enabled for testing. RECOMMENDED
        /// </summary>
        public static IBlitzCache CreateWithStatistics() =>
            new BlitzCache(defaultMilliseconds: LongTimeoutMs, enableStatistics: true,
                cleanupInterval: TimeSpan.FromMilliseconds(ShortTimeoutMs));
        #endregion

        #region Core Wait Methods

        public static Task WaitForEvictionCallbacks() => Task.Delay(EvictionCallbackWaitMs);
        public static void WaitForEvictionCallbacksSync() => Thread.Sleep(EvictionCallbackWaitMs);

        public static Task ShortDelay() => Task.Delay(VeryShortTimeoutMs);
        public static Task WaitForShortExpiration() => Task.Delay(ShortTimeoutMs + ExpirationBufferMs);

        public static Task StandardDelay() => Task.Delay(ShortTimeoutMs);
        public static Task WaitForStandardExpiration() => Task.Delay(StandardTimeoutMs + ExpirationBufferMs);

        public static Task LongDelay() => Task.Delay(LongTimeoutMs);
        
        public static Task WaitForSemaphoreExpiration() => Task.Delay(BlitzSemaphore.BlitzSemaphoreExpirationSeconds * 1000 + ExpirationBufferMs);
                
        #endregion
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;

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

        /// <summary>Small delay for eviction callback execution (5ms - reduced for faster tests)</summary>
        public const int EvictionCallbackWaitMs = 5;

        /// <summary>Very short timeout for quick expiration tests (25ms - reduced for faster tests)</summary>
        public const int VeryShortTimeoutMs = 25;
        
        /// <summary>Short timeout for quick tests (100ms) - RECOMMENDED</summary>
        public const int ShortTimeoutMs = 100;
        
        /// <summary>Standard timeout for most tests (1000ms) - RECOMMENDED</summary>
        public const int StandardTimeoutMs = 500;
        
        /// <summary>Default timeout matching BlitzCache default (60 seconds) - RECOMMENDED</summary>
        public const int LongTimeoutMs = 60000;
        
        public const int ExpirationBufferMs = 100;
        
        #endregion

        #region Core Factory Methods - Use These for New Tests

        /// <summary>
        /// Creates a basic BlitzCache instance for general testing. RECOMMENDED
        /// </summary>
        public static IBlitzCache CreateBasic() =>
            new BlitzCache(defaultMilliseconds: LongTimeoutMs, enableStatistics: false,
                cleanupInterval: TimeSpan.FromMilliseconds(300));
        
        /// <summary>
        /// Creates a BlitzCache instance with statistics enabled for testing. RECOMMENDED
        /// </summary>
        public static IBlitzCache CreateWithStatistics() => 
            new BlitzCache(defaultMilliseconds: LongTimeoutMs, enableStatistics: true, 
                cleanupInterval: TimeSpan.FromMilliseconds(300));
        
        /// <summary>
        /// Creates a BlitzCache instance optimized for performance testing. RECOMMENDED
        /// </summary>
        public static IBlitzCache CreateForPerformanceTests() => 
            new BlitzCache(defaultMilliseconds: LongTimeoutMs, enableStatistics: true, 
                cleanupInterval: TimeSpan.FromMilliseconds(300));
        
        /// <summary>
        /// Creates a BlitzCache instance optimized for cleanup tests. RECOMMENDED
        /// </summary>
        public static IBlitzCache CreateForCleanupTests() => 
            new BlitzCache(defaultMilliseconds: LongTimeoutMs, enableStatistics: false, 
                cleanupInterval: TimeSpan.FromMilliseconds(ShortTimeoutMs));

        #endregion

        #region Core Wait Methods - Use These for New Tests
        
        public static Task WaitForEvictionCallbacks() => Task.Delay(EvictionCallbackWaitMs);
        public static void WaitForEvictionCallbacksSync() => Thread.Sleep(EvictionCallbackWaitMs);

        public static Task ShortDelay() => Task.Delay(VeryShortTimeoutMs);
        public static Task WaitForShortExpiration() => Task.Delay(ShortTimeoutMs + ExpirationBufferMs);
        
        public static Task StandardDelay() => Task.Delay(ShortTimeoutMs);
        public static Task WaitForStandardExpiration() => Task.Delay(StandardTimeoutMs + ExpirationBufferMs);
        

        public static Task LongDelay() => Task.Delay(500);
                
        #endregion

        #region Legacy Methods and Constants - TODO: Migrate to Core Methods Above

        #region Test-Specific TimeSpans
        
        /// <summary>Maximum wait time for test operations (1.5 seconds - aggressively reduced for faster tests)</summary>
        public static readonly TimeSpan MaxTestWaitTime = TimeSpan.FromSeconds(1.5);
        
        #endregion

        #region Performance Test Constants

        /// <summary>Reduced iteration count for fast cache hit tests (5000 - reduced for faster tests)</summary>
        public const int FastHitIterations = 5000;
        
        /// <summary>Standard iteration count for cache miss tests (500 - reduced for faster tests)</summary>
        public const int StandardMissIterations = 500;
        
        /// <summary>Async operation iteration count (500 - reduced for faster tests)</summary>
        public const int AsyncIterations = 500;
        
        /// <summary>Concurrent operation thread count</summary>
        public const int ConcurrentThreads = 10;
        
        /// <summary>Operations per thread in concurrent tests (500 - reduced for faster tests)</summary>
        public const int OperationsPerThread = 500;

        #endregion

        #region Wait Times for Timing Tests
    
        
    

        
        
        
        #endregion
        
        #endregion
    }
}

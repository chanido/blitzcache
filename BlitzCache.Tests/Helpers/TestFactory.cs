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
        #region Core Timeout Constants - Use These for New Tests

        /// <summary>Small delay for eviction callback execution (5ms - reduced for faster tests)</summary>
        public const int EvictionCallbackWaitMs = 5;

        /// <summary>Very short timeout for quick expiration tests (25ms - reduced for faster tests)</summary>
        public const long VeryShortTimeoutMs = 25;
        
        /// <summary>Short timeout for quick tests (100ms) - RECOMMENDED</summary>
        public const long ShortTimeoutMs = 100;

        /// <summary>Fast timeout for quick tests (200ms - aggressively reduced for faster tests)</summary>
        public const long FastTimeoutMs = 200;
        
        /// <summary>Standard timeout for most tests (1000ms) - RECOMMENDED</summary>
        public const long StandardTimeoutMs = 1000;
        
        /// <summary>Default timeout matching BlitzCache default (60 seconds) - RECOMMENDED</summary>
        public const long DefaultTimeoutMs = 60000;
        
        /// <summary>Long timeout for performance tests (2 minutes) - RECOMMENDED</summary>
        public const long LongTimeoutMs = 120000;
        
        #endregion

        #region Core Factory Methods - Use These for New Tests
        
        /// <summary>
        /// Creates a basic BlitzCache instance for general testing. RECOMMENDED
        /// </summary>
        public static IBlitzCache CreateBasic() => 
            new BlitzCache(defaultMilliseconds: DefaultTimeoutMs, enableStatistics: false, 
                cleanupInterval: TimeSpan.FromMilliseconds(300));
        
        /// <summary>
        /// Creates a BlitzCache instance with statistics enabled for testing. RECOMMENDED
        /// </summary>
        public static IBlitzCache CreateWithStatistics() => 
            new BlitzCache(defaultMilliseconds: DefaultTimeoutMs, enableStatistics: true, 
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
            new BlitzCache(defaultMilliseconds: DefaultTimeoutMs, enableStatistics: false, 
                cleanupInterval: TimeSpan.FromMilliseconds(100));

        #endregion

        #region Core Wait Methods - Use These for New Tests
        
        public static Task WaitForEvictionCallbacks() => Task.Delay(EvictionCallbackWaitMs);
        public static void WaitForEvictionCallbacksSync() => Thread.Sleep(EvictionCallbackWaitMs);
        
        /// <summary>
        /// Short delay for eviction callbacks and small operations (10ms). RECOMMENDED
        /// </summary>
        public static Task SmallDelay() => Task.Delay(10);
        
        /// <summary>
        /// Standard delay for test synchronization (100ms). RECOMMENDED
        /// </summary>
        public static Task StandardDelay() => Task.Delay(100);
        
        /// <summary>
        /// Long delay for cleanup and expiration tests (500ms). RECOMMENDED
        /// </summary>
        public static Task LongDelay() => Task.Delay(500);
        
        /// <summary>
        /// Synchronous version of StandardDelay for non-async tests. RECOMMENDED
        /// </summary>
        public static void StandardSyncWait() => Thread.Sleep(100);
        
        #endregion

        #region Legacy Methods and Constants - TODO: Migrate to Core Methods Above

        #region Test-Specific TimeSpans
        
        /// <summary>Maximum wait time for test operations (1.5 seconds - aggressively reduced for faster tests)</summary>
        public static readonly TimeSpan MaxTestWaitTime = TimeSpan.FromSeconds(1.5);
        
        #endregion

        #region Legacy BlitzCache Factory Methods
    
        
        
        /// <summary>
        /// Waits for standard cache expiration with buffer (200ms + 100ms buffer).
        /// </summary>
        public static Task WaitForStandardExpiration() => Task.Delay(StandardExpirationMs + ExpirationBufferMs);

        
        /// <summary>
        /// Waits for very short cache expiration with short buffer (50ms + 50ms buffer).
        /// </summary>
        public static Task WaitForVeryShortExpiration() => Task.Delay(VeryShortExpirationMs + ShortExpirationBufferMs);
        
        /// <summary>
        /// Waits for standard cache expiration with short buffer (200ms + 50ms buffer).
        /// </summary>
        public static Task WaitForStandardExpirationShort() => Task.Delay(StandardExpirationMs + ShortExpirationBufferMs);
        
        /// <summary>
        /// Waits for short cache expiration with short buffer (100ms + 50ms buffer).
        /// </summary>
        public static Task WaitForShortExpirationShort() => Task.Delay(ShortExpirationMs + ShortExpirationBufferMs);
        
        /// <summary>
        /// Waits for specific cache expiration (500ms + 100ms buffer) for cleanup tests.
        /// </summary>
        public static Task WaitForCleanupExpiration() => Task.Delay(500 + ExpirationBufferMs);
        
        /// <summary>
        /// Medium delay for concurrency testing.
        /// </summary>
        public static Task MediumDelay() => Task.Delay(MediumDelayMs);
        
        /// <summary>
        /// Wait for semaphore cleanup operations.
        /// </summary>
        public static Task WaitForSemaphoreCleanup() => Task.Delay(SemaphoreCleanupWaitMs);
        
        /// <summary>
        /// Wait for extended semaphore cleanup operations.
        /// </summary>
        public static Task WaitForExtendedSemaphoreCleanup() => Task.Delay(ExtendedSemaphoreCleanupWaitMs);
        
        /// <summary>
        /// Wait for long cleanup operations in memory tests.
        /// </summary>
        public static Task WaitForLongCleanup() => Task.Delay(LongCleanupWaitMs);
        
        /// <summary>
        /// Wait beyond memory protection window.
        /// </summary>
        public static Task WaitForMemoryProtection() => Task.Delay(MemoryProtectionWaitMs);
        
        /// <summary>
        /// Short wait for protection window tests.
        /// </summary>
        public static Task WaitForShortProtection() => Task.Delay(ShortProtectionWaitMs);
        
        /// <summary>
        /// Wait for two complete cleanup cycles.
        /// </summary>
        public static Task WaitForTwoCleanupCycles() => Task.Delay(TwoCleanupCyclesWaitMs);
        
        /// <summary>
        /// Mixed concurrency operations delay.
        /// </summary>
        public static Task WaitForMixedConcurrency() => Task.Delay(MixedConcurrencyDelayMs);
        
        /// <summary>
        /// Short synchronous wait for integration tests.
        /// </summary>
        public static void ShortSyncWait() => Thread.Sleep(ShortSyncWaitMs);
        
        /// <summary>
        /// Extended synchronous wait for expiration tests.
        /// </summary>
        public static void ExtendedSyncWait() => Thread.Sleep(ExtendedSyncWaitMs);
        
        /// <summary>
        /// Long synchronous wait for example scenarios.
        /// </summary>
        public static void LongSyncWait() => Thread.Sleep(LongSyncWaitMs);
        
        /// <summary>
        /// Protection window test delay.
        /// </summary>
        public static Task WaitForProtectionWindowTest() => Task.Delay(ProtectionWindowTestMs);

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

        
        
        /// <summary>Short delay for async operations (5ms - reduced for faster tests)</summary>
        public const int AsyncOperationDelayMs = 5;
        
        /// <summary>Cleanup check interval for tests (100ms - reduced for faster tests)</summary>
        public const int CleanupCheckIntervalMs = 100;
        
        /// <summary>Protection window aging wait (600ms - reduced for faster tests)</summary>
        public const int ProtectionWindowWaitMs = 600;
        
        /// <summary>Semaphore cleanup wait for tests (300ms - reduced for faster tests)</summary>
        public const int SemaphoreCleanupWaitMs = 300;
        
        /// <summary>Expiration buffer wait (additional time beyond expiration for cleanup - 100ms)</summary>
        public const int ExpirationBufferMs = 100;
        
        /// <summary>Short expiration buffer for mixed tests (50ms)</summary>
        public const int ShortExpirationBufferMs = 50;
        
        /// <summary>Small delay for concurrency testing and thread yielding (1ms)</summary>
        public const int SmallDelayMs = 1;
        
        /// <summary>Medium delay for concurrency testing (10ms)</summary>
        public const int MediumDelayMs = 10;
        
        /// <summary>Extended semaphore cleanup wait time (700ms)</summary>
        public const int ExtendedSemaphoreCleanupWaitMs = 700;
        
        /// <summary>Long cleanup wait for memory tests (1100ms)</summary>
        public const int LongCleanupWaitMs = 1100;
        
        /// <summary>Memory protection window wait (1200ms)</summary>
        public const int MemoryProtectionWaitMs = 1200;
        
        /// <summary>Short protection window test wait (200ms)</summary>
        public const int ShortProtectionWaitMs = 200;
        
        /// <summary>Two cleanup cycles wait time (2000ms)</summary>
        public const int TwoCleanupCyclesWaitMs = 2000;
        
        /// <summary>Mixed concurrency operations delay (100ms)</summary>
        public const int MixedConcurrencyDelayMs = 100;
        
        /// <summary>Standard test expiration time (200ms)</summary>
        public const int StandardExpirationMs = 200;
        
        /// <summary>Short test expiration time (100ms)</summary>
        public const int ShortExpirationMs = 100;
        
        /// <summary>Very short test expiration time (50ms)</summary>
        public const int VeryShortExpirationMs = 50;
        
        /// <summary>Short wait for sync operations (60ms)</summary>
        public const int ShortSyncWaitMs = 60;
        
        /// <summary>Standard sync wait time (100ms)</summary>
        public const int StandardSyncWaitMs = 100;
        
        /// <summary>Extended sync wait time (110ms)</summary>
        public const int ExtendedSyncWaitMs = 110;
        
        /// <summary>Long sync wait time (150ms)</summary>
        public const int LongSyncWaitMs = 150;
        
        /// <summary>Protection window test time (300ms)</summary>
        public const int ProtectionWindowTestMs = 300;
        
        /// <summary>Circuit breaker cache time (5000ms - 5 seconds)</summary>
        public const int CircuitBreakerCacheMs = 5000;
        
        /// <summary>Long term cache time (3600000ms - 1 hour)</summary>
        public const int LongTermCacheMs = 3600000;
        
        /// <summary>Standard cache batch size (50 items)</summary>
        public const int StandardBatchSize = 50;
        
        /// <summary>Concurrent operations count (100 operations)</summary>
        public const int ConcurrentOperationsCount = 100;
        
        /// <summary>Test hit ratio threshold (0.5 for 50%)</summary>
        public const double TestHitRatio = 0.5;
        
        /// <summary>Hit ratio tolerance for assertions (0.001)</summary>
        public const double HitRatioTolerance = 0.001;
        
        /// <summary>Batch modulo value for periodic operations (100)</summary>
        public const int BatchModulo = 100;
        
        /// <summary>Warmup iterations for performance tests (1000)</summary>
        public const int WarmupIterations = 1000;
        
        /// <summary>Total hits expected in performance tests (6000)</summary>
        public const int TotalHitsExpected = 6000;
        
        /// <summary>Key repetition modulo for concurrent tests (10)</summary>
        public const int KeyRepetitionModulo = 10;
        
        /// <summary>Invalid negative timeout for error testing (-1000)</summary>
        public const int InvalidNegativeTimeout = -1000;
        
        /// <summary>Concurrent pressure test iterations (50)</summary>
        public const int ConcurrentPressureTestIterations = 50;
        
        /// <summary>Memory test batch count (20)</summary>
        public const int MemoryTestBatchCount = 20;
        
        /// <summary>Small loop count for tests (10)</summary>
        public const int SmallLoopCount = 10;
        
        /// <summary>Large loop count for stress tests (1000)</summary>
        public const int LargeLoopCount = 1000;
        
        #endregion
        
        #endregion
    }
}

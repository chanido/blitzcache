using System;

namespace BlitzCacheCore.Tests.Helpers
{
    /// <summary>
    /// Factory class for creating commonly used test configurations and constants.
    /// Centralizes test timeouts and cache configurations for easier maintenance.
    /// </summary>
    public static class TestFactory
    {
        /// <summary>
        /// Creates a basic BlitzCache instance for general testing.
        /// </summary>
        public static IBlitzCache CreateBlitzCacheGlobal() => new BlitzCache(defaultMilliseconds: TestConstants.LongTimeoutMs);

        /// <summary>
        /// Creates a BlitzCache instance with statistics enabled for testing.
        /// </summary>
        public static IBlitzCacheInstance CreateBlitzCacheInstance() =>
            new BlitzCacheInstance(TestConstants.LongTimeoutMs, cleanupInterval: TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs));
    }
}

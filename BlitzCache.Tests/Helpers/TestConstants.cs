namespace BlitzCacheCore.Tests.Helpers
{
    internal static class TestConstants
    {

        public const int ConcurrentOperationsCount = 100;
        public const int SmallLoopCount = 10;
        public const int LargeLoopCount = 1000;

        public const int EvictionCallbackWaitMs = 1;

        public const int VeryShortTimeoutMs = EvictionCallbackWaitMs * 5;

        public const int StandardTimeoutMs = EvictionCallbackWaitMs * 25;

        public const int LongTimeoutMs = EvictionCallbackWaitMs * 100;

        public const int ExpirationBufferMs = VeryShortTimeoutMs;
    }
}
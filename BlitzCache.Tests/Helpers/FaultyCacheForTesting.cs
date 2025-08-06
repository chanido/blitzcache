using BlitzCacheCore.Statistics;

namespace BlitzCacheCore.Tests.Helpers
{
    /// <summary>
    /// Test implementation of IBlitzCache that throws exceptions to test error handling.
    /// </summary>
    internal class FaultyCacheForTesting : NullBlitzCacheForTesting
    {
        public new ICacheStatistics Statistics => new FaultyTestCacheStatistics();
    }
}
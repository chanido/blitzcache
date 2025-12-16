using BlitzCacheCore.Logging;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System.Linq;

namespace BlitzCacheCore.Tests.Statistics
{
    [TestFixture]
    public class DisabledTrackingOptimizationTests
    {
        [Test]
        public void When_TopHeaviest_And_NoSizeLimit_DisablesSizeTracking()
        {
            using var cache = new BlitzCacheInstance(maxTopHeaviest: 0, maxCacheSizeBytes: null);
            cache.InitializeStatistics();

            cache.BlitzGet("k1", () => new byte[2048]);
            cache.BlitzGet("k2", () => new byte[4096]);
            TestDelays.WaitForEvictionCallbacksSync();

            var stats = cache.Statistics!;
            Assert.That(stats.ApproximateMemoryBytes, Is.EqualTo(0), "Memory bytes should remain zero when size tracking is disabled");
            Assert.That(stats.TopHeaviestEntries.Any(), Is.False, "TopHeaviestEntries should be empty when disabled");
        }

        [Test]
        public void Logger_Omits_Disabled_Sections()
        {
            // Top lists disabled
            var cache = new BlitzCacheInstance(maxTopHeaviest: 0, maxTopSlowest: 0);
            cache.InitializeStatistics();
            cache.BlitzGet("k1", () => "value");

            var testLogger = new TestLoggerForBlitzCache();
            var loggerInstance = new BlitzLoggerInstance(cache, identifier: "Test", logInterval: System.TimeSpan.Zero);

            loggerInstance.Log(testLogger);
            var output = string.Join("\n", testLogger.GetLogs());

            Assert.That(output.Contains("Top Heaviest:"), Is.False, "Log should omit Top Heaviest section when disabled");
            Assert.That(output.Contains("Top Slowest Queries:"), Is.False, "Log should omit Top Slowest Queries section when disabled");
        }
    }
}

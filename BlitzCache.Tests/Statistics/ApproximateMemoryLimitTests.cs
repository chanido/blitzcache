using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System.Linq;

namespace BlitzCacheCore.Tests.Statistics
{
    [TestFixture]
    public class ApproximateMemoryLimitTests
    {
        [Test]
        public void ApproximateMemoryBytes_DoesNotGrow_Unbounded_WithLimit()
        {
            const long maxCacheSizeBytes = 30_000;
            const int valueBytes = 8_000;

            using var cache = new BlitzCacheInstance(maxCacheSizeBytes: maxCacheSizeBytes);
            cache.InitializeStatistics();

            // Push many entries
            for (int i = 0; i < 20; i++)
            {
                cache.BlitzGet($"m{i}", () => new byte[valueBytes], TestConstants.LongTimeoutMs);
            }

            TestDelays.WaitForEvictionCallbacksSync();

            var approx = cache.Statistics!.ApproximateMemoryBytes;
            Assert.That(approx, Is.LessThanOrEqualTo(maxCacheSizeBytes), "Approximate memory should respect the configured limit");

            // Insert a bigger value, expect more evictions
            cache.BlitzGet("big", () => new byte[valueBytes * 2], TestConstants.LongTimeoutMs);
            TestDelays.WaitForEvictionCallbacksSync();

            var afterBig = cache.Statistics!.ApproximateMemoryBytes;
            Assert.That(afterBig, Is.LessThanOrEqualTo(maxCacheSizeBytes), "Even after larger insert, cache should stay within the limit");
        }
    }
}

using BlitzCacheCore.Logging;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;

namespace BlitzCacheCore.Tests.Statistics
{
    [TestFixture]
    public class BlitzLoggerInstanceTests
    {
        private IBlitzCacheInstance cacheInstance;
        private TestLoggerForBlitzCache testLogger;

        [SetUp]
        public void Setup()
        {
            cacheInstance = TestFactory.CreateBlitzCacheInstance();
            testLogger = new TestLoggerForBlitzCache();
        }

        [Test]
        public void Constructor_ThrowsOnNullInstance() => Assert.Throws<ArgumentNullException>(() => new BlitzLoggerInstance(null));

        [Test]
        public void Constructor_SetsPropertiesCorrectly()
        {
            var loggerInstance = new BlitzLoggerInstance(cacheInstance, "TestId", TimeSpan.FromMinutes(5));
            Assert.That(loggerInstance.Identifier, Is.EqualTo("TestId"));
            Assert.That(loggerInstance.LogInterval, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void Log_LogsStatisticsCorrectly()
        {
            var loggerInstance = new BlitzLoggerInstance(cacheInstance, "TestId", TimeSpan.FromMilliseconds(1));
            loggerInstance.Log(testLogger);
            var logs = testLogger.GetLogs();
            Assert.That(logs.Count, Is.GreaterThan(0));
            Assert.That(logs[0], Does.Contain("Hits: "));
        }

        [Test]
        public void Log_HandlesStatisticsException()
        {
            var mockCache = new FaultyCacheForTesting();
            var loggerInstance = new BlitzLoggerInstance(mockCache, "FaultyTest");
            loggerInstance.Log(testLogger);
            var logs = testLogger.GetLogs();
            Assert.That(logs.Exists(l => l.Contains("Error occurred while logging")));
        }

        [Test]
        public void Log_ReportsTopSlowestQueries()
        {
            // Arrange: cache with TopSlowestQueries enabled
            var cache = new BlitzCacheInstance(maxTopSlowest: 2);
            cache.InitializeStatistics();
            // Add slow queries
            cache.BlitzGet("slow1", () => { System.Threading.Thread.Sleep(30); return "v1"; });
            cache.BlitzGet("slow2", () => { System.Threading.Thread.Sleep(50); return "v2"; });
            cache.BlitzGet("slow3", () => { System.Threading.Thread.Sleep(10); return "v3"; });

            var loggerInstance = new BlitzLoggerInstance(cache, "TestId", TimeSpan.FromMilliseconds(1));
            var testLogger = new TestLoggerForBlitzCache();

            // Act
            loggerInstance.Log(testLogger);
            var logs = testLogger.GetLogs();
            var joinedLogs = string.Join("\n", logs);

            // Assert
            Assert.That(logs.Count, Is.GreaterThan(0));
            Assert.That(joinedLogs, Does.Contain("Top Slowest Queries"));
            Assert.That(joinedLogs, Does.Contain("slow1").Or.Contain("slow2").Or.Contain("slow3"), "Should log at least one slow query");
        }

        [Test]
        public void Log_ReportsNoTopSlowestQueriesWhenDisabled()
        {
            // Arrange: cache with TopSlowestQueries disabled
            var cache = new BlitzCacheInstance(maxTopSlowest: 0);
            cache.InitializeStatistics();
            cache.BlitzGet("q1", () => "v1");
            var loggerInstance = new BlitzLoggerInstance(cache, "TestId", TimeSpan.FromMilliseconds(1));
            var testLogger = new TestLoggerForBlitzCache();

            // Act
            loggerInstance.Log(testLogger);
            var logs = testLogger.GetLogs();
            var joinedLogs = string.Join("\n", logs);

            // Assert
            Assert.That(logs.Count, Is.GreaterThan(0));
            // New behavior: section omitted entirely when tracking disabled for performance
            Assert.That(joinedLogs, Does.Not.Contain("Top Slowest Queries"), "Section should be omitted when disabled");
        }
    }
}

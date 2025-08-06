using BlitzCacheCore.Logging;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;

namespace BlitzCacheCore.Tests
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
    }
}

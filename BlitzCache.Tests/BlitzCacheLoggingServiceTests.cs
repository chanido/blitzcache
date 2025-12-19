using BlitzCacheCore.Logging;
using BlitzCacheCore.Tests.Helpers;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace BlitzCacheCore.Tests
{
    [TestFixture]
    public class BlitzCacheLoggingServiceTests
    {
        private IBlitzCache testCache;
        private TestLoggerForBlitzCache testLogger;
        private BlitzCacheLoggingService loggingService;
        private TimeSpan testInterval;

        [SetUp]
        public void Setup()
        {
            testCache = TestFactory.CreateBlitzCacheInstance();
            testLogger = new TestLoggerForBlitzCache();
            testInterval = TimeSpan.FromMilliseconds(TestConstants.VeryShortTimeoutMs);

            BlitzCacheLoggingService.ClearForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            loggingService?.Dispose();
            BlitzCache.ClearGlobalForTesting();
        }

        [Test]
        public void Constructor_ValidatesArguments()
        {
            Assert.DoesNotThrow(() => new BlitzCacheLoggingService(testLogger, logInterval: testInterval));

            Assert.Throws<ArgumentNullException>(() =>
                new BlitzCacheLoggingService(null, cache: testCache, logInterval: testInterval));

            var service = new BlitzCacheLoggingService(testLogger, cache: testCache, logInterval: testInterval);
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public async Task ExecuteAsync_WithValidStatistics_LogsStatisticsPeriodically()
        {
            await GenerateCacheActivity();
            loggingService = new BlitzCacheLoggingService(testLogger, cache: testCache, logInterval: testInterval);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs)).Token;

            await loggingService.StartAsync(cancellationToken);
            await TestDelays.LongDelay();
            await loggingService.StopAsync(cancellationToken);

            var logs = testLogger.GetLogs();
            Assert.That(logs.Any(l => l.Contains("BlitzCache statistics logging started")), Is.True);
            Assert.That(logs.Any(l => l.Contains("BlitzCache statistics logging stopped")), Is.True);
            Assert.That(logs.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public async Task ExecuteAsync_WithUnexpectedException_HandlesGracefully()
        {
            var faultyCache = new FaultyCacheForTesting();
            loggingService = new BlitzCacheLoggingService(testLogger, faultyCache, logInterval: testInterval);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs)).Token;

            Exception caughtException = null;
            try
            {
                await loggingService.StartAsync(cancellationToken);
                await TestDelays.LongDelay();
                await loggingService.StopAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }

            Assert.That(caughtException, Is.Null);
            var logs = testLogger.GetLogs();
            Assert.That(logs.Any(l => l.Contains("BlitzCache statistics logging started")), Is.True);
            Assert.That(logs.Any(l => l.Contains("BlitzCache statistics logging stopped")), Is.True);
        }

        [Test]
        public void GetApplicationIdentifier_HandlesCustomAndNullIdentifiers()
        {
            var method = typeof(BlitzLoggerInstance).GetMethod("GetApplicationIdentifier", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var customResult = method.Invoke(null, ["  MyCustomApp  "]) as string;
            Assert.That(customResult, Is.EqualTo("MyCustomApp"));

            var autoResult = method.Invoke(null, [null]) as string;
            Assert.That(autoResult, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task CustomApplicationIdentifier_AppearsInLogs()
        {
            const string customIdentifier = "TestMicroservice-API";
            loggingService = new BlitzCacheLoggingService(testLogger, testCache, customIdentifier, testInterval);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs)).Token;

            await loggingService.StartAsync(cancellationToken);
            await TestDelays.LongDelay();
            await loggingService.StopAsync(cancellationToken);

            var logs = testLogger.GetLogs();
            Assert.That(logs.Any(l => l.Contains(customIdentifier)), Is.True);
        }

        [Test]
        public async Task WorksWithBlitzCacheAndInstances()
        {
            loggingService = new BlitzCacheLoggingService(testLogger, testCache, "GlobalCache", testInterval);
            BlitzCacheLoggingService.Add(new BlitzCacheInstance(), "CacheInstance", testInterval);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs)).Token;

            await loggingService.StartAsync(cancellationToken);
            await TestDelays.LongDelay();
            await loggingService.StopAsync(cancellationToken);

            var logs = testLogger.GetLogs();
            Assert.That(logs.Any(l => l.Contains("GlobalCache")), Is.True);
            Assert.That(logs.Any(l => l.Contains("CacheInstance")), Is.True);
        }

        [Test]
        public async Task WorksWithInstances()
        {
            BlitzCacheLoggingService.Add(new BlitzCacheInstance(), "CacheInstance1", testInterval);
            BlitzCacheLoggingService.Add(new BlitzCacheInstance(), "CacheInstance2", testInterval);
            loggingService = new BlitzCacheLoggingService(testLogger, testInterval);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs)).Token;

            await loggingService.StartAsync(cancellationToken);
            await TestDelays.LongDelay();
            await loggingService.StopAsync(cancellationToken);

            var logs = testLogger.GetLogs();
            Assert.That(logs.Any(l => l.Contains("CacheInstance1")), Is.True);
            Assert.That(logs.Any(l => l.Contains("CacheInstance2")), Is.True);
        }

        [Test]
        public async Task WorksWithNoInstances()
        {
            loggingService = new BlitzCacheLoggingService(testLogger, testInterval);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestConstants.StandardTimeoutMs)).Token;

            await loggingService.StartAsync(cancellationToken);
            await TestDelays.LongDelay();
            await loggingService.StopAsync(cancellationToken);

            var logs = testLogger.GetLogs();
            Assert.That(logs.Count, Is.EqualTo(2));
            Assert.That(logs.Any(l => l.Contains("BlitzCache statistics logging started")), Is.True);
            Assert.That(logs.Any(l => l.Contains("BlitzCache statistics logging stopped")), Is.True);
        }

        [Test]
        public void RegisterBlitzCacheInstanceIsIdempotent()
        {
            loggingService = new BlitzCacheLoggingService(testLogger);
            var cacheInstance = new BlitzCacheInstance();
            BlitzCacheLoggingService.Add(cacheInstance, "CacheInstance1", testInterval);
            BlitzCacheLoggingService.Add(cacheInstance, "CacheInstance2", testInterval);

            Assert.That(BlitzCacheLoggingService.GetInstances().Count, Is.EqualTo(1), "Should only register once even with multiple calls");
        }

        private async Task GenerateCacheActivity()
        {
            // Generate some cache activity to create interesting statistics
            await testCache.BlitzGet("test-key-1", () => Task.FromResult("value1"));
            await testCache.BlitzGet("test-key-2", () => Task.FromResult("value2"));
            await testCache.BlitzGet("test-key-1", () => Task.FromResult("value1")); // This should be a hit
        }
    }
}

using BlitzCacheCore.Logging;
using BlitzCacheCore.Statistics;
using BlitzCacheCore.Tests.Helpers;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
            testCache = TestHelpers.CreateWithStatistics();
            testLogger = new TestLoggerForBlitzCache();
            testInterval = TimeSpan.FromMilliseconds(TestHelpers.VeryShortTimeoutMs);
        }

        [TearDown]
        public void TearDown()
        {
            loggingService?.Dispose();
        }

        [Test]
        public void Constructor_ValidatesArguments()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new BlitzCacheLoggingService(null, testLogger, testInterval));
            
            Assert.Throws<ArgumentNullException>(() => 
                new BlitzCacheLoggingService(testCache, null, testInterval));
            
            var service = new BlitzCacheLoggingService(testCache, testLogger, testInterval);
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public async Task ExecuteAsync_WithNullStatistics_LogsWarningAndReturns()
        {
            loggingService = new BlitzCacheLoggingService(TestHelpers.CreateBasic(), testLogger, testInterval);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestHelpers.StandardTimeoutMs)).Token;

            await loggingService.StartAsync(cancellationToken);
            await TestHelpers.MinimumDelay();
            await loggingService.StopAsync(cancellationToken);

            var logs = testLogger.GetLogs();
            Assert.That(logs.Any(l => l.Contains("BlitzCache statistics logging started")), Is.True);
            Assert.That(logs.Any(l => l.Contains("BlitzCache statistics are disabled")), Is.True);
        }

        [Test]
        public async Task ExecuteAsync_WithValidStatistics_LogsStatisticsPeriodically()
        {
            await GenerateCacheActivity();
            loggingService = new BlitzCacheLoggingService(testCache, testLogger, testInterval);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestHelpers.StandardTimeoutMs)).Token;

            await loggingService.StartAsync(cancellationToken);
            await Task.Delay(testInterval * 3, CancellationToken.None);
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
            loggingService = new BlitzCacheLoggingService(faultyCache, testLogger, testInterval);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestHelpers.StandardTimeoutMs)).Token;

            Exception caughtException = null;
            try
            {
                await loggingService.StartAsync(cancellationToken);
                await Task.Delay(testInterval * 5, CancellationToken.None);
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
            var method = typeof(BlitzCacheLoggingService).GetMethod("GetApplicationIdentifier", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var customResult = method.Invoke(null, new object[] { "  MyCustomApp  " }) as string;
            Assert.That(customResult, Is.EqualTo("MyCustomApp"));

            var autoResult = method.Invoke(null, new object[] { null }) as string;
            Assert.That(autoResult, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task CustomApplicationIdentifier_AppearsInLogs()
        {
            const string customIdentifier = "TestMicroservice-API";
            loggingService = new BlitzCacheLoggingService(testCache, testLogger, testInterval, customIdentifier);
            var cancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(TestHelpers.StandardTimeoutMs)).Token;

            await loggingService.StartAsync(cancellationToken);
            await TestHelpers.MinimumDelay();
            await loggingService.StopAsync(cancellationToken);

            var logs = testLogger.GetLogs();
            Assert.That(logs.Any(l => l.Contains(customIdentifier)), Is.True);
        }

        private async Task GenerateCacheActivity()
        {
            // Generate some cache activity to create interesting statistics
            await testCache.BlitzGet("test-key-1", () => Task.FromResult("value1"));
            await testCache.BlitzGet("test-key-2", () => Task.FromResult("value2"));
            await testCache.BlitzGet("test-key-1", () => Task.FromResult("value1")); // This should be a hit
        }
    }

    /// <summary>
    /// Test implementation of IBlitzCache that throws exceptions to test error handling.
    /// </summary>
    internal class FaultyCacheForTesting : IBlitzCache
    {
        public ICacheStatistics Statistics => new FaultyTestCacheStatistics();

        public void Dispose() { }

        public T BlitzGet<T>(Func<T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            function();

        public T BlitzGet<T>(Func<Nuances, T> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            function(new Nuances());

        public Task<T> BlitzGet<T>(Func<Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            function();

        public Task<T> BlitzGet<T>(Func<Nuances, Task<T>> function, long? milliseconds = null, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string sourceFilePath = "") =>
            function(new Nuances());

        public T BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null) =>
            function();

        public T BlitzGet<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds = null) =>
            function(new Nuances());

        public Task<T> BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null) =>
            function();

        public Task<T> BlitzGet<T>(string cacheKey, Func<Nuances, Task<T>> function, long? milliseconds = null) =>
            function(new Nuances());

        public void BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds) { }

        public Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds) =>
            Task.CompletedTask;

        public void Remove(string cacheKey) { }

        public int GetSemaphoreCount() => 0;
    }

    /// <summary>
    /// Test implementation of ICacheStatistics that throws exceptions to test error handling.
    /// </summary>
    internal class FaultyTestCacheStatistics : ICacheStatistics
    {
        private int accessCount = 0;

        public long HitCount 
        {
            get
            {
                var count = ++accessCount;
                if (count <= 2) // Throw exception on first two accesses
                    throw new InvalidOperationException("Test exception for error handling");
                return 10;
            }
        }
        
        public long MissCount => 5;
        public double HitRatio => 0.667;
        public long EntryCount => 8;
        public long EvictionCount => 2;
        public int ActiveSemaphoreCount => 3;
        public long TotalOperations => 15;
        public void Reset() { }
    }
}

/// <summary>
/// Enhanced test logger that captures log messages for verification
/// </summary>
public class TestLoggerForBlitzCache : ILogger<BlitzCacheLoggingService>
{
    private readonly List<string> logs = new List<string>();

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        var message = formatter(state, exception);
        var logEntry = $"[{logLevel}] BlitzCacheLoggingService: {message}";
        lock (logs)
        {
            logs.Add(logEntry);
        }
        TestContext.WriteLine(logEntry);
    }

    public List<string> GetLogs()
    {
        lock (logs)
        {
            return new List<string>(logs);
        }
    }

    public void ClearLogs()
    {
        lock (logs)
        {
            logs.Clear();
        }
    }
}

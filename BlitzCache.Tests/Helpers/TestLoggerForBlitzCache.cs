using BlitzCacheCore.Logging;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace BlitzCacheCore.Tests.Helpers
{
    /// <summary>
    /// Enhanced test logger that captures log messages for verification
    /// </summary>
    public class TestLoggerForBlitzCache : ILogger<BlitzCacheLoggingService>
    {
        private readonly List<string> logs = [];

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
}

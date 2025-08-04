using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;

namespace BlitzCache.Tests.Helpers
{
    /// <summary>
    /// Simple test logger provider that outputs to NUnit test context
    /// </summary>
    public class TestLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new TestLogger(categoryName);

        public void Dispose() { }
    }

    public class TestLogger : ILogger
    {
        private readonly string categoryName;

        public TestLogger(string categoryName) => this.categoryName = categoryName;

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            TestContext.WriteLine($"[{logLevel}] {categoryName}: {message}");
        }
    }
}

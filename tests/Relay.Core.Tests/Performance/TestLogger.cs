using System;
using Microsoft.Extensions.Logging;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Simple test logger for benchmarks
/// </summary>
internal class TestLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}
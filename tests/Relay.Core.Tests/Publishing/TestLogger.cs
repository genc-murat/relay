using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
// Test helper class for logging verification
internal class TestLogger<T> : ILogger<T>
{
    private readonly object _lock = new();
    public List<(LogLevel LogLevel, string Message)> LoggedMessages { get; } = new();

    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (_lock)
        {
            LoggedMessages.Add((logLevel, formatter(state, exception)));
        }
    }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}

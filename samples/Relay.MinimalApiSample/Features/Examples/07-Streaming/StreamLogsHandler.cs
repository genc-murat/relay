using Relay.Core.Contracts.Handlers;
using System.Runtime.CompilerServices;

namespace Relay.MinimalApiSample.Features.Examples.Streaming;

public class StreamLogsHandler : IRequestHandler<StreamLogsRequest, IAsyncEnumerable<LogEntry>>
{
    private readonly ILogger<StreamLogsHandler> _logger;

    public StreamLogsHandler(ILogger<StreamLogsHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<IAsyncEnumerable<LogEntry>> HandleAsync(
        StreamLogsRequest request,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(StreamLogsAsync(request, cancellationToken));
    }

    private async IAsyncEnumerable<LogEntry> StreamLogsAsync(
        StreamLogsRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting log stream from {StartDate}", request.StartDate);

        // Simulate streaming logs
        var levels = new[] { "INFO", "DEBUG", "WARNING", "ERROR" };
        var messages = new[]
        {
            "Application started",
            "User logged in",
            "Database query executed",
            "Cache miss",
            "API request processed"
        };

        for (int i = 0; i < 10; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Log stream cancelled");
                yield break;
            }

            // Simulate delay between log entries
            await Task.Delay(100, cancellationToken);

            var logEntry = new LogEntry(
                Guid.NewGuid(),
                DateTime.UtcNow,
                levels[i % levels.Length],
                $"{messages[i % messages.Length]} (Entry #{i + 1})"
            );

            _logger.LogDebug("Streaming log entry: {Message}", logEntry.Message);

            yield return logEntry;
        }

        _logger.LogInformation("Log stream completed");
    }
}

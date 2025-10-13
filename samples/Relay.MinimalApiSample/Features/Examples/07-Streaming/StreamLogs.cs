using Relay.Core.Contracts.Requests;

namespace Relay.MinimalApiSample.Features.Examples.Streaming;

public record StreamLogsRequest(
    DateTime StartDate
) : IRequest<IAsyncEnumerable<LogEntry>>;

public record LogEntry(
    Guid Id,
    DateTime Timestamp,
    string Level,
    string Message
);

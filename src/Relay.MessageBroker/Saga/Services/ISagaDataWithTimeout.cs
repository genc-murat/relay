using Relay.MessageBroker.Saga.Interfaces;

namespace Relay.MessageBroker.Saga.Services;

/// <summary>
/// Extended interface for saga data that supports custom timeout configuration.
/// </summary>
public interface ISagaDataWithTimeout : ISagaData
{
    /// <summary>
    /// Gets the timeout duration for this saga.
    /// </summary>
    TimeSpan Timeout { get; }
}

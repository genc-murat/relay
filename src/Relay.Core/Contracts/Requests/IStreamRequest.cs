namespace Relay.Core.Contracts.Requests;

/// <summary>
/// Marker interface for streaming requests that return multiple responses.
/// </summary>
/// <typeparam name="TResponse">The type of the response items.</typeparam>
public interface IStreamRequest<out TResponse>
{
}
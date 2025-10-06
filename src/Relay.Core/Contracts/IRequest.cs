namespace Relay.Core;

/// <summary>
/// Marker interface for requests that return a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequest<out TResponse>
{
}

/// <summary>
/// Marker interface for requests that do not return a response.
/// </summary>
public interface IRequest
{
}

namespace Relay.Core
{
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

    /// <summary>
    /// Marker interface for streaming requests that return multiple responses.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response items.</typeparam>
    public interface IStreamRequest<out TResponse>
    {
    }


}
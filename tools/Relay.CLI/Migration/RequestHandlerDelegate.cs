namespace Relay.CLI.Migration;

/// <summary>
/// Delegate for invoking the next handler in the pipeline
/// </summary>
/// <typeparam name="TResponse">The response type</typeparam>
/// <returns>A ValueTask containing the response</returns>
public delegate ValueTask<TResponse> RequestHandlerDelegate<TResponse>();

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Represents a single behavior configuration for a mock handler.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
internal class MockHandlerBehavior<TRequest, TResponse>
{
    public MockBehaviorType BehaviorType { get; set; }
    public TResponse? Response { get; set; }
    public Func<TRequest, TResponse>? ResponseFactory { get; set; }
    public Func<TRequest, CancellationToken, Task<TResponse>>? AsyncResponseFactory { get; set; }
    public Exception? Exception { get; set; }
    public Func<object, Exception>? ExceptionFactory { get; set; }
    public TimeSpan? Delay { get; set; }
}

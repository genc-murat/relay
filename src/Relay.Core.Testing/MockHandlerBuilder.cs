using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing;

/// <summary>
/// Builder for creating mock request handlers with fluent configuration API.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public class MockHandlerBuilder<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly List<MockHandlerBehavior<TRequest, TResponse>> _behaviors = new();
    private readonly HandlerVerifier<TRequest, TResponse> _verifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockHandlerBuilder{TRequest, TResponse}"/> class.
    /// </summary>
    public MockHandlerBuilder()
    {
        _verifier = new HandlerVerifier<TRequest, TResponse>();
    }

    /// <summary>
    /// Configures the handler to return a specific response.
    /// </summary>
    /// <param name="response">The response to return.</param>
    /// <returns>The builder instance for chaining.</returns>
    public MockHandlerBuilder<TRequest, TResponse> Returns(TResponse response)
    {
        _behaviors.Add(new MockHandlerBehavior<TRequest, TResponse>
        {
            Response = response,
            BehaviorType = MockBehaviorType.Return
        });
        return this;
    }

    /// <summary>
    /// Configures the handler to return the result of a function.
    /// </summary>
    /// <param name="responseFactory">The function that creates the response.</param>
    /// <returns>The builder instance for chaining.</returns>
    public MockHandlerBuilder<TRequest, TResponse> Returns(Func<TRequest, TResponse> responseFactory)
    {
        _behaviors.Add(new MockHandlerBehavior<TRequest, TResponse>
        {
            ResponseFactory = responseFactory,
            BehaviorType = MockBehaviorType.ReturnFactory
        });
        return this;
    }

    /// <summary>
    /// Configures the handler to return the result of an asynchronous function.
    /// </summary>
    /// <param name="responseFactory">The asynchronous function that creates the response.</param>
    /// <returns>The builder instance for chaining.</returns>
    public MockHandlerBuilder<TRequest, TResponse> Returns(Func<TRequest, CancellationToken, Task<TResponse>> responseFactory)
    {
        _behaviors.Add(new MockHandlerBehavior<TRequest, TResponse>
        {
            AsyncResponseFactory = responseFactory,
            BehaviorType = MockBehaviorType.ReturnAsyncFactory
        });
        return this;
    }

    /// <summary>
    /// Configures the handler to throw a specific exception.
    /// </summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="exception">The exception to throw.</param>
    /// <returns>The builder instance for chaining.</returns>
    public MockHandlerBuilder<TRequest, TResponse> Throws<TException>(TException exception)
        where TException : Exception
    {
        _behaviors.Add(new MockHandlerBehavior<TRequest, TResponse>
        {
            Exception = exception,
            BehaviorType = MockBehaviorType.Throw
        });
        return this;
    }

    /// <summary>
    /// Configures the handler to throw an exception created by a function.
    /// </summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="exceptionFactory">The function that creates the exception.</param>
    /// <returns>The builder instance for chaining.</returns>
    public MockHandlerBuilder<TRequest, TResponse> Throws<TException>(Func<TRequest, TException> exceptionFactory)
        where TException : Exception
    {
        _behaviors.Add(new MockHandlerBehavior<TRequest, TResponse>
        {
            ExceptionFactory = r => exceptionFactory((TRequest)r),
            BehaviorType = MockBehaviorType.ThrowFactory
        });
        return this;
    }

    /// <summary>
    /// Configures the handler to delay the response by a specified time.
    /// </summary>
    /// <param name="delay">The delay duration.</param>
    /// <returns>The builder instance for chaining.</returns>
    public MockHandlerBuilder<TRequest, TResponse> Delays(TimeSpan delay)
    {
        if (_behaviors.Count == 0)
        {
            throw new InvalidOperationException("Must configure a return or throw behavior before adding delay.");
        }

        _behaviors.Last().Delay = delay;
        return this;
    }

    /// <summary>
    /// Configures the handler to return responses in sequence for multiple calls.
    /// </summary>
    /// <param name="responses">The sequence of responses to return.</param>
    /// <returns>The builder instance for chaining.</returns>
    public MockHandlerBuilder<TRequest, TResponse> ReturnsInSequence(params TResponse[] responses)
    {
        foreach (var response in responses)
        {
            Returns(response);
        }
        return this;
    }

    /// <summary>
    /// Configures the handler to throw exceptions in sequence for multiple calls.
    /// </summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    /// <param name="exceptions">The sequence of exceptions to throw.</param>
    /// <returns>The builder instance for chaining.</returns>
    public MockHandlerBuilder<TRequest, TResponse> ThrowsInSequence<TException>(params TException[] exceptions)
        where TException : Exception
    {
        foreach (var exception in exceptions)
        {
            Throws(exception);
        }
        return this;
    }

    /// <summary>
    /// Gets the handler verifier for asserting call patterns.
    /// </summary>
    public HandlerVerifier<TRequest, TResponse> Verifier => _verifier;

    /// <summary>
    /// Builds the handler function that can be registered with TestRelay.
    /// </summary>
    /// <returns>The handler function.</returns>
    public Func<TRequest, CancellationToken, ValueTask<TResponse>> Build()
    {
        return (request, cancellationToken) =>
        {
            _verifier.RecordCall(request);

            if (_behaviors.Count == 0)
            {
                throw new InvalidOperationException("No behavior configured for mock handler.");
            }

            var behaviorIndex = (_verifier.CallCount - 1) % _behaviors.Count;
            var behavior = _behaviors[behaviorIndex];

            // Apply delay if configured
            if (behavior.Delay.HasValue)
            {
                return DelayAndExecuteAsync(behavior, request, cancellationToken);
            }

            // Execute behavior synchronously
            return ExecuteBehavior(behavior, request);
        };
    }

    private async ValueTask<TResponse> DelayAndExecuteAsync(MockHandlerBehavior<TRequest, TResponse> behavior, TRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(behavior.Delay!.Value, cancellationToken);
        return await ExecuteBehaviorAsync(behavior, request, cancellationToken);
    }

    private ValueTask<TResponse> ExecuteBehavior(MockHandlerBehavior<TRequest, TResponse> behavior, TRequest request)
    {
        switch (behavior.BehaviorType)
        {
            case MockBehaviorType.Return:
                return ValueTask.FromResult(behavior.Response!);

            case MockBehaviorType.ReturnFactory:
                return ValueTask.FromResult(behavior.ResponseFactory!(request));

            case MockBehaviorType.ReturnAsyncFactory:
                throw new InvalidOperationException("Async factory cannot be executed synchronously. Use Delays() to enable async execution.");

            case MockBehaviorType.Throw:
                throw behavior.Exception!;

            case MockBehaviorType.ThrowFactory:
                throw behavior.ExceptionFactory!(request);

            default:
                throw new InvalidOperationException($"Unknown behavior type: {behavior.BehaviorType}");
        }
    }

    private async ValueTask<TResponse> ExecuteBehaviorAsync(MockHandlerBehavior<TRequest, TResponse> behavior, TRequest request, CancellationToken cancellationToken)
    {
        switch (behavior.BehaviorType)
        {
            case MockBehaviorType.Return:
                return behavior.Response!;

            case MockBehaviorType.ReturnFactory:
                return behavior.ResponseFactory!(request);

            case MockBehaviorType.ReturnAsyncFactory:
                return await behavior.AsyncResponseFactory!(request, cancellationToken);

            case MockBehaviorType.Throw:
                throw behavior.Exception!;

            case MockBehaviorType.ThrowFactory:
                throw behavior.ExceptionFactory!(request);

            default:
                throw new InvalidOperationException($"Unknown behavior type: {behavior.BehaviorType}");
        }
    }
}

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

/// <summary>
/// Defines the types of mock behaviors.
/// </summary>
internal enum MockBehaviorType
{
    Return,
    ReturnFactory,
    ReturnAsyncFactory,
    Throw,
    ThrowFactory
}
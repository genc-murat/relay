using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using Relay.Core.Telemetry;

namespace Relay.Core.Tests.Testing;

/// <summary>
/// Fluent API builder for creating test Relay instances with type-safe handler registration
/// </summary>
public class RelayTestBuilder
{
    private readonly ServiceCollection _services;
    private readonly List<object> _handlerInstances;
    private readonly List<Type> _handlerTypes;
    private readonly List<Type> _pipelineTypes;
    private readonly Dictionary<Type, object> _mockResponses;
    private readonly Dictionary<Type, Exception> _mockExceptions;
    private bool _enableTelemetry = false;
    private bool _enableTracing = false;

    private RelayTestBuilder()
    {
        _services = new ServiceCollection();
        _handlerInstances = new List<object>();
        _handlerTypes = new List<Type>();
        _pipelineTypes = new List<Type>();
        _mockResponses = new Dictionary<Type, object>();
        _mockExceptions = new Dictionary<Type, Exception>();

        // Add default null telemetry provider
        _services.AddSingleton<ITelemetryProvider, NullTelemetryProvider>();
    }

    /// <summary>
    /// Creates a new RelayTestBuilder instance
    /// </summary>
    public static RelayTestBuilder Create() => new();

    /// <summary>
    /// Adds a handler type to be resolved from DI
    /// </summary>
    /// <typeparam name="T">The handler type</typeparam>
    public RelayTestBuilder WithHandler<T>() where T : class
    {
        _handlerTypes.Add(typeof(T));
        _services.AddTransient<T>();
        RegisterHandlerInterfaces(typeof(T));
        return this;
    }

    /// <summary>
    /// Adds a handler instance to the test setup
    /// </summary>
    /// <typeparam name="T">The handler type</typeparam>
    /// <param name="handler">The handler instance</param>
    public RelayTestBuilder WithHandler<T>(T handler) where T : class
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        _handlerInstances.Add(handler);
        var handlerType = handler.GetType();
        _services.AddSingleton(handlerType, handler);
        RegisterHandlerInterfaces(handlerType, handler);
        return this;
    }

    /// <summary>
    /// Adds a mock handler that returns the specified response for the given request type
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="response">The response to return</param>
    public RelayTestBuilder WithMockHandler<TRequest, TResponse>(TResponse response)
        where TRequest : IRequest<TResponse>
    {
        var mockHandler = new MockRequestHandler<TRequest, TResponse>(response);
        _handlerInstances.Add(mockHandler);
        _services.AddSingleton<IRequestHandler<TRequest, TResponse>>(mockHandler);
        _mockResponses[typeof(TRequest)] = response!;
        return this;
    }

    /// <summary>
    /// Adds a mock handler that throws the specified exception for the given request type
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="exception">The exception to throw</param>
    public RelayTestBuilder WithMockHandler<TRequest, TResponse>(Exception exception)
        where TRequest : IRequest<TResponse>
    {
        var mockHandler = new MockRequestHandler<TRequest, TResponse>(exception);
        _handlerInstances.Add(mockHandler);
        _services.AddSingleton<IRequestHandler<TRequest, TResponse>>(mockHandler);
        _mockExceptions[typeof(TRequest)] = exception;
        return this;
    }

    /// <summary>
    /// Adds a mock handler for requests without responses
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    public RelayTestBuilder WithMockHandler<TRequest>() where TRequest : IRequest
    {
        var mockHandler = new MockVoidRequestHandler<TRequest>();
        _handlerInstances.Add(mockHandler);
        _services.AddSingleton<IRequestHandler<TRequest>>(mockHandler);
        return this;
    }

    /// <summary>
    /// Adds a mock handler for requests without responses that throws an exception
    /// </summary>
    /// <typeparam name="TRequest">The request type</typeparam>
    /// <param name="exception">The exception to throw</param>
    public RelayTestBuilder WithMockHandler<TRequest>(Exception exception) where TRequest : IRequest
    {
        var mockHandler = new MockVoidRequestHandler<TRequest>(exception);
        _handlerInstances.Add(mockHandler);
        _services.AddSingleton<IRequestHandler<TRequest>>(mockHandler);
        _mockExceptions[typeof(TRequest)] = exception;
        return this;
    }

    /// <summary>
    /// Adds a mock notification handler
    /// </summary>
    /// <typeparam name="TNotification">The notification type</typeparam>
    public RelayTestBuilder WithMockNotificationHandler<TNotification>() where TNotification : INotification
    {
        var mockHandler = new MockNotificationHandler<TNotification>();
        _handlerInstances.Add(mockHandler);
        _services.AddSingleton<INotificationHandler<TNotification>>(mockHandler);
        return this;
    }

    /// <summary>
    /// Adds a mock notification handler that throws an exception
    /// </summary>
    /// <typeparam name="TNotification">The notification type</typeparam>
    /// <param name="exception">The exception to throw</param>
    public RelayTestBuilder WithMockNotificationHandler<TNotification>(Exception exception) where TNotification : INotification
    {
        var mockHandler = new MockNotificationHandler<TNotification>(exception);
        _handlerInstances.Add(mockHandler);
        _services.AddSingleton<INotificationHandler<TNotification>>(mockHandler);
        return this;
    }

    /// <summary>
    /// Adds a mock stream handler that yields the specified items
    /// </summary>
    /// <typeparam name="TRequest">The stream request type</typeparam>
    /// <typeparam name="TResponse">The response item type</typeparam>
    /// <param name="items">The items to yield</param>
    public RelayTestBuilder WithMockStreamHandler<TRequest, TResponse>(IEnumerable<TResponse> items)
        where TRequest : IStreamRequest<TResponse>
    {
        var mockHandler = new MockStreamHandler<TRequest, TResponse>(items);
        _handlerInstances.Add(mockHandler);
        _services.AddSingleton<IStreamHandler<TRequest, TResponse>>(mockHandler);
        return this;
    }

    /// <summary>
    /// Adds a pipeline behavior type
    /// </summary>
    /// <typeparam name="T">The pipeline behavior type</typeparam>
    public RelayTestBuilder WithPipeline<T>() where T : class
    {
        _pipelineTypes.Add(typeof(T));
        _services.AddTransient<T>();
        return this;
    }

    /// <summary>
    /// Adds a pipeline behavior instance
    /// </summary>
    /// <typeparam name="T">The pipeline behavior type</typeparam>
    /// <param name="pipeline">The pipeline instance</param>
    public RelayTestBuilder WithPipeline<T>(T pipeline) where T : class
    {
        if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));

        _pipelineTypes.Add(typeof(T));
        _services.AddSingleton(pipeline);
        return this;
    }

    /// <summary>
    /// Adds a service to the DI container
    /// </summary>
    /// <typeparam name="TInterface">The service interface</typeparam>
    /// <typeparam name="TImplementation">The service implementation</typeparam>
    public RelayTestBuilder WithService<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        _services.AddTransient<TInterface, TImplementation>();
        return this;
    }

    /// <summary>
    /// Adds a singleton service to the DI container
    /// </summary>
    /// <typeparam name="TInterface">The service interface</typeparam>
    /// <typeparam name="TImplementation">The service implementation</typeparam>
    public RelayTestBuilder WithSingleton<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        _services.AddSingleton<TInterface, TImplementation>();
        return this;
    }

    /// <summary>
    /// Adds a singleton service instance to the DI container
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <param name="instance">The service instance</param>
    public RelayTestBuilder WithSingleton<T>(T instance) where T : class
    {
        _services.AddSingleton(instance);
        return this;
    }

    /// <summary>
    /// Enables telemetry for the test relay
    /// </summary>
    public RelayTestBuilder WithTelemetry()
    {
        _enableTelemetry = true;
        _services.AddSingleton<ITelemetryProvider, TestTelemetryProvider>();
        return this;
    }

    /// <summary>
    /// Enables request tracing for the test relay
    /// </summary>
    public RelayTestBuilder WithTracing()
    {
        _enableTracing = true;
        return this;
    }

    /// <summary>
    /// Enables diagnostics for the test relay
    /// </summary>
    public RelayTestBuilder WithDiagnostics()
    {
        _services.AddSingleton<IRelayDiagnostics, DefaultRelayDiagnostics>();
        return this;
    }

    /// <summary>
    /// Uses a custom telemetry provider
    /// </summary>
    /// <typeparam name="T">The telemetry provider type</typeparam>
    public RelayTestBuilder WithTelemetry<T>() where T : class, ITelemetryProvider
    {
        _enableTelemetry = true;
        _services.AddSingleton<ITelemetryProvider, T>();
        return this;
    }

    /// <summary>
    /// Uses a custom telemetry provider instance
    /// </summary>
    /// <param name="telemetryProvider">The telemetry provider instance</param>
    public RelayTestBuilder WithTelemetry(ITelemetryProvider telemetryProvider)
    {
        _enableTelemetry = true;
        _services.AddSingleton(telemetryProvider);
        return this;
    }

    /// <summary>
    /// Builds the configured IRelay instance
    /// </summary>
    public IRelay Build()
    {
        // Add core Relay services
        if (_enableTelemetry)
        {
            _services.AddSingleton<IRelay, TelemetryRelay>();
            _services.AddSingleton<RelayImplementation>();
        }
        else
        {
            _services.AddSingleton<IRelay, RelayImplementation>();
        }

        // Add test dispatchers
        _services.AddSingleton<IRequestDispatcher, TestRequestDispatcher>();
        _services.AddSingleton<IStreamDispatcher, TestStreamDispatcher>();
        _services.AddSingleton<INotificationDispatcher, TestNotificationDispatcher>();

        // Add tracing if enabled
        if (_enableTracing)
        {
            _services.AddSingleton<IRequestTracer, RequestTracer>();
        }

        var serviceProvider = _services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IRelay>();
    }

    /// <summary>
    /// Builds the configured IRelay instance and returns it along with the service provider
    /// </summary>
    public (IRelay Relay, IServiceProvider ServiceProvider) BuildWithProvider()
    {
        // Add core Relay services
        _services.AddSingleton<IRelay, RelayImplementation>();

        // Add test dispatchers
        _services.AddSingleton<IRequestDispatcher, TestRequestDispatcher>();
        _services.AddSingleton<IStreamDispatcher, TestStreamDispatcher>();
        _services.AddSingleton<INotificationDispatcher, TestNotificationDispatcher>();

        // Add tracing if enabled
        if (_enableTracing)
        {
            _services.AddSingleton<IRequestTracer, RequestTracer>();
        }

        var serviceProvider = _services.BuildServiceProvider();
        var relay = serviceProvider.GetRequiredService<IRelay>();
        return (relay, serviceProvider);
    }

    private void RegisterHandlerInterfaces(Type handlerType, object? instance = null)
    {
        var interfaces = handlerType.GetInterfaces();

        foreach (var iface in interfaces)
        {
            if (IsHandlerInterface(iface))
            {
                if (instance != null)
                {
                    _services.AddSingleton(iface, instance);
                }
                else
                {
                    _services.AddTransient(iface, handlerType);
                }
            }
        }
    }

    private static bool IsHandlerInterface(Type type)
    {
        if (!type.IsGenericType) return false;

        var genericType = type.GetGenericTypeDefinition();
        return genericType == typeof(IRequestHandler<,>) ||
               genericType == typeof(IRequestHandler<>) ||
               genericType == typeof(IStreamHandler<,>) ||
               genericType == typeof(INotificationHandler<>) ||
               genericType == typeof(IPipelineBehavior<,>) ||
               genericType == typeof(IStreamPipelineBehavior<,>);
    }
}

/// <summary>
/// Mock request handler for testing
/// </summary>
internal class MockRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly TResponse? _response;
    private readonly Exception? _exception;

    public bool WasCalled { get; private set; }
    public TRequest? LastRequest { get; private set; }
    public int CallCount { get; private set; }

    public MockRequestHandler(TResponse response)
    {
        _response = response;
    }

    public MockRequestHandler(Exception exception)
    {
        _exception = exception;
    }

    public ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastRequest = request;
        CallCount++;

        if (_exception != null)
        {
            throw _exception;
        }

        return ValueTask.FromResult(_response!);
    }
}

/// <summary>
/// Mock void request handler for testing
/// </summary>
internal class MockVoidRequestHandler<TRequest> : IRequestHandler<TRequest>
    where TRequest : IRequest
{
    private readonly Exception? _exception;

    public bool WasCalled { get; private set; }
    public TRequest? LastRequest { get; private set; }
    public int CallCount { get; private set; }

    public MockVoidRequestHandler(Exception? exception = null)
    {
        _exception = exception;
    }

    public ValueTask HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastRequest = request;
        CallCount++;

        if (_exception != null)
        {
            throw _exception;
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Mock notification handler for testing
/// </summary>
internal class MockNotificationHandler<TNotification> : INotificationHandler<TNotification>
    where TNotification : INotification
{
    private readonly Exception? _exception;

    public bool WasCalled { get; private set; }
    public TNotification? LastNotification { get; private set; }
    public int CallCount { get; private set; }

    public MockNotificationHandler(Exception? exception = null)
    {
        _exception = exception;
    }

    public ValueTask HandleAsync(TNotification notification, CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastNotification = notification;
        CallCount++;

        if (_exception != null)
        {
            throw _exception;
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Mock stream handler for testing
/// </summary>
internal class MockStreamHandler<TRequest, TResponse> : IStreamHandler<TRequest, TResponse>
    where TRequest : IStreamRequest<TResponse>
{
    private readonly IEnumerable<TResponse> _items;
    private readonly Exception? _exception;

    public bool WasCalled { get; private set; }
    public TRequest? LastRequest { get; private set; }
    public int CallCount { get; private set; }

    public MockStreamHandler(IEnumerable<TResponse> items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    public MockStreamHandler(Exception exception)
    {
        _items = Array.Empty<TResponse>();
        _exception = exception;
    }

    public async IAsyncEnumerable<TResponse> HandleAsync(TRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastRequest = request;
        CallCount++;

        if (_exception != null)
        {
            throw _exception;
        }

        foreach (var item in _items)
        {
            yield return item;
            await Task.Yield(); // Allow for cancellation
        }
    }
}
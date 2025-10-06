using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;

namespace Relay.Core.Tests.Testing;

/// <summary>
/// Test request dispatcher that uses DI to resolve handlers and supports pipelines
/// </summary>
public class TestRequestDispatcher : IRequestDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PipelineExecutor _pipelineExecutor;

    public TestRequestDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _pipelineExecutor = new PipelineExecutor(serviceProvider);
    }

    public async ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        var concreteType = request.GetType();
        var method = typeof(TestRequestDispatcher)
            .GetMethod(nameof(DispatchWithPipelinesAsync), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(concreteType, typeof(TResponse));
        var result = method.Invoke(this, new object[] { request, cancellationToken });
        return await (ValueTask<TResponse>)result!;
    }

    private async ValueTask<TResponse> DispatchWithPipelinesAsync<TConcreteRequest, TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
        where TConcreteRequest : IRequest<TResponse>
    {
        return await _pipelineExecutor.ExecuteAsync<TConcreteRequest, TResponse>(
            (TConcreteRequest)request,
            async (req, ct) =>
            {
                var handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TConcreteRequest), typeof(TResponse));
                var handler = _serviceProvider.GetService(handlerType);

                if (handler == null)
                {
                    throw new HandlerNotFoundException(typeof(TConcreteRequest).Name);
                }

                var method = handlerType.GetMethod("HandleAsync");
                if (method == null)
                {
                    throw new InvalidOperationException($"HandleAsync method not found on {handlerType.Name}");
                }

                object? result;
                try
                {
                    result = method.Invoke(handler, new object[] { req!, ct });

                    if (result is ValueTask<TResponse> valueTask)
                    {
                        return await valueTask;
                    }

                    if (result is Task<TResponse> task)
                    {
                        return await task;
                    }

                    if (result is TResponse syncResult)
                    {
                        return syncResult;
                    }

                    throw new InvalidOperationException($"Unexpected return type from handler: {result?.GetType()}");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException ?? ex;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Handler execution failed for {typeof(TConcreteRequest).Name}", ex);
                }
            },
            cancellationToken);
    }


    public async ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(object));
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            throw new HandlerNotFoundException(request.GetType().Name);
        }

        var method = handlerType.GetMethod("HandleAsync");
        if (method == null)
        {
            throw new InvalidOperationException($"HandleAsync method not found on {handlerType.Name}");
        }

        var result = method.Invoke(handler, new object[] { request, cancellationToken });

        if (result is ValueTask valueTask)
        {
            await valueTask;
        }
        else if (result is Task task)
        {
            await task;
        }
    }

    public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
    {
        // For testing, named handlers are not implemented - just delegate to regular dispatch
        return DispatchAsync(request, cancellationToken);
    }

    public ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
    {
        // For testing, named handlers are not implemented - just delegate to regular dispatch
        return DispatchAsync(request, cancellationToken);
    }
}

/// <summary>
/// Test stream dispatcher that uses DI to resolve handlers
/// </summary>
public class TestStreamDispatcher : IStreamDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly PipelineExecutor _pipelineExecutor;

    public TestStreamDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _pipelineExecutor = new PipelineExecutor(serviceProvider);
    }

    public IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        var concreteType = request.GetType();
        var method = typeof(TestStreamDispatcher)
            .GetMethod(nameof(DispatchStreamWithPipelines), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(concreteType, typeof(TResponse));
        try
        {
            return (IAsyncEnumerable<TResponse>)method.Invoke(this, new object[] { request, cancellationToken })!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            throw ex.InnerException;
        }
    }

    private IAsyncEnumerable<TResponse> DispatchStreamWithPipelines<TConcreteRequest, TResponse>(
        IStreamRequest<TResponse> request,
        CancellationToken cancellationToken)
        where TConcreteRequest : IStreamRequest<TResponse>
    {
        return _pipelineExecutor.ExecuteStreamAsync<TConcreteRequest, TResponse>(
            (TConcreteRequest)request,
            (req, ct) =>
            {
                var handlerType = typeof(IStreamHandler<,>).MakeGenericType(typeof(TConcreteRequest), typeof(TResponse));
                var handler = _serviceProvider.GetService(handlerType);

                if (handler == null)
                {
                    throw new HandlerNotFoundException(typeof(TConcreteRequest).Name);
                }

                var method = handlerType.GetMethod("HandleAsync");
                if (method == null)
                {
                    throw new InvalidOperationException($"HandleAsync method not found on {handlerType.Name}");
                }

                try
                {
                    var result = method.Invoke(handler, new object[] { req!, ct });

                    if (result is IAsyncEnumerable<TResponse> asyncEnumerable)
                    {
                        return asyncEnumerable;
                    }

                    throw new InvalidOperationException($"Unexpected return type from handler: {result?.GetType()}");
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            },
            cancellationToken);
    }

    public IAsyncEnumerable<TResponse> DispatchAsync<TResponse>(
        IStreamRequest<TResponse> request,
        string handlerName,
        CancellationToken cancellationToken)
    {
        // For testing, named handlers are not implemented - just delegate to regular dispatch
        return DispatchAsync(request, cancellationToken);
    }
}

/// <summary>
/// Test notification dispatcher that uses DI to resolve handlers
/// </summary>
public class TestNotificationDispatcher : INotificationDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public TestNotificationDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async ValueTask DispatchAsync<TNotification>(TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(typeof(TNotification));
        var handlers = _serviceProvider.GetServices(handlerType);

        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            var method = handlerType.GetMethod("HandleAsync");
            if (method == null) continue;

            try
            {
                var result = method.Invoke(handler, new object[] { notification, cancellationToken });

                if (result is ValueTask valueTask)
                {
                    tasks.Add(valueTask.AsTask());
                }
                else if (result is Task task)
                {
                    tasks.Add(task);
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }
}

/// <summary>
/// Null telemetry provider for testing scenarios where telemetry is not needed
/// </summary>
public class NullTelemetryProvider : ITelemetryProvider
{
    public IMetricsProvider? MetricsProvider => null;

    public System.Diagnostics.Activity? StartActivity(string operationName, Type requestType, string? correlationId = null)
    {
        return null;
    }

    public void RecordHandlerExecution(Type requestType, Type? responseType, string? handlerName, TimeSpan duration, bool success, Exception? exception = null)
    {
        // No-op
    }

    public void RecordNotificationPublish(Type notificationType, int handlerCount, TimeSpan duration, bool success, Exception? exception = null)
    {
        // No-op
    }

    public void RecordStreamingOperation(Type requestType, Type responseType, string? handlerName, TimeSpan duration, long itemCount, bool success, Exception? exception = null)
    {
        // No-op
    }

    public void SetCorrelationId(string correlationId)
    {
        // No-op
    }

    public string? GetCorrelationId()
    {
        return null;
    }
}
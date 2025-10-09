using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Telemetry;

/// <summary>
/// Base class for telemetry decorators that provides common telemetry functionality.
/// Eliminates code duplication across all telemetry dispatcher implementations.
/// </summary>
public abstract class TelemetryDispatcherBase
{
    /// <summary>
    /// Gets the telemetry provider for recording metrics and activities.
    /// </summary>
    protected ITelemetryProvider TelemetryProvider { get; }

    /// <summary>
    /// Initializes a new instance of the TelemetryDispatcherBase class.
    /// </summary>
    /// <param name="telemetryProvider">The telemetry provider.</param>
    /// <exception cref="ArgumentNullException">Thrown when telemetryProvider is null.</exception>
    protected TelemetryDispatcherBase(ITelemetryProvider telemetryProvider)
    {
        TelemetryProvider = telemetryProvider ?? throw new ArgumentNullException(nameof(telemetryProvider));
    }

    /// <summary>
    /// Validates that the inner dispatcher is not null.
    /// </summary>
    /// <typeparam name="T">The type of the inner dispatcher.</typeparam>
    /// <param name="inner">The inner dispatcher to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when inner is null.</exception>
    protected static void ValidateInnerDispatcher<T>(T inner) where T : class
    {
        if (inner == null)
            throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// Executes an operation with telemetry tracking for handler execution.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The request being processed.</param>
    /// <param name="operationName">The name of the operation for telemetry.</param>
    /// <param name="handlerName">Optional handler name for telemetry.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    protected async ValueTask<TResponse> ExecuteWithTelemetryAsync<TRequest, TResponse>(
        TRequest request,
        string operationName,
        string? handlerName,
        Func<TRequest, CancellationToken, ValueTask<TResponse>> operation,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var correlationId = TelemetryProvider.GetCorrelationId();

        using var activity = TelemetryProvider.StartActivity(operationName, requestType, correlationId);
        if (handlerName != null)
        {
            activity?.SetTag("relay.handler_name", handlerName);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation(request, cancellationToken);
            stopwatch.Stop();

            TelemetryProvider.RecordHandlerExecution(requestType, responseType, handlerName, stopwatch.Elapsed, true);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TelemetryProvider.RecordHandlerExecution(requestType, responseType, handlerName, stopwatch.Elapsed, false, ex);
            throw;
        }
    }

    /// <summary>
    /// Executes an operation with telemetry tracking for void handler execution.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The request being processed.</param>
    /// <param name="operationName">The name of the operation for telemetry.</param>
    /// <param name="handlerName">Optional handler name for telemetry.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    protected async ValueTask ExecuteWithTelemetryAsync<TRequest>(
        TRequest request,
        string operationName,
        string? handlerName,
        Func<TRequest, CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        var requestType = request.GetType();
        var correlationId = TelemetryProvider.GetCorrelationId();

        using var activity = TelemetryProvider.StartActivity(operationName, requestType, correlationId);
        if (handlerName != null)
        {
            activity?.SetTag("relay.handler_name", handlerName);
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await operation(request, cancellationToken);
            stopwatch.Stop();

            TelemetryProvider.RecordHandlerExecution(requestType, null, handlerName, stopwatch.Elapsed, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TelemetryProvider.RecordHandlerExecution(requestType, null, handlerName, stopwatch.Elapsed, false, ex);
            throw;
        }
    }

    /// <summary>
    /// Executes an operation with telemetry tracking for notification publishing.
    /// </summary>
    /// <typeparam name="TNotification">The notification type.</typeparam>
    /// <param name="notification">The notification being published.</param>
    /// <param name="operationName">The name of the operation for telemetry.</param>
    /// <param name="handlerCount">The number of handlers that processed the notification.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A ValueTask representing the asynchronous operation.</returns>
    protected async ValueTask ExecuteNotificationWithTelemetryAsync<TNotification>(
        TNotification notification,
        string operationName,
        int handlerCount,
        Func<TNotification, CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken)
        where TNotification : class
    {
        var notificationType = notification.GetType();
        var correlationId = TelemetryProvider.GetCorrelationId();

        using var activity = TelemetryProvider.StartActivity(operationName, notificationType, correlationId);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await operation(notification, cancellationToken);
            stopwatch.Stop();

            TelemetryProvider.RecordNotificationPublish(notificationType, handlerCount, stopwatch.Elapsed, true);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TelemetryProvider.RecordNotificationPublish(notificationType, handlerCount, stopwatch.Elapsed, false, ex);
            throw;
        }
    }


}
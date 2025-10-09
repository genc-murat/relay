using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Telemetry;

/// <summary>
/// Decorator for IRequestDispatcher that adds telemetry capabilities
/// </summary>
public class TelemetryRequestDispatcher : TelemetryDispatcherBase, IRequestDispatcher
{
    private readonly IRequestDispatcher _inner;

    public TelemetryRequestDispatcher(IRequestDispatcher inner, ITelemetryProvider telemetryProvider)
        : base(telemetryProvider)
    {
        ValidateInnerDispatcher(inner);
        _inner = inner;
    }

    public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        return ExecuteWithTelemetryAsync<IRequest<TResponse>, TResponse>(
            request,
            "Relay.Request",
            null,
            (req, ct) => _inner.DispatchAsync(req, ct),
            cancellationToken);
    }

    public ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)
    {
        return ExecuteWithTelemetryAsync(
            request,
            "Relay.Request",
            null,
            (req, ct) => _inner.DispatchAsync(req, ct),
            cancellationToken);
    }

    public ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)
    {
        return ExecuteWithTelemetryAsync<IRequest<TResponse>, TResponse>(
            request,
            "Relay.NamedRequest",
            handlerName,
            (req, ct) => _inner.DispatchAsync(req, handlerName, ct),
            cancellationToken);
    }

    public ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)
    {
        return ExecuteWithTelemetryAsync(
            request,
            "Relay.NamedRequest",
            handlerName,
            (req, ct) => _inner.DispatchAsync(req, handlerName, ct),
            cancellationToken);
    }
}
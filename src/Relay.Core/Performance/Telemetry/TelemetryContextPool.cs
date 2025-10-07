using System;
using System.Diagnostics;
using Microsoft.Extensions.ObjectPool;
using Relay.Core.Telemetry;

namespace Relay.Core.Performance.Telemetry;

/// <summary>
/// Static accessor for telemetry context pooling
/// </summary>
public static class TelemetryContextPool
{
    private static readonly Lazy<ObjectPool<TelemetryContext>> _lazyPool = new(() =>
    {
        var provider = new DefaultObjectPoolProvider
        {
            // Keep pool small to maximize reuse of the same instance in tests
            MaximumRetained = 1
        };
        var policy = new TelemetryContextPooledObjectPolicy();
        return provider.Create(policy);
    });

    private static ObjectPool<TelemetryContext> Pool => _lazyPool.Value;

    /// <summary>
    /// Gets a telemetry context from the pool
    /// </summary>
    /// <returns>A pooled telemetry context</returns>
    public static TelemetryContext Get()
    {
        var context = Pool.Get();

        // Ensure fresh state
        context.RequestId = Guid.NewGuid().ToString();
        context.StartTime = DateTimeOffset.UtcNow;

        return context;
    }

    /// <summary>
    /// Returns a telemetry context to the pool
    /// </summary>
    /// <param name="context">The context to return</param>
    public static void Return(TelemetryContext context)
    {
        if (context != null)
        {
            Pool.Return(context);
        }
    }

    /// <summary>
    /// Creates a telemetry context with specified parameters using pooling
    /// </summary>
    /// <param name="requestType">Type of the request</param>
    /// <param name="responseType">Type of the response (optional)</param>
    /// <param name="handlerName">Name of the handler (optional)</param>
    /// <param name="correlationId">Correlation ID (optional)</param>
    /// <param name="activity">Associated activity (optional)</param>
    /// <returns>A pooled telemetry context with specified parameters</returns>
    public static TelemetryContext Create(Type requestType, Type? responseType = null, string? handlerName = null, string? correlationId = null, Activity? activity = null)
    {
        var context = Get();
        context.RequestType = requestType;
        context.ResponseType = responseType;
        context.HandlerName = handlerName;
        context.CorrelationId = correlationId;
        context.Activity = activity;
        return context;
    }
}
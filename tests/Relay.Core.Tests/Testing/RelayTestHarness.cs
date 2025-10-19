using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Diagnostics.Tracing;
using Relay.Core.Telemetry;

namespace Relay.Core.Tests.Testing;

/// <summary>
/// Test harness for easily setting up Relay instances for testing
/// </summary>
public class RelayTestHarness : RelayTestHarnessCore
{
    /// <summary>
    /// Creates a mockable relay for testing
    /// </summary>
    public static Mock<IRelay> CreateMockRelay()
    {
        return MockRelayUtilities.CreateMockRelay();
    }

    /// <summary>
    /// Adds a handler instance to the test harness
    /// </summary>
    public new RelayTestHarness AddHandler(object handler)
    {
        base.AddHandler(handler);
        return this;
    }

    /// <summary>
    /// Adds a handler type to be resolved from DI
    /// </summary>
    public new RelayTestHarness AddHandler<T>() where T : class
    {
        base.AddHandler<T>();
        return this;
    }

    /// <summary>
    /// Adds a pipeline behavior type
    /// </summary>
    public new RelayTestHarness AddPipeline<T>() where T : class
    {
        base.AddPipeline<T>();
        return this;
    }

    /// <summary>
    /// Adds a service to the DI container
    /// </summary>
    public new RelayTestHarness AddService<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        base.AddService<TInterface, TImplementation>();
        return this;
    }

    /// <summary>
    /// Adds a singleton service to the DI container
    /// </summary>
    public new RelayTestHarness AddSingleton<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        base.AddSingleton<TInterface, TImplementation>();
        return this;
    }

    /// <summary>
    /// Adds a singleton service instance to the DI container
    /// </summary>
    public new RelayTestHarness AddSingleton<T>(T instance) where T : class
    {
        base.AddSingleton(instance);
        return this;
    }

    /// <summary>
    /// Configures the test telemetry provider
    /// </summary>
    public new RelayTestHarness WithTelemetry<T>() where T : class, ITelemetryProvider
    {
        base.WithTelemetry<T>();
        return this;
    }

    /// <summary>
    /// Disables telemetry for testing
    /// </summary>
    public new RelayTestHarness WithoutTelemetry()
    {
        base.WithoutTelemetry();
        return this;
    }

    /// <summary>
    /// Builds the relay instance
    /// </summary>
    public new IRelay Build()
    {
        return base.Build();
    }

    /// <summary>
    /// Gets the service provider for advanced scenarios
    /// </summary>
    public new IServiceProvider GetServiceProvider()
    {
        return base.GetServiceProvider();
    }

    /// <summary>
    /// Gets a service from the DI container
    /// </summary>
    public new T GetService<T>() where T : notnull
    {
        return base.GetService<T>();
    }

    /// <summary>
    /// Gets the test telemetry provider if configured
    /// </summary>
    public new TestTelemetryProvider? GetTestTelemetryProvider()
    {
        return base.GetTestTelemetryProvider();
    }

    /// <summary>
    /// Enables request tracing for the test harness
    /// </summary>
    public new RelayTestHarness EnableTracing()
    {
        base.EnableTracing();
        return this;
    }

    /// <summary>
    /// Enables performance metrics collection for the test harness
    /// </summary>
    public new RelayTestHarness EnablePerformanceMetrics()
    {
        base.EnablePerformanceMetrics();
        return this;
    }

    /// <summary>
    /// Captures the execution trace for a request
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="request">The request to trace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response and execution trace</returns>
    public async Task<(TResponse Response, RequestTrace? Trace)> CaptureTraceAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var relay = Build();
        var tracer = GetService<IRequestTracer>();
        return await TracingUtilities.CaptureTraceAsync(relay, tracer, request, cancellationToken);
    }

    /// <summary>
    /// Captures the execution trace for a void request
    /// </summary>
    /// <param name="request">The request to trace</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The execution trace</returns>
    public async Task<RequestTrace?> CaptureTraceAsync(
        IRequest request,
        CancellationToken cancellationToken = default)
    {
        var relay = Build();
        var tracer = GetService<IRequestTracer>();
        return await TracingUtilities.CaptureTraceAsync(relay, tracer, request, cancellationToken);
    }

    /// <summary>
    /// Measures the performance of a request execution
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="request">The request to measure</param>
    /// <param name="iterations">Number of iterations to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance benchmark results</returns>
    public async Task<BenchmarkResult> BenchmarkAsync<TResponse>(
        IRequest<TResponse> request,
        int iterations = 100,
        CancellationToken cancellationToken = default)
    {
        var relay = Build();
        return await BenchmarkingUtilities.BenchmarkAsync(relay, request, iterations, cancellationToken);
    }

    /// <summary>
    /// Measures the performance of a void request execution
    /// </summary>
    /// <param name="request">The request to measure</param>
    /// <param name="iterations">Number of iterations to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance benchmark results</returns>
    public async Task<BenchmarkResult> BenchmarkAsync(
        IRequest request,
        int iterations = 100,
        CancellationToken cancellationToken = default)
    {
        var relay = Build();
        return await BenchmarkingUtilities.BenchmarkAsync(relay, request, iterations, cancellationToken);
    }

    /// <summary>
    /// Measures memory allocation during request execution
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <param name="request">The request to measure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The response and memory allocation information</returns>
    public async Task<(TResponse Response, long AllocatedBytes)> MeasureAllocationAsync<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        var relay = Build();
        return await MemoryUtilities.MeasureAllocationAsync(relay, request, cancellationToken);
    }
}
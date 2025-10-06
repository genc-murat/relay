using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using Relay.Core.Diagnostics.Configuration;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Diagnostics.Metrics;
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using Relay.Core.Diagnostics.Validation;
using Relay.Core.Telemetry;

namespace Relay.Core.Tests.Testing;

/// <summary>
/// Test harness for easily setting up Relay instances for testing
/// </summary>
public class RelayTestHarness : IDisposable
{
    private readonly ServiceCollection _services;
    private readonly List<object> _handlers;
    private readonly List<Type> _pipelineTypes;
    private IServiceProvider? _serviceProvider;
    private IRelay? _relay;
    private bool _enableTracing = false;
    private bool _enablePerformanceMetrics = false;
    private readonly List<IDisposable> _disposables = new();

    public RelayTestHarness()
    {
        _services = new ServiceCollection();
        _handlers = new List<object>();
        _pipelineTypes = new List<Type>();

        // Add default test telemetry provider
        _services.AddSingleton<ITelemetryProvider, TestTelemetryProvider>();
    }

    /// <summary>
    /// Creates a test relay with the specified handlers
    /// </summary>
    public static IRelay CreateTestRelay(params object[] handlers)
    {
        var harness = new RelayTestHarness();
        foreach (var handler in handlers)
        {
            harness.AddHandler(handler);
        }
        return harness.Build();
    }

    /// <summary>
    /// Creates a mockable relay for testing
    /// </summary>
    public static Mock<IRelay> CreateMockRelay()
    {
        var mock = new Mock<IRelay>();

        // Set up default behaviors for common scenarios
        mock.Setup(r => r.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Set up generic request handling for TestRequest<T>
        mock.Setup(r => r.SendAsync(It.IsAny<TestRequest<string>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(string.Empty));

        // Set up for other specific types
        mock.Setup(r => r.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(string.Empty));

        mock.Setup(r => r.SendAsync(It.IsAny<IRequest<int>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(0));

        mock.Setup(r => r.SendAsync(It.IsAny<IRequest<bool>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(false));

        mock.Setup(r => r.SendAsync(It.IsAny<IRequest<object>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<object>(null!));

        mock.Setup(r => r.PublishAsync(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        return mock;
    }

    /// <summary>
    /// Adds a handler instance to the test harness
    /// </summary>
    public RelayTestHarness AddHandler(object handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        _handlers.Add(handler);

        // Register the handler instance
        var handlerType = handler.GetType();
        _services.AddSingleton(handlerType, handler);

        // Register handler interfaces
        RegisterHandlerInterfaces(handlerType, handler);

        return this;
    }

    /// <summary>
    /// Adds a handler type to be resolved from DI
    /// </summary>
    public RelayTestHarness AddHandler<T>() where T : class
    {
        _services.AddTransient<T>();
        RegisterHandlerInterfaces(typeof(T));
        return this;
    }

    /// <summary>
    /// Adds a pipeline behavior type
    /// </summary>
    public RelayTestHarness AddPipeline<T>() where T : class
    {
        _pipelineTypes.Add(typeof(T));
        if (!_services.Any(sd => sd.ServiceType == typeof(T)))
        {
            _services.AddTransient<T>();
        }

        // Register as pipeline behavior interfaces
        var pipelineInterfaces = typeof(T).GetInterfaces()
            .Where(i => i.IsGenericType &&
                       (i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) ||
                        i.GetGenericTypeDefinition() == typeof(IStreamPipelineBehavior<,>)))
            .ToList();

        foreach (var iface in pipelineInterfaces)
        {
            if (!_services.Any(sd => sd.ServiceType == iface))
            {
                _services.AddTransient(iface, typeof(T));
            }
        }

        return this;
    }

    /// <summary>
    /// Adds a service to the DI container
    /// </summary>
    public RelayTestHarness AddService<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        _services.AddTransient<TInterface, TImplementation>();
        return this;
    }

    /// <summary>
    /// Adds a singleton service to the DI container
    /// </summary>
    public RelayTestHarness AddSingleton<TInterface, TImplementation>()
        where TInterface : class
        where TImplementation : class, TInterface
    {
        _services.AddSingleton<TInterface, TImplementation>();
        return this;
    }

    /// <summary>
    /// Adds a singleton service instance to the DI container
    /// </summary>
    public RelayTestHarness AddSingleton<T>(T instance) where T : class
    {
        _services.AddSingleton(instance);
        // Also register under implemented pipeline interfaces so DI can resolve them generically
        var interfaces = typeof(T).GetInterfaces()
            .Where(i => i.IsGenericType &&
                        (i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) ||
                         i.GetGenericTypeDefinition() == typeof(IStreamPipelineBehavior<,>)))
            .ToList();
        foreach (var iface in interfaces)
        {
            _services.AddSingleton(iface, sp => instance);
        }
        return this;
    }

    /// <summary>
    /// Configures the test telemetry provider
    /// </summary>
    public RelayTestHarness WithTelemetry<T>() where T : class, ITelemetryProvider
    {
        _services.AddSingleton<ITelemetryProvider, T>();
        return this;
    }

    /// <summary>
    /// Disables telemetry for testing
    /// </summary>
    public RelayTestHarness WithoutTelemetry()
    {
        _services.AddSingleton<ITelemetryProvider, NullTelemetryProvider>();
        return this;
    }

    /// <summary>
    /// Builds the relay instance
    /// </summary>
    public IRelay Build()
    {
        if (_relay != null) return _relay;

        // Add diagnostics services if tracing or performance metrics are enabled
        if (_enableTracing || _enablePerformanceMetrics)
        {
            _services.AddSingleton<IRequestTracer, RequestTracer>();
            _services.AddSingleton<IRelayDiagnostics, DefaultRelayDiagnostics>();

            // Configure diagnostics options
            _services.AddSingleton(new DiagnosticsOptions
            {
                EnableRequestTracing = _enableTracing,
                EnablePerformanceMetrics = _enablePerformanceMetrics,
                TraceBufferSize = 1000,
                MetricsRetentionPeriod = TimeSpan.FromMinutes(10)
            });
        }

        // Add core Relay services
        _services.AddSingleton<IRelay, RelayImplementation>();
        // Ensure telemetry is captured at relay level in tests
        _services.Decorate<IRelay, Relay.Core.Telemetry.TelemetryRelay>();

        // Add mock dispatchers for testing
        _services.AddSingleton<IRequestDispatcher, TestRequestDispatcher>();
        _services.AddSingleton<IStreamDispatcher, TestStreamDispatcher>();
        _services.AddSingleton<INotificationDispatcher, TestNotificationDispatcher>();

        _serviceProvider = _services.BuildServiceProvider();
        _relay = _serviceProvider.GetRequiredService<IRelay>();

        return _relay;
    }

    /// <summary>
    /// Gets the service provider for advanced scenarios
    /// </summary>
    public IServiceProvider GetServiceProvider()
    {
        if (_serviceProvider == null)
        {
            Build();
        }
        return _serviceProvider!;
    }

    /// <summary>
    /// Gets a service from the DI container
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        return GetServiceProvider().GetRequiredService<T>();
    }

    /// <summary>
    /// Gets the test telemetry provider if configured
    /// </summary>
    public TestTelemetryProvider? GetTestTelemetryProvider()
    {
        return GetServiceProvider().GetService<ITelemetryProvider>() as TestTelemetryProvider;
    }

    /// <summary>
    /// Enables request tracing for the test harness
    /// </summary>
    public RelayTestHarness EnableTracing()
    {
        _enableTracing = true;
        return this;
    }

    /// <summary>
    /// Enables performance metrics collection for the test harness
    /// </summary>
    public RelayTestHarness EnablePerformanceMetrics()
    {
        _enablePerformanceMetrics = true;
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

        // Start tracing
        var trace = tracer.StartTrace(request);

        try
        {
            var response = await relay.SendAsync(request, cancellationToken);
            tracer.CompleteTrace(true);
            return (response, trace);
        }
        catch (Exception ex)
        {
            tracer.RecordException(ex);
            tracer.CompleteTrace(false);
            throw;
        }
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

        // Start tracing
        var trace = tracer.StartTrace(request);

        try
        {
            await relay.SendAsync(request, cancellationToken);
            tracer.CompleteTrace(true);
            return trace;
        }
        catch (Exception ex)
        {
            tracer.RecordException(ex);
            tracer.CompleteTrace(false);
            throw;
        }
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
        var results = new List<TimeSpan>();
        var startTime = DateTimeOffset.UtcNow;
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < iterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var iterationStart = DateTimeOffset.UtcNow;
            await relay.SendAsync(request, cancellationToken);
            var iterationEnd = DateTimeOffset.UtcNow;

            results.Add(iterationEnd - iterationStart);
        }

        var finalMemory = GC.GetTotalMemory(false);
        var totalTime = results.Aggregate(TimeSpan.Zero, (sum, time) => sum + time);
        var avgTime = TimeSpan.FromTicks(totalTime.Ticks / iterations);
        var minTime = results.Min();
        var maxTime = results.Max();

        // Calculate standard deviation
        var avgTicks = totalTime.Ticks / iterations;
        var variance = results.Select(t => Math.Pow(t.Ticks - avgTicks, 2)).Average();
        var stdDev = TimeSpan.FromTicks((long)Math.Sqrt(variance));

        return new BenchmarkResult
        {
            RequestType = typeof(TResponse).Name,
            HandlerType = "Test",
            Iterations = iterations,
            TotalTime = totalTime,
            MinTime = minTime,
            MaxTime = maxTime,
            StandardDeviation = stdDev,
            TotalAllocatedBytes = Math.Max(0, finalMemory - initialMemory),
            Timestamp = startTime
        };
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
        var results = new List<TimeSpan>();
        var startTime = DateTimeOffset.UtcNow;
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < iterations; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var iterationStart = DateTimeOffset.UtcNow;
            await relay.SendAsync(request, cancellationToken);
            var iterationEnd = DateTimeOffset.UtcNow;

            results.Add(iterationEnd - iterationStart);
        }

        var finalMemory = GC.GetTotalMemory(false);
        var totalTime = results.Aggregate(TimeSpan.Zero, (sum, time) => sum + time);
        var avgTime = TimeSpan.FromTicks(totalTime.Ticks / iterations);
        var minTime = results.Min();
        var maxTime = results.Max();

        // Calculate standard deviation
        var avgTicks = totalTime.Ticks / iterations;
        var variance = results.Select(t => Math.Pow(t.Ticks - avgTicks, 2)).Average();
        var stdDev = TimeSpan.FromTicks((long)Math.Sqrt(variance));

        return new BenchmarkResult
        {
            RequestType = request.GetType().Name,
            HandlerType = "Test",
            Iterations = iterations,
            TotalTime = totalTime,
            MinTime = minTime,
            MaxTime = maxTime,
            StandardDeviation = stdDev,
            TotalAllocatedBytes = Math.Max(0, finalMemory - initialMemory),
            Timestamp = startTime
        };
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
        var initialMemory = GC.GetTotalMemory(true);

        var response = await relay.SendAsync(request, cancellationToken);

        var finalMemory = GC.GetTotalMemory(false);
        var allocatedBytes = Math.Max(0, finalMemory - initialMemory);

        return (response, allocatedBytes);
    }

    /// <summary>
    /// Resets the test harness state for test isolation
    /// </summary>
    public void Reset()
    {
        // Dispose of any disposable resources
        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
        _disposables.Clear();

        // Clear telemetry data
        var telemetryProvider = GetTestTelemetryProvider();
        telemetryProvider?.Activities.Clear();
        telemetryProvider?.HandlerExecutions.Clear();
        telemetryProvider?.NotificationPublishes.Clear();
        telemetryProvider?.StreamingOperations.Clear();

        // Clear diagnostics data
        var diagnostics = GetServiceProvider().GetService<IRelayDiagnostics>();
        diagnostics?.ClearDiagnosticData();

        // Force garbage collection to clean up memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    /// <summary>
    /// Gets the current diagnostic summary
    /// </summary>
    public DiagnosticSummary GetDiagnosticSummary()
    {
        var diagnostics = GetService<IRelayDiagnostics>();
        return diagnostics.GetDiagnosticSummary();
    }

    /// <summary>
    /// Gets all completed traces
    /// </summary>
    public IEnumerable<RequestTrace> GetCompletedTraces(DateTimeOffset? since = null)
    {
        var diagnostics = GetService<IRelayDiagnostics>();
        return diagnostics.GetCompletedTraces(since);
    }

    /// <summary>
    /// Validates the current configuration
    /// </summary>
    public ValidationResult ValidateConfiguration()
    {
        var diagnostics = GetService<IRelayDiagnostics>();
        return diagnostics.ValidateConfiguration();
    }

    /// <summary>
    /// Disposes of the test harness and cleans up resources
    /// </summary>
    public void Dispose()
    {
        Reset();

        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
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
               genericType == typeof(IStreamHandler<,>) ||
               genericType == typeof(INotificationHandler<>) ||
               genericType == typeof(IPipelineBehavior<,>) ||
               genericType == typeof(IStreamPipelineBehavior<,>);
    }
}
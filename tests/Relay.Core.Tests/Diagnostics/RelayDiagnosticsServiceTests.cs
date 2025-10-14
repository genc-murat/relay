using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics;
using Relay.Core.Diagnostics.Configuration;
using Relay.Core.Diagnostics.Metrics;
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Diagnostics.Registry;
using Relay.Core.Diagnostics.Validation;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

public class RelayDiagnosticsServiceTests
{
    private class TestRequest : IRequest<string>
    {
        public string Data { get; set; } = "test";
    }

    private class TestVoidRequest : IRequest
    {
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRelay, TestRelay>();
        return services.BuildServiceProvider();
    }

    private class TestRelay : IRelay
    {
        public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default)
        {
            return new ValueTask<TResponse>(default(TResponse)!);
        }

        public ValueTask SendAsync(IRequest request, System.Threading.CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public System.Collections.Generic.IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, System.Threading.CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask PublishAsync<TNotification>(TNotification notification, System.Threading.CancellationToken cancellationToken = default) where TNotification : INotification
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullDiagnostics()
    {
        // Arrange
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions());
        var serviceProvider = CreateServiceProvider();

        // Act
        Action act = () => new RelayDiagnosticsService(null!, tracer, options, serviceProvider);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("diagnostics", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullTracer()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var options = Options.Create(new DiagnosticsOptions());
        var serviceProvider = CreateServiceProvider();

        // Act
        Action act = () => new RelayDiagnosticsService(diagnostics, null!, options, serviceProvider);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("tracer", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullOptions()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var serviceProvider = CreateServiceProvider();

        // Act
        Action act = () => new RelayDiagnosticsService(diagnostics, tracer, null!, serviceProvider);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullServiceProvider()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions());

        // Act
        Action act = () => new RelayDiagnosticsService(diagnostics, tracer, options, null!);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("serviceProvider", exception.ParamName);
    }

    [Fact]
    public void GetHandlers_ShouldReturnNotFound_WhenEndpointsDisabled()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = false });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetHandlers();

        // Assert
        Assert.NotNull(response);
        Assert.Equal(404, response.StatusCode);
        Assert.Contains("Diagnostic endpoints are disabled", response.ErrorMessage);
    }

    [Fact]
    public void GetHandlers_ShouldReturnSuccess_WhenEndpointsEnabled()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetHandlers();

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public void GetMetrics_ShouldReturnNotFound_WhenEndpointsDisabled()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = false });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetMetrics();

        // Assert
        Assert.Equal(404, response.StatusCode);
    }

    [Fact]
    public void GetMetrics_ShouldReturnSuccess_WhenEndpointsEnabled()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions { EnablePerformanceMetrics = true });
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true, EnablePerformanceMetrics = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetMetrics();

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public void GetHandlerMetrics_ShouldReturnNotFound_WhenRequestTypeNotFound()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions { EnablePerformanceMetrics = true });
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetHandlerMetrics("NonExistentRequest");

        // Assert
        Assert.Equal(404, response.StatusCode);
        Assert.Contains("No metrics found", response.ErrorMessage);
    }

    [Fact]
    public void GetHealth_ShouldReturnSuccess_WhenConfigurationValid()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions
        {
            EnableRequestTracing = true,
            EnablePerformanceMetrics = true,
            TraceBufferSize = 100,
            MetricsRetentionPeriod = TimeSpan.FromHours(1)
        });
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetHealth();

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.True(response.Data.IsValid);
    }

    [Fact]
    public void ClearDiagnosticData_ShouldReturnSuccess()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.ClearDiagnosticData();

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public async Task RunBenchmark_ShouldReturnBadRequest_WhenRequestTypeEmpty()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        var benchmarkRequest = new BenchmarkRequest { RequestType = "" };

        // Act
        var response = await service.RunBenchmark(benchmarkRequest);

        // Assert
        Assert.Equal(400, response.StatusCode);
    }

    [Fact]
    public async Task RunBenchmark_ShouldReturnBadRequest_WhenIterationsInvalid()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        var benchmarkRequest = new BenchmarkRequest
        {
            RequestType = "TestRequest",
            Iterations = 0
        };

        // Act
        var response = await service.RunBenchmark(benchmarkRequest);

        // Assert
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Iterations must be greater than 0", response.ErrorMessage);
    }

    [Fact]
    public async Task RunBenchmark_ShouldReturnNotFound_WhenRequestTypeNotFound()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        var benchmarkRequest = new BenchmarkRequest
        {
            RequestType = "NonExistentRequestType",
            Iterations = 10
        };

        // Act
        var response = await service.RunBenchmark(benchmarkRequest);

        // Assert
        Assert.Equal(404, response.StatusCode);
        Assert.Contains("Request type not found", response.ErrorMessage);
    }

    [Fact]
    public void GetSummary_ShouldReturnSuccess()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetSummary();

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public void GetTraces_ShouldReturnEmpty_WhenTracingDisabled()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions { EnableRequestTracing = false });
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions
        {
            EnableDiagnosticEndpoints = true,
            EnableRequestTracing = false
        });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTraces();

        // Assert
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Request tracing is disabled", response.ErrorMessage);
    }

    [Fact]
    public void GetHandlerMetrics_ShouldReturnSuccess_WhenRequestTypeExists()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions { EnablePerformanceMetrics = true });
        // Add some metrics using reflection
        var recordMethod = typeof(DefaultRelayDiagnostics).GetMethod("RecordHandlerMetrics", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        recordMethod?.Invoke(diagnostics, new object[] { "TestRequest", "TestHandler", TimeSpan.FromMilliseconds(100), true, 1024L });

        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetHandlerMetrics("TestRequest");

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal("TestRequest", response.Data.RequestType);
        Assert.Equal(1, response.Data.InvocationCount);
    }

    [Fact]
    public void GetHealth_ShouldReturnNotFound_WhenEndpointsDisabled()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = false });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetHealth();

        // Assert
        Assert.Equal(404, response.StatusCode);
        Assert.Contains("Diagnostic endpoints are disabled", response.ErrorMessage);
    }

    [Fact]
    public void GetHealth_ShouldReturnServiceUnavailable_WhenValidationFails()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions
        {
            TraceBufferSize = 0, // This will cause validation to fail
            MetricsRetentionPeriod = TimeSpan.FromHours(1)
        });
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetHealth();

        // Assert
        Assert.Equal(503, response.StatusCode);
        Assert.Contains("Configuration validation failed", response.ErrorMessage);
    }

    [Fact]
    public void GetSummary_ShouldReturnNotFound_WhenEndpointsDisabled()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = false });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetSummary();

        // Assert
        Assert.Equal(404, response.StatusCode);
        Assert.Contains("Diagnostic endpoints are disabled", response.ErrorMessage);
    }

    [Fact]
    public void GetTraces_ShouldReturnSuccess_WhenTracingEnabled()
    {
        // Arrange
        var tracer = new RequestTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, new DiagnosticsOptions { EnableRequestTracing = true });
        var options = Options.Create(new DiagnosticsOptions
        {
            EnableDiagnosticEndpoints = true,
            EnableRequestTracing = true
        });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTraces();

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public void GetTraces_ShouldReturnTextFormat_WhenFormatSpecified()
    {
        // Arrange
        var tracer = new RequestTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, new DiagnosticsOptions { EnableRequestTracing = true });
        var options = Options.Create(new DiagnosticsOptions
        {
            EnableDiagnosticEndpoints = true,
            EnableRequestTracing = true
        });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTraces(format: "text");

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        var data = response.Data as dynamic;
        Assert.Equal("text/plain", data.contentType);
    }

    [Fact]
    public void GetTrace_ShouldReturnNotFound_WhenTraceNotFound()
    {
        // Arrange
        var tracer = new RequestTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, new DiagnosticsOptions { EnableRequestTracing = true });
        var options = Options.Create(new DiagnosticsOptions
        {
            EnableDiagnosticEndpoints = true,
            EnableRequestTracing = true
        });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTrace(Guid.NewGuid());

        // Assert
        Assert.Equal(404, response.StatusCode);
        Assert.Contains("Trace not found", response.ErrorMessage);
    }

    [Fact]
    public void GetTrace_ShouldReturnBadRequest_WhenTracingDisabled()
    {
        // Arrange
        var tracer = new RequestTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, new DiagnosticsOptions { EnableRequestTracing = false });
        var options = Options.Create(new DiagnosticsOptions
        {
            EnableDiagnosticEndpoints = true,
            EnableRequestTracing = false
        });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTrace(Guid.NewGuid());

        // Assert
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Request tracing is disabled", response.ErrorMessage);
    }

    [Fact]
    public void ClearDiagnosticData_ShouldReturnNotFound_WhenEndpointsDisabled()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = false });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.ClearDiagnosticData();

        // Assert
        Assert.Equal(404, response.StatusCode);
        Assert.Contains("Diagnostic endpoints are disabled", response.ErrorMessage);
    }

    [Fact]
    public async Task RunBenchmark_ShouldReturnSuccess_WhenValidRequest()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        var benchmarkRequest = new BenchmarkRequest
        {
            RequestType = "TestRequest",
            Iterations = 5 // Small number for quick test
        };

        // Act
        var response = await service.RunBenchmark(benchmarkRequest);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal("TestRequest", response.Data.RequestType);
        Assert.Equal(5, response.Data.Iterations);
    }

    private class ThrowingDiagnostics : IRelayDiagnostics
    {
        public HandlerRegistryInfo GetHandlerRegistry() => throw new Exception("Test exception");
        public IEnumerable<HandlerMetrics> GetHandlerMetrics() => throw new Exception("Test exception");
        public RequestTrace? GetCurrentTrace() => throw new Exception("Test exception");
        public IEnumerable<RequestTrace> GetCompletedTraces(DateTimeOffset? since = null) => throw new Exception("Test exception");
        public ValidationResult ValidateConfiguration() => throw new Exception("Test exception");
        public Task<BenchmarkResult> BenchmarkHandlerAsync<TRequest>(TRequest request, int iterations = 1000, CancellationToken cancellationToken = default)
            where TRequest : IRequest => throw new Exception("Test exception");
        public void ClearDiagnosticData() => throw new Exception("Test exception");
        public DiagnosticSummary GetDiagnosticSummary() => throw new Exception("Test exception");
    }

    private class ThrowingTracer : IRequestTracer
    {
        public RequestTrace StartTrace<TRequest>(TRequest request, string? correlationId = null) => throw new Exception("Test exception");
        public RequestTrace? GetCurrentTrace() => throw new Exception("Test exception");
        public void AddStep(string stepName, TimeSpan duration, string category = "Unknown", object? metadata = null) => throw new Exception("Test exception");
        public void AddHandlerStep(string stepName, TimeSpan duration, Type handlerType, string category = "Handler", object? metadata = null) => throw new Exception("Test exception");
        public void RecordException(Exception exception, string? stepName = null) => throw new Exception("Test exception");
        public void CompleteTrace(bool success = true) => throw new Exception("Test exception");
        public IEnumerable<RequestTrace> GetCompletedTraces(DateTimeOffset? since = null) => throw new Exception("Test exception");
        public void ClearTraces() => throw new Exception("Test exception");
        public int ActiveTraceCount => throw new Exception("Test exception");
        public int CompletedTraceCount => throw new Exception("Test exception");
        public bool IsEnabled { get => throw new Exception("Test exception"); set => throw new Exception("Test exception"); }
    }

    [Fact]
    public void GetHandlers_ShouldReturnError_WhenExceptionThrown()
    {
        // Arrange
        var diagnostics = new ThrowingDiagnostics();
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetHandlers();

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Contains("Failed to retrieve handler registry", response.ErrorMessage);
    }

    [Fact]
    public void GetMetrics_ShouldReturnError_WhenExceptionThrown()
    {
        // Arrange
        var diagnostics = new ThrowingDiagnostics();
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true, EnablePerformanceMetrics = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetMetrics();

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Contains("Failed to retrieve handler metrics", response.ErrorMessage);
    }

    [Fact]
    public void GetHealth_ShouldReturnError_WhenExceptionThrown()
    {
        // Arrange
        var diagnostics = new ThrowingDiagnostics();
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetHealth();

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Contains("Failed to validate configuration", response.ErrorMessage);
    }

    [Fact]
    public void GetSummary_ShouldReturnError_WhenExceptionThrown()
    {
        // Arrange
        var diagnostics = new ThrowingDiagnostics();
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetSummary();

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Contains("Failed to retrieve diagnostic summary", response.ErrorMessage);
    }

    [Fact]
    public void GetTraces_ShouldReturnError_WhenExceptionThrown()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new ThrowingTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true, EnableRequestTracing = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTraces();

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Contains("Failed to retrieve traces", response.ErrorMessage);
    }

    [Fact]
    public void GetTrace_ShouldReturnError_WhenExceptionThrown()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new ThrowingTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true, EnableRequestTracing = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTrace(Guid.NewGuid());

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Contains("Failed to retrieve trace", response.ErrorMessage);
    }

    [Fact]
    public void ClearDiagnosticData_ShouldReturnError_WhenExceptionThrown()
    {
        // Arrange
        var diagnostics = new ThrowingDiagnostics();
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.ClearDiagnosticData();

        // Act & Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Contains("Failed to clear diagnostic data", response.ErrorMessage);
    }

    private class ThrowingRelay : IRelay
    {
        public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new Exception("Test exception");
        public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default) => throw new Exception("Test exception");
        public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new Exception("Test exception");
        public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new Exception("Test exception");
    }
}
using System;
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
}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics;
using Relay.Core.Diagnostics.Configuration;
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Diagnostics.Metrics;
using Relay.Core.Diagnostics.Registry;
using Relay.Core.Diagnostics.Validation;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

/// <summary>
/// Tests for RelayDiagnosticsService metrics functionality
/// </summary>
public class RelayDiagnosticsServiceMetricsTests
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
        public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return new ValueTask<TResponse>(default(TResponse)!);
        }

        public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            throw new NotImplementedException();
        }
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
    public void GetHandlerMetrics_ShouldReturnSuccess_WhenRequestTypeExists()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions { EnablePerformanceMetrics = true });
        // Add some metrics using reflection
        var recordMethod = typeof(DefaultRelayDiagnostics).GetMethod("RecordHandlerMetrics", BindingFlags.Public | BindingFlags.Instance);
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
}
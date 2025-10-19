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
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Diagnostics.Metrics;
using Relay.Core.Diagnostics.Registry;
using Relay.Core.Diagnostics.Validation;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

/// <summary>
/// Tests for RelayDiagnosticsService health check functionality
/// </summary>
public class RelayDiagnosticsServiceHealthTests
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
    public void GetHealth_ShouldReturnSuccess_WithWarnings()
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
    }
}
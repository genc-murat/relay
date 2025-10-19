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
/// Tests for RelayDiagnosticsService ClearDiagnosticData functionality
/// </summary>
public class RelayDiagnosticsServiceClearDataTests
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
}
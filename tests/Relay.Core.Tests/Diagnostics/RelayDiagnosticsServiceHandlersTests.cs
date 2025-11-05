using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics.Configuration;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Diagnostics.Metrics;
using Relay.Core.Diagnostics.Registry;
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using Relay.Core.Diagnostics.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

/// <summary>
/// Tests for RelayDiagnosticsService GetHandlers functionality
/// </summary>
public class RelayDiagnosticsServiceHandlersTests
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
            return EmptyAsync<TResponse>();
        }

        private static async IAsyncEnumerable<T> EmptyAsync<T>()
        {
            yield break;
        }

        public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            return ValueTask.CompletedTask;
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

        // Verify HandlerRegistryInfo content
        var registry = response.Data;
        Assert.NotNull(registry.AssemblyName);
        Assert.NotEmpty(registry.AssemblyName);
        Assert.True(registry.GenerationTime > DateTime.MinValue);
        Assert.NotNull(registry.Handlers);
        Assert.Empty(registry.Handlers); // Default implementation returns empty list
        Assert.Equal(0, registry.TotalHandlers);
        Assert.NotNull(registry.Pipelines);
        Assert.Empty(registry.Pipelines);
        Assert.Equal(0, registry.TotalPipelines);
        Assert.NotNull(registry.Warnings);
        Assert.Empty(registry.Warnings);
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
        Assert.NotNull(response.ErrorDetails);
    }

    [Fact]
    public void GetHandlers_ShouldIncludeExceptionDetails_WhenExceptionThrown()
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
        Assert.NotNull(response.ErrorDetails);

        // Check that ErrorDetails contains exception information
        var details = response.ErrorDetails;
        Assert.NotNull(details);
        var typeProperty = details.GetType().GetProperty("Type")?.GetValue(details);
        var messageProperty = details.GetType().GetProperty("Message")?.GetValue(details);
        var stackTraceProperty = details.GetType().GetProperty("StackTrace")?.GetValue(details);

        Assert.Equal("Exception", typeProperty);
        Assert.Equal("Test exception", messageProperty);
        Assert.NotNull(stackTraceProperty);
    }
}

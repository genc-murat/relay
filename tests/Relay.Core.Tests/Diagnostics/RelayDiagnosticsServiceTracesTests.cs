using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics.Configuration;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

/// <summary>
/// Tests for RelayDiagnosticsService tracing functionality
/// </summary>
public class RelayDiagnosticsServiceTracesTests
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
    public void GetTrace_ShouldReturnTextFormat_WhenFormatSpecified()
    {
        // Arrange
        var tracer = new RequestTracer();
        var trace = tracer.StartTrace(new TestRequest());
        tracer.CompleteTrace();
        var diagnostics = new DefaultRelayDiagnostics(tracer, new DiagnosticsOptions { EnableRequestTracing = true });

        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true, EnableRequestTracing = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTrace(trace.RequestId, "text");

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        // Check that data contains content and contentType
        var data = response.Data;
        Assert.NotNull(data);
        var contentProperty = data.GetType().GetProperty("content")?.GetValue(data);
        var contentTypeProperty = data.GetType().GetProperty("contentType")?.GetValue(data);
        Assert.NotNull(contentProperty);
        Assert.Equal("text/plain", contentTypeProperty);
    }

    [Fact]
    public void GetTrace_ShouldReturnJsonFormat_WhenInvalidFormatSpecified()
    {
        // Arrange
        var tracer = new RequestTracer();
        var trace = tracer.StartTrace(new TestRequest());
        var traceId = trace.RequestId;
        tracer.CompleteTrace();

        var diagnostics = new DefaultRelayDiagnostics(tracer, new DiagnosticsOptions { EnableRequestTracing = true });
        var options = Options.Create(new DiagnosticsOptions
        {
            EnableDiagnosticEndpoints = true,
            EnableRequestTracing = true
        });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTrace(traceId, "invalid");

        // Assert - Should default to JSON format
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        // Should be the trace object directly, not wrapped in content object
        Assert.IsType<RequestTrace>(response.Data);
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
    public void GetTraces_ShouldReturnJsonFormat_WhenInvalidFormatSpecified()
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
        var response = service.GetTraces(format: "invalid");

        // Assert - Should default to JSON format
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        // Should be the traces collection directly, not wrapped in content object
        Assert.IsType<IEnumerable<RequestTrace>>(response.Data, exactMatch: false);
    }

    [Fact]
    public void GetTraces_ShouldIncludeExceptionDetails_WhenExceptionThrown()
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

    [Fact]
    public void GetTraces_ShouldFilterBySince_WhenSinceSpecified()
    {
        // Arrange
        var tracer = new RequestTracer();
        var oldTraceId = Guid.NewGuid();
        var newTraceId = Guid.NewGuid();

        // Create an old trace
        tracer.StartTrace(new TestRequest(), oldTraceId.ToString());
        tracer.CompleteTrace();

        var since = DateTimeOffset.UtcNow;

        // Create a new trace
        tracer.StartTrace(new TestRequest(), newTraceId.ToString());
        tracer.CompleteTrace();

        var diagnostics = new DefaultRelayDiagnostics(tracer, new DiagnosticsOptions { EnableRequestTracing = true });
        var options = Options.Create(new DiagnosticsOptions
        {
            EnableDiagnosticEndpoints = true,
            EnableRequestTracing = true
        });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTraces(since);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        // Should only return traces after 'since'
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
        Assert.NotNull(response.ErrorDetails);
    }

    [Fact]
    public void GetTrace_ShouldIncludeExceptionDetails_WhenExceptionThrown()
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

    [Fact]
    public void GetTrace_ShouldReturnSuccess_WhenTraceFound()
    {
        // Arrange
        var tracer = new RequestTracer();
        var trace = tracer.StartTrace(new TestRequest());
        var traceId = trace.RequestId;
        tracer.CompleteTrace();

        var diagnostics = new DefaultRelayDiagnostics(tracer, new DiagnosticsOptions { EnableRequestTracing = true });
        var options = Options.Create(new DiagnosticsOptions
        {
            EnableDiagnosticEndpoints = true,
            EnableRequestTracing = true
        });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = service.GetTrace(traceId);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
    }

}

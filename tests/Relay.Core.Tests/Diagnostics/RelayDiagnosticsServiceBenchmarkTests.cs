using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics.Configuration;
using Relay.Core.Diagnostics.Metrics;
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

/// <summary>
/// Tests for RelayDiagnosticsService benchmark functionality
/// </summary>
public class RelayDiagnosticsServiceBenchmarkTests
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

    private class ThrowingRelay : IRelay
    {
        public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new Exception("Test exception");
        public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default) => throw new Exception("Test exception");
        public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new Exception("Test exception");
        public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new Exception("Test exception");
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

    [Fact]
    public async Task RunBenchmark_ShouldReturnError_WhenRelayNotAvailable()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var services = new ServiceCollection();
        // Don't add IRelay to make it unavailable
        var serviceProvider = services.BuildServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        var benchmarkRequest = new BenchmarkRequest
        {
            RequestType = "TestRequest",
            Iterations = 5
        };

        // Act
        var response = await service.RunBenchmark(benchmarkRequest);

        // Assert
        Assert.False(response.IsSuccess);
        Assert.Equal(500, response.StatusCode);
        Assert.Contains("IRelay service not available", response.ErrorMessage);
    }

    [Fact]
    public async Task RunBenchmark_ShouldReturnSuccess_WithVoidRequest()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        var benchmarkRequest = new BenchmarkRequest
        {
            RequestType = "TestVoidRequest",
            Iterations = 3
        };

        // Act
        var response = await service.RunBenchmark(benchmarkRequest);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal("TestVoidRequest", response.Data.RequestType);
        Assert.Equal(3, response.Data.Iterations);
    }

    [Fact]
    public async Task RunBenchmark_ShouldReturnBadRequest_WhenRequestNull()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var serviceProvider = CreateServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        // Act
        var response = await service.RunBenchmark(null!);

        // Assert
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Invalid benchmark request", response.ErrorMessage);
    }

    [Fact]
    public async Task RunBenchmark_ShouldReturnBadRequest_WhenCreateInstanceFails()
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
            RequestData = "{ invalid json }", // This should cause deserialization to fail
            Iterations = 5
        };

        // Act
        var response = await service.RunBenchmark(benchmarkRequest);

        // Assert
        Assert.Equal(400, response.StatusCode);
        Assert.Contains("Failed to create instance", response.ErrorMessage);
    }

    [Fact]
    public async Task RunBenchmark_ShouldReturnSuccess_WhenValidJsonRequestDataProvided()
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
            RequestData = "{\"Data\":\"custom data\"}", // Valid JSON for TestRequest
            Iterations = 3
        };

        // Act
        var response = await service.RunBenchmark(benchmarkRequest);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(response.Data);
        Assert.Equal("TestRequest", response.Data.RequestType);
        Assert.Equal(3, response.Data.Iterations);
    }

    [Fact]
    public async Task RunBenchmark_ShouldHandleSendAsyncExceptions()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions { EnableDiagnosticEndpoints = true });
        var services = new ServiceCollection();
        services.AddSingleton<IRelay, ThrowingRelay>();
        var serviceProvider = services.BuildServiceProvider();
        var service = new RelayDiagnosticsService(diagnostics, tracer, options, serviceProvider);

        var benchmarkRequest = new BenchmarkRequest
        {
            RequestType = "TestRequest",
            Iterations = 5
        };

        // Act
        var response = await service.RunBenchmark(benchmarkRequest);

        // Assert
        Assert.True(response.IsSuccess); // Should succeed even with exceptions in iterations
        Assert.NotNull(response.Data);
        Assert.Equal(5, response.Data.Iterations);
        // Failed iterations should be tracked
        var metrics = response.Data.Metrics as Dictionary<string, object>;
        Assert.True(metrics.ContainsKey("FailedIterations"));
    }
}
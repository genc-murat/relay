using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics.Configuration;
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using Relay.Core.Diagnostics.Validation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

public class DiagnosticsTests
{
    private class TestRequest : IRequest
    {
    }

    private static RequestTracer CreateTracer() => new RequestTracer();

    [Fact]
    public void DefaultRelayDiagnostics_Constructor_ShouldThrowOnNullTracer()
    {
        // Arrange & Act
        Action act = () => new DefaultRelayDiagnostics(null!, new DiagnosticsOptions());

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("tracer", exception.ParamName);
    }

    [Fact]
    public void DefaultRelayDiagnostics_Constructor_ShouldThrowOnNullOptions()
    {
        // Arrange
        var tracer = CreateTracer();

        // Act
        Action act = () => new DefaultRelayDiagnostics(tracer, null!);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void GetHandlerRegistry_ShouldReturnValidRegistry()
    {
        // Arrange
        var options = new DiagnosticsOptions();
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var registry = diagnostics.GetHandlerRegistry();

        // Assert
        Assert.NotNull(registry);
        Assert.False(string.IsNullOrEmpty(registry.AssemblyName));
        Assert.NotNull(registry.Handlers);
        Assert.NotNull(registry.Pipelines);
        Assert.NotNull(registry.Warnings);
    }

    [Fact]
    public void GetHandlerMetrics_ShouldReturnEmpty_WhenMetricsDisabled()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = false };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var metrics = diagnostics.GetHandlerMetrics();

        // Assert
        Assert.Empty(metrics);
    }

    [Fact]
    public void RecordHandlerMetrics_ShouldNotRecord_WhenMetricsDisabled()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = false };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(100), true);
        var metrics = diagnostics.GetHandlerMetrics();

        // Assert
        Assert.Empty(metrics);
    }

    [Fact]
    public void RecordHandlerMetrics_ShouldRecordSuccessfulExecution()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = true };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(100), true, 1024);
        var metrics = diagnostics.GetHandlerMetrics().ToList();

        // Assert
        Assert.Single(metrics);
        var metric = metrics[0];
        Assert.Equal("TestRequest", metric.RequestType);
        Assert.Equal(1, metric.InvocationCount);
        Assert.Equal(1, metric.SuccessCount);
        Assert.Equal(0, metric.ErrorCount);
        Assert.Equal(1024, metric.TotalAllocatedBytes);
    }

    [Fact]
    public void RecordHandlerMetrics_ShouldRecordFailedExecution()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = true };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(50), false);
        var metrics = diagnostics.GetHandlerMetrics().ToList();

        // Assert
        Assert.Single(metrics);
        var metric = metrics[0];
        Assert.Equal(0, metric.SuccessCount);
        Assert.Equal(1, metric.ErrorCount);
    }

    [Fact]
    public void RecordHandlerMetrics_ShouldAggregateMultipleInvocations()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = true };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(100), true, 512);
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(150), true, 256);
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(200), false, 128);
        var metrics = diagnostics.GetHandlerMetrics().ToList();

        // Assert
        Assert.Single(metrics);
        var metric = metrics[0];
        Assert.Equal(3, metric.InvocationCount);
        Assert.Equal(2, metric.SuccessCount);
        Assert.Equal(1, metric.ErrorCount);
        Assert.Equal(TimeSpan.FromMilliseconds(100), metric.MinExecutionTime);
        Assert.Equal(TimeSpan.FromMilliseconds(200), metric.MaxExecutionTime);
        Assert.Equal(896, metric.TotalAllocatedBytes);
    }

    [Fact]
    public void ValidateConfiguration_ShouldReturnValid_WhenProperlyConfigured()
    {
        // Arrange
        var options = new DiagnosticsOptions
        {
            EnableRequestTracing = true,
            EnablePerformanceMetrics = true,
            TraceBufferSize = 100,
            MetricsRetentionPeriod = TimeSpan.FromHours(1)
        };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var result = diagnostics.ValidateConfiguration();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues.Where(i => i.Severity == ValidationSeverity.Error));
    }

    [Fact]
    public void ValidateConfiguration_ShouldWarn_WhenBothTracingAndMetricsDisabled()
    {
        // Arrange
        var options = new DiagnosticsOptions
        {
            EnableRequestTracing = false,
            EnablePerformanceMetrics = false,
            TraceBufferSize = 100,
            MetricsRetentionPeriod = TimeSpan.FromHours(1)
        };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var result = diagnostics.ValidateConfiguration();

        // Assert
        var warnings = result.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
        Assert.Single(warnings);
        Assert.Contains("Both request tracing and performance metrics are disabled", warnings.First().Message);
    }

    [Fact]
    public void ValidateConfiguration_ShouldError_WhenTraceBufferSizeInvalid()
    {
        // Arrange
        var options = new DiagnosticsOptions
        {
            EnableRequestTracing = true,
            TraceBufferSize = 0,
            MetricsRetentionPeriod = TimeSpan.FromHours(1)
        };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var result = diagnostics.ValidateConfiguration();

        // Assert
        Assert.False(result.IsValid);
        var errors = result.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
        Assert.Single(errors);
        Assert.Contains("Trace buffer size must be greater than 0", errors.First().Message);
    }

    [Fact]
    public void ValidateConfiguration_ShouldError_WhenMetricsRetentionPeriodInvalid()
    {
        // Arrange
        var options = new DiagnosticsOptions
        {
            EnableRequestTracing = true,
            TraceBufferSize = 100,
            MetricsRetentionPeriod = TimeSpan.Zero
        };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var result = diagnostics.ValidateConfiguration();

        // Assert
        Assert.False(result.IsValid);
        var errors = result.Issues.Where(i => i.Severity == ValidationSeverity.Error).ToList();
        Assert.Single(errors);
        Assert.Contains("Metrics retention period must be greater than 0", errors.First().Message);
    }

    [Fact]
    public void ValidateConfiguration_ShouldWarn_WhenDiagnosticEndpointsWithoutAuth()
    {
        // Arrange
        var options = new DiagnosticsOptions
        {
            EnableRequestTracing = true,
            EnablePerformanceMetrics = true,
            TraceBufferSize = 100,
            MetricsRetentionPeriod = TimeSpan.FromHours(1),
            EnableDiagnosticEndpoints = true,
            RequireAuthentication = false
        };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var result = diagnostics.ValidateConfiguration();

        // Assert
        var warnings = result.Issues.Where(i => i.Severity == ValidationSeverity.Warning).ToList();
        Assert.Contains(warnings, w => w.Message.Contains("Diagnostic endpoints are enabled without authentication"));
    }

    [Fact]
    public async Task BenchmarkHandlerAsync_ShouldThrowOnInvalidIterations()
    {
        // Arrange
        var options = new DiagnosticsOptions();
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);
        var request = new TestRequest();

        // Act
        Func<Task> act = async () => await diagnostics.BenchmarkHandlerAsync(request, 0);

        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(act);
        Assert.Equal("iterations", exception.ParamName);
    }

    [Fact]
    public async Task BenchmarkHandlerAsync_ShouldReturnValidResult()
    {
        // Arrange
        var options = new DiagnosticsOptions();
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);
        var request = new TestRequest();

        // Act
        var result = await diagnostics.BenchmarkHandlerAsync(request, 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("TestRequest", result.RequestType);
        Assert.Equal(10, result.Iterations);
        Assert.True(result.TotalTime > TimeSpan.Zero);
        Assert.True(result.MinTime > TimeSpan.Zero);
        Assert.True(result.MaxTime > TimeSpan.Zero);
        Assert.True(result.StandardDeviation >= TimeSpan.Zero);
    }

    [Fact]
    public async Task BenchmarkHandlerAsync_ShouldRespectCancellation()
    {
        // Arrange
        var options = new DiagnosticsOptions();
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);
        var request = new TestRequest();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        Func<Task> act = async () => await diagnostics.BenchmarkHandlerAsync(request, 1000, cts.Token);

        // Assert
        await Assert.ThrowsAsync<OperationCanceledException>(act);
    }

    [Fact]
    public void ClearDiagnosticData_ShouldClearMetrics()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = true };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(100), true);

        // Act
        diagnostics.ClearDiagnosticData();
        var metrics = diagnostics.GetHandlerMetrics();

        // Assert
        Assert.Empty(metrics);
    }

    [Fact]
    public void GetDiagnosticSummary_ShouldReturnValidSummary()
    {
        // Arrange
        var options = new DiagnosticsOptions
        {
            EnableRequestTracing = true,
            EnablePerformanceMetrics = true
        };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var summary = diagnostics.GetDiagnosticSummary();

        // Assert
        Assert.NotNull(summary);
        Assert.True(summary.IsTracingEnabled);
        Assert.True(summary.IsMetricsEnabled);
        Assert.True(summary.Uptime > TimeSpan.Zero);
    }

    [Fact]
    public void GetDiagnosticSummary_ShouldIncludeMetrics()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = true };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        diagnostics.RecordHandlerMetrics("Request1", "Handler1", TimeSpan.FromMilliseconds(100), true, 512);
        diagnostics.RecordHandlerMetrics("Request2", "Handler2", TimeSpan.FromMilliseconds(200), false, 1024);

        // Act
        var summary = diagnostics.GetDiagnosticSummary();

        // Assert
        Assert.Equal(2, summary.TotalInvocations);
        Assert.Equal(1, summary.TotalSuccessfulInvocations);
        Assert.Equal(1, summary.TotalFailedInvocations);
        Assert.Equal(1536, summary.TotalAllocatedBytes);
    }

    [Fact]
    public void GetCurrentTrace_ShouldReturnNull_WhenNoActiveTrace()
    {
        // Arrange
        var options = new DiagnosticsOptions();
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var trace = diagnostics.GetCurrentTrace();

        // Assert
        Assert.Null(trace);
    }

    [Fact]
    public void GetCompletedTraces_ShouldReturnEmpty_WhenNoTraces()
    {
        // Arrange
        var options = new DiagnosticsOptions();
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var traces = diagnostics.GetCompletedTraces();

        // Assert
        Assert.Empty(traces);
    }

    [Fact]
    public void RecordHandlerMetrics_ShouldTrackMinExecutionTime()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = true };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(200), true);
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(50), true);
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(150), true);

        var metrics = diagnostics.GetHandlerMetrics().First();

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(50), metrics.MinExecutionTime);
    }

    [Fact]
    public void RecordHandlerMetrics_ShouldTrackMaxExecutionTime()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = true };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(100), true);
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(300), true);
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(150), true);

        var metrics = diagnostics.GetHandlerMetrics().First();

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(300), metrics.MaxExecutionTime);
    }

    [Fact]
    public void RecordHandlerMetrics_ShouldUpdateLastInvocationTime()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = true };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(100), true);
        var firstTime = diagnostics.GetHandlerMetrics().First().LastInvocation;

        System.Threading.Thread.Sleep(10);

        diagnostics.RecordHandlerMetrics("TestRequest", "TestHandler", TimeSpan.FromMilliseconds(100), true);
        var secondTime = diagnostics.GetHandlerMetrics().First().LastInvocation;

        // Assert
        Assert.True(secondTime > firstTime);
    }

    [Fact]
    public void RecordHandlerMetrics_ShouldTrackDifferentHandlers()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = true };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        diagnostics.RecordHandlerMetrics("Request1", "Handler1", TimeSpan.FromMilliseconds(100), true);
        diagnostics.RecordHandlerMetrics("Request2", "Handler2", TimeSpan.FromMilliseconds(200), true);

        var metrics = diagnostics.GetHandlerMetrics().ToList();

        // Assert
        Assert.Equal(2, metrics.Count);
    }

    [Fact]
    public async Task BenchmarkHandlerAsync_ShouldCalculateStandardDeviation()
    {
        // Arrange
        var options = new DiagnosticsOptions();
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);
        var request = new TestRequest();

        // Act
        var result = await diagnostics.BenchmarkHandlerAsync(request, 100);

        // Assert
        Assert.NotEqual(TimeSpan.Zero, result.StandardDeviation);
    }

    [Fact]
    public void ValidateConfiguration_ShouldHandleMultipleErrors()
    {
        // Arrange
        var options = new DiagnosticsOptions
        {
            EnableRequestTracing = false,
            EnablePerformanceMetrics = false,
            TraceBufferSize = -1,
            MetricsRetentionPeriod = TimeSpan.FromMilliseconds(-1)
        };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        // Act
        var result = diagnostics.ValidateConfiguration();

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Issues.Count(i => i.Severity == ValidationSeverity.Error) > 1);
    }

    [Fact]
    public void GetDiagnosticSummary_ShouldCalculateAverageExecutionTime()
    {
        // Arrange
        var options = new DiagnosticsOptions { EnablePerformanceMetrics = true };
        var tracer = CreateTracer();
        var diagnostics = new DefaultRelayDiagnostics(tracer, options);

        diagnostics.RecordHandlerMetrics("Request1", "Handler1", TimeSpan.FromMilliseconds(100), true);
        diagnostics.RecordHandlerMetrics("Request2", "Handler2", TimeSpan.FromMilliseconds(200), true);

        // Act
        var summary = diagnostics.GetDiagnosticSummary();

        // Assert
        Assert.True(summary.AverageExecutionTime > TimeSpan.Zero);
    }
}
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics;
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
        act.Should().Throw<ArgumentNullException>().WithParameterName("tracer");
    }

    [Fact]
    public void DefaultRelayDiagnostics_Constructor_ShouldThrowOnNullOptions()
    {
        // Arrange
        var tracer = CreateTracer();

        // Act
        Action act = () => new DefaultRelayDiagnostics(tracer, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
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
        registry.Should().NotBeNull();
        registry.AssemblyName.Should().NotBeNullOrEmpty();
        registry.Handlers.Should().NotBeNull();
        registry.Pipelines.Should().NotBeNull();
        registry.Warnings.Should().NotBeNull();
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
        metrics.Should().BeEmpty();
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
        metrics.Should().BeEmpty();
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
        metrics.Should().HaveCount(1);
        var metric = metrics[0];
        metric.RequestType.Should().Be("TestRequest");
        metric.InvocationCount.Should().Be(1);
        metric.SuccessCount.Should().Be(1);
        metric.ErrorCount.Should().Be(0);
        metric.TotalAllocatedBytes.Should().Be(1024);
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
        metrics.Should().HaveCount(1);
        var metric = metrics[0];
        metric.SuccessCount.Should().Be(0);
        metric.ErrorCount.Should().Be(1);
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
        metrics.Should().HaveCount(1);
        var metric = metrics[0];
        metric.InvocationCount.Should().Be(3);
        metric.SuccessCount.Should().Be(2);
        metric.ErrorCount.Should().Be(1);
        metric.MinExecutionTime.Should().Be(TimeSpan.FromMilliseconds(100));
        metric.MaxExecutionTime.Should().Be(TimeSpan.FromMilliseconds(200));
        metric.TotalAllocatedBytes.Should().Be(896);
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
        result.IsValid.Should().BeTrue();
        result.Issues.Where(i => i.Severity == ValidationSeverity.Error).Should().BeEmpty();
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
        result.Issues.Where(i => i.Severity == ValidationSeverity.Warning).Should().ContainSingle();
        result.Issues.Where(i => i.Severity == ValidationSeverity.Warning).First().Message.Should().Contain("Both request tracing and performance metrics are disabled");
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
        result.IsValid.Should().BeFalse();
        result.Issues.Where(i => i.Severity == ValidationSeverity.Error).Should().ContainSingle();
        result.Issues.Where(i => i.Severity == ValidationSeverity.Error).First().Message.Should().Contain("Trace buffer size must be greater than 0");
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
        result.IsValid.Should().BeFalse();
        result.Issues.Where(i => i.Severity == ValidationSeverity.Error).Should().ContainSingle();
        result.Issues.Where(i => i.Severity == ValidationSeverity.Error).First().Message.Should().Contain("Metrics retention period must be greater than 0");
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
        result.Issues.Where(i => i.Severity == ValidationSeverity.Warning).Should().Contain(w => w.Message.Contains("Diagnostic endpoints are enabled without authentication"));
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
        await act.Should().ThrowAsync<ArgumentException>().WithParameterName("iterations");
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
        result.Should().NotBeNull();
        result.RequestType.Should().Be("TestRequest");
        result.Iterations.Should().Be(10);
        result.TotalTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.MinTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.MaxTime.Should().BeGreaterThan(TimeSpan.Zero);
        result.StandardDeviation.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
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
        await act.Should().ThrowAsync<OperationCanceledException>();
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
        metrics.Should().BeEmpty();
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
        summary.Should().NotBeNull();
        summary.IsTracingEnabled.Should().BeTrue();
        summary.IsMetricsEnabled.Should().BeTrue();
        summary.Uptime.Should().BeGreaterThan(TimeSpan.Zero);
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
        summary.TotalInvocations.Should().Be(2);
        summary.TotalSuccessfulInvocations.Should().Be(1);
        summary.TotalFailedInvocations.Should().Be(1);
        summary.TotalAllocatedBytes.Should().Be(1536);
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
        trace.Should().BeNull();
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
        traces.Should().BeEmpty();
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
        metrics.MinExecutionTime.Should().Be(TimeSpan.FromMilliseconds(50));
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
        metrics.MaxExecutionTime.Should().Be(TimeSpan.FromMilliseconds(300));
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
        secondTime.Should().BeAfter(firstTime);
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
        metrics.Should().HaveCount(2);
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
        result.StandardDeviation.Should().NotBe(TimeSpan.Zero);
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
        result.IsValid.Should().BeFalse();
        result.Issues.Count(i => i.Severity == ValidationSeverity.Error).Should().BeGreaterThan(1);
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
        summary.AverageExecutionTime.Should().BeGreaterThan(TimeSpan.Zero);
    }
}

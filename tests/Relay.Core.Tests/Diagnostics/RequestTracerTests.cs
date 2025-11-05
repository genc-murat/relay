using System;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics;
using Relay.Core.Diagnostics.Tracing;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

public class RequestTracerTests
{
    private class TestRequest : IRequest
    {
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var tracer = new RequestTracer();

        // Assert
        Assert.True(tracer.IsEnabled);
        Assert.Equal(0, tracer.ActiveTraceCount);
        Assert.Equal(0, tracer.CompletedTraceCount);
    }

    [Fact]
    public void StartTrace_ShouldCreateNewTrace()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();

        // Act
        var trace = tracer.StartTrace(request);

        // Assert
        Assert.NotNull(trace);
        Assert.NotEqual(Guid.Empty, trace.RequestId);
        Assert.Equal(typeof(TestRequest), trace.RequestType);
        Assert.True((DateTimeOffset.UtcNow - trace.StartTime).Duration() < TimeSpan.FromSeconds(1));
        Assert.Equal(1, tracer.ActiveTraceCount);
    }

    [Fact]
    public void StartTrace_ShouldSetCorrelationId()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        var correlationId = "test-correlation-id";

        // Act
        var trace = tracer.StartTrace(request, correlationId);

        // Assert
        Assert.Equal(correlationId, trace.CorrelationId);
    }

    [Fact]
    public void StartTrace_ShouldGenerateCorrelationId_WhenNotProvided()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();

        // Act
        var trace = tracer.StartTrace(request);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(trace.CorrelationId));
    }

    [Fact]
    public void StartTrace_ShouldReturnEmptyTrace_WhenDisabled()
    {
        // Arrange
        var tracer = new RequestTracer { IsEnabled = false };
        var request = new TestRequest();

        // Act
        var trace = tracer.StartTrace(request);

        // Assert
        Assert.Equal(Guid.Empty, trace.RequestId);
        Assert.Equal(0, tracer.ActiveTraceCount);
    }

    [Fact]
    public void GetCurrentTrace_ShouldReturnActiveTrace()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        var startedTrace = tracer.StartTrace(request);

        // Act
        var currentTrace = tracer.GetCurrentTrace();

        // Assert
        Assert.NotNull(currentTrace);
        Assert.Equal(startedTrace.RequestId, currentTrace!.RequestId);
    }

    [Fact]
    public void GetCurrentTrace_ShouldReturnNull_WhenNoActiveTrace()
    {
        // Arrange
        var tracer = new RequestTracer();

        // Act
        var currentTrace = tracer.GetCurrentTrace();

        // Assert
        Assert.Null(currentTrace);
    }

    [Fact]
    public void GetCurrentTrace_ShouldReturnNull_WhenDisabled()
    {
        // Arrange
        var tracer = new RequestTracer { IsEnabled = false };
        var request = new TestRequest();
        tracer.StartTrace(request);

        // Act
        var currentTrace = tracer.GetCurrentTrace();

        // Assert
        Assert.Null(currentTrace);
    }

    [Fact]
    public void AddStep_ShouldAddStepToCurrentTrace()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        tracer.StartTrace(request);

        // Act
        tracer.AddStep("TestStep", TimeSpan.FromMilliseconds(100), "TestCategory");
        var currentTrace = tracer.GetCurrentTrace();

        // Assert
        Assert.Single(currentTrace!.Steps);
        Assert.Equal("TestStep", currentTrace.Steps[0].Name);
        Assert.Equal(TimeSpan.FromMilliseconds(100), currentTrace.Steps[0].Duration);
        Assert.Equal("TestCategory", currentTrace.Steps[0].Category);
    }

    [Fact]
    public void AddStep_ShouldNotAddStep_WhenDisabled()
    {
        // Arrange
        var tracer = new RequestTracer { IsEnabled = false };

        // Act
        tracer.AddStep("TestStep", TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.Null(tracer.GetCurrentTrace());
    }

    [Fact]
    public void AddStep_ShouldNotAddStep_WhenNoActiveTrace()
    {
        // Arrange
        var tracer = new RequestTracer();

        // Act
        tracer.AddStep("TestStep", TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.Null(tracer.GetCurrentTrace());
    }

    [Fact]
    public void AddHandlerStep_ShouldAddStepWithHandlerType()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        tracer.StartTrace(request);

        // Act
        tracer.AddHandlerStep("HandlerExecution", TimeSpan.FromMilliseconds(50), typeof(TestRequest));
        var currentTrace = tracer.GetCurrentTrace();

        // Assert
        Assert.Single(currentTrace!.Steps);
        Assert.Equal("HandlerExecution", currentTrace.Steps[0].Name);
        Assert.Equal("TestRequest", currentTrace.Steps[0].HandlerType);
    }

    [Fact]
    public void RecordException_ShouldSetExceptionOnTrace()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        tracer.StartTrace(request);
        var exception = new InvalidOperationException("Test error");

        // Act
        tracer.RecordException(exception);
        var currentTrace = tracer.GetCurrentTrace();

        // Assert
        Assert.Same(exception, currentTrace!.Exception);
    }

    [Fact]
    public void RecordException_WithStepName_ShouldAddExceptionStep()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        tracer.StartTrace(request);
        var exception = new InvalidOperationException("Test error");

        // Act
        tracer.RecordException(exception, "ErrorStep");
        var currentTrace = tracer.GetCurrentTrace();

        // Assert
        Assert.Same(exception, currentTrace!.Exception);
        Assert.Single(currentTrace.Steps, s => s.Category == "Exception");
        Assert.Equal("ErrorStep", currentTrace.Steps[0].Name);
        Assert.Same(exception, currentTrace.Steps[0].Exception);
    }

    [Fact]
    public void CompleteTrace_ShouldMoveTraceToCompleted()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        tracer.StartTrace(request);

        // Act
        tracer.CompleteTrace(success: true);

        // Assert
        Assert.Equal(0, tracer.ActiveTraceCount);
        Assert.Equal(1, tracer.CompletedTraceCount);
        Assert.Null(tracer.GetCurrentTrace());
    }

    [Fact]
    public void CompleteTrace_ShouldSetEndTime()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        var trace = tracer.StartTrace(request);
        var startTime = trace.StartTime;

        // Act
        tracer.CompleteTrace();
        var completedTraces = tracer.GetCompletedTraces();

        // Assert
        Assert.Single(completedTraces);
        var completedTrace = completedTraces.First();
        Assert.True(completedTrace.EndTime > startTime);
    }

    [Fact]
    public void CompleteTrace_ShouldSetFailedMetadata_WhenNotSuccessful()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        tracer.StartTrace(request);

        // Act
        tracer.CompleteTrace(success: false);
        var completedTraces = tracer.GetCompletedTraces();

        // Assert
        Assert.Single(completedTraces);
        var completedTrace = completedTraces.First();
        Assert.Contains("CompletedSuccessfully", completedTrace.Metadata);
        Assert.Equal(false, completedTrace.Metadata["CompletedSuccessfully"]);
    }

    [Fact]
    public void GetCompletedTraces_ShouldReturnEmpty_WhenNoTraces()
    {
        // Arrange
        var tracer = new RequestTracer();

        // Act
        var traces = tracer.GetCompletedTraces();

        // Assert
        Assert.Empty(traces);
    }

    [Fact]
    public void GetCompletedTraces_ShouldReturnAllTraces()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();

        tracer.StartTrace(request);
        tracer.CompleteTrace();

        tracer.StartTrace(request);
        tracer.CompleteTrace();

        // Act
        var traces = tracer.GetCompletedTraces();

        // Assert
        Assert.Equal(2, traces.Count());
    }

    [Fact]
    public void GetCompletedTraces_ShouldFilterBySince()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();

        tracer.StartTrace(request);
        tracer.CompleteTrace();

        var cutoffTime = DateTimeOffset.UtcNow.AddSeconds(1);

        System.Threading.Thread.Sleep(1100);

        tracer.StartTrace(request);
        tracer.CompleteTrace();

        // Act
        var traces = tracer.GetCompletedTraces(cutoffTime);

        // Assert
        Assert.Single(traces);
    }

    [Fact]
    public void GetCompletedTraces_ShouldReturnEmpty_WhenDisabled()
    {
        // Arrange
        var tracer = new RequestTracer { IsEnabled = false };

        // Act
        var traces = tracer.GetCompletedTraces();

        // Assert
        Assert.Empty(traces);
    }

    [Fact]
    public void ClearTraces_ShouldRemoveAllCompletedTraces()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();

        tracer.StartTrace(request);
        tracer.CompleteTrace();

        tracer.StartTrace(request);
        tracer.CompleteTrace();

        // Act
        tracer.ClearTraces();

        // Assert
        Assert.Empty(tracer.GetCompletedTraces());
    }

    [Fact]
    public void MaxCompletedTraces_ShouldLimitStoredTraces()
    {
        // Arrange
        var tracer = new RequestTracer { MaxCompletedTraces = 5 };
        var request = new TestRequest();

        // Act - create 10 traces
        for (int i = 0; i < 10; i++)
        {
            tracer.StartTrace(request);
            tracer.CompleteTrace();
        }

        // Assert
        Assert.Equal(5, tracer.GetCompletedTraces().Count());
    }

    [Fact]
    public void MaxCompletedTraces_ShouldNotAllowZeroOrNegative()
    {
        // Arrange
        var tracer = new RequestTracer();

        // Act
        tracer.MaxCompletedTraces = -1;

        // Assert
        Assert.Equal(1, tracer.MaxCompletedTraces);
    }

    [Fact]
    public void AddStep_ShouldIncludeMetadata()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        tracer.StartTrace(request);
        var metadata = new { Key = "Value", Count = 42 };

        // Act
        tracer.AddStep("TestStep", TimeSpan.FromMilliseconds(100), "TestCategory", metadata);
        var currentTrace = tracer.GetCurrentTrace();

        // Assert
        Assert.Same(metadata, currentTrace!.Steps[0].Metadata);
    }

    [Fact]
    public void AddHandlerStep_ShouldIncludeMetadata()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();
        tracer.StartTrace(request);
        var metadata = new { HandlerName = "TestHandler" };

        // Act
        tracer.AddHandlerStep("HandlerStep", TimeSpan.FromMilliseconds(100), typeof(TestRequest), "Handler", metadata);
        var currentTrace = tracer.GetCurrentTrace();

        // Assert
        Assert.Same(metadata, currentTrace!.Steps[0].Metadata);
    }

    [Fact]
    public void StartTrace_ShouldAddRequestMetadata()
    {
        // Arrange
        var tracer = new RequestTracer();
        var request = new TestRequest();

        // Act
        var trace = tracer.StartTrace(request);

        // Assert
        Assert.Contains("RequestTypeName", trace.Metadata);
        Assert.Equal("TestRequest", trace.Metadata["RequestTypeName"]);
    }
}

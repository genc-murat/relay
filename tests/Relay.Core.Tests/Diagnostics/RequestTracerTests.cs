using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics;
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
        tracer.IsEnabled.Should().BeTrue();
        tracer.ActiveTraceCount.Should().Be(0);
        tracer.CompletedTraceCount.Should().Be(0);
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
        trace.Should().NotBeNull();
        trace.RequestId.Should().NotBe(Guid.Empty);
        trace.RequestType.Should().Be(typeof(TestRequest));
        trace.StartTime.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        tracer.ActiveTraceCount.Should().Be(1);
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
        trace.CorrelationId.Should().Be(correlationId);
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
        trace.CorrelationId.Should().NotBeNullOrWhiteSpace();
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
        trace.RequestId.Should().Be(Guid.Empty);
        tracer.ActiveTraceCount.Should().Be(0);
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
        currentTrace.Should().NotBeNull();
        currentTrace!.RequestId.Should().Be(startedTrace.RequestId);
    }

    [Fact]
    public void GetCurrentTrace_ShouldReturnNull_WhenNoActiveTrace()
    {
        // Arrange
        var tracer = new RequestTracer();

        // Act
        var currentTrace = tracer.GetCurrentTrace();

        // Assert
        currentTrace.Should().BeNull();
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
        currentTrace.Should().BeNull();
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
        currentTrace!.Steps.Should().HaveCount(1);
        currentTrace.Steps[0].Name.Should().Be("TestStep");
        currentTrace.Steps[0].Duration.Should().Be(TimeSpan.FromMilliseconds(100));
        currentTrace.Steps[0].Category.Should().Be("TestCategory");
    }

    [Fact]
    public void AddStep_ShouldNotAddStep_WhenDisabled()
    {
        // Arrange
        var tracer = new RequestTracer { IsEnabled = false };

        // Act
        tracer.AddStep("TestStep", TimeSpan.FromMilliseconds(100));

        // Assert - should not throw
        tracer.GetCurrentTrace().Should().BeNull();
    }

    [Fact]
    public void AddStep_ShouldNotAddStep_WhenNoActiveTrace()
    {
        // Arrange
        var tracer = new RequestTracer();

        // Act
        tracer.AddStep("TestStep", TimeSpan.FromMilliseconds(100));

        // Assert - should not throw
        tracer.GetCurrentTrace().Should().BeNull();
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
        currentTrace!.Steps.Should().HaveCount(1);
        currentTrace.Steps[0].Name.Should().Be("HandlerExecution");
        currentTrace.Steps[0].HandlerType.Should().Be("TestRequest");
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
        currentTrace!.Exception.Should().Be(exception);
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
        currentTrace!.Exception.Should().Be(exception);
        currentTrace.Steps.Should().ContainSingle(s => s.Category == "Exception");
        currentTrace.Steps[0].Name.Should().Be("ErrorStep");
        currentTrace.Steps[0].Exception.Should().Be(exception);
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
        tracer.ActiveTraceCount.Should().Be(0);
        tracer.CompletedTraceCount.Should().Be(1);
        tracer.GetCurrentTrace().Should().BeNull();
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
        completedTraces.Should().HaveCount(1);
        var completedTrace = completedTraces.First();
        completedTrace.EndTime.Should().BeAfter(startTime);
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
        completedTraces.Should().HaveCount(1);
        var completedTrace = completedTraces.First();
        completedTrace.Metadata.Should().ContainKey("CompletedSuccessfully");
        completedTrace.Metadata["CompletedSuccessfully"].Should().Be(false);
    }

    [Fact]
    public void GetCompletedTraces_ShouldReturnEmpty_WhenNoTraces()
    {
        // Arrange
        var tracer = new RequestTracer();

        // Act
        var traces = tracer.GetCompletedTraces();

        // Assert
        traces.Should().BeEmpty();
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
        traces.Should().HaveCount(2);
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
        traces.Should().HaveCount(1);
    }

    [Fact]
    public void GetCompletedTraces_ShouldReturnEmpty_WhenDisabled()
    {
        // Arrange
        var tracer = new RequestTracer { IsEnabled = false };

        // Act
        var traces = tracer.GetCompletedTraces();

        // Assert
        traces.Should().BeEmpty();
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
        tracer.GetCompletedTraces().Should().BeEmpty();
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
        tracer.GetCompletedTraces().Should().HaveCount(5);
    }

    [Fact]
    public void MaxCompletedTraces_ShouldNotAllowZeroOrNegative()
    {
        // Arrange
        var tracer = new RequestTracer();

        // Act
        tracer.MaxCompletedTraces = -1;

        // Assert
        tracer.MaxCompletedTraces.Should().Be(1);
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
        currentTrace!.Steps[0].Metadata.Should().Be(metadata);
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
        currentTrace!.Steps[0].Metadata.Should().Be(metadata);
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
        trace.Metadata.Should().ContainKey("RequestTypeName");
        trace.Metadata["RequestTypeName"].Should().Be("TestRequest");
    }
}

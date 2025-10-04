using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Relay.Core.Diagnostics;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

public class TraceFormatterTests
{
    private class TestRequest : IRequest
    {
    }

    private RequestTrace CreateSampleTrace(bool completed = true, bool hasException = false)
    {
        var trace = new RequestTrace
        {
            RequestId = Guid.NewGuid(),
            RequestType = typeof(TestRequest),
            StartTime = DateTimeOffset.UtcNow.AddSeconds(-5),
            CorrelationId = "test-123",
            Steps = new List<TraceStep>
            {
                new TraceStep
                {
                    Name = "Step1",
                    Timestamp = DateTimeOffset.UtcNow.AddSeconds(-4),
                    Duration = TimeSpan.FromMilliseconds(100),
                    Category = "Handler",
                    HandlerType = "TestHandler"
                },
                new TraceStep
                {
                    Name = "Step2",
                    Timestamp = DateTimeOffset.UtcNow.AddSeconds(-3),
                    Duration = TimeSpan.FromMilliseconds(150),
                    Category = "Pipeline"
                }
            }
        };

        if (completed)
        {
            trace.EndTime = DateTimeOffset.UtcNow;
        }

        if (hasException)
        {
            trace.Exception = new InvalidOperationException("Test exception");
        }

        trace.Metadata["TestKey"] = "TestValue";

        return trace;
    }

    [Fact]
    public void FormatTrace_ShouldReturnMessage_WhenTraceIsNull()
    {
        // Act
        var result = TraceFormatter.FormatTrace(null!);

        // Assert
        result.Should().Be("No trace available");
    }

    [Fact]
    public void FormatTrace_ShouldIncludeBasicInformation()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        result.Should().Contain("Request Trace: TestRequest");
        result.Should().Contain($"Request ID: {trace.RequestId}");
        result.Should().Contain($"Correlation ID: {trace.CorrelationId}");
        result.Should().Contain("Status: Success");
    }

    [Fact]
    public void FormatTrace_ShouldShowInProgress_WhenNotCompleted()
    {
        // Arrange
        var trace = CreateSampleTrace(completed: false);

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        result.Should().Contain("Status: In Progress");
        result.Should().NotContain("End Time:");
        result.Should().NotContain("Total Duration:");
    }

    [Fact]
    public void FormatTrace_ShouldShowFailed_WhenHasException()
    {
        // Arrange
        var trace = CreateSampleTrace(hasException: true);

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        result.Should().Contain("Status: Failed");
        result.Should().Contain("Exception: InvalidOperationException - Test exception");
    }

    [Fact]
    public void FormatTrace_ShouldIncludeSteps()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        result.Should().Contain("Execution Steps:");
        result.Should().Contain("1. [Handler] Step1");
        result.Should().Contain("2. [Pipeline] Step2");
        result.Should().Contain("Duration: 100");
        result.Should().Contain("Duration: 150");
    }

    [Fact]
    public void FormatTrace_ShouldIncludeHandlerType_WhenPresent()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        result.Should().Contain("Handler: TestHandler");
    }

    [Fact]
    public void FormatTrace_ShouldIncludeMetadata_WhenRequested()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace, includeMetadata: true);

        // Assert
        result.Should().Contain("Request Metadata:");
        result.Should().Contain("TestKey:");
        result.Should().Contain("TestValue");
    }

    [Fact]
    public void FormatTrace_ShouldNotIncludeMetadata_WhenNotRequested()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace, includeMetadata: false);

        // Assert
        result.Should().NotContain("Request Metadata:");
    }

    [Fact]
    public void FormatTrace_ShouldIncludeStepException()
    {
        // Arrange
        var trace = CreateSampleTrace();
        trace.Steps[0].Exception = new ArgumentException("Step error");

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        result.Should().Contain("Exception: ArgumentException - Step error");
    }

    [Fact]
    public void FormatTrace_ShouldCalculateTotalStepTime()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        result.Should().Contain("Total Step Time: 250");
    }

    [Fact]
    public void FormatTraceAsJson_ShouldReturnNull_WhenTraceIsNull()
    {
        // Act
        var result = TraceFormatter.FormatTraceAsJson(null!);

        // Assert
        result.Should().Be("null");
    }

    [Fact]
    public void FormatTraceAsJson_ShouldReturnValidJson()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTraceAsJson(trace);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("requestId");
        result.Should().Contain("requestType");
        result.Should().Contain("steps");
    }

    [Fact]
    public void FormatTraceAsJson_ShouldIncludeException_WhenPresent()
    {
        // Arrange
        var trace = CreateSampleTrace(hasException: true);

        // Act
        var result = TraceFormatter.FormatTraceAsJson(trace);

        // Assert
        result.Should().Contain("exception");
        result.Should().Contain("InvalidOperationException");
        result.Should().Contain("Test exception");
    }

    [Fact]
    public void FormatTraceAsJson_ShouldSupportCompactFormat()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTraceAsJson(trace, indented: false);

        // Assert
        result.Should().NotContain("\n");
        result.Should().NotContain("  ");
    }

    [Fact]
    public void FormatTraceAsJson_ShouldSupportIndentedFormat()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTraceAsJson(trace, indented: true);

        // Assert
        result.Should().Contain("\n");
    }

    [Fact]
    public void FormatTraceSummary_ShouldReturnMessage_WhenNoTraces()
    {
        // Arrange
        var traces = Enumerable.Empty<RequestTrace>();

        // Act
        var result = TraceFormatter.FormatTraceSummary(traces);

        // Assert
        result.Should().Be("No traces available");
    }

    [Fact]
    public void FormatTraceSummary_ShouldIncludeTotalCount()
    {
        // Arrange
        var traces = new[]
        {
            CreateSampleTrace(),
            CreateSampleTrace(),
            CreateSampleTrace(completed: false)
        };

        // Act
        var result = TraceFormatter.FormatTraceSummary(traces);

        // Assert
        result.Should().Contain("Trace Summary (3 traces)");
        result.Should().Contain("Completed: 2");
        result.Should().Contain("In Progress: 1");
    }

    [Fact]
    public void FormatTraceSummary_ShouldIncludeSuccessFailedCounts()
    {
        // Arrange
        var traces = new[]
        {
            CreateSampleTrace(completed: true, hasException: false),
            CreateSampleTrace(completed: true, hasException: false),
            CreateSampleTrace(completed: true, hasException: true)
        };

        // Act
        var result = TraceFormatter.FormatTraceSummary(traces);

        // Assert
        result.Should().Contain("Successful: 2");
        result.Should().Contain("Failed: 1");
    }

    [Fact]
    public void FormatTraceSummary_ShouldIncludePerformanceSummary()
    {
        // Arrange
        var traces = new[]
        {
            CreateSampleTrace(),
            CreateSampleTrace()
        };

        // Act
        var result = TraceFormatter.FormatTraceSummary(traces);

        // Assert
        result.Should().Contain("Performance Summary:");
        result.Should().Contain("Average Duration:");
        result.Should().Contain("Min Duration:");
        result.Should().Contain("Max Duration:");
    }

    [Fact]
    public void FormatTraceSummary_ShouldIncludeRequestTypeBreakdown()
    {
        // Arrange
        var traces = new[]
        {
            CreateSampleTrace(),
            CreateSampleTrace(),
            CreateSampleTrace()
        };

        // Act
        var result = TraceFormatter.FormatTraceSummary(traces);

        // Assert
        result.Should().Contain("Request Types:");
        result.Should().Contain("TestRequest: 3");
    }

    [Fact]
    public void FormatTraceSummary_ShouldHandleMultipleRequestTypes()
    {
        // Arrange
        var trace1 = CreateSampleTrace();
        var trace2 = CreateSampleTrace();
        trace2.RequestType = typeof(string); // Different type

        var traces = new[] { trace1, trace2 };

        // Act
        var result = TraceFormatter.FormatTraceSummary(traces);

        // Assert
        result.Should().Contain("TestRequest:");
        result.Should().Contain("String:");
    }

    [Fact]
    public void FormatTrace_ShouldHandleEmptySteps()
    {
        // Arrange
        var trace = CreateSampleTrace();
        trace.Steps.Clear();

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        result.Should().NotContain("Execution Steps:");
    }

    [Fact]
    public void FormatTrace_ShouldIncludeStepMetadata_WhenRequested()
    {
        // Arrange
        var trace = CreateSampleTrace();
        trace.Steps[0].Metadata = new { Key = "Value", Count = 123 };

        // Act
        var result = TraceFormatter.FormatTrace(trace, includeMetadata: true);

        // Assert
        result.Should().Contain("Metadata:");
    }

    [Fact]
    public void FormatTraceAsJson_ShouldIncludeStepMetadata()
    {
        // Arrange
        var trace = CreateSampleTrace();
        trace.Steps[0].Metadata = new { TestData = "Value" };

        // Act
        var result = TraceFormatter.FormatTraceAsJson(trace);

        // Assert
        result.Should().Contain("\"metadata\"");
    }

    [Fact]
    public void FormatTraceSummary_ShouldNotIncludePerformance_WhenNoCompletedTraces()
    {
        // Arrange
        var traces = new[]
        {
            CreateSampleTrace(completed: false),
            CreateSampleTrace(completed: false)
        };

        // Act
        var result = TraceFormatter.FormatTraceSummary(traces);

        // Assert
        result.Should().NotContain("Performance Summary:");
    }

    [Fact]
    public void FormatTraceAsJson_ShouldIncludeStepException()
    {
        // Arrange
        var trace = CreateSampleTrace();
        trace.Steps[0].Exception = new ArgumentException("Step error");

        // Act
        var result = TraceFormatter.FormatTraceAsJson(trace);

        // Assert
        result.Should().Contain("ArgumentException");
        result.Should().Contain("Step error");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics;
using Relay.Core.Diagnostics.Tracing;
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
        Assert.Equal("No trace available", result);
    }

    [Fact]
    public void FormatTrace_ShouldIncludeBasicInformation()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        Assert.Contains("Request Trace: TestRequest", result);
        Assert.Contains($"Request ID: {trace.RequestId}", result);
        Assert.Contains($"Correlation ID: {trace.CorrelationId}", result);
        Assert.Contains("Status: Success", result);
    }

    [Fact]
    public void FormatTrace_ShouldShowInProgress_WhenNotCompleted()
    {
        // Arrange
        var trace = CreateSampleTrace(completed: false);

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        Assert.Contains("Status: In Progress", result);
        Assert.DoesNotContain("End Time:", result);
        Assert.DoesNotContain("Total Duration:", result);
    }

    [Fact]
    public void FormatTrace_ShouldShowFailed_WhenHasException()
    {
        // Arrange
        var trace = CreateSampleTrace(hasException: true);

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        Assert.Contains("Status: Failed", result);
        Assert.Contains("Exception: InvalidOperationException - Test exception", result);
    }

    [Fact]
    public void FormatTrace_ShouldIncludeSteps()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        Assert.Contains("Execution Steps:", result);
        Assert.Contains("1. [Handler] Step1", result);
        Assert.Contains("2. [Pipeline] Step2", result);
        Assert.Contains("Duration: 100", result);
        Assert.Contains("Duration: 150", result);
    }

    [Fact]
    public void FormatTrace_ShouldIncludeHandlerType_WhenPresent()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        Assert.Contains("Handler: TestHandler", result);
    }

    [Fact]
    public void FormatTrace_ShouldIncludeMetadata_WhenRequested()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace, includeMetadata: true);

        // Assert
        Assert.Contains("Request Metadata:", result);
        Assert.Contains("TestKey:", result);
        Assert.Contains("TestValue", result);
    }

    [Fact]
    public void FormatTrace_ShouldNotIncludeMetadata_WhenNotRequested()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace, includeMetadata: false);

        // Assert
        Assert.DoesNotContain("Request Metadata:", result);
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
        Assert.Contains("Exception: ArgumentException - Step error", result);
    }

    [Fact]
    public void FormatTrace_ShouldCalculateTotalStepTime()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTrace(trace);

        // Assert
        Assert.Contains("Total Step Time: 250", result);
    }

    [Fact]
    public void FormatTraceAsJson_ShouldReturnNull_WhenTraceIsNull()
    {
        // Act
        var result = TraceFormatter.FormatTraceAsJson(null!);

        // Assert
        Assert.Equal("null", result);
    }

    [Fact]
    public void FormatTraceAsJson_ShouldReturnValidJson()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTraceAsJson(trace);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Contains("requestId", result);
        Assert.Contains("requestType", result);
        Assert.Contains("steps", result);
    }

    [Fact]
    public void FormatTraceAsJson_ShouldIncludeException_WhenPresent()
    {
        // Arrange
        var trace = CreateSampleTrace(hasException: true);

        // Act
        var result = TraceFormatter.FormatTraceAsJson(trace);

        // Assert
        Assert.Contains("exception", result);
        Assert.Contains("InvalidOperationException", result);
        Assert.Contains("Test exception", result);
    }

    [Fact]
    public void FormatTraceAsJson_ShouldSupportCompactFormat()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTraceAsJson(trace, indented: false);

        // Assert
        Assert.DoesNotContain("\n", result);
        Assert.DoesNotContain("  ", result);
    }

    [Fact]
    public void FormatTraceAsJson_ShouldSupportIndentedFormat()
    {
        // Arrange
        var trace = CreateSampleTrace();

        // Act
        var result = TraceFormatter.FormatTraceAsJson(trace, indented: true);

        // Assert
        Assert.Contains("\n", result);
    }

    [Fact]
    public void FormatTraceSummary_ShouldReturnMessage_WhenNoTraces()
    {
        // Arrange
        var traces = Enumerable.Empty<RequestTrace>();

        // Act
        var result = TraceFormatter.FormatTraceSummary(traces);

        // Assert
        Assert.Equal("No traces available", result);
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
        Assert.Contains("Trace Summary (3 traces)", result);
        Assert.Contains("Completed: 2", result);
        Assert.Contains("In Progress: 1", result);
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
        Assert.Contains("Successful: 2", result);
        Assert.Contains("Failed: 1", result);
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
        Assert.Contains("Performance Summary:", result);
        Assert.Contains("Average Duration:", result);
        Assert.Contains("Min Duration:", result);
        Assert.Contains("Max Duration:", result);
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
        Assert.Contains("Request Types:", result);
        Assert.Contains("TestRequest: 3", result);
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
        Assert.Contains("TestRequest:", result);
        Assert.Contains("String:", result);
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
        Assert.DoesNotContain("Execution Steps:", result);
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
        Assert.Contains("Metadata:", result);
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
        Assert.Contains("\"metadata\"", result);
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
        Assert.DoesNotContain("Performance Summary:", result);
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
        Assert.Contains("ArgumentException", result);
        Assert.Contains("Step error", result);
    }
}

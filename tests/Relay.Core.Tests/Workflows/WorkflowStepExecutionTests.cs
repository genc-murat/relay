using System;
using System.Collections.Generic;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class WorkflowStepExecutionTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var execution = new WorkflowStepExecution();

        // Assert
        Assert.NotNull(execution);
        Assert.Equal(string.Empty, execution.StepName);
        Assert.Equal(StepStatus.Running, execution.Status);
        Assert.Equal(default, execution.StartedAt);
        Assert.Null(execution.CompletedAt);
        Assert.Null(execution.Output);
        Assert.Null(execution.Error);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var execution = new WorkflowStepExecution();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(5);
        var output = new { Result = "Success", Count = 42 };

        // Act
        execution.StepName = "TestStep";
        execution.Status = StepStatus.Completed;
        execution.StartedAt = startTime;
        execution.CompletedAt = endTime;
        execution.Output = output;
        execution.Error = null;

        // Assert
        Assert.Equal("TestStep", execution.StepName);
        Assert.Equal(StepStatus.Completed, execution.Status);
        Assert.Equal(startTime, execution.StartedAt);
        Assert.Equal(endTime, execution.CompletedAt);
        Assert.Equal(output, execution.Output);
        Assert.Null(execution.Error);
    }

    [Fact]
    public void StepStatus_ShouldSupportAllStatuses()
    {
        // Arrange & Act
        var runningExecution = new WorkflowStepExecution { Status = StepStatus.Running };
        var completedExecution = new WorkflowStepExecution { Status = StepStatus.Completed };
        var failedExecution = new WorkflowStepExecution { Status = StepStatus.Failed };
        var skippedExecution = new WorkflowStepExecution { Status = StepStatus.Skipped };

        // Assert
        Assert.Equal(StepStatus.Running, runningExecution.Status);
        Assert.Equal(StepStatus.Completed, completedExecution.Status);
        Assert.Equal(StepStatus.Failed, failedExecution.Status);
        Assert.Equal(StepStatus.Skipped, skippedExecution.Status);
    }

    [Fact]
    public void SuccessfulExecution_ShouldHaveNoError()
    {
        // Arrange & Act
        var execution = new WorkflowStepExecution
        {
            StepName = "SuccessfulStep",
            Status = StepStatus.Completed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddSeconds(2),
            Output = new { Message = "Success" }
        };

        // Assert
        Assert.Equal(StepStatus.Completed, execution.Status);
        Assert.Null(execution.Error);
        Assert.NotNull(execution.Output);
        Assert.NotNull(execution.CompletedAt);
    }

    [Fact]
    public void FailedExecution_ShouldCaptureErrorDetails()
    {
        // Arrange & Act
        var execution = new WorkflowStepExecution
        {
            StepName = "FailedStep",
            Status = StepStatus.Failed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddSeconds(1),
            Error = "Connection timeout after 30 seconds"
        };

        // Assert
        Assert.Equal(StepStatus.Failed, execution.Status);
        Assert.NotNull(execution.Error);
        Assert.Contains("timeout", execution.Error);
        Assert.NotNull(execution.CompletedAt);
    }

    [Fact]
    public void RunningExecution_ShouldNotHaveCompletionTime()
    {
        // Arrange & Act
        var execution = new WorkflowStepExecution
        {
            StepName = "RunningStep",
            Status = StepStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(StepStatus.Running, execution.Status);
        Assert.Null(execution.CompletedAt);
        Assert.Null(execution.Error);
    }

    [Fact]
    public void SkippedExecution_ShouldHaveSkippedStatus()
    {
        // Arrange & Act
        var execution = new WorkflowStepExecution
        {
            StepName = "SkippedStep",
            Status = StepStatus.Skipped,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal(StepStatus.Skipped, execution.Status);
        Assert.NotNull(execution.CompletedAt);
    }

    [Fact]
    public void Output_ShouldSupportDifferentTypes()
    {
        // Arrange & Act
        var stringOutputExecution = new WorkflowStepExecution
        {
            StepName = "StringStep",
            Output = "Simple string output"
        };

        var numberOutputExecution = new WorkflowStepExecution
        {
            StepName = "NumberStep",
            Output = 42
        };

        var objectOutputExecution = new WorkflowStepExecution
        {
            StepName = "ObjectStep",
            Output = new { Name = "Test", Value = 123 }
        };

        var arrayOutputExecution = new WorkflowStepExecution
        {
            StepName = "ArrayStep",
            Output = new[] { 1, 2, 3, 4, 5 }
        };

        // Assert
        Assert.IsType<string>(stringOutputExecution.Output);
        Assert.IsType<int>(numberOutputExecution.Output);
        Assert.NotNull(objectOutputExecution.Output);
        Assert.IsType<int[]>(arrayOutputExecution.Output);
    }

    [Fact]
    public void Execution_ShouldTrackDuration()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddSeconds(3);

        // Act
        var execution = new WorkflowStepExecution
        {
            StepName = "TimedStep",
            Status = StepStatus.Completed,
            StartedAt = startTime,
            CompletedAt = endTime
        };

        // Assert
        Assert.NotNull(execution.CompletedAt);
        var duration = execution.CompletedAt.Value - execution.StartedAt;
        Assert.Equal(3, duration.TotalSeconds, precision: 0);
    }

    [Fact]
    public void MultipleExecutions_ShouldBeIndependent()
    {
        // Arrange & Act
        var execution1 = new WorkflowStepExecution
        {
            StepName = "Step1",
            Status = StepStatus.Completed,
            Output = "Result1"
        };

        var execution2 = new WorkflowStepExecution
        {
            StepName = "Step2",
            Status = StepStatus.Failed,
            Error = "Error2"
        };

        var execution3 = new WorkflowStepExecution
        {
            StepName = "Step3",
            Status = StepStatus.Running
        };

        // Assert
        Assert.Equal("Step1", execution1.StepName);
        Assert.Equal(StepStatus.Completed, execution1.Status);
        Assert.Equal("Result1", execution1.Output);

        Assert.Equal("Step2", execution2.StepName);
        Assert.Equal(StepStatus.Failed, execution2.Status);
        Assert.Equal("Error2", execution2.Error);

        Assert.Equal("Step3", execution3.StepName);
        Assert.Equal(StepStatus.Running, execution3.Status);
        Assert.Null(execution3.Output);
        Assert.Null(execution3.Error);
    }

    [Fact]
    public void Execution_WithComplexOutput_ShouldPreserveStructure()
    {
        // Arrange
        var complexOutput = new ComplexOutput
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Data = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 },
                { "key3", true }
            },
            Items = new[] { "item1", "item2", "item3" }
        };

        // Act
        var execution = new WorkflowStepExecution
        {
            StepName = "ComplexStep",
            Status = StepStatus.Completed,
            Output = complexOutput
        };

        // Assert
        Assert.NotNull(execution.Output);
        var retrievedOutput = execution.Output as ComplexOutput;
        Assert.NotNull(retrievedOutput);
        Assert.NotNull(retrievedOutput.Id);
        Assert.Equal(3, retrievedOutput.Data.Count);
        Assert.Equal(3, retrievedOutput.Items.Length);
    }

    [Fact]
    public void Execution_Lifecycle_ShouldTransitionCorrectly()
    {
        // Arrange
        var execution = new WorkflowStepExecution
        {
            StepName = "LifecycleStep",
            Status = StepStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        // Act - Simulate successful completion
        Assert.Equal(StepStatus.Running, execution.Status);
        Assert.Null(execution.CompletedAt);

        execution.Status = StepStatus.Completed;
        execution.CompletedAt = DateTime.UtcNow.AddSeconds(2);
        execution.Output = new { Result = "Success" };

        // Assert
        Assert.Equal(StepStatus.Completed, execution.Status);
        Assert.NotNull(execution.CompletedAt);
        Assert.NotNull(execution.Output);
        Assert.Null(execution.Error);
    }

    [Fact]
    public void Execution_FailureScenario_ShouldCaptureTimingAndError()
    {
        // Arrange
        var startTime = DateTime.UtcNow;

        // Act
        var execution = new WorkflowStepExecution
        {
            StepName = "FailingStep",
            Status = StepStatus.Running,
            StartedAt = startTime
        };

        // Simulate failure after 1 second
        execution.Status = StepStatus.Failed;
        execution.CompletedAt = startTime.AddSeconds(1);
        execution.Error = "Network connection lost";

        // Assert
        Assert.Equal(StepStatus.Failed, execution.Status);
        Assert.NotNull(execution.CompletedAt);
        Assert.NotNull(execution.Error);
        Assert.Contains("Network", execution.Error);
        var duration = execution.CompletedAt.Value - execution.StartedAt;
        Assert.Equal(1, duration.TotalSeconds, precision: 0);
    }

    private class ComplexOutput
    {
        public string Id { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();
        public string[] Items { get; set; } = Array.Empty<string>();
    }
}

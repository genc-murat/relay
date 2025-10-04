using System;
using System.Collections.Generic;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class WorkflowExecutionTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var execution = new WorkflowExecution();

        // Assert
        Assert.NotNull(execution);
        Assert.Equal(string.Empty, execution.Id);
        Assert.Equal(string.Empty, execution.WorkflowDefinitionId);
        Assert.Equal(WorkflowStatus.Running, execution.Status);
        Assert.Equal(default, execution.StartedAt);
        Assert.Null(execution.CompletedAt);
        Assert.Null(execution.Input);
        Assert.Null(execution.Output);
        Assert.Null(execution.Error);
        Assert.Equal(0, execution.CurrentStepIndex);
        Assert.NotNull(execution.Context);
        Assert.Empty(execution.Context);
        Assert.NotNull(execution.StepExecutions);
        Assert.Empty(execution.StepExecutions);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var execution = new WorkflowExecution();
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(5);
        var input = new { Name = "Test" };
        var output = new { Result = "Success" };

        // Act
        execution.Id = "exec-123";
        execution.WorkflowDefinitionId = "workflow-456";
        execution.Status = WorkflowStatus.Completed;
        execution.StartedAt = startTime;
        execution.CompletedAt = endTime;
        execution.Input = input;
        execution.Output = output;
        execution.Error = "Some error";
        execution.CurrentStepIndex = 5;

        // Assert
        Assert.Equal("exec-123", execution.Id);
        Assert.Equal("workflow-456", execution.WorkflowDefinitionId);
        Assert.Equal(WorkflowStatus.Completed, execution.Status);
        Assert.Equal(startTime, execution.StartedAt);
        Assert.Equal(endTime, execution.CompletedAt);
        Assert.Equal(input, execution.Input);
        Assert.Equal(output, execution.Output);
        Assert.Equal("Some error", execution.Error);
        Assert.Equal(5, execution.CurrentStepIndex);
    }

    [Fact]
    public void Context_ShouldBeModifiable()
    {
        // Arrange
        var execution = new WorkflowExecution();

        // Act
        execution.Context["key1"] = "value1";
        execution.Context["key2"] = 42;
        execution.Context["key3"] = new { Nested = "object" };

        // Assert
        Assert.Equal(3, execution.Context.Count);
        Assert.Equal("value1", execution.Context["key1"]);
        Assert.Equal(42, execution.Context["key2"]);
        Assert.NotNull(execution.Context["key3"]);
    }

    [Fact]
    public void StepExecutions_ShouldBeModifiable()
    {
        // Arrange
        var execution = new WorkflowExecution();

        // Act
        execution.StepExecutions.Add(new WorkflowStepExecution
        {
            StepName = "Step1",
            Status = StepStatus.Completed
        });
        execution.StepExecutions.Add(new WorkflowStepExecution
        {
            StepName = "Step2",
            Status = StepStatus.Running
        });

        // Assert
        Assert.Equal(2, execution.StepExecutions.Count);
        Assert.Equal("Step1", execution.StepExecutions[0].StepName);
        Assert.Equal(StepStatus.Completed, execution.StepExecutions[0].Status);
        Assert.Equal("Step2", execution.StepExecutions[1].StepName);
        Assert.Equal(StepStatus.Running, execution.StepExecutions[1].Status);
    }

    [Fact]
    public void WorkflowStatus_ShouldSupportAllStatuses()
    {
        // Arrange & Act
        var runningExecution = new WorkflowExecution { Status = WorkflowStatus.Running };
        var completedExecution = new WorkflowExecution { Status = WorkflowStatus.Completed };
        var failedExecution = new WorkflowExecution { Status = WorkflowStatus.Failed };
        var cancelledExecution = new WorkflowExecution { Status = WorkflowStatus.Cancelled };

        // Assert
        Assert.Equal(WorkflowStatus.Running, runningExecution.Status);
        Assert.Equal(WorkflowStatus.Completed, completedExecution.Status);
        Assert.Equal(WorkflowStatus.Failed, failedExecution.Status);
        Assert.Equal(WorkflowStatus.Cancelled, cancelledExecution.Status);
    }

    [Fact]
    public void Execution_WithComplexInput_ShouldPreserveData()
    {
        // Arrange
        var complexInput = new ComplexInput
        {
            Id = 123,
            Name = "Test",
            Tags = new List<string> { "tag1", "tag2" },
            Metadata = new Dictionary<string, object>
            {
                { "key1", "value1" },
                { "key2", 42 }
            }
        };

        // Act
        var execution = new WorkflowExecution
        {
            Id = "exec-1",
            WorkflowDefinitionId = "workflow-1",
            Input = complexInput
        };

        // Assert
        Assert.NotNull(execution.Input);
        var retrievedInput = execution.Input as ComplexInput;
        Assert.NotNull(retrievedInput);
        Assert.Equal(123, retrievedInput.Id);
        Assert.Equal("Test", retrievedInput.Name);
        Assert.Equal(2, retrievedInput.Tags.Count);
        Assert.Equal(2, retrievedInput.Metadata.Count);
    }

    [Fact]
    public void Execution_ShouldTrackLifecycle()
    {
        // Arrange
        var execution = new WorkflowExecution
        {
            Id = "exec-1",
            WorkflowDefinitionId = "workflow-1",
            Status = WorkflowStatus.Running,
            StartedAt = DateTime.UtcNow
        };

        // Act - Simulate workflow progression
        execution.CurrentStepIndex = 0;
        execution.StepExecutions.Add(new WorkflowStepExecution
        {
            StepName = "Step1",
            Status = StepStatus.Completed,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddSeconds(2)
        });

        execution.CurrentStepIndex = 1;
        execution.StepExecutions.Add(new WorkflowStepExecution
        {
            StepName = "Step2",
            Status = StepStatus.Completed,
            StartedAt = DateTime.UtcNow.AddSeconds(2),
            CompletedAt = DateTime.UtcNow.AddSeconds(4)
        });

        execution.Status = WorkflowStatus.Completed;
        execution.CompletedAt = DateTime.UtcNow.AddSeconds(4);

        // Assert
        Assert.Equal(WorkflowStatus.Completed, execution.Status);
        Assert.NotNull(execution.CompletedAt);
        Assert.Equal(2, execution.StepExecutions.Count);
        Assert.Equal(1, execution.CurrentStepIndex);
        Assert.All(execution.StepExecutions, step =>
        {
            Assert.Equal(StepStatus.Completed, step.Status);
            Assert.NotNull(step.CompletedAt);
        });
    }

    [Fact]
    public void Execution_WithFailure_ShouldCaptureErrorDetails()
    {
        // Arrange & Act
        var execution = new WorkflowExecution
        {
            Id = "exec-1",
            WorkflowDefinitionId = "workflow-1",
            Status = WorkflowStatus.Failed,
            Error = "Step 2 failed: Connection timeout",
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow.AddSeconds(5)
        };

        execution.StepExecutions.Add(new WorkflowStepExecution
        {
            StepName = "Step1",
            Status = StepStatus.Completed
        });

        execution.StepExecutions.Add(new WorkflowStepExecution
        {
            StepName = "Step2",
            Status = StepStatus.Failed,
            Error = "Connection timeout"
        });

        // Assert
        Assert.Equal(WorkflowStatus.Failed, execution.Status);
        Assert.NotNull(execution.Error);
        Assert.Contains("timeout", execution.Error);
        Assert.Equal(StepStatus.Failed, execution.StepExecutions[1].Status);
        Assert.NotNull(execution.StepExecutions[1].Error);
    }

    [Fact]
    public void Context_ShouldSupportDifferentValueTypes()
    {
        // Arrange
        var execution = new WorkflowExecution();

        // Act
        execution.Context["string"] = "value";
        execution.Context["int"] = 42;
        execution.Context["bool"] = true;
        execution.Context["double"] = 3.14;
        execution.Context["datetime"] = DateTime.UtcNow;
        execution.Context["object"] = new { Name = "Test" };
        execution.Context["list"] = new List<string> { "a", "b", "c" };

        // Assert
        Assert.Equal(7, execution.Context.Count);
        Assert.IsType<string>(execution.Context["string"]);
        Assert.IsType<int>(execution.Context["int"]);
        Assert.IsType<bool>(execution.Context["bool"]);
        Assert.IsType<double>(execution.Context["double"]);
        Assert.IsType<DateTime>(execution.Context["datetime"]);
    }

    private class ComplexInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}

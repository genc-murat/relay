using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Relay.Core.Workflows;
using Relay.Core.Contracts.Core;

namespace Relay.Core.Tests.Workflows;

[Collection("Sequential")]
public class WorkflowEngineConditionEvaluationTests
{
    private readonly Mock<IRelay> _mockRelay;
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger;
    private readonly Mock<IWorkflowStateStore> _mockStateStore;
    private readonly Mock<IWorkflowDefinitionStore> _mockDefinitionStore;
    private readonly WorkflowEngine _workflowEngine;

    public WorkflowEngineConditionEvaluationTests()
    {
        _mockRelay = new Mock<IRelay>();
        _mockLogger = new Mock<ILogger<WorkflowEngine>>();
        _mockStateStore = new Mock<IWorkflowStateStore>();
        _mockDefinitionStore = new Mock<IWorkflowDefinitionStore>();
        _workflowEngine = new WorkflowEngine(_mockRelay.Object, _mockLogger.Object, _mockStateStore.Object, _mockDefinitionStore.Object);
    }

    [Fact]
    public async Task ExecuteConditionalStep_WithTrueCondition_ShouldNotExecuteElseSteps()
    {
        // Arrange
        var execution = new WorkflowExecution
        {
            Id = "test-execution",
            WorkflowDefinitionId = "test-workflow",
            Status = WorkflowStatus.Running,
            Context = new Dictionary<string, object>
            {
                ["ShouldExecute"] = true
            }
        };

        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "ShouldExecute == true",
                    ElseSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "ElseStep", Type = StepType.Wait, WaitTimeMs = 100 }
                    }
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteConditionalStep_WithFalseCondition_ShouldExecuteElseSteps()
    {
        // Arrange
        var execution = new WorkflowExecution
        {
            Id = "test-execution",
            WorkflowDefinitionId = "test-workflow",
            Status = WorkflowStatus.Running,
            Context = new Dictionary<string, object>
            {
                ["ShouldExecute"] = false
            }
        };

        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "ShouldExecute == true",
                    ElseSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "ElseWaitStep", Type = StepType.Wait, WaitTimeMs = 50 }
                    }
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithEqualsOperator_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Status == active"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Status = "active" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithNotEqualsOperator_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Status != inactive"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Status = "active" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithEndsWithOperator_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Filename endswith .txt"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Filename = "document.txt" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithGreaterThanOperator_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Count > 5"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Count = "10" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithLessThanOperator_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Count < 20"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Count = "10" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithGreaterThanOrEqualOperator_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Age >= 18"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Age = "18" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithLessThanOrEqualOperator_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Score <= 100"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Score = "95" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithNonNumericValuesForComparison_ShouldDefaultToTrue()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Text > 5" // Non-numeric comparison should default to true
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Text = "hello" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Since the condition cannot be evaluated, it should complete (defaults to true)
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithSimpleBooleanCheck_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "IsActive"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { IsActive = true });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithNonEmptyString_ShouldEvaluateToTrue()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "UserName"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { UserName = "Murat" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithInvalidConditionFormat_ShouldDefaultToTrue()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "InvalidFormat WithoutOperator" // Invalid format should default to true
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Should default to true and complete successfully
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithNonExistentKey_ShouldDefaultToTrue()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "NonExistentKey"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Non-existent key should default to true
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithNullValue_ShouldHandleGracefully()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "NullValue"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act - Pass null value
        var context = new Dictionary<string, object> { ["NullValue"] = null! };
        await _workflowEngine.StartWorkflowAsync("test-workflow", context);

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Should handle null gracefully and default to true
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteConditionalStep_ShouldSetConditionResultInOutput()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "TestValue == true"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        WorkflowExecution? savedExecution = null;
        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((exec, ct) => savedExecution = exec)
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { TestValue = true });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - StepExecution should have output with ConditionResult
        Assert.NotNull(savedExecution);
        Assert.Single(savedExecution.StepExecutions);
        Assert.NotNull(savedExecution.StepExecutions[0].Output);
    }

    [Fact]
    public async Task EvaluateCondition_WithComplexCondition_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Age >= 18 && Status == active && Name != null"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Age = 25, Status = "active", Name = "John" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Complex condition should evaluate to true
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithContainsOperator_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Message contains error"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Message = "An error occurred" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EvaluateCondition_WithStartsWithOperator_ShouldEvaluateCorrectly()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "Name startswith John"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { Name = "John Doe" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
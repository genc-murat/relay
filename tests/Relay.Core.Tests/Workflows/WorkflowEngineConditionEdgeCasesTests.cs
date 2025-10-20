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
public class WorkflowEngineConditionEdgeCasesTests
{
    private readonly Mock<IRelay> _mockRelay;
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger;
    private readonly Mock<IWorkflowStateStore> _mockStateStore;
    private readonly Mock<IWorkflowDefinitionStore> _mockDefinitionStore;
    private readonly WorkflowEngine _workflowEngine;

    public WorkflowEngineConditionEdgeCasesTests()
    {
        _mockRelay = new Mock<IRelay>();
        _mockLogger = new Mock<ILogger<WorkflowEngine>>();
        _mockStateStore = new Mock<IWorkflowStateStore>();
        _mockDefinitionStore = new Mock<IWorkflowDefinitionStore>();
        _workflowEngine = new WorkflowEngine(_mockRelay.Object, _mockLogger.Object, _mockStateStore.Object, _mockDefinitionStore.Object);
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
}
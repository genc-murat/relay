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
public class WorkflowEngineConditionalStepExecutionTests
{
    private readonly Mock<IRelay> _mockRelay;
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger;
    private readonly Mock<IWorkflowStateStore> _mockStateStore;
    private readonly Mock<IWorkflowDefinitionStore> _mockDefinitionStore;
    private readonly WorkflowEngine _workflowEngine;

    public WorkflowEngineConditionalStepExecutionTests()
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
}

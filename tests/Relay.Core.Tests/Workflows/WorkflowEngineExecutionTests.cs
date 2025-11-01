using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Relay.Core.Workflows;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Tests.Workflows;

[Collection("Sequential")]
public class WorkflowEngineExecutionTests
{
    private readonly Mock<IRelay> _mockRelay;
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger;
    private readonly Mock<IWorkflowStateStore> _mockStateStore;
    private readonly Mock<IWorkflowDefinitionStore> _mockDefinitionStore;
    private readonly WorkflowEngine _workflowEngine;

    public WorkflowEngineExecutionTests()
    {
        _mockRelay = new Mock<IRelay>();
        _mockLogger = new Mock<ILogger<WorkflowEngine>>();
        _mockStateStore = new Mock<IWorkflowStateStore>();
        _mockDefinitionStore = new Mock<IWorkflowDefinitionStore>();
        _workflowEngine = new WorkflowEngine(_mockRelay.Object, _mockLogger.Object, _mockStateStore.Object, _mockDefinitionStore.Object);
    }

    [Fact]
    public async Task StartWorkflowAsync_ShouldCreateNewExecution()
    {
        // Arrange
        var workflowId = "test-workflow";
        var input = new { Name = "Test" };
        WorkflowExecution? savedExecution = null;

        // Mock a valid workflow definition
        var definition = new WorkflowDefinition
        {
            Id = workflowId,
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Wait", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((exec, ct) => savedExecution = exec)
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _workflowEngine.StartWorkflowAsync(workflowId, input);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        Assert.NotEqual(string.Empty, result.Id);
        Assert.Equal(workflowId, result.WorkflowDefinitionId);
        Assert.Equal(WorkflowStatus.Running, result.Status);
        Assert.Equal(input, result.Input);
        Assert.Equal(0, result.CurrentStepIndex);
        Assert.NotEqual(default, result.StartedAt);

        _mockStateStore.Verify(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartWorkflowAsync_ShouldGenerateUniqueExecutionIds()
    {
        // Arrange
        var workflowId = "test-workflow";
        var input = new { Name = "Test" };

        // Mock a valid workflow definition
        var definition = new WorkflowDefinition
        {
            Id = workflowId,
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Wait", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(workflowId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result1 = await _workflowEngine.StartWorkflowAsync(workflowId, input);
        var result2 = await _workflowEngine.StartWorkflowAsync(workflowId, input);

        // Assert
        Assert.NotEqual(result1.Id, result2.Id);
    }

    [Fact]
    public async Task StartWorkflowAsync_WithDifferentInputTypes_ShouldPreserveInputData()
    {
        // Arrange
        var workflowId = "test-workflow";
        var stringInput = "test string";
        var objectInput = new TestInput { Id = 123, Name = "Test" };

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var stringResult = await _workflowEngine.StartWorkflowAsync(workflowId, stringInput);
        var objectResult = await _workflowEngine.StartWorkflowAsync(workflowId, objectInput);

        // Assert
        Assert.Equal(stringInput, stringResult.Input);
        Assert.Equal(objectInput, objectResult.Input);
    }

    [Fact]
    public async Task StartWorkflowAsync_ShouldInitializeEmptyContext()
    {
        // Arrange
        var workflowId = "test-workflow";
        var input = new { };

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await _workflowEngine.StartWorkflowAsync(workflowId, input);

        // Assert
        Assert.NotNull(result.Context);
        Assert.Empty(result.Context);
        Assert.NotNull(result.StepExecutions);
        Assert.Empty(result.StepExecutions);
    }

    [Fact]
    public async Task StartWorkflowAsync_WithCancellationToken_ShouldPassTokenToStateStore()
    {
        // Arrange
        var workflowId = "test-workflow";
        var input = new { };
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        CancellationToken capturedToken = default;

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((exec, ct) => capturedToken = ct)
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync(workflowId, input, token);

        // Assert
        Assert.Equal(token, capturedToken);
    }

    [Fact]
    public async Task ExecuteWorkflow_WithCancellation_ShouldHandleCancellationGracefully()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "WaitStep", Type = StepType.Wait, WaitTimeMs = 500 }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { }, cts.Token);

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Workflow should be cancelled or completed (depending on timing)
        _mockStateStore.Verify(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
    private class TestInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
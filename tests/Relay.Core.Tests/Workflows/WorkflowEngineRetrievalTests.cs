using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Relay.Core.Workflows;
using Relay.Core.Contracts.Core;

namespace Relay.Core.Tests.Workflows;

[Collection("Sequential")]
public class WorkflowEngineRetrievalTests
{
    private readonly Mock<IRelay> _mockRelay;
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger;
    private readonly Mock<IWorkflowStateStore> _mockStateStore;
    private readonly Mock<IWorkflowDefinitionStore> _mockDefinitionStore;
    private readonly WorkflowEngine _workflowEngine;

    public WorkflowEngineRetrievalTests()
    {
        _mockRelay = new Mock<IRelay>();
        _mockLogger = new Mock<ILogger<WorkflowEngine>>();
        _mockStateStore = new Mock<IWorkflowStateStore>();
        _mockDefinitionStore = new Mock<IWorkflowDefinitionStore>();
        _workflowEngine = new WorkflowEngine(_mockRelay.Object, _mockLogger.Object, _mockStateStore.Object, _mockDefinitionStore.Object);
    }

    [Fact]
    public async Task GetExecutionAsync_ShouldReturnExecutionFromStateStore()
    {
        // Arrange
        var executionId = "execution-123";
        var expectedExecution = new WorkflowExecution
        {
            Id = executionId,
            WorkflowDefinitionId = "workflow-1",
            Status = WorkflowStatus.Running
        };

        _mockStateStore.Setup(x => x.GetExecutionAsync(executionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedExecution);

        // Act
        var result = await _workflowEngine.GetExecutionAsync(executionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(executionId, result.Id);
        Assert.Equal("workflow-1", result.WorkflowDefinitionId);
        Assert.Equal(WorkflowStatus.Running, result.Status);

        _mockStateStore.Verify(x => x.GetExecutionAsync(executionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetExecutionAsync_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        var executionId = "nonexistent-execution";

        _mockStateStore.Setup(x => x.GetExecutionAsync(executionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowExecution?)null);

        // Act
        var result = await _workflowEngine.GetExecutionAsync(executionId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetWorkflowDefinition_WithValidId_ShouldReturnDefinition()
    {
        // Arrange
        var definitionId = "test-workflow";
        var expectedDefinition = new WorkflowDefinition
        {
            Id = definitionId,
            Name = "Test Workflow",
            Steps = new System.Collections.Generic.List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Request, RequestType = "TestRequest" }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(definitionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDefinition);

        // We need to access the private method via reflection or test it indirectly through StartWorkflowAsync
        // For now, let's verify it's called through the workflow execution
        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var execution = await _workflowEngine.StartWorkflowAsync(definitionId, new { });

        // Small delay to allow background execution to start
        await Task.Delay(100);

        // Assert
        _mockDefinitionStore.Verify(x => x.GetDefinitionAsync(definitionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetWorkflowDefinition_WithNullDefinition_ShouldFailWorkflow()
    {
        // Arrange
        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("nonexistent-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowDefinition?)null);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(100);

        // Assert - Workflow should fail due to missing definition
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetWorkflowDefinition_WithException_ShouldFailWorkflow()
    {
        // Arrange
        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("error-workflow", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Track the number of times SaveExecutionAsync is called to debug the issue
        int saveCallCount = 0;
        WorkflowExecution? savedExecution = null;
        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((exec, ct) =>
            {
                saveCallCount++;
                savedExecution = exec;
            })
            .Returns(ValueTask.CompletedTask);

        // Act
        var execution = await _workflowEngine.StartWorkflowAsync("error-workflow", new { });

        // Wait for background execution
        await Task.Delay(500); // Increased delay to ensure background execution completes

        // Assert - Workflow should fail due to exception
        Assert.NotNull(savedExecution);
        Assert.Equal(WorkflowStatus.Failed, savedExecution.Status);
        
        // Verify SaveExecutionAsync was called at least once
        _mockStateStore.Verify(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
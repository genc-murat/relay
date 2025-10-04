using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class WorkflowEngineTests
{
    private readonly Mock<IRelay> _mockRelay;
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger;
    private readonly Mock<IWorkflowStateStore> _mockStateStore;
    private readonly WorkflowEngine _workflowEngine;

    public WorkflowEngineTests()
    {
        _mockRelay = new Mock<IRelay>();
        _mockLogger = new Mock<ILogger<WorkflowEngine>>();
        _mockStateStore = new Mock<IWorkflowStateStore>();
        _workflowEngine = new WorkflowEngine(_mockRelay.Object, _mockLogger.Object, _mockStateStore.Object);
    }

    [Fact]
    public void Constructor_WithNullRelay_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(null!, _mockLogger.Object, _mockStateStore.Object));
        Assert.Equal("relay", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(_mockRelay.Object, null!, _mockStateStore.Object));
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullStateStore_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(_mockRelay.Object, _mockLogger.Object, null!));
        Assert.Equal("stateStore", exception.ParamName);
    }

    [Fact]
    public async Task StartWorkflowAsync_ShouldCreateNewExecution()
    {
        // Arrange
        var workflowId = "test-workflow";
        var input = new { Name = "Test" };
        WorkflowExecution? savedExecution = null;

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

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result1 = await _workflowEngine.StartWorkflowAsync(workflowId, input);
        var result2 = await _workflowEngine.StartWorkflowAsync(workflowId, input);

        // Assert
        Assert.NotEqual(result1.Id, result2.Id);
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

    private class TestInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

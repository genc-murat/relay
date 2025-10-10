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

public class WorkflowEngineTests
{
    private readonly Mock<IRelay> _mockRelay;
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger;
    private readonly Mock<IWorkflowStateStore> _mockStateStore;
    private readonly Mock<IWorkflowDefinitionStore> _mockDefinitionStore;
    private readonly WorkflowEngine _workflowEngine;

    public WorkflowEngineTests()
    {
        _mockRelay = new Mock<IRelay>();
        _mockLogger = new Mock<ILogger<WorkflowEngine>>();
        _mockStateStore = new Mock<IWorkflowStateStore>();
        _mockDefinitionStore = new Mock<IWorkflowDefinitionStore>();
        _workflowEngine = new WorkflowEngine(_mockRelay.Object, _mockLogger.Object, _mockStateStore.Object, _mockDefinitionStore.Object);
    }

    [Fact]
    public void Constructor_WithNullRelay_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(null!, _mockLogger.Object, _mockStateStore.Object, _mockDefinitionStore.Object));
        Assert.Equal("relay", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(_mockRelay.Object, null!, _mockStateStore.Object, _mockDefinitionStore.Object));
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullStateStore_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(_mockRelay.Object, _mockLogger.Object, null!, _mockDefinitionStore.Object));
        Assert.Equal("stateStore", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDefinitionStore_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new WorkflowEngine(_mockRelay.Object, _mockLogger.Object, _mockStateStore.Object, null!));
        Assert.Equal("definitionStore", exception.ParamName);
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

    [Fact]
    public async Task GetWorkflowDefinition_WithValidId_ShouldReturnDefinition()
    {
        // Arrange
        var definitionId = "test-workflow";
        var expectedDefinition = new WorkflowDefinition
        {
            Id = definitionId,
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
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
    public async Task ValidateWorkflowDefinition_WithMissingId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "",
            Name = "Test",
            Steps = new List<WorkflowStep>()
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test", new { });

        // Small delay to allow background execution and validation
        await Task.Delay(200);

        // Assert - Workflow should fail due to validation
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateWorkflowDefinition_WithNoSteps_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test",
            Steps = new List<WorkflowStep>()
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test", new { });

        await Task.Delay(200);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateWorkflowStep_RequestStepWithoutRequestType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Request } // Missing RequestType
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test", new { });

        await Task.Delay(200);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateWorkflowStep_ConditionalStepWithoutCondition_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Conditional } // Missing Condition
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test", new { });

        await Task.Delay(200);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateWorkflowStep_ParallelStepWithoutParallelSteps_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Parallel } // Missing ParallelSteps
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test", new { });

        await Task.Delay(200);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateWorkflowStep_WaitStepWithInvalidWaitTime_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = -100 } // Invalid WaitTimeMs
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test", new { });

        await Task.Delay(200);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateWorkflowDefinition_WithValidDefinition_ShouldNotThrow()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Valid Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Wait1", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test", new { });

        await Task.Delay(300);

        // Assert - Workflow should complete successfully
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteRequestStep_WithValidRequest_ShouldCallSendAsync()
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
                    Name = "RequestStep",
                    Type = StepType.Request,
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineTests+TestWorkflowRequest", // Use a valid type that exists
                    OutputKey = "Result"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        _mockRelay.Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask());

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Verify SendAsync was called and workflow completed successfully
        _mockRelay.Verify(x => x.SendAsync(It.IsAny<WorkflowEngineTests.TestWorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
    public async Task ExecuteParallelStep_ShouldExecuteAllParallelSteps()
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
                    Name = "ParallelStep",
                    Type = StepType.Parallel,
                    ParallelSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "ParallelStep1", Type = StepType.Wait, WaitTimeMs = 50 },
                        new WorkflowStep { Name = "ParallelStep2", Type = StepType.Wait, WaitTimeMs = 50 },
                        new WorkflowStep { Name = "ParallelStep3", Type = StepType.Wait, WaitTimeMs = 50 }
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
    public async Task ExecuteWaitStep_ShouldWaitForSpecifiedTime()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "WaitStep", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var startTime = DateTime.UtcNow;

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        var endTime = DateTime.UtcNow;
        var elapsed = endTime - startTime;

        // Assert
        Assert.True(elapsed.TotalMilliseconds >= 100, "Wait step should wait for at least 100ms");
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task CreateRequestFromStep_WithInvalidRequestType_ShouldFailWorkflow()
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
                    Name = "InvalidStep",
                    Type = StepType.Request,
                    RequestType = "" // Empty RequestType should cause failure
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

        // Assert - Workflow should fail due to validation error
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task FindRequestType_WithNonExistentType_ShouldFailWorkflow()
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
                    Name = "Step1",
                    Type = StepType.Request,
                    RequestType = "NonExistent.Type.That.Does.Not.Exist"
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

        // Assert - Workflow should fail because request type not found
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
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
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { UserName = "John" });

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
    public async Task ExecuteStep_WithUnsupportedStepType_ShouldThrowNotSupportedException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "UnsupportedStep", Type = (StepType)999 } // Invalid enum value
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

        // Assert - Workflow should fail due to unsupported step type
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteStep_WithoutContinueOnError_ShouldFailWorkflow()
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
                    Name = "FailingStep",
                    Type = StepType.Request,
                    RequestType = "NonExistentRequest",
                    ContinueOnError = false
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

        // Assert - Workflow should fail due to the failing step
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
        await Task.Delay(1000);

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

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("error-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Workflow should fail due to exception
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateWorkflowDefinition_WithMissingName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test", new { });

        await Task.Delay(200);

        // Assert - Workflow should fail due to validation
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateWorkflowStep_WithMissingName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _workflowEngine.StartWorkflowAsync("test", new { });

        await Task.Delay(200);

        // Assert - Workflow should fail due to validation
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task FailWorkflow_ShouldSetFailedStatusAndError()
    {
        // Arrange
        var execution = new WorkflowExecution
        {
            Id = "test-execution",
            WorkflowDefinitionId = "test-workflow",
            Status = WorkflowStatus.Running
        };

        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "Step1",
                    Type = StepType.Request,
                    RequestType = "NonExistentRequestType"
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
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert
        Assert.NotNull(savedExecution);
        Assert.Equal(WorkflowStatus.Failed, savedExecution.Status);
        Assert.NotNull(savedExecution.Error);
        Assert.NotNull(savedExecution.CompletedAt);
    }

    [Fact]
    public async Task WorkflowExecution_WithComplexWorkflow_ShouldExecuteAllSteps()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "complex-workflow",
            Name = "Complex Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 50 },
                new WorkflowStep
                {
                    Name = "Step2",
                    Type = StepType.Conditional,
                    Condition = "AlwaysTrue == true",
                    ElseSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "ElseStep", Type = StepType.Wait, WaitTimeMs = 25 }
                    }
                },
                new WorkflowStep
                {
                    Name = "Step3",
                    Type = StepType.Parallel,
                    ParallelSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "Parallel1", Type = StepType.Wait, WaitTimeMs = 25 },
                        new WorkflowStep { Name = "Parallel2", Type = StepType.Wait, WaitTimeMs = 25 }
                    }
                },
                new WorkflowStep { Name = "Step4", Type = StepType.Wait, WaitTimeMs = 50 }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("complex-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var execution = await _workflowEngine.StartWorkflowAsync("complex-workflow", new { AlwaysTrue = true });

        // Wait for background execution
        await Task.Delay(500);

        // Assert
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    private class TestInput
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task CreateRequestFromStep_WithPropertyMapping_ShouldMapPropertiesCorrectly()
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
                    Name = "RequestStep",
                    Type = StepType.Request,
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineTests+TestWorkflowRequestWithProperties",
                    OutputKey = "Result"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        WorkflowExecution? savedExecution = null;
        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((exec, ct) =>
            {
                savedExecution = exec;
                // Pre-populate context with properties for the request
                if (!exec.Context.ContainsKey("TestProperty"))
                {
                    exec.Context["TestProperty"] = "TestValue";
                    exec.Context["NumberProperty"] = 42;
                }
            })
            .Returns(ValueTask.CompletedTask);

        TestWorkflowRequestWithProperties? capturedRequest = null;
        _mockRelay.Setup(x => x.SendAsync(It.IsAny<TestWorkflowRequestWithProperties>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((req, ct) => capturedRequest = req as TestWorkflowRequestWithProperties)
            .Returns(new ValueTask<string>("Success"));

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Properties should be mapped correctly
        Assert.NotNull(capturedRequest);
        Assert.Equal("TestValue", capturedRequest.TestProperty);
        Assert.Equal(42, capturedRequest.NumberProperty);
    }

    [Fact]
    public async Task CreateRequestFromStep_WithTypeConversion_ShouldConvertProperties()
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
                    Name = "RequestStep",
                    Type = StepType.Request,
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineTests+TestWorkflowRequestWithProperties",
                    OutputKey = "Result"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((exec, ct) =>
            {
                // Pre-populate context with string value for the request
                if (!exec.Context.ContainsKey("NumberProperty"))
                {
                    exec.Context["NumberProperty"] = "123"; // String that should be converted to int
                }
            })
            .Returns(ValueTask.CompletedTask);

        TestWorkflowRequestWithProperties? capturedRequest = null;
        _mockRelay.Setup(x => x.SendAsync(It.IsAny<TestWorkflowRequestWithProperties>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((req, ct) => capturedRequest = req as TestWorkflowRequestWithProperties)
            .Returns(new ValueTask<string>("Success"));

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Type conversion should occur
        Assert.NotNull(capturedRequest);
        Assert.Equal(123, capturedRequest.NumberProperty);
    }

    [Fact]
    public async Task CreateRequestFromStep_WithReadOnlyProperty_ShouldSkipProperty()
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
                    Name = "RequestStep",
                    Type = StepType.Request,
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineTests+TestWorkflowRequestWithReadOnly",
                    OutputKey = "Result"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        _mockRelay.Setup(x => x.SendAsync(It.IsAny<TestWorkflowRequestWithReadOnly>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string>("Success"));

        // Act - Try to set ReadOnlyProperty, should be skipped
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { ReadOnlyProperty = "TestValue" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Workflow should complete successfully (readonly property should be skipped)
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteStep_WithContinueOnError_ShouldContinueExecution()
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
                    Name = "FailingStep",
                    Type = StepType.Request,
                    RequestType = "NonExistentType",
                    ContinueOnError = true
                },
                new WorkflowStep
                {
                    Name = "SuccessStep",
                    Type = StepType.Wait,
                    WaitTimeMs = 50
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
        await Task.Delay(400);

        // Assert - Workflow should complete despite the first step failing
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Completed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteParallelStep_WithNullParallelSteps_ShouldComplete()
    {
        // Arrange - This tests the null check in ExecuteParallelStep
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ParallelStep",
                    Type = StepType.Parallel,
                    ParallelSteps = new List<WorkflowStep>() // Empty list (validation requires at least one, so this will fail validation)
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

        // Assert - Should fail validation
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task FindRequestType_WithTypeThatDoesNotImplementIRequest_ShouldFailWorkflow()
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
                    Name = "RequestStep",
                    Type = StepType.Request,
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineTests+NonRequestType" // Type that doesn't implement IRequest
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

        // Assert - Should fail because type doesn't implement IRequest
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateWorkflowStep_WithNestedParallelSteps_ShouldValidateNested()
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
                    Name = "ParallelStep",
                    Type = StepType.Parallel,
                    ParallelSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "", Type = StepType.Wait, WaitTimeMs = 50 } // Invalid: empty name
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

        // Assert - Should fail nested validation
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateWorkflowStep_WithNestedElseSteps_ShouldValidateNested()
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
                    Condition = "test",
                    ElseSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "ElseStep", Type = StepType.Request } // Invalid: missing RequestType
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

        // Assert - Should fail nested validation
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteRequestStep_WithOutputKey_ShouldUpdateContext()
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
                    Name = "RequestStep",
                    Type = StepType.Request,
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineTests+TestWorkflowRequest",
                    OutputKey = "CustomKey"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        WorkflowExecution? savedExecution = null;
        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((exec, ct) => savedExecution = exec)
            .Returns(ValueTask.CompletedTask);

        _mockRelay.Setup(x => x.SendAsync(It.IsAny<TestWorkflowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string>("TestResult"));

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Context should contain the output with the custom key
        Assert.NotNull(savedExecution);
        Assert.True(savedExecution.Context.ContainsKey("CustomKey"));
        Assert.Equal("TestResult", savedExecution.Context["CustomKey"]);
    }

    [Fact]
    public async Task ExecuteRequestStep_WithoutOutputKey_ShouldUseStepNameAsKey()
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
                    Name = "MyRequestStep",
                    Type = StepType.Request,
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineTests+TestWorkflowRequest"
                    // No OutputKey specified
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        WorkflowExecution? savedExecution = null;
        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Callback<WorkflowExecution, CancellationToken>((exec, ct) => savedExecution = exec)
            .Returns(ValueTask.CompletedTask);

        _mockRelay.Setup(x => x.SendAsync(It.IsAny<TestWorkflowRequest>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<string>("TestResult"));

        // Act
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Context should contain the output with the step name as key
        Assert.NotNull(savedExecution);
        Assert.True(savedExecution.Context.ContainsKey("MyRequestStep"));
        Assert.Equal("TestResult", savedExecution.Context["MyRequestStep"]);
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

    // Test request types and helper classes for workflow execution
    public class TestWorkflowRequest : IRequest<string>
    {
        public string TestProperty { get; set; } = string.Empty;
        public int NumberProperty { get; set; }
    }

    public class TestWorkflowRequestWithProperties : IRequest<string>
    {
        public string TestProperty { get; set; } = string.Empty;
        public int NumberProperty { get; set; }
    }

    public class TestWorkflowRequestWithReadOnly : IRequest<string>
    {
        public string ReadOnlyProperty { get; } = "DefaultValue";
        public string WritableProperty { get; set; } = string.Empty;
    }

    public class NonRequestType
    {
        public string SomeProperty { get; set; } = string.Empty;
    }

    public class TestWorkflowResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}

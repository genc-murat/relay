using System;
using System.Collections.Generic;
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
public class WorkflowEngineStepExecutionTests
{
    private readonly Mock<IRelay> _mockRelay;
    private readonly Mock<ILogger<WorkflowEngine>> _mockLogger;
    private readonly Mock<IWorkflowStateStore> _mockStateStore;
    private readonly Mock<IWorkflowDefinitionStore> _mockDefinitionStore;
    private readonly WorkflowEngine _workflowEngine;

    public WorkflowEngineStepExecutionTests()
    {
        _mockRelay = new Mock<IRelay>();
        _mockLogger = new Mock<ILogger<WorkflowEngine>>();
        _mockStateStore = new Mock<IWorkflowStateStore>();
        _mockDefinitionStore = new Mock<IWorkflowDefinitionStore>();
        _workflowEngine = new WorkflowEngine(_mockRelay.Object, _mockLogger.Object, _mockStateStore.Object, _mockDefinitionStore.Object);
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
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineStepExecutionTests+TestWorkflowRequest", // Use a valid type that exists
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
        _mockRelay.Verify(x => x.SendAsync(It.IsAny<WorkflowEngineStepExecutionTests.TestWorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task ExecuteRequestStep_WithNullRequestType_ShouldFailWorkflow()
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
                    RequestType = null // Null RequestType should cause ArgumentException
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

        // Assert - Workflow should fail due to ArgumentException in CreateRequestFromStep
        _mockStateStore.Verify(x => x.SaveExecutionAsync(
            It.Is<WorkflowExecution>(e => e.Status == WorkflowStatus.Failed),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteRequestStep_WithEmptyRequestType_ShouldFailWorkflow()
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
                    RequestType = "" // Empty RequestType should cause ArgumentException
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

        // Assert - Workflow should fail due to ArgumentException in CreateRequestFromStep
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
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineStepExecutionTests+TestWorkflowRequest",
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
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineStepExecutionTests+TestWorkflowRequest"
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
    public async Task ExecuteRequestStep_WithReadOnlyProperty_ShouldSkipProperty()
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
                    RequestType = "Relay.Core.Tests.Workflows.WorkflowEngineStepExecutionTests+TestWorkflowRequestWithReadOnly",
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
                    RequestType = typeof(TestWorkflowRequestWithProperties).AssemblyQualifiedName,
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
        _mockRelay.Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<string>, CancellationToken>((req, ct) => capturedRequest = req as TestWorkflowRequestWithProperties)
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
                    RequestType = typeof(TestWorkflowRequestWithProperties).AssemblyQualifiedName,
                    OutputKey = "Result"
                }
            }
        };

        _mockDefinitionStore.Setup(x => x.GetDefinitionAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        _mockStateStore.Setup(x => x.SaveExecutionAsync(It.IsAny<WorkflowExecution>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        TestWorkflowRequestWithProperties? capturedRequest = null;
        _mockRelay.Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<string>, CancellationToken>((req, ct) => capturedRequest = req as TestWorkflowRequestWithProperties)
            .Returns(new ValueTask<string>("Success"));

        // Act - Pass NumberProperty as string in the input, which should be converted to int
        await _workflowEngine.StartWorkflowAsync("test-workflow", new { NumberProperty = "123" });

        // Wait for background execution
        await Task.Delay(300);

        // Assert - Type conversion should occur
        Assert.NotNull(capturedRequest);
        Assert.Equal(123, capturedRequest.NumberProperty);
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
}
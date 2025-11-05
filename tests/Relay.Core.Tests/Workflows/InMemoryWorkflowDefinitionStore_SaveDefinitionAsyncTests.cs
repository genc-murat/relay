using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class InMemoryWorkflowDefinitionStore_SaveDefinitionAsyncTests
{
    private readonly InMemoryWorkflowDefinitionStore _store;

    public InMemoryWorkflowDefinitionStore_SaveDefinitionAsyncTests()
    {
        _store = new InMemoryWorkflowDefinitionStore();
    }

    [Fact]
    public async Task SaveDefinitionAsync_WithValidDefinition_ShouldSaveSuccessfully()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "new-workflow",
            Name = "New Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 50 }
            }
        };

        // Act
        await _store.SaveDefinitionAsync(definition);

        // Assert
        var retrieved = await _store.GetDefinitionAsync("new-workflow");
        Assert.NotNull(retrieved);
        Assert.Equal("new-workflow", retrieved.Id);
        Assert.Equal("New Workflow", retrieved.Name);
    }

    [Fact]
    public async Task SaveDefinitionAsync_WithNullDefinition_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _store.SaveDefinitionAsync(null!));
    }

    [Fact]
    public async Task SaveDefinitionAsync_WithEmptyId_ShouldThrowArgumentException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "",
            Name = "Invalid Workflow"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await _store.SaveDefinitionAsync(definition));
        Assert.Contains("Workflow definition must have an Id", exception.Message);
    }

    [Fact]
    public async Task SaveDefinitionAsync_WithNullId_ShouldThrowArgumentException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = null!,
            Name = "Invalid Workflow"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await _store.SaveDefinitionAsync(definition));
        Assert.Contains("Workflow definition must have an Id", exception.Message);
    }

    [Fact]
    public async Task SaveDefinitionAsync_WithWhitespaceId_ShouldThrowArgumentException()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "   ",
            Name = "Invalid Workflow"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () => await _store.SaveDefinitionAsync(definition));
        Assert.Contains("Workflow definition must have an Id", exception.Message);
    }

    [Fact]
    public async Task SaveDefinitionAsync_WithExistingId_ShouldOverwriteDefinition()
    {
        // Arrange
        var originalDefinition = new WorkflowDefinition
        {
            Id = "update-workflow",
            Name = "Original Workflow",
            Description = "Original description",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "OriginalStep", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        var updatedDefinition = new WorkflowDefinition
        {
            Id = "update-workflow",
            Name = "Updated Workflow",
            Description = "Updated description",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "UpdatedStep", Type = StepType.Wait, WaitTimeMs = 200 }
            }
        };

        // Act
        await _store.SaveDefinitionAsync(originalDefinition);
        await _store.SaveDefinitionAsync(updatedDefinition);

        // Assert
        var retrieved = await _store.GetDefinitionAsync("update-workflow");
        Assert.NotNull(retrieved);
        Assert.Equal("Updated Workflow", retrieved.Name);
        Assert.Equal("Updated description", retrieved.Description);
        Assert.Equal("UpdatedStep", retrieved.Steps[0].Name);
        Assert.Equal(200, retrieved.Steps[0].WaitTimeMs);
    }

    [Fact]
    public async Task SaveDefinitionAsync_WithComplexWorkflow_ShouldSaveSuccessfully()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "complex-workflow",
            Name = "Complex Workflow",
            Description = "A complex workflow with multiple step types",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ParallelStep",
                    Type = StepType.Parallel,
                    ParallelSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "SubStep1", Type = StepType.Wait, WaitTimeMs = 100 },
                        new WorkflowStep { Name = "SubStep2", Type = StepType.Wait, WaitTimeMs = 200 }
                    }
                },
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "input.Value > 10",
                    ElseSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "ElseStep", Type = StepType.Wait, WaitTimeMs = 50 }
                    }
                },
                new WorkflowStep
                {
                    Name = "RequestStep",
                    Type = StepType.Request,
                    RequestType = "ProcessDataRequest",
                    OutputKey = "processedResult",
                    ContinueOnError = true
                }
            }
        };

        // Act
        await _store.SaveDefinitionAsync(definition);

        // Assert
        var retrieved = await _store.GetDefinitionAsync("complex-workflow");
        Assert.NotNull(retrieved);
        Assert.Equal("complex-workflow", retrieved.Id);
        Assert.Equal("Complex Workflow", retrieved.Name);
        Assert.Equal("A complex workflow with multiple step types", retrieved.Description);
        Assert.Equal(3, retrieved.Steps.Count);

        // Verify parallel step
        var parallelStep = retrieved.Steps[0];
        Assert.Equal("ParallelStep", parallelStep.Name);
        Assert.Equal(StepType.Parallel, parallelStep.Type);
        Assert.NotNull(parallelStep.ParallelSteps);
        Assert.Equal(2, parallelStep.ParallelSteps.Count);

        // Verify conditional step
        var conditionalStep = retrieved.Steps[1];
        Assert.Equal("ConditionalStep", conditionalStep.Name);
        Assert.Equal(StepType.Conditional, conditionalStep.Type);
        Assert.Equal("input.Value > 10", conditionalStep.Condition);
        Assert.NotNull(conditionalStep.ElseSteps);
        Assert.Single(conditionalStep.ElseSteps);

        // Verify request step
        var requestStep = retrieved.Steps[2];
        Assert.Equal("RequestStep", requestStep.Name);
        Assert.Equal(StepType.Request, requestStep.Type);
        Assert.Equal("ProcessDataRequest", requestStep.RequestType);
        Assert.Equal("processedResult", requestStep.OutputKey);
        Assert.True(requestStep.ContinueOnError);
    }

    [Fact]
    public async Task SaveDefinitionAsync_WithEmptySteps_ShouldSaveSuccessfully()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "empty-steps-workflow",
            Name = "Empty Steps Workflow",
            Description = "Workflow with no steps",
            Steps = new List<WorkflowStep>()
        };

        // Act
        await _store.SaveDefinitionAsync(definition);

        // Assert
        var retrieved = await _store.GetDefinitionAsync("empty-steps-workflow");
        Assert.NotNull(retrieved);
        Assert.Equal("empty-steps-workflow", retrieved.Id);
        Assert.Empty(retrieved.Steps);
    }

    [Fact]
    public async Task SaveDefinitionAsync_WithNullSteps_ShouldSaveSuccessfully()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "null-steps-workflow",
            Name = "Null Steps Workflow",
            Steps = null!
        };

        // Act
        await _store.SaveDefinitionAsync(definition);

        // Assert
        var retrieved = await _store.GetDefinitionAsync("null-steps-workflow");
        Assert.NotNull(retrieved);
        Assert.Equal("null-steps-workflow", retrieved.Id);
        Assert.Null(retrieved.Steps);
    }
}

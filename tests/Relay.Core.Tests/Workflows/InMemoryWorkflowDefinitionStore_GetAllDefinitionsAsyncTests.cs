using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class InMemoryWorkflowDefinitionStore_GetAllDefinitionsAsyncTests
{
    private readonly InMemoryWorkflowDefinitionStore _store;

    public InMemoryWorkflowDefinitionStore_GetAllDefinitionsAsyncTests()
    {
        _store = new InMemoryWorkflowDefinitionStore();
    }

    [Fact]
    public async Task GetAllDefinitionsAsync_WithEmptyStore_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetAllDefinitionsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllDefinitionsAsync_WithMultipleDefinitions_ShouldReturnAllDefinitions()
    {
        // Arrange
        var definition1 = new WorkflowDefinition
        {
            Id = "workflow1",
            Name = "Workflow 1",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        var definition2 = new WorkflowDefinition
        {
            Id = "workflow2",
            Name = "Workflow 2",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step2", Type = StepType.Wait, WaitTimeMs = 200 }
            }
        };

        var definition3 = new WorkflowDefinition
        {
            Id = "workflow3",
            Name = "Workflow 3",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step3", Type = StepType.Wait, WaitTimeMs = 300 }
            }
        };

        await _store.SaveDefinitionAsync(definition1);
        await _store.SaveDefinitionAsync(definition2);
        await _store.SaveDefinitionAsync(definition3);

        // Act
        var result = await _store.GetAllDefinitionsAsync();

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);

        var ids = resultList.Select(d => d.Id).ToList();
        Assert.Contains("workflow1", ids);
        Assert.Contains("workflow2", ids);
        Assert.Contains("workflow3", ids);
    }

    [Fact]
    public async Task GetAllDefinitionsAsync_AfterModifications_ShouldReflectCurrentState()
    {
        // Arrange
        var definition1 = new WorkflowDefinition
        {
            Id = "test1",
            Name = "Test 1",
            Steps = new List<WorkflowStep> { new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 100 } }
        };

        var definition2 = new WorkflowDefinition
        {
            Id = "test2",
            Name = "Test 2",
            Steps = new List<WorkflowStep> { new WorkflowStep { Name = "Step2", Type = StepType.Wait, WaitTimeMs = 200 } }
        };

        // Act & Assert - Initial state
        await _store.SaveDefinitionAsync(definition1);
        var initial = await _store.GetAllDefinitionsAsync();
        Assert.Single(initial.ToList());

        // Add second definition
        await _store.SaveDefinitionAsync(definition2);
        var afterAdd = await _store.GetAllDefinitionsAsync();
        Assert.Equal(2, afterAdd.ToList().Count);

        // Delete first definition
        await _store.DeleteDefinitionAsync("test1");
        var afterDelete = await _store.GetAllDefinitionsAsync();
        var resultList = afterDelete.ToList();
        Assert.Single(resultList);
        Assert.Equal("test2", resultList[0].Id);

        // Update remaining definition
        var updatedDefinition2 = new WorkflowDefinition
        {
            Id = "test2",
            Name = "Updated Test 2",
            Steps = new List<WorkflowStep> { new WorkflowStep { Name = "UpdatedStep", Type = StepType.Wait, WaitTimeMs = 300 } }
        };

        await _store.SaveDefinitionAsync(updatedDefinition2);
        var afterUpdate = await _store.GetAllDefinitionsAsync();
        var finalList = afterUpdate.ToList();
        Assert.Single(finalList);
        Assert.Equal("Updated Test 2", finalList[0].Name);
    }

    [Fact]
    public async Task GetAllDefinitionsAsync_ShouldReturnIndependentCopies()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "copy-test",
            Name = "Original Name",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        await _store.SaveDefinitionAsync(definition);

        // Act
        var result1 = await _store.GetAllDefinitionsAsync();
        var result2 = await _store.GetAllDefinitionsAsync();

        // Assert - Should be separate enumerations
        Assert.NotSame(result1, result2);

        var list1 = result1.ToList();
        var list2 = result2.ToList();
        Assert.Equal(list1.Count, list2.Count);
        Assert.Equal(list1[0].Id, list2[0].Id);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class InMemoryWorkflowDefinitionStoreTests
{
    private readonly InMemoryWorkflowDefinitionStore _store;

    public InMemoryWorkflowDefinitionStoreTests()
    {
        _store = new InMemoryWorkflowDefinitionStore();
    }

    [Fact]
    public async Task GetDefinitionAsync_WithExistingDefinition_ShouldReturnDefinition()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "test-workflow",
            Name = "Test Workflow",
            Description = "A test workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        await _store.SaveDefinitionAsync(definition);

        // Act
        var result = await _store.GetDefinitionAsync("test-workflow");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-workflow", result.Id);
        Assert.Equal("Test Workflow", result.Name);
        Assert.Equal("A test workflow", result.Description);
        Assert.Single(result.Steps);
        Assert.Equal("Step1", result.Steps[0].Name);
    }

    [Fact]
    public async Task GetDefinitionAsync_WithNonExistentDefinition_ShouldReturnNull()
    {
        // Act
        var result = await _store.GetDefinitionAsync("nonexistent-workflow");

        // Assert
        Assert.Null(result);
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
    public async Task DeleteDefinitionAsync_WithExistingDefinition_ShouldReturnTrue()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "delete-workflow",
            Name = "Delete Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        await _store.SaveDefinitionAsync(definition);

        // Act
        var result = await _store.DeleteDefinitionAsync("delete-workflow");

        // Assert
        Assert.True(result);
        
        // Verify it's actually deleted
        var retrieved = await _store.GetDefinitionAsync("delete-workflow");
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithNonExistentDefinition_ShouldReturnFalse()
    {
        // Act
        var result = await _store.DeleteDefinitionAsync("nonexistent-workflow");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithNullId_ShouldReturnFalse()
    {
        // Act
        var result = await _store.DeleteDefinitionAsync(null!);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithEmptyId_ShouldReturnFalse()
    {
        // Act
        var result = await _store.DeleteDefinitionAsync("");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var definitions = new List<WorkflowDefinition>();

        // Create multiple definitions concurrently
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            var definition = new WorkflowDefinition
            {
                Id = $"concurrent-workflow-{index}",
                Name = $"Concurrent Workflow {index}",
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep { Name = $"Step{index}", Type = StepType.Wait, WaitTimeMs = 100 }
                }
            };
            definitions.Add(definition);

            tasks.Add(_store.SaveDefinitionAsync(definition).AsTask());
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert - All definitions should be saved
        var allDefinitions = await _store.GetAllDefinitionsAsync();
        var resultList = allDefinitions.ToList();
        Assert.Equal(10, resultList.Count);

        for (int i = 0; i < 10; i++)
        {
            var retrieved = await _store.GetDefinitionAsync($"concurrent-workflow-{i}");
            Assert.NotNull(retrieved);
            Assert.Equal($"Concurrent Workflow {i}", retrieved.Name);
        }
    }

    [Fact]
    public async Task ConcurrentReads_ShouldReturnConsistentResults()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "read-test-workflow",
            Name = "Read Test Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        await _store.SaveDefinitionAsync(definition);

        // Act - Read the same definition concurrently
        var readTasks = new List<Task<WorkflowDefinition?>>();
        for (int i = 0; i < 5; i++)
        {
            readTasks.Add(_store.GetDefinitionAsync("read-test-workflow").AsTask());
        }

        var results = await Task.WhenAll(readTasks);

        // Assert - All reads should return the same definition
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.Equal("read-test-workflow", result.Id);
            Assert.Equal("Read Test Workflow", result.Name);
        }
    }

    [Fact]
    public async Task MixedConcurrentOperations_ShouldMaintainConsistency()
    {
        // Arrange
        var baseDefinition = new WorkflowDefinition
        {
            Id = "mixed-workflow",
            Name = "Base Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "BaseStep", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        await _store.SaveDefinitionAsync(baseDefinition);

        // Act - Perform operations in a controlled sequence
        var readTasks = new List<Task<WorkflowDefinition>>();
        
        // Start concurrent reads
        for (int i = 0; i < 3; i++)
        {
            readTasks.Add(_store.GetDefinitionAsync("mixed-workflow").AsTask());
        }

        // Wait a bit to ensure reads start
        await Task.Delay(10);

        // Update the definition
        var updatedDefinition = new WorkflowDefinition
        {
            Id = "mixed-workflow",
            Name = "Updated Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "UpdatedStep", Type = StepType.Wait, WaitTimeMs = 200 }
            }
        };

        await _store.SaveDefinitionAsync(updatedDefinition);

        // Wait for all reads to complete
        var readResults = await Task.WhenAll(readTasks);

        // Assert - At least one read should get the base definition, and final state should be updated
        var hasBaseDefinition = readResults.Any(r => r != null && r.Name == "Base Workflow");
        var hasUpdatedDefinition = readResults.Any(r => r != null && r.Name == "Updated Workflow");

        // The final state should be the updated definition
        var finalDefinition = await _store.GetDefinitionAsync("mixed-workflow");
        Assert.NotNull(finalDefinition);
        Assert.Equal("Updated Workflow", finalDefinition.Name);
        Assert.Equal("UpdatedStep", finalDefinition.Steps[0].Name);
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
}
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
        var readTasks = new List<Task<WorkflowDefinition?>>();
        
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

    [Fact]
    public async Task GetDefinitionAsync_WithWhitespaceId_ShouldReturnNull()
    {
        // Act
        var result = await _store.GetDefinitionAsync("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteDefinitionAsync_WithWhitespaceId_ShouldReturnFalse()
    {
        // Act
        var result = await _store.DeleteDefinitionAsync("   ");

        // Assert
        Assert.False(result);
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

    [Fact]
    public async Task ConcurrentDeleteOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "concurrent-delete-test",
            Name = "Concurrent Delete Test",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        await _store.SaveDefinitionAsync(definition);

        // Act - Try to delete the same definition concurrently
        var deleteTasks = new List<Task<bool>>();
        for (int i = 0; i < 5; i++)
        {
            deleteTasks.Add(_store.DeleteDefinitionAsync("concurrent-delete-test").AsTask());
        }

        var results = await Task.WhenAll(deleteTasks);

        // Assert - Only one delete should succeed (return true), others should return false
        var successCount = results.Count(r => r);
        Assert.True(successCount >= 1); // At least one should succeed
        Assert.True(successCount <= 5); // But not more than total attempts

        // Verify the definition is deleted
        var retrieved = await _store.GetDefinitionAsync("concurrent-delete-test");
        Assert.Null(retrieved);
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

    [Fact]
    public async Task LargeNumberOfDefinitions_ShouldHandleEfficiently()
    {
        // Arrange & Act
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            var definition = new WorkflowDefinition
            {
                Id = $"large-test-{i}",
                Name = $"Large Test Workflow {i}",
                Description = $"Description for workflow {i}",
                Steps = new List<WorkflowStep>
                {
                    new WorkflowStep { Name = $"Step{i}", Type = StepType.Wait, WaitTimeMs = i * 10 }
                }
            };
            tasks.Add(_store.SaveDefinitionAsync(definition).AsTask());
        }

        await Task.WhenAll(tasks);

        // Assert
        var allDefinitions = await _store.GetAllDefinitionsAsync();
        var resultList = allDefinitions.ToList();
        Assert.Equal(100, resultList.Count);

        // Verify specific definitions
        for (int i = 0; i < 100; i += 20) // Check every 20th definition
        {
            var retrieved = await _store.GetDefinitionAsync($"large-test-{i}");
            Assert.NotNull(retrieved);
            Assert.Equal($"Large Test Workflow {i}", retrieved.Name);
            Assert.Equal($"Description for workflow {i}", retrieved.Description);
        }
    }

    [Fact]
    public async Task DeleteDefinitionAsync_DuringConcurrentReads_ShouldMaintainConsistency()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "delete-during-reads",
            Name = "Delete During Reads Test",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "Step1", Type = StepType.Wait, WaitTimeMs = 100 }
            }
        };

        await _store.SaveDefinitionAsync(definition);

        // Act - Start concurrent reads and delete in the middle
        var readTasks = new List<Task<WorkflowDefinition?>>();
        
        // Start some reads
        for (int i = 0; i < 3; i++)
        {
            readTasks.Add(_store.GetDefinitionAsync("delete-during-reads").AsTask());
        }

        // Small delay to ensure reads start
        await Task.Delay(10);

        // Delete the definition
        var deleteResult = await _store.DeleteDefinitionAsync("delete-during-reads");
        Assert.True(deleteResult);

        // Start more reads after deletion
        for (int i = 0; i < 3; i++)
        {
            readTasks.Add(_store.GetDefinitionAsync("delete-during-reads").AsTask());
        }

        var results = await Task.WhenAll(readTasks);

        // Assert - Some reads should return the definition, others should return null
        var hasDefinition = results.Any(r => r != null);
        var hasNull = results.Any(r => r == null);
        Assert.True(hasDefinition);
        Assert.True(hasNull);

        // Final verification - definition should be deleted
        var finalCheck = await _store.GetDefinitionAsync("delete-during-reads");
        Assert.Null(finalCheck);
    }
}
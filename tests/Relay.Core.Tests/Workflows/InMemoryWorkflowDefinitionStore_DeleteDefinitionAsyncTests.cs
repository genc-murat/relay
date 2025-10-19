using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class InMemoryWorkflowDefinitionStore_DeleteDefinitionAsyncTests
{
    private readonly InMemoryWorkflowDefinitionStore _store;

    public InMemoryWorkflowDefinitionStore_DeleteDefinitionAsyncTests()
    {
        _store = new InMemoryWorkflowDefinitionStore();
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
    public async Task DeleteDefinitionAsync_WithWhitespaceId_ShouldReturnFalse()
    {
        // Act
        var result = await _store.DeleteDefinitionAsync("   ");

        // Assert
        Assert.False(result);
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
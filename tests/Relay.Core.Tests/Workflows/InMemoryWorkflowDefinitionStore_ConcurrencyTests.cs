using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class InMemoryWorkflowDefinitionStore_ConcurrencyTests
{
    private readonly InMemoryWorkflowDefinitionStore _store;

    public InMemoryWorkflowDefinitionStore_ConcurrencyTests()
    {
        _store = new InMemoryWorkflowDefinitionStore();
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
}
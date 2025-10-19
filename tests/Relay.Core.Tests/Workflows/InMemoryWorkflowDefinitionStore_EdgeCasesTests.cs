using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class InMemoryWorkflowDefinitionStore_EdgeCasesTests
{
    private readonly InMemoryWorkflowDefinitionStore _store;

    public InMemoryWorkflowDefinitionStore_EdgeCasesTests()
    {
        _store = new InMemoryWorkflowDefinitionStore();
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
}
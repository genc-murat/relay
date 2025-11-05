using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class InMemoryWorkflowDefinitionStore_GetDefinitionAsyncTests
{
    private readonly InMemoryWorkflowDefinitionStore _store;

    public InMemoryWorkflowDefinitionStore_GetDefinitionAsyncTests()
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
    public async Task GetDefinitionAsync_WithWhitespaceId_ShouldReturnNull()
    {
        // Act
        var result = await _store.GetDefinitionAsync("   ");

        // Assert
        Assert.Null(result);
    }
}

using System.Collections.Generic;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class WorkflowDefinitionTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var definition = new WorkflowDefinition();

        // Assert
        Assert.NotNull(definition);
        Assert.Equal(string.Empty, definition.Id);
        Assert.Equal(string.Empty, definition.Name);
        Assert.Equal(string.Empty, definition.Description);
        Assert.NotNull(definition.Steps);
        Assert.Empty(definition.Steps);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var definition = new WorkflowDefinition();
        var steps = new List<WorkflowStep>
        {
            new WorkflowStep { Name = "Step1", Type = StepType.Request }
        };

        // Act
        definition.Id = "workflow-1";
        definition.Name = "Test Workflow";
        definition.Description = "A test workflow";
        definition.Steps = steps;

        // Assert
        Assert.Equal("workflow-1", definition.Id);
        Assert.Equal("Test Workflow", definition.Name);
        Assert.Equal("A test workflow", definition.Description);
        Assert.Equal(steps, definition.Steps);
        Assert.Single(definition.Steps);
    }

    [Fact]
    public void Steps_ShouldBeModifiable()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "workflow-1",
            Name = "Test Workflow"
        };

        // Act
        definition.Steps.Add(new WorkflowStep { Name = "Step1", Type = StepType.Request });
        definition.Steps.Add(new WorkflowStep { Name = "Step2", Type = StepType.Conditional });
        definition.Steps.Add(new WorkflowStep { Name = "Step3", Type = StepType.Wait });

        // Assert
        Assert.Equal(3, definition.Steps.Count);
        Assert.Equal("Step1", definition.Steps[0].Name);
        Assert.Equal(StepType.Request, definition.Steps[0].Type);
        Assert.Equal("Step2", definition.Steps[1].Name);
        Assert.Equal(StepType.Conditional, definition.Steps[1].Type);
        Assert.Equal("Step3", definition.Steps[2].Name);
        Assert.Equal(StepType.Wait, definition.Steps[2].Type);
    }

    [Fact]
    public void WorkflowDefinition_ShouldSupportComplexStepHierarchy()
    {
        // Arrange & Act
        var definition = new WorkflowDefinition
        {
            Id = "complex-workflow",
            Name = "Complex Workflow",
            Description = "A workflow with nested steps",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "ParentStep",
                    Type = StepType.Parallel,
                    ParallelSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "Child1", Type = StepType.Request },
                        new WorkflowStep { Name = "Child2", Type = StepType.Request }
                    }
                },
                new WorkflowStep
                {
                    Name = "ConditionalStep",
                    Type = StepType.Conditional,
                    Condition = "status == success",
                    ElseSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "ErrorHandler", Type = StepType.Request }
                    }
                }
            }
        };

        // Assert
        Assert.Equal(2, definition.Steps.Count);
        Assert.NotNull(definition.Steps[0].ParallelSteps);
        Assert.Equal(2, definition.Steps[0].ParallelSteps.Count);
        Assert.NotNull(definition.Steps[1].ElseSteps);
        Assert.Single(definition.Steps[1].ElseSteps);
    }
}

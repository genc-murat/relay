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
        Assert.Equal(2, definition.Steps[0].ParallelSteps!.Count);
        Assert.NotNull(definition.Steps[1].ElseSteps);
        Assert.Single(definition.Steps[1].ElseSteps!);
    }

    [Fact]
    public void WorkflowDefinition_ShouldHandleEmptyWorkflow()
    {
        // Arrange & Act
        var definition = new WorkflowDefinition
        {
            Id = "empty-workflow",
            Name = "Empty Workflow",
            Description = "A workflow with no steps",
            Steps = new List<WorkflowStep>()
        };

        // Assert
        Assert.Equal("empty-workflow", definition.Id);
        Assert.Equal("Empty Workflow", definition.Name);
        Assert.Equal("A workflow with no steps", definition.Description);
        Assert.Empty(definition.Steps);
    }

    [Fact]
    public void WorkflowDefinition_ShouldHandleLargeWorkflowWithManySteps()
    {
        // Arrange
        var steps = new List<WorkflowStep>();
        for (int i = 0; i < 1000; i++)
        {
            steps.Add(new WorkflowStep { Name = $"Step{i}", Type = StepType.Request });
        }

        // Act
        var definition = new WorkflowDefinition
        {
            Id = "large-workflow",
            Name = "Large Workflow",
            Steps = steps
        };

        // Assert
        Assert.Equal(1000, definition.Steps.Count);
        Assert.Equal("Step0", definition.Steps[0].Name);
        Assert.Equal("Step999", definition.Steps[999].Name);
    }

    [Fact]
    public void WorkflowDefinition_ShouldAllowNullDescription()
    {
        // Arrange & Act
        var definition = new WorkflowDefinition
        {
            Id = "workflow-no-desc",
            Name = "Workflow Without Description",
            Description = null,
            Steps = new List<WorkflowStep> { new WorkflowStep { Name = "Step1", Type = StepType.Request } }
        };

        // Assert
        Assert.Null(definition.Description);
        Assert.Single(definition.Steps);
    }

    [Fact]
    public void WorkflowDefinition_ShouldHandleSpecialCharactersInProperties()
    {
        // Arrange & Act
        var definition = new WorkflowDefinition
        {
            Id = "workflow@#$%^&*()",
            Name = "Workflow with special chars: 测试🚀",
            Description = "Description with <>&\"'",
            Steps = new List<WorkflowStep> { new WorkflowStep { Name = "Step1", Type = StepType.Request } }
        };

        // Assert
        Assert.Equal("workflow@#$%^&*()", definition.Id);
        Assert.Equal("Workflow with special chars: 测试🚀", definition.Name);
        Assert.Equal("Description with <>&\"'", definition.Description);
    }

    [Fact]
    public void WorkflowDefinition_ShouldAllowEmptyIdAndName()
    {
        // Arrange & Act
        var definition = new WorkflowDefinition
        {
            Id = "",
            Name = "",
            Description = "Empty id and name",
            Steps = new List<WorkflowStep>()
        };

        // Assert
        Assert.Equal("", definition.Id);
        Assert.Equal("", definition.Name);
        Assert.Equal("Empty id and name", definition.Description);
        Assert.Empty(definition.Steps);
    }

    [Fact]
    public void WorkflowDefinition_ShouldHandleVeryLongStrings()
    {
        // Arrange
        var longId = new string('A', 10000);
        var longName = new string('B', 10000);
        var longDescription = new string('C', 10000);

        // Act
        var definition = new WorkflowDefinition
        {
            Id = longId,
            Name = longName,
            Description = longDescription,
            Steps = new List<WorkflowStep> { new WorkflowStep { Name = "Step1", Type = StepType.Request } }
        };

        // Assert
        Assert.Equal(longId, definition.Id);
        Assert.Equal(longName, definition.Name);
        Assert.Equal(longDescription, definition.Description);
    }

    [Fact]
    public void WorkflowDefinition_ShouldSupportDeeplyNestedStepHierarchy()
    {
        // Arrange & Act
        var definition = new WorkflowDefinition
        {
            Id = "deep-nested-workflow",
            Name = "Deep Nested Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep
                {
                    Name = "Level1",
                    Type = StepType.Parallel,
                    ParallelSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep
                        {
                            Name = "Level2A",
                            Type = StepType.Conditional,
                            Condition = "true",
                            ElseSteps = new List<WorkflowStep>
                            {
                                new WorkflowStep
                                {
                                    Name = "Level3",
                                    Type = StepType.Parallel,
                                    ParallelSteps = new List<WorkflowStep>
                                    {
                                        new WorkflowStep { Name = "Level4", Type = StepType.Request }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        // Assert
        Assert.Single(definition.Steps);
        var level1 = definition.Steps[0];
        Assert.NotNull(level1.ParallelSteps);
        var level2A = level1.ParallelSteps[0];
        Assert.NotNull(level2A.ElseSteps);
        var level3 = level2A.ElseSteps[0];
        Assert.NotNull(level3.ParallelSteps);
        Assert.Single(level3.ParallelSteps);
        Assert.Equal("Level4", level3.ParallelSteps[0].Name);
    }

    [Fact]
    public void WorkflowDefinition_ShouldAllowStepsWithAllStepTypes()
    {
        // Arrange & Act
        var definition = new WorkflowDefinition
        {
            Id = "all-types-workflow",
            Name = "All Step Types Workflow",
            Steps = new List<WorkflowStep>
            {
                new WorkflowStep { Name = "RequestStep", Type = StepType.Request },
                new WorkflowStep { Name = "ConditionalStep", Type = StepType.Conditional },
                new WorkflowStep { Name = "ParallelStep", Type = StepType.Parallel },
                new WorkflowStep { Name = "WaitStep", Type = StepType.Wait }
            }
        };

        // Assert
        Assert.Equal(4, definition.Steps.Count);
        Assert.Contains(definition.Steps, s => s.Type == StepType.Request);
        Assert.Contains(definition.Steps, s => s.Type == StepType.Conditional);
        Assert.Contains(definition.Steps, s => s.Type == StepType.Parallel);
        Assert.Contains(definition.Steps, s => s.Type == StepType.Wait);
    }

    [Fact]
    public void WorkflowDefinition_Steps_ShouldBeReplaceable()
    {
        // Arrange
        var definition = new WorkflowDefinition
        {
            Id = "replaceable-steps",
            Steps = new List<WorkflowStep> { new WorkflowStep { Name = "OldStep", Type = StepType.Request } }
        };

        var newSteps = new List<WorkflowStep>
        {
            new WorkflowStep { Name = "NewStep1", Type = StepType.Request },
            new WorkflowStep { Name = "NewStep2", Type = StepType.Conditional }
        };

        // Act
        definition.Steps = newSteps;

        // Assert
        Assert.Equal(2, definition.Steps.Count);
        Assert.Equal("NewStep1", definition.Steps[0].Name);
        Assert.Equal("NewStep2", definition.Steps[1].Name);
    }

    [Fact]
    public void WorkflowDefinition_ShouldHandleNullStepsList()
    {
        // Arrange & Act
        var definition = new WorkflowDefinition
        {
            Id = "null-steps",
            Name = "Null Steps Workflow",
            Steps = null
        };

        // Assert
        Assert.Null(definition.Steps);
    }
} 

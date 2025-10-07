using System.Collections.Generic;
using Xunit;
using Relay.Core.Workflows;

namespace Relay.Core.Tests.Workflows;

public class WorkflowStepTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var step = new WorkflowStep();

        // Assert
        Assert.NotNull(step);
        Assert.Equal(string.Empty, step.Name);
        Assert.Equal(StepType.Request, step.Type);
        Assert.Null(step.RequestType);
        Assert.Null(step.OutputKey);
        Assert.Null(step.Condition);
        Assert.False(step.ContinueOnError);
        Assert.Null(step.WaitTimeMs);
        Assert.Null(step.ParallelSteps);
        Assert.Null(step.ElseSteps);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var step = new WorkflowStep();

        // Act
        step.Name = "TestStep";
        step.Type = StepType.Request;
        step.RequestType = "TestRequest";
        step.OutputKey = "result";
        step.Condition = "status == success";
        step.ContinueOnError = true;
        step.WaitTimeMs = 5000;

        // Assert
        Assert.Equal("TestStep", step.Name);
        Assert.Equal(StepType.Request, step.Type);
        Assert.Equal("TestRequest", step.RequestType);
        Assert.Equal("result", step.OutputKey);
        Assert.Equal("status == success", step.Condition);
        Assert.True(step.ContinueOnError);
        Assert.Equal(5000, step.WaitTimeMs);
    }

    [Fact]
    public void StepType_ShouldSupportAllTypes()
    {
        // Arrange & Act
        var requestStep = new WorkflowStep { Type = StepType.Request };
        var conditionalStep = new WorkflowStep { Type = StepType.Conditional };
        var parallelStep = new WorkflowStep { Type = StepType.Parallel };
        var waitStep = new WorkflowStep { Type = StepType.Wait };

        // Assert
        Assert.Equal(StepType.Request, requestStep.Type);
        Assert.Equal(StepType.Conditional, conditionalStep.Type);
        Assert.Equal(StepType.Parallel, parallelStep.Type);
        Assert.Equal(StepType.Wait, waitStep.Type);
    }

    [Fact]
    public void RequestStep_ShouldConfigureRequestProperties()
    {
        // Arrange & Act
        var step = new WorkflowStep
        {
            Name = "FetchData",
            Type = StepType.Request,
            RequestType = "GetUserRequest",
            OutputKey = "userData"
        };

        // Assert
        Assert.Equal("FetchData", step.Name);
        Assert.Equal(StepType.Request, step.Type);
        Assert.Equal("GetUserRequest", step.RequestType);
        Assert.Equal("userData", step.OutputKey);
    }

            [Fact]
            public void ConditionalStep_ShouldSupportConditionAndElseSteps()
            {
                // Arrange & Act
                var step = new WorkflowStep
                {
                    Name = "CheckStatus",
                    Type = StepType.Conditional,
                    Condition = "status == success",
                    ElseSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "HandleError", Type = StepType.Request }
                    }
                };
    
                // Assert
                Assert.Equal("CheckStatus", step.Name);
                Assert.Equal(StepType.Conditional, step.Type);
                Assert.Equal("status == success", step.Condition);
                Assert.NotNull(step.ElseSteps);
                var elseStep = Assert.Single(step.ElseSteps);
                Assert.Equal("HandleError", elseStep.Name);
            }
    
            [Fact]
            public void ParallelStep_ShouldSupportParallelSteps()
            {
                // Arrange & Act
                var step = new WorkflowStep
                {
                    Name = "ProcessParallel",
                    Type = StepType.Parallel,
                    ParallelSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep { Name = "Task1", Type = StepType.Request },
                        new WorkflowStep { Name = "Task2", Type = StepType.Request },
                        new WorkflowStep { Name = "Task3", Type = StepType.Request }
                    }
                };
    
                // Assert
                Assert.Equal("ProcessParallel", step.Name);
                Assert.Equal(StepType.Parallel, step.Type);
                Assert.NotNull(step.ParallelSteps);
                Assert.Equal(3, step.ParallelSteps.Count);
                Assert.Equal("Task1", step.ParallelSteps[0].Name);
                Assert.Equal("Task2", step.ParallelSteps[1].Name);
                Assert.Equal("Task3", step.ParallelSteps[2].Name);
            }
    
            [Fact]
            public void WaitStep_ShouldConfigureWaitTime()
            {
                // Arrange & Act
                var step = new WorkflowStep
                {
                    Name = "WaitForProcessing",
                    Type = StepType.Wait,
                    WaitTimeMs = 3000
                };
    
                // Assert
                Assert.Equal("WaitForProcessing", step.Name);
                Assert.Equal(StepType.Wait, step.Type);
                Assert.Equal(3000, step.WaitTimeMs);
            }
    
            [Fact]
            public void ContinueOnError_ShouldAllowStepToFailGracefully()
            {
                // Arrange & Act
                var step = new WorkflowStep
                {
                    Name = "OptionalStep",
                    Type = StepType.Request,
                    RequestType = "OptionalRequest",
                    ContinueOnError = true
                };
    
                // Assert
                Assert.True(step.ContinueOnError);
            }
    
            [Fact]
            public void Step_ShouldSupportNestedStepHierarchy()
            {
                // Arrange & Act
                var step = new WorkflowStep
                {
                    Name = "ParentStep",
                    Type = StepType.Parallel,
                    ParallelSteps = new List<WorkflowStep>
                    {
                        new WorkflowStep
                        {
                            Name = "NestedConditional",
                            Type = StepType.Conditional,
                            Condition = "value > 10",
                            ElseSteps = new List<WorkflowStep>
                            {
                                new WorkflowStep { Name = "DefaultAction", Type = StepType.Request }
                            }
                        },
                        new WorkflowStep
                        {
                            Name = "NestedParallel",
                            Type = StepType.Parallel,
                            ParallelSteps = new List<WorkflowStep>
                            {
                                new WorkflowStep { Name = "SubTask1", Type = StepType.Request },
                                new WorkflowStep { Name = "SubTask2", Type = StepType.Request }
                            }
                        }
                    }
                };
    
                // Assert
                Assert.NotNull(step.ParallelSteps);
                Assert.Equal(2, step.ParallelSteps.Count);
    
                var nestedConditional = step.ParallelSteps[0];
                Assert.Equal(StepType.Conditional, nestedConditional.Type);
                Assert.NotNull(nestedConditional.ElseSteps);
                var defaultAction = Assert.Single(nestedConditional.ElseSteps);
                Assert.Equal("DefaultAction", defaultAction.Name);
    
                var nestedParallel = step.ParallelSteps[1];
                Assert.Equal(StepType.Parallel, nestedParallel.Type);
                Assert.NotNull(nestedParallel.ParallelSteps);
                Assert.Equal(2, nestedParallel.ParallelSteps.Count);
            }
    [Fact]
    public void OutputKey_ShouldSpecifyContextStorageKey()
    {
        // Arrange & Act
        var step1 = new WorkflowStep
        {
            Name = "GetUser",
            Type = StepType.Request,
            RequestType = "GetUserRequest",
            OutputKey = "currentUser"
        };

        var step2 = new WorkflowStep
        {
            Name = "GetUserDefault",
            Type = StepType.Request,
            RequestType = "GetUserRequest"
            // OutputKey not specified, should use step name
        };

        // Assert
        Assert.Equal("currentUser", step1.OutputKey);
        Assert.Null(step2.OutputKey); // Will use step name by default in execution
    }

    [Fact]
    public void Condition_ShouldSupportVariousExpressions()
    {
        // Arrange & Act
        var equalityStep = new WorkflowStep
        {
            Name = "CheckEquality",
            Type = StepType.Conditional,
            Condition = "status == completed"
        };

        var comparisonStep = new WorkflowStep
        {
            Name = "CheckThreshold",
            Type = StepType.Conditional,
            Condition = "count > 100"
        };

        var stringStep = new WorkflowStep
        {
            Name = "CheckContains",
            Type = StepType.Conditional,
            Condition = "message contains error"
        };

        // Assert
        Assert.Equal("status == completed", equalityStep.Condition);
        Assert.Equal("count > 100", comparisonStep.Condition);
        Assert.Equal("message contains error", stringStep.Condition);
    }

    [Fact]
    public void ParallelSteps_ShouldBeModifiable()
    {
        // Arrange
        var step = new WorkflowStep
        {
            Name = "DynamicParallel",
            Type = StepType.Parallel,
            ParallelSteps = new List<WorkflowStep>()
        };

        // Act
        step.ParallelSteps.Add(new WorkflowStep { Name = "Task1", Type = StepType.Request });
        step.ParallelSteps.Add(new WorkflowStep { Name = "Task2", Type = StepType.Request });

        // Assert
        Assert.Equal(2, step.ParallelSteps.Count);
    }

    [Fact]
    public void ElseSteps_ShouldBeModifiable()
    {
        // Arrange
        var step = new WorkflowStep
        {
            Name = "ConditionalWithElse",
            Type = StepType.Conditional,
            Condition = "status == success",
            ElseSteps = new List<WorkflowStep>()
        };

        // Act
        step.ElseSteps.Add(new WorkflowStep { Name = "ErrorHandler1", Type = StepType.Request });
        step.ElseSteps.Add(new WorkflowStep { Name = "ErrorHandler2", Type = StepType.Request });

        // Assert
        Assert.Equal(2, step.ElseSteps.Count);
    }
}

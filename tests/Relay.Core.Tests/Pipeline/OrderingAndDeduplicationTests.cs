using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests.Pipeline;

public class OrderingAndDeduplicationTests
{
    [Fact]
    public async Task PipelineExecutor_Should_Execute_In_Correct_Order_SystemModules_Pipelines_Handler()
    {
        // Arrange
        var services = new ServiceCollection();
        var globalExecutionOrder = new List<string>();

        var systemModule1 = new TestSystemModuleWithGlobalOrder(1, globalExecutionOrder, "System1");
        var systemModule2 = new TestSystemModuleWithGlobalOrder(2, globalExecutionOrder, "System2");
        var behavior1 = new TestPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior1");
        var behavior2 = new TestPipelineBehavior(globalExecutionOrder, "Behavior2");

        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(systemModule1);
        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(systemModule2);
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>>>(new Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>[] { behavior1, behavior2 });
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            globalExecutionOrder.Add("Handler");
            return new ValueTask<string>("result");
        }

        // Act
        var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

        // Assert
        Assert.Equal("result_Modified_Modified", result);
        // System modules execute first (in order), then pipeline behaviors (in reverse order), then handler
        Assert.Equal(new[] { "System1_Before", "System2_Before", "Behavior2_Before", "Behavior1_Before", "Handler", "Behavior1_After", "Behavior2_After", "System2_After", "System1_After" }, globalExecutionOrder);
    }

    [Fact]
    public async Task PipelineExecutor_Should_DeDuplicate_Pipeline_Behaviors_By_Type()
    {
        // Arrange
        var services = new ServiceCollection();
        var globalExecutionOrder = new List<string>();

        // Create behaviors of the same type - they should be de-duplicated
        var behavior1 = new TestPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior1");
        var duplicateBehavior = new TestPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Duplicate"); // Same type, should replace behavior1
        var behavior2 = new TestPipelineBehavior(globalExecutionOrder, "Behavior2"); // Different type

        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>>>(new Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>[] { behavior1, duplicateBehavior, behavior2 });
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            globalExecutionOrder.Add("Handler");
            return new ValueTask<string>("result");
        }

        // Act
        var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

        // Assert
        Assert.Equal("result_Modified_Modified", result); // Only 2 behaviors executed (duplicate replaced the first)
        // Execution order: behavior2, then duplicateBehavior (replaced behavior1), then handler
        Assert.Equal(new[] { "Behavior2_Before", "Duplicate_Before", "Handler", "Duplicate_After", "Behavior2_After" }, globalExecutionOrder);
    }

    [Fact]
    public async Task PipelineExecutor_Should_Order_System_Modules_By_Order_Property()
    {
        // Arrange
        var services = new ServiceCollection();
        var globalExecutionOrder = new List<string>();

        // Add system modules with different orders (out of sequence)
        var module3 = new TestSystemModuleWithGlobalOrder(3, globalExecutionOrder, "Module3");
        var module1 = new TestSystemModuleWithGlobalOrder(1, globalExecutionOrder, "Module1");
        var module2 = new TestSystemModuleWithGlobalOrder(2, globalExecutionOrder, "Module2");

        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(module3);
        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(module1);
        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(module2);
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            globalExecutionOrder.Add("Handler");
            return new ValueTask<string>("result");
        }

        // Act
        var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

        // Assert
        Assert.Equal("result", result);
        // System modules should execute in order: 1, 2, 3
        Assert.Equal(new[] { "Module1_Before", "Module2_Before", "Module3_Before", "Handler", "Module3_After", "Module2_After", "Module1_After" }, globalExecutionOrder);
    }

    [Fact]
    public async Task PipelineExecutor_Should_Execute_Multiple_Behaviors_Of_Different_Types()
    {
        // Arrange
        var services = new ServiceCollection();
        var globalExecutionOrder = new List<string>();

        // Create behaviors of different types
        var behavior1 = new TestPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior1");
        var behavior2 = new TestPipelineBehaviorWithCancellationCheck(new List<CancellationToken>()); // Different type
        var behavior3 = new TestPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior3"); // Different type

        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>>>(
            new Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>[] { behavior1, behavior2, behavior3 });
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            globalExecutionOrder.Add("Handler");
            return new ValueTask<string>("result");
        }

        // Act
        var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

        // Assert
        Assert.Equal("result_Modified_Modified", result); // behavior3 replaces behavior1, behavior2 also modifies
        // Execution order: behavior3 (replaces behavior1), behavior2, handler, behavior2, behavior3
        // But only behavior3 tracks execution
        Assert.Equal(new[] { "Behavior3_Before", "Handler", "Behavior3_After" }, globalExecutionOrder);
    }
}

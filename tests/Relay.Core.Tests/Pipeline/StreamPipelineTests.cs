using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests.Pipeline;

public class StreamPipelineTests
{
    [Fact]
    public async Task PipelineExecutor_Should_Execute_Stream_With_System_Modules()
    {
        // Arrange
        var services = new ServiceCollection();
        var globalExecutionOrder = new List<string>();

        var systemModule = new TestSystemModuleWithGlobalOrder(1, globalExecutionOrder, "System");
        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(systemModule);
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestStreamRequest();

        IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
        {
            return GenerateStreamItems();
        }

        static async IAsyncEnumerable<string> GenerateStreamItems()
        {
            await Task.CompletedTask;
            yield return "item1";
            yield return "item2";
        }

        // Act
        var results = new List<string>();
        await foreach (var item in executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None))
        {
            results.Add(item);
            globalExecutionOrder.Add($"Received_{item}");
        }

        // Assert
        Assert.Equal(new[] { "item1", "item2" }, results);
        Assert.Contains("System_Stream_Before", globalExecutionOrder);
        Assert.Contains("System_Stream_Item", globalExecutionOrder);
        Assert.Contains("System_Stream_After", globalExecutionOrder);
    }

    [Fact]
    public async Task PipelineExecutor_Should_Execute_Stream_With_Pipeline_Behaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        var globalExecutionOrder = new List<string>();

        var streamBehavior = new TestStreamPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "StreamBehavior");
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IStreamPipelineBehavior<TestStreamRequest, string>>>(new Relay.Core.Contracts.Pipeline.IStreamPipelineBehavior<TestStreamRequest, string>[] { streamBehavior });
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestStreamRequest();

        IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
        {
            return GenerateStreamItems();
        }

        static async IAsyncEnumerable<string> GenerateStreamItems()
        {
            await Task.CompletedTask;
            yield return "item1";
            yield return "item2";
        }

        // Act
        var results = new List<string>();
        await foreach (var item in executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(new[] { "item1_Modified", "item2_Modified" }, results);
        Assert.Contains("StreamBehavior_Before", globalExecutionOrder);
        Assert.Contains("StreamBehavior_Item: item1", globalExecutionOrder);
        Assert.Contains("StreamBehavior_Item: item2", globalExecutionOrder);
        Assert.Contains("StreamBehavior_After", globalExecutionOrder);
    }

    [Fact]
    public async Task PipelineExecutor_Should_Execute_Stream_With_System_Modules_And_Behaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        var globalExecutionOrder = new List<string>();

        var systemModule = new TestSystemModuleWithGlobalOrder(1, globalExecutionOrder, "System");
        var streamBehavior = new TestStreamPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "StreamBehavior");

        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(systemModule);
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IStreamPipelineBehavior<TestStreamRequest, string>>>(new Relay.Core.Contracts.Pipeline.IStreamPipelineBehavior<TestStreamRequest, string>[] { streamBehavior });
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestStreamRequest();

        IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
        {
            return GenerateStreamItems();
        }

        static async IAsyncEnumerable<string> GenerateStreamItems()
        {
            await Task.CompletedTask;
            yield return "item1";
            yield return "item2";
        }

        // Act
        var results = new List<string>();
        await foreach (var item in executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(new[] { "item1_Modified", "item2_Modified" }, results);
        // System modules execute first, then behaviors
        var expectedOrder = new[] {
            "System_Stream_Before",
            "StreamBehavior_Before",
            "StreamBehavior_Item: item1",
            "System_Stream_Item",
            "StreamBehavior_Item: item2",
            "System_Stream_Item",
            "StreamBehavior_After",
            "System_Stream_After"
        };
        foreach (var expected in expectedOrder)
        {
            Assert.Contains(expected, globalExecutionOrder);
        }
    }

    [Fact]
    public async Task PipelineExecutor_Should_DeDuplicate_Stream_Pipeline_Behaviors_By_Type()
    {
        // Arrange
        var services = new ServiceCollection();
        var globalExecutionOrder = new List<string>();

        // Create stream behaviors of the same type - they should be de-duplicated
        var behavior1 = new TestStreamPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior1");
        var duplicateBehavior = new TestStreamPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Duplicate"); // Same type, should replace behavior1
        var behavior2 = new TestStreamPipelineBehaviorWithGlobalOrder(globalExecutionOrder, "Behavior2"); // Different type

        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IStreamPipelineBehavior<TestStreamRequest, string>>>(new Relay.Core.Contracts.Pipeline.IStreamPipelineBehavior<TestStreamRequest, string>[] { behavior1, duplicateBehavior, behavior2 });
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestStreamRequest();

        IAsyncEnumerable<string> Handler(TestStreamRequest req, CancellationToken ct)
        {
            return GenerateStreamItems();
        }

        static async IAsyncEnumerable<string> GenerateStreamItems()
        {
            await Task.CompletedTask;
            yield return "item1";
            yield return "item2";
        }

        // Act
        var results = new List<string>();
        await foreach (var item in executor.ExecuteStreamAsync<TestStreamRequest, string>(request, Handler, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(new[] { "item1_Modified", "item2_Modified" }, results); // All behaviors are same type, so only the last one (behavior2) executes
        // Execution order: only behavior2 executes
        Assert.Equal(new[] { "Behavior2_Before", "Behavior2_Item: item1", "Behavior2_Item: item2", "Behavior2_After" }, globalExecutionOrder);
    }
}

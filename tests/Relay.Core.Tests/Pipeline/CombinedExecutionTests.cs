using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests.Pipeline;

public class CombinedExecutionTests
{
    [Fact]
    public async Task PipelineExecutor_Should_Execute_With_System_Modules_And_Pipeline_Behaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        var systemModule = new TestSystemModule(1);
        var behavior = new TestPipelineBehavior();
        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(systemModule);
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>>>(new Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>[] { behavior });
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            return new ValueTask<string>("result");
        }

        // Act
        var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

        // Assert
        Assert.Equal("result_Modified", result);
        Assert.Equal(2, systemModule.ExecutionOrder.Count);
        Assert.Equal("SystemModule_1_Before", systemModule.ExecutionOrder[0]);
        Assert.Equal("SystemModule_1_After", systemModule.ExecutionOrder[1]);
        Assert.Equal(2, behavior.ExecutionOrder.Count);
        Assert.Equal("Before", behavior.ExecutionOrder[0]);
        Assert.Equal("After", behavior.ExecutionOrder[1]);
    }
}

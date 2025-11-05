using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests.Pipeline;

public class SystemModuleTests
{
    [Fact]
    public async Task PipelineExecutor_Should_Execute_With_System_Modules_Only()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionOrder = new List<string>();
        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(new TestSystemModule(1, executionOrder));
        services.AddSingleton<Relay.Core.Contracts.Core.ISystemModule>(new TestSystemModule(2, executionOrder));
        var serviceProvider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(serviceProvider);

        var request = new TestRequest();

        ValueTask<string> Handler(TestRequest req, CancellationToken ct)
        {
            executionOrder.Add("Handler");
            return new ValueTask<string>("result");
        }

        // Act
        var result = await executor.ExecuteAsync<TestRequest, string>(request, Handler, CancellationToken.None);

        // Assert
        Assert.Equal("result", result);
        Assert.Equal(5, executionOrder.Count);
        Assert.Equal("SystemModule_1_Before", executionOrder[0]);
        Assert.Equal("SystemModule_2_Before", executionOrder[1]);
        Assert.Equal("Handler", executionOrder[2]);
        Assert.Equal("SystemModule_2_After", executionOrder[3]);
        Assert.Equal("SystemModule_1_After", executionOrder[4]);
    }
}

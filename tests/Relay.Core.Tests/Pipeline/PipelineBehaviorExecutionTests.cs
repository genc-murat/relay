using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Implementation.Core;
using Xunit;


namespace Relay.Core.Tests.Pipeline;

public class PipelineBehaviorExecutionTests
{
    [Fact]
    public async Task PipelineExecutor_Should_Execute_With_Pipeline_Behaviors_Only()
    {
        // Arrange
        var services = new ServiceCollection();
        var receivedTokens = new List<CancellationToken>();
        var behavior1 = new TestPipelineBehavior();
        var behavior2 = new TestPipelineBehaviorWithCancellationCheck(receivedTokens);
        services.AddSingleton<IEnumerable<Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>>>(new Relay.Core.Contracts.Pipeline.IPipelineBehavior<TestRequest, string>[] { behavior1, behavior2 });
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
        Assert.Equal("result_Modified_Modified", result); // Each behavior appends "_Modified"
        Assert.Equal(2, behavior1.ExecutionOrder.Count);
        Assert.Equal("Before", behavior1.ExecutionOrder[0]);
        Assert.Equal("After", behavior1.ExecutionOrder[1]);
        Assert.Single(receivedTokens); // behavior2 should have received the cancellation token
    }
}
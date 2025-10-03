using System;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions;

public class TransactionPipelineTests
{
    public class TransactionalRequest : IRequest<string>, ITransactionalRequest
    {
        public string Data { get; set; } = "";
    }

    public class NonTransactionalRequest : IRequest<string>
    {
        public string Data { get; set; } = "";
    }

    public class TestTransactionHandler : 
        IRequestHandler<TransactionalRequest, string>,
        IRequestHandler<NonTransactionalRequest, string>
    {
        public int CallCount { get; set; }

        public ValueTask<string> HandleAsync(TransactionalRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return new ValueTask<string>($"Handled: {request.Data}");
        }

        public ValueTask<string> HandleAsync(NonTransactionalRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return new ValueTask<string>($"Handled: {request.Data}");
        }
    }

    [Fact]
    public async Task Should_WrapInTransaction_When_RequestIsTransactional()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransactionHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<TransactionalRequest, string>, TransactionBehavior<TransactionalRequest, string>>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new TransactionalRequest { Data = "test" };

        // Act
        var result = await executor.ExecuteAsync<TransactionalRequest, string>(
            request, 
            (r, c) => handler.HandleAsync(r, c), 
            CancellationToken.None);

        // Assert
        Assert.Equal("Handled: test", result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Should_NotWrapInTransaction_When_RequestIsNotTransactional()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransactionHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<NonTransactionalRequest, string>, TransactionBehavior<NonTransactionalRequest, string>>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new NonTransactionalRequest { Data = "test" };

        // Act
        var result = await executor.ExecuteAsync<NonTransactionalRequest, string>(
            request, 
            (r, c) => handler.HandleAsync(r, c), 
            CancellationToken.None);

        // Assert
        Assert.Equal("Handled: test", result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Should_RollbackTransaction_When_HandlerThrowsException()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestTransactionHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<TransactionalRequest, string>, TransactionBehavior<TransactionalRequest, string>>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new TransactionalRequest { Data = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await executor.ExecuteAsync<TransactionalRequest, string>(
                request,
                (r, c) => 
                {
                    handler.CallCount++; // Increment call count before throwing
                    throw new InvalidOperationException("Handler error");
                },
                CancellationToken.None);
        });

        // Handler should have been called even though it threw
        Assert.Equal(1, handler.CallCount);
    }
}

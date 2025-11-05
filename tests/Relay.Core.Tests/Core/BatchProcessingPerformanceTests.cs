using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests.Core;

public class BatchProcessingPerformanceTests
{
    public class BatchRequest : IRequest<List<string>>
    {
        public List<string> Items { get; set; } = new();
    }

    public class BatchHandler : IRequestHandler<BatchRequest, List<string>>
    {
        public ValueTask<List<string>> HandleAsync(BatchRequest request, CancellationToken cancellationToken)
        {
            var results = request.Items.Select(item => item.ToUpperInvariant()).ToList();
            return new ValueTask<List<string>>(results);
        }
    }

    [Fact]
    public async Task Should_ProcessLargeBatch()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new BatchHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var largeList = Enumerable.Range(1, 1000).Select(i => $"item{i}").ToList();
        var request = new BatchRequest { Items = largeList };

        // Act
        var result = await executor.ExecuteAsync<BatchRequest, List<string>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(1000, result.Count);
        Assert.Equal("ITEM1", result[0]);
        Assert.Equal("ITEM1000", result[999]);
    }

    [Fact]
    public async Task Should_HandleBatchWithCancellation()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new BatchNumberHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchWithValidationRequest
        {
            Numbers = new List<int> { 1, 2, 3 }
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await executor.ExecuteAsync<BatchWithValidationRequest, List<int>>(
                request,
                async (r, c) =>
                {
                    c.ThrowIfCancellationRequested();
                    return await handler.HandleAsync(r, c);
                },
                cts.Token);
        });
    }

    public class BatchWithValidationRequest : IRequest<List<int>>
    {
        public List<int> Numbers { get; set; } = new();
    }

    public class BatchNumberHandler : IRequestHandler<BatchWithValidationRequest, List<int>>
    {
        public ValueTask<List<int>> HandleAsync(BatchWithValidationRequest request, CancellationToken cancellationToken)
        {
            var results = request.Numbers.Select(n => n * 2).ToList();
            return new ValueTask<List<int>>(results);
        }
    }
}

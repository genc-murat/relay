using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests.Core;

public class BatchProcessingBasicTests
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
    public async Task Should_ProcessBatchSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new BatchHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchRequest
        {
            Items = new List<string> { "item1", "item2", "item3" }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchRequest, List<string>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("ITEM1", result);
        Assert.Contains("ITEM2", result);
        Assert.Contains("ITEM3", result);
    }

    [Fact]
    public async Task Should_HandleEmptyBatch()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new BatchHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchRequest
        {
            Items = new List<string>()
        };

        // Act
        var result = await executor.ExecuteAsync<BatchRequest, List<string>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task Should_ProcessBatchWithSingleItem()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new BatchHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchRequest
        {
            Items = new List<string> { "single" }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchRequest, List<string>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("SINGLE", result[0]);
    }

    [Fact]
    public async Task Should_ProcessBatchWithZeroValues()
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
            Numbers = new List<int> { 0, 0, 0 }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchWithValidationRequest, List<int>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(new List<int> { 0, 0, 0 }, result);
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

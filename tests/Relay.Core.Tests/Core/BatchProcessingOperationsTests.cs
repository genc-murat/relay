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

public class BatchProcessingOperationsTests
{
    public class BatchWithDuplicatesRequest : IRequest<List<string>>
    {
        public List<string> Items { get; set; } = new();
    }

    public class DeduplicatingHandler : IRequestHandler<BatchWithDuplicatesRequest, List<string>>
    {
        public ValueTask<List<string>> HandleAsync(BatchWithDuplicatesRequest request, CancellationToken cancellationToken)
        {
            var results = request.Items.Distinct().ToList();
            return new ValueTask<List<string>>(results);
        }
    }

    [Fact]
    public async Task Should_DeduplicateBatchItems()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new DeduplicatingHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchWithDuplicatesRequest
        {
            Items = new List<string> { "a", "b", "a", "c", "b", "a" }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchWithDuplicatesRequest, List<string>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("a", result);
        Assert.Contains("b", result);
        Assert.Contains("c", result);
    }

    public class BatchPartitionRequest : IRequest<Dictionary<bool, List<int>>>
    {
        public List<int> Numbers { get; set; } = new();
    }

    public class PartitionHandler : IRequestHandler<BatchPartitionRequest, Dictionary<bool, List<int>>>
    {
        public ValueTask<Dictionary<bool, List<int>>> HandleAsync(BatchPartitionRequest request, CancellationToken cancellationToken)
        {
            var results = request.Numbers
                .GroupBy(n => n % 2 == 0)
                .ToDictionary(g => g.Key, g => g.ToList());
            return new ValueTask<Dictionary<bool, List<int>>>(results);
        }
    }

    [Fact]
    public async Task Should_PartitionBatchByPredicate()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new PartitionHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchPartitionRequest
        {
            Numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchPartitionRequest, Dictionary<bool, List<int>>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(new List<int> { 2, 4, 6, 8, 10 }, result[true]);
        Assert.Equal(new List<int> { 1, 3, 5, 7, 9 }, result[false]);
    }

    public class BatchAggregationRequest : IRequest<int>
    {
        public List<int> Numbers { get; set; } = new();
    }

    public class SumHandler : IRequestHandler<BatchAggregationRequest, int>
    {
        public ValueTask<int> HandleAsync(BatchAggregationRequest request, CancellationToken cancellationToken)
        {
            var sum = request.Numbers.Sum();
            return new ValueTask<int>(sum);
        }
    }

    [Fact]
    public async Task Should_AggregateBatchToSingleValue()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new SumHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchAggregationRequest
        {
            Numbers = new List<int> { 1, 2, 3, 4, 5 }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchAggregationRequest, int>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(15, result);
    }

    public class BatchTransformRequest : IRequest<List<string>>
    {
        public List<int> Numbers { get; set; } = new();
    }

    public class TransformHandler : IRequestHandler<BatchTransformRequest, List<string>>
    {
        public ValueTask<List<string>> HandleAsync(BatchTransformRequest request, CancellationToken cancellationToken)
        {
            var results = request.Numbers.Select(n => $"Number: {n}").ToList();
            return new ValueTask<List<string>>(results);
        }
    }

    [Fact]
    public async Task Should_TransformBatchItemTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TransformHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchTransformRequest
        {
            Numbers = new List<int> { 1, 2, 3 }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchTransformRequest, List<string>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(new List<string> { "Number: 1", "Number: 2", "Number: 3" }, result);
    }

    public class BatchFilterRequest : IRequest<List<int>>
    {
        public List<int> Numbers { get; set; } = new();
        public int Threshold { get; set; }
    }

    public class FilterHandler : IRequestHandler<BatchFilterRequest, List<int>>
    {
        public ValueTask<List<int>> HandleAsync(BatchFilterRequest request, CancellationToken cancellationToken)
        {
            var results = request.Numbers.Where(n => n > request.Threshold).ToList();
            return new ValueTask<List<int>>(results);
        }
    }

    [Fact]
    public async Task Should_FilterBatchItems()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new FilterHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchFilterRequest
        {
            Numbers = new List<int> { 1, 5, 3, 8, 2, 9, 4 },
            Threshold = 5
        };

        // Act
        var result = await executor.ExecuteAsync<BatchFilterRequest, List<int>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(new List<int> { 8, 9 }, result);
    }

    [Fact]
    public async Task Should_HandleBatchWithAllItemsFiltered()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new FilterHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchFilterRequest
        {
            Numbers = new List<int> { 1, 2, 3 },
            Threshold = 10
        };

        // Act
        var result = await executor.ExecuteAsync<BatchFilterRequest, List<int>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    public class BatchSortRequest : IRequest<List<int>>
    {
        public List<int> Numbers { get; set; } = new();
        public bool Descending { get; set; }
    }

    public class SortHandler : IRequestHandler<BatchSortRequest, List<int>>
    {
        public ValueTask<List<int>> HandleAsync(BatchSortRequest request, CancellationToken cancellationToken)
        {
            var results = request.Descending
                ? request.Numbers.OrderByDescending(n => n).ToList()
                : request.Numbers.OrderBy(n => n).ToList();
            return new ValueTask<List<int>>(results);
        }
    }

    [Fact]
    public async Task Should_SortBatchAscending()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new SortHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchSortRequest
        {
            Numbers = new List<int> { 5, 2, 8, 1, 9, 3 },
            Descending = false
        };

        // Act
        var result = await executor.ExecuteAsync<BatchSortRequest, List<int>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(new List<int> { 1, 2, 3, 5, 8, 9 }, result);
    }

    [Fact]
    public async Task Should_SortBatchDescending()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new SortHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchSortRequest
        {
            Numbers = new List<int> { 5, 2, 8, 1, 9, 3 },
            Descending = true
        };

        // Act
        var result = await executor.ExecuteAsync<BatchSortRequest, List<int>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(new List<int> { 9, 8, 5, 3, 2, 1 }, result);
    }

    public class BatchCountRequest : IRequest<Dictionary<string, int>>
    {
        public List<string> Items { get; set; } = new();
    }

    public class CountHandler : IRequestHandler<BatchCountRequest, Dictionary<string, int>>
    {
        public ValueTask<Dictionary<string, int>> HandleAsync(BatchCountRequest request, CancellationToken cancellationToken)
        {
            var results = request.Items
                .GroupBy(item => item)
                .ToDictionary(g => g.Key, g => g.Count());
            return new ValueTask<Dictionary<string, int>>(results);
        }
    }

    [Fact]
    public async Task Should_CountBatchItemOccurrences()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new CountHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchCountRequest
        {
            Items = new List<string> { "a", "b", "a", "c", "b", "a" }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchCountRequest, Dictionary<string, int>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        Assert.Equal(3, result["a"]);
        Assert.Equal(2, result["b"]);
        Assert.Equal(1, result["c"]);
    }
}

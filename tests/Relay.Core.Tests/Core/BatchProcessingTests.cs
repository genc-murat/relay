using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Xunit;

namespace Relay.Core.Tests.Core;

public class BatchProcessingTests
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
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "ITEM1", "ITEM2", "ITEM3" });
    }

    public class BatchWithValidationRequest : IRequest<List<int>>
    {
        public List<int> Numbers { get; set; } = new();
    }

    public class BatchValidationPipeline : IPipelineBehavior<BatchWithValidationRequest, List<int>>
    {
        public async ValueTask<List<int>> HandleAsync(
            BatchWithValidationRequest request,
            RequestHandlerDelegate<List<int>> next,
            CancellationToken cancellationToken)
        {
            // Validate all items
            if (request.Numbers.Any(n => n < 0))
            {
                throw new ArgumentException("Negative numbers not allowed");
            }

            return await next();
        }
    }

    public class BatchNumberHandler : IRequestHandler<BatchWithValidationRequest, List<int>>
    {
        public ValueTask<List<int>> HandleAsync(BatchWithValidationRequest request, CancellationToken cancellationToken)
        {
            var results = request.Numbers.Select(n => n * 2).ToList();
            return new ValueTask<List<int>>(results);
        }
    }

    [Fact]
    public async Task Should_ValidateBatchItems_BeforeProcessing()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new BatchNumberHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<BatchWithValidationRequest, List<int>>, BatchValidationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchWithValidationRequest
        {
            Numbers = new List<int> { 1, 2, -3, 4 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await executor.ExecuteAsync<BatchWithValidationRequest, List<int>>(
                request,
                (r, c) => handler.HandleAsync(r, c),
                CancellationToken.None);
        });
    }

    [Fact]
    public async Task Should_ProcessValidBatch_Successfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new BatchNumberHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<BatchWithValidationRequest, List<int>>, BatchValidationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchWithValidationRequest
        {
            Numbers = new List<int> { 1, 2, 3, 4 }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchWithValidationRequest, List<int>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        result.Should().Equal(2, 4, 6, 8);
    }

    [Fact]
    public async Task Should_HandleEmptyBatch()
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
            Numbers = new List<int>()
        };

        // Act
        var result = await executor.ExecuteAsync<BatchWithValidationRequest, List<int>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
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
        result.Should().HaveCount(1000);
        result[0].Should().Be("ITEM1");
        result[999].Should().Be("ITEM1000");
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

    public class BatchWithNullItemsRequest : IRequest<List<string>>
    {
        public List<string?> Items { get; set; } = new();
    }

    public class NullSafeHandler : IRequestHandler<BatchWithNullItemsRequest, List<string>>
    {
        public ValueTask<List<string>> HandleAsync(BatchWithNullItemsRequest request, CancellationToken cancellationToken)
        {
            var results = request.Items
                .Where(item => item != null)
                .Select(item => item!.ToUpperInvariant())
                .ToList();
            return new ValueTask<List<string>>(results);
        }
    }

    [Fact]
    public async Task Should_HandleBatchWithNullItems()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new NullSafeHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchWithNullItemsRequest
        {
            Items = new List<string?> { "item1", null, "item2", null, "item3" }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchWithNullItemsRequest, List<string>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Equal("ITEM1", "ITEM2", "ITEM3");
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
        result.Should().HaveCount(1);
        result[0].Should().Be("SINGLE");
    }

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
        result.Should().HaveCount(3);
        result.Should().Contain(new[] { "a", "b", "c" });
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
        result[true].Should().Equal(2, 4, 6, 8, 10);
        result[false].Should().Equal(1, 3, 5, 7, 9);
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
        result.Should().Be(15);
    }

    [Fact]
    public async Task Should_ValidateAllBatchItems()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new BatchNumberHandler();

        services.AddLogging();
        services.AddSingleton(handler);
        services.AddTransient<IPipelineBehavior<BatchWithValidationRequest, List<int>>, BatchValidationPipeline>();

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchWithValidationRequest
        {
            Numbers = new List<int> { -1, -2, -3 }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await executor.ExecuteAsync<BatchWithValidationRequest, List<int>>(
                request,
                (r, c) => handler.HandleAsync(r, c),
                CancellationToken.None);
        });
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
        result.Should().Equal("Number: 1", "Number: 2", "Number: 3");
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
        result.Should().Equal(8, 9);
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
        result.Should().BeEmpty();
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
        result.Should().Equal(1, 2, 3, 5, 8, 9);
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
        result.Should().Equal(9, 8, 5, 3, 2, 1);
    }

    public class BatchWithSpecialCharsRequest : IRequest<List<string>>
    {
        public List<string> Items { get; set; } = new();
    }

    public class SpecialCharsHandler : IRequestHandler<BatchWithSpecialCharsRequest, List<string>>
    {
        public ValueTask<List<string>> HandleAsync(BatchWithSpecialCharsRequest request, CancellationToken cancellationToken)
        {
            var results = request.Items.Select(item => item.Trim()).ToList();
            return new ValueTask<List<string>>(results);
        }
    }

    [Fact]
    public async Task Should_HandleBatchWithSpecialCharacters()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new SpecialCharsHandler();

        services.AddLogging();
        services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        var executor = new PipelineExecutor(provider);
        var request = new BatchWithSpecialCharsRequest
        {
            Items = new List<string> { "  hello  ", "\ttab\t", "\nnewline\n" }
        };

        // Act
        var result = await executor.ExecuteAsync<BatchWithSpecialCharsRequest, List<string>>(
            request,
            (r, c) => handler.HandleAsync(r, c),
            CancellationToken.None);

        // Assert
        result.Should().Equal("hello", "tab", "newline");
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
        result.Should().Equal(0, 0, 0);
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
        result["a"].Should().Be(3);
        result["b"].Should().Be(2);
        result["c"].Should().Be(1);
    }
}

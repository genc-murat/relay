using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
}

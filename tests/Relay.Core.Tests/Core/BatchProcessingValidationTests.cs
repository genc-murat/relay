using System;
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

public class BatchProcessingValidationTests
{
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
        Assert.Equal(new List<int> { 2, 4, 6, 8 }, result);
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
}

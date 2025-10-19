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

public class BatchProcessingEdgeCasesTests
{
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
        Assert.Equal(3, result.Count);
        Assert.Equal(new List<string> { "ITEM1", "ITEM2", "ITEM3" }, result);
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
        Assert.Equal(new List<string> { "hello", "tab", "newline" }, result);
    }
}
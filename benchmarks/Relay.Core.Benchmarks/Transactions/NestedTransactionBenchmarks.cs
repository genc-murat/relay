using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Transactions;
using System.Data;

namespace Relay.Core.Benchmarks.Transactions;

/// <summary>
/// Benchmarks for nested transaction scenarios.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class NestedTransactionBenchmarks
{
    private ServiceProvider _serviceProvider = null!;
    private IRelay _relay = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddRelayWithHandlerDiscovery(new[] { typeof(OuterTransactionalCommand).Assembly });
        services.AddRelayTransactions(options =>
        {
            options.DefaultTimeout = TimeSpan.FromSeconds(30);
            options.EnableMetrics = true;
            options.EnableNestedTransactions = true;
        });
        services.AddSingleton<IUnitOfWork, BenchmarkUnitOfWork>();
        
        _serviceProvider = services.BuildServiceProvider();
        _relay = _serviceProvider.GetRequiredService<IRelay>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task<int> NestedTransaction_TwoLevels()
    {
        return await _relay.SendAsync(new OuterTransactionalCommand { NestedLevels = 1 });
    }

    [Benchmark]
    public async Task<int> NestedTransaction_ThreeLevels()
    {
        return await _relay.SendAsync(new OuterTransactionalCommand { NestedLevels = 2 });
    }

    [Benchmark]
    public async Task<int> NestedTransaction_FiveLevels()
    {
        return await _relay.SendAsync(new OuterTransactionalCommand { NestedLevels = 4 });
    }
}

[Transaction(IsolationLevel.ReadCommitted)]
public record OuterTransactionalCommand : IRequest<int>, ITransactionalRequest
{
    public int NestedLevels { get; init; }
}

public class OuterTransactionalCommandHandler : IRequestHandler<OuterTransactionalCommand, int>
{
    private readonly IRelay _relay;

    public OuterTransactionalCommandHandler(IRelay relay)
    {
        _relay = relay;
    }

    public async ValueTask<int> HandleAsync(OuterTransactionalCommand request, CancellationToken cancellationToken = default)
    {
        if (request.NestedLevels > 0)
        {
            // Call nested transactional command
            return await _relay.SendAsync(new InnerTransactionalCommand { RemainingLevels = request.NestedLevels - 1 }, cancellationToken);
        }
        return 1;
    }
}

[Transaction(IsolationLevel.ReadCommitted)]
public record InnerTransactionalCommand : IRequest<int>, ITransactionalRequest
{
    public int RemainingLevels { get; init; }
}

public class InnerTransactionalCommandHandler : IRequestHandler<InnerTransactionalCommand, int>
{
    private readonly IRelay _relay;

    public InnerTransactionalCommandHandler(IRelay relay)
    {
        _relay = relay;
    }

    public async ValueTask<int> HandleAsync(InnerTransactionalCommand request, CancellationToken cancellationToken = default)
    {
        if (request.RemainingLevels > 0)
        {
            return await _relay.SendAsync(new InnerTransactionalCommand { RemainingLevels = request.RemainingLevels - 1 }, cancellationToken);
        }
        return 1;
    }
}

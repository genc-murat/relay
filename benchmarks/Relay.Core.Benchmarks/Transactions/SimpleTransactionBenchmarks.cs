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
/// Benchmarks for simple transaction scenarios without nesting or savepoints.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class SimpleTransactionBenchmarks
{
    private ServiceProvider _serviceProvider = null!;
    private IRelay _relay = null!;
    private SimpleTransactionalCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddRelayWithHandlerDiscovery(new[] { typeof(SimpleTransactionalCommand).Assembly });
        services.AddRelayTransactions(options =>
        {
            options.DefaultTimeout = TimeSpan.FromSeconds(30);
            options.EnableMetrics = true;
        });
        services.AddSingleton<IUnitOfWork, BenchmarkUnitOfWork>();
        
        _serviceProvider = services.BuildServiceProvider();
        _relay = _serviceProvider.GetRequiredService<IRelay>();
        _command = new SimpleTransactionalCommand();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _serviceProvider?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task<int> SimpleTransaction_ReadCommitted()
    {
        return await _relay.SendAsync(_command);
    }

    [Benchmark]
    public async Task<int> SimpleTransaction_Serializable()
    {
        var command = new SimpleTransactionalCommand { IsolationLevel = IsolationLevel.Serializable };
        return await _relay.SendAsync(command);
    }

    [Benchmark]
    public async Task<int> SimpleTransaction_ReadOnly()
    {
        var command = new SimpleTransactionalCommand { IsReadOnly = true };
        return await _relay.SendAsync(command);
    }

    [Benchmark]
    public async Task<int> MultipleSequentialTransactions()
    {
        var result = 0;
        for (int i = 0; i < 10; i++)
        {
            result += await _relay.SendAsync(_command);
        }
        return result;
    }
}

[Transaction(IsolationLevel.ReadCommitted)]
public record SimpleTransactionalCommand : IRequest<int>, ITransactionalRequest
{
    public IsolationLevel IsolationLevel { get; init; } = IsolationLevel.ReadCommitted;
    public bool IsReadOnly { get; init; }
}

public class SimpleTransactionalCommandHandler : IRequestHandler<SimpleTransactionalCommand, int>
{
    public ValueTask<int> HandleAsync(SimpleTransactionalCommand request, CancellationToken cancellationToken = default)
    {
        // Simulate some work
        return ValueTask.FromResult(42);
    }
}

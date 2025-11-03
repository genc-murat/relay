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
/// Benchmarks for savepoint operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class SavepointBenchmarks
{
    private ServiceProvider _serviceProvider = null!;
    private IRelay _relay = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddRelayWithHandlerDiscovery(new[] { typeof(SavepointCommand).Assembly });
        services.AddRelayTransactions(options =>
        {
            options.DefaultTimeout = TimeSpan.FromSeconds(30);
            options.EnableMetrics = true;
            options.EnableSavepoints = true;
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
    public async Task<int> CreateSingleSavepoint()
    {
        return await _relay.SendAsync(new SavepointCommand { SavepointCount = 1 });
    }

    [Benchmark]
    public async Task<int> CreateMultipleSavepoints()
    {
        return await _relay.SendAsync(new SavepointCommand { SavepointCount = 5 });
    }

    [Benchmark]
    public async Task<int> CreateAndRollbackSavepoint()
    {
        return await _relay.SendAsync(new SavepointCommand { SavepointCount = 1, RollbackToFirst = true });
    }

    [Benchmark]
    public async Task<int> CreateMultipleAndRollback()
    {
        return await _relay.SendAsync(new SavepointCommand { SavepointCount = 5, RollbackToFirst = true });
    }
}

[Transaction(IsolationLevel.ReadCommitted)]
public record SavepointCommand : IRequest<int>, ITransactionalRequest
{
    public int SavepointCount { get; init; }
    public bool RollbackToFirst { get; init; }
}

public class SavepointCommandHandler : IRequestHandler<SavepointCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public SavepointCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async ValueTask<int> HandleAsync(SavepointCommand request, CancellationToken cancellationToken = default)
    {
        var savepoints = new List<ISavepoint>();
        
        for (int i = 0; i < request.SavepointCount; i++)
        {
            var savepoint = await _unitOfWork.CreateSavepointAsync($"sp_{i}", cancellationToken);
            savepoints.Add(savepoint);
        }

        if (request.RollbackToFirst && savepoints.Count > 0)
        {
            await _unitOfWork.RollbackToSavepointAsync("sp_0", cancellationToken);
        }

        return savepoints.Count;
    }
}

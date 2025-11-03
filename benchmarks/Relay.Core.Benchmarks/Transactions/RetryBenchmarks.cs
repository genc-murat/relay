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
/// Benchmarks for transaction retry scenarios.
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[MarkdownExporter]
public class RetryBenchmarks
{
    private ServiceProvider _serviceProvider = null!;
    private IRelay _relay = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddRelayWithHandlerDiscovery(new[] { typeof(RetryCommand).Assembly });
        services.AddRelayTransactions(options =>
        {
            options.DefaultTimeout = TimeSpan.FromSeconds(30);
            options.EnableMetrics = true;
            options.DefaultRetryPolicy = new TransactionRetryPolicy
            {
                MaxRetries = 3,
                InitialDelay = TimeSpan.FromMilliseconds(10),
                Strategy = RetryStrategy.ExponentialBackoff
            };
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
    public async Task<int> NoRetry_Success()
    {
        RetryCommandHandler.FailureCount = 0;
        return await _relay.SendAsync(new RetryCommand());
    }

    [Benchmark]
    public async Task<int> LinearRetry_OneFailure()
    {
        RetryCommandHandler.FailureCount = 1;
        return await _relay.SendAsync(new RetryCommand { RetryStrategy = RetryStrategy.Linear });
    }

    [Benchmark]
    public async Task<int> ExponentialRetry_OneFailure()
    {
        RetryCommandHandler.FailureCount = 1;
        return await _relay.SendAsync(new RetryCommand { RetryStrategy = RetryStrategy.ExponentialBackoff });
    }

    [Benchmark]
    public async Task<int> LinearRetry_TwoFailures()
    {
        RetryCommandHandler.FailureCount = 2;
        return await _relay.SendAsync(new RetryCommand { RetryStrategy = RetryStrategy.Linear });
    }

    [Benchmark]
    public async Task<int> ExponentialRetry_TwoFailures()
    {
        RetryCommandHandler.FailureCount = 2;
        return await _relay.SendAsync(new RetryCommand { RetryStrategy = RetryStrategy.ExponentialBackoff });
    }
}

[Transaction(IsolationLevel.ReadCommitted)]
[TransactionRetry(MaxRetries = 3, InitialDelayMs = 10)]
public record RetryCommand : IRequest<int>, ITransactionalRequest
{
    public RetryStrategy RetryStrategy { get; init; } = RetryStrategy.ExponentialBackoff;
}

public class RetryCommandHandler : IRequestHandler<RetryCommand, int>
{
    public static int FailureCount { get; set; }
    private static int _attemptCount;

    public ValueTask<int> HandleAsync(RetryCommand request, CancellationToken cancellationToken = default)
    {
        _attemptCount++;
        
        if (_attemptCount <= FailureCount)
        {
            // Simulate transient error
            throw new InvalidOperationException("Simulated transient error");
        }

        var result = _attemptCount;
        _attemptCount = 0; // Reset for next benchmark
        return ValueTask.FromResult(result);
    }
}

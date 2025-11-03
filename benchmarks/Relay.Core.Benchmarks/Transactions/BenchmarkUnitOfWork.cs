using Relay.Core.Transactions;
using System.Data;
using SystemIDbTransaction = System.Data.IDbTransaction;
using RelayIDbTransaction = Relay.Core.Transactions.IDbTransaction;

namespace Relay.Core.Benchmarks.Transactions;

/// <summary>
/// Lightweight unit of work implementation for benchmarking that minimizes overhead.
/// </summary>
public class BenchmarkUnitOfWork : IUnitOfWork
{
    private readonly Dictionary<string, BenchmarkSavepoint> _savepoints = new();
    private BenchmarkTransaction? _currentTransaction;

    public ITransactionContext? CurrentTransactionContext { get; private set; }
    public bool IsReadOnly { get; set; }

    public Task<RelayIDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        _currentTransaction = new BenchmarkTransaction(isolationLevel);
        CurrentTransactionContext = new BenchmarkTransactionContext(isolationLevel);
        return Task.FromResult<RelayIDbTransaction>(_currentTransaction);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (IsReadOnly)
        {
            throw new ReadOnlyTransactionViolationException("Cannot save changes in read-only transaction");
        }
        return Task.FromResult(1);
    }

    public Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        var savepoint = new BenchmarkSavepoint(name);
        _savepoints[name] = savepoint;
        return Task.FromResult<ISavepoint>(savepoint);
    }

    public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!_savepoints.ContainsKey(name))
        {
            throw new SavepointException($"Savepoint '{name}' not found");
        }
        return Task.CompletedTask;
    }
}

internal class BenchmarkTransaction : RelayIDbTransaction
{
    public BenchmarkTransaction(IsolationLevel isolationLevel)
    {
        IsolationLevel = isolationLevel;
    }

    public IDbConnection? Connection => null;
    public IsolationLevel IsolationLevel { get; }

    public void Commit() { }
    public void Rollback() { }
    public void Dispose() { }
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal class BenchmarkSavepoint : ISavepoint
{
    public BenchmarkSavepoint(string name)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    public string Name { get; }
    public DateTime CreatedAt { get; }

    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ReleaseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal class BenchmarkTransactionContext : ITransactionContext
{
    public BenchmarkTransactionContext(IsolationLevel isolationLevel)
    {
        TransactionId = Guid.NewGuid().ToString();
        IsolationLevel = isolationLevel;
        StartedAt = DateTime.UtcNow;
    }

    public string TransactionId { get; }
    public int NestingLevel { get; set; }
    public IsolationLevel IsolationLevel { get; }
    public bool IsReadOnly { get; }
    public DateTime StartedAt { get; }
    public RelayIDbTransaction? CurrentTransaction { get; set; }

    public Task<ISavepoint> CreateSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ISavepoint>(new BenchmarkSavepoint(name));
    }

    public Task RollbackToSavepointAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Transactions;

/// <summary>
/// Implementation of <see cref="ISavepoint"/> that manages savepoint lifecycle and operations.
/// </summary>
internal sealed class Savepoint : ISavepoint
{
    private readonly IDbTransaction _transaction;
    private readonly IDbCommand _command;
    private bool _isDisposed;
    private bool _isReleased;
    private bool _isRolledBack;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Savepoint"/> class.
    /// </summary>
    /// <param name="name">The name of the savepoint.</param>
    /// <param name="transaction">The database transaction.</param>
    /// <exception cref="ArgumentNullException">Thrown when name or transaction is null.</exception>
    /// <exception cref="ArgumentException">Thrown when name is empty or whitespace.</exception>
    public Savepoint(string name, IDbTransaction transaction)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Savepoint name cannot be null or whitespace.", nameof(name));

        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        
        var connection = transaction.Connection;
        if (connection == null)
            throw new InvalidOperationException("Transaction connection is not available.");
            
        _command = connection.CreateCommand();
        
        // Cast to System.Data.IDbTransaction for command assignment
        if (transaction is System.Data.IDbTransaction sysTransaction)
        {
            _command.Transaction = sysTransaction;
        }

        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_isReleased)
            throw new SavepointException($"Cannot rollback savepoint '{Name}' because it has been released.");

        try
        {
            _command.CommandText = $"ROLLBACK TO SAVEPOINT {Name}";
            await ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            _isRolledBack = true;
        }
        catch (Exception ex) when (ex is not SavepointException)
        {
            throw new SavepointException($"Failed to rollback to savepoint '{Name}'.", ex);
        }
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (_isReleased)
            return; // Already released, no-op

        try
        {
            _command.CommandText = $"RELEASE SAVEPOINT {Name}";
            await ExecuteCommandAsync(cancellationToken).ConfigureAwait(false);
            _isReleased = true;
        }
        catch (Exception ex) when (ex is not SavepointException)
        {
            throw new SavepointException($"Failed to release savepoint '{Name}'.", ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        try
        {
            // Release the savepoint if it hasn't been released or rolled back
            if (!_isReleased && !_isRolledBack)
            {
                try
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
                catch
                {
                    // Suppress exceptions during disposal
                }
            }
        }
        finally
        {
            _command?.Dispose();
            _isDisposed = true;
        }
    }

    private async Task ExecuteCommandAsync(CancellationToken cancellationToken)
    {
        // Since IDbCommand doesn't have async methods, we wrap in Task.Run
        // In a real implementation, you might use DbCommand which has async methods
        await Task.Run(() => _command.ExecuteNonQuery(), cancellationToken).ConfigureAwait(false);
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(Savepoint), $"Savepoint '{Name}' has been disposed.");
    }
}

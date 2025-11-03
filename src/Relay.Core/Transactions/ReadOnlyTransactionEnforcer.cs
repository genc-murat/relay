using Microsoft.Extensions.Logging;
using System;

namespace Relay.Core.Transactions;

/// <summary>
/// Utility class for enforcing read-only transaction constraints.
/// </summary>
/// <remarks>
/// This class provides helper methods for IUnitOfWork implementations to enforce
/// read-only transaction behavior. It should be used in SaveChangesAsync implementations
/// to prevent data modifications in read-only transactions.
/// 
/// <para>Example usage in a DbContext implementation:</para>
/// <code>
/// public class ApplicationDbContext : DbContext, IUnitOfWork
/// {
///     private bool _isReadOnly;
///     private ITransactionContext? _currentTransactionContext;
///     
///     public bool IsReadOnly 
///     { 
///         get => _isReadOnly;
///         set => _isReadOnly = value;
///     }
///     
///     public ITransactionContext? CurrentTransactionContext => _currentTransactionContext;
///     
///     public override async Task&lt;int&gt; SaveChangesAsync(CancellationToken cancellationToken = default)
///     {
///         // Enforce read-only transaction constraint
///         ReadOnlyTransactionEnforcer.ThrowIfReadOnly(this);
///         
///         return await base.SaveChangesAsync(cancellationToken);
///     }
/// }
/// </code>
/// </remarks>
public static class ReadOnlyTransactionEnforcer
{
    /// <summary>
    /// Throws a <see cref="ReadOnlyTransactionViolationException"/> if the unit of work is in read-only mode.
    /// </summary>
    /// <param name="unitOfWork">The unit of work to check.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    /// <exception cref="ReadOnlyTransactionViolationException">Thrown when the unit of work is in read-only mode.</exception>
    public static void ThrowIfReadOnly(IUnitOfWork unitOfWork)
    {
        if (unitOfWork == null)
            throw new ArgumentNullException(nameof(unitOfWork));

        if (unitOfWork.IsReadOnly)
        {
            var transactionId = unitOfWork.CurrentTransactionContext?.TransactionId ?? "unknown";
            throw new ReadOnlyTransactionViolationException(transactionId);
        }
    }

    /// <summary>
    /// Throws a <see cref="ReadOnlyTransactionViolationException"/> if the unit of work is in read-only mode,
    /// including the request type in the exception message.
    /// </summary>
    /// <param name="unitOfWork">The unit of work to check.</param>
    /// <param name="requestType">The type of request attempting the modification.</param>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    /// <exception cref="ReadOnlyTransactionViolationException">Thrown when the unit of work is in read-only mode.</exception>
    public static void ThrowIfReadOnly(IUnitOfWork unitOfWork, string requestType)
    {
        if (unitOfWork == null)
            throw new ArgumentNullException(nameof(unitOfWork));

        if (unitOfWork.IsReadOnly)
        {
            var transactionId = unitOfWork.CurrentTransactionContext?.TransactionId ?? "unknown";
            throw new ReadOnlyTransactionViolationException(transactionId, requestType);
        }
    }

    /// <summary>
    /// Configures the database transaction as read-only if supported by the database provider.
    /// </summary>
    /// <param name="transaction">The database transaction to configure.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <remarks>
    /// This method logs that a read-only transaction has been configured.
    /// The actual read-only enforcement is done through the <see cref="ThrowIfReadOnly"/> method
    /// which prevents SaveChanges calls.
    /// 
    /// <para>Database-specific read-only optimizations should be implemented in the IUnitOfWork
    /// implementation (e.g., DbContext) by checking the IsReadOnly property when beginning a transaction.</para>
    /// </remarks>
    public static void ConfigureReadOnlyTransaction(IRelayDbTransaction transaction, ILogger? logger = null)
    {
        if (transaction == null)
            throw new ArgumentNullException(nameof(transaction));

        // Log that read-only mode is configured
        // The actual enforcement happens through the ThrowIfReadOnly method which prevents SaveChanges calls
        // Database-specific optimizations should be implemented in the IUnitOfWork implementation
        logger?.LogDebug(
            "Transaction configured as read-only. SaveChanges operations will be blocked.");
    }

    /// <summary>
    /// Checks if the unit of work is in read-only mode.
    /// </summary>
    /// <param name="unitOfWork">The unit of work to check.</param>
    /// <returns>True if the unit of work is in read-only mode; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when unitOfWork is null.</exception>
    public static bool IsReadOnly(IUnitOfWork unitOfWork)
    {
        if (unitOfWork == null)
            throw new ArgumentNullException(nameof(unitOfWork));

        return unitOfWork.IsReadOnly;
    }
}

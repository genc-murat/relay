using System;
using System.Data;

namespace Relay.Core.Transactions;

/// <summary>
/// Attribute for declarative transaction configuration on transactional requests.
/// This attribute is REQUIRED on all types implementing <see cref="ITransactionalRequest"/>.
/// </summary>
/// <remarks>
/// <para>
/// The isolation level must be explicitly specified and cannot be <see cref="IsolationLevel.Unspecified"/>.
/// This ensures that developers make conscious decisions about transaction isolation behavior.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// [Transaction(IsolationLevel.ReadCommitted, TimeoutSeconds = 30)]
/// public record CreateOrderCommand : IRequest&lt;OrderResult&gt;, ITransactionalRequest;
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class TransactionAttribute : Attribute
{
    /// <summary>
    /// Gets the isolation level for the transaction.
    /// This value is required and cannot be <see cref="IsolationLevel.Unspecified"/>.
    /// </summary>
    public IsolationLevel IsolationLevel { get; }

    /// <summary>
    /// Gets or sets the transaction timeout in seconds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Default is 30 seconds. This overrides the <see cref="TransactionOptions.DefaultTimeout"/> setting.
    /// </para>
    /// <para>
    /// To disable timeout enforcement for this specific request, set to:
    /// <list type="bullet">
    /// <item><description>0 - No timeout (infinite)</description></item>
    /// <item><description>-1 - No timeout (infinite)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// When timeout is enabled, if the transaction exceeds this duration, it will be automatically
    /// rolled back and a <see cref="TransactionTimeoutException"/> will be thrown.
    /// </para>
    /// </remarks>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether this is a read-only transaction.
    /// Read-only transactions can be optimized by the database and will prevent
    /// any modification operations.
    /// </summary>
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use distributed transaction coordination.
    /// When true, a <see cref="System.Transactions.TransactionScope"/> will be used
    /// to coordinate transactions across multiple resources.
    /// </summary>
    public bool UseDistributedTransaction { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionAttribute"/> class.
    /// </summary>
    /// <param name="isolationLevel">
    /// The isolation level for the transaction. Must not be <see cref="IsolationLevel.Unspecified"/>.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="isolationLevel"/> is <see cref="IsolationLevel.Unspecified"/>.
    /// </exception>
    public TransactionAttribute(IsolationLevel isolationLevel)
    {
        if (isolationLevel == IsolationLevel.Unspecified)
        {
            throw new ArgumentException(
                "Transaction isolation level cannot be Unspecified. " +
                "You must explicitly specify an isolation level (e.g., IsolationLevel.ReadCommitted, " +
                "IsolationLevel.RepeatableRead, IsolationLevel.Serializable, etc.).",
                nameof(isolationLevel));
        }

        IsolationLevel = isolationLevel;
    }

    /// <summary>
    /// Validates the attribute configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the attribute configuration is invalid.
    /// </exception>
    internal void Validate()
    {
        if (IsolationLevel == IsolationLevel.Unspecified)
        {
            throw new InvalidOperationException(
                "Transaction isolation level cannot be Unspecified. " +
                "You must explicitly specify an isolation level.");
        }

        if (TimeoutSeconds < -1)
        {
            throw new InvalidOperationException(
                $"Transaction timeout cannot be less than -1. Current value: {TimeoutSeconds}. " +
                "Use 0 or -1 to disable timeout, or a positive value for timeout in seconds.");
        }

        if (IsReadOnly && UseDistributedTransaction)
        {
            throw new InvalidOperationException(
                "Read-only transactions cannot be used with distributed transactions. " +
                "Please set either IsReadOnly or UseDistributedTransaction to false.");
        }
    }
}

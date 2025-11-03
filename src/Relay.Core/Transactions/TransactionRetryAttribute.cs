using System;

namespace Relay.Core.Transactions;

/// <summary>
/// Attribute for configuring transaction retry policy on transactional requests.
/// When applied, the transaction will be automatically retried on transient failures
/// according to the configured policy.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is optional. If not specified, the default retry policy from
/// <see cref="TransactionOptions"/> will be used (if configured).
/// </para>
/// <para>
/// Example usage:
/// <code>
/// [Transaction(IsolationLevel.ReadCommitted)]
/// [TransactionRetry(MaxRetries = 5, InitialDelayMs = 200, Strategy = RetryStrategy.ExponentialBackoff)]
/// public record CreateOrderCommand : IRequest&lt;OrderResult&gt;, ITransactionalRequest;
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class TransactionRetryAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// Default is 3. Set to 0 to disable retries.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay in milliseconds before the first retry.
    /// For linear strategy, this is the delay between all retries.
    /// For exponential backoff, this is the base delay that gets multiplied.
    /// Default is 100ms.
    /// </summary>
    public int InitialDelayMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the retry strategy to use.
    /// Default is <see cref="RetryStrategy.ExponentialBackoff"/>.
    /// </summary>
    public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;

    /// <summary>
    /// Validates the attribute configuration.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the attribute configuration is invalid.
    /// </exception>
    internal void Validate()
    {
        if (MaxRetries < 0)
        {
            throw new InvalidOperationException(
                $"MaxRetries cannot be negative. Current value: {MaxRetries}. " +
                "Use 0 to disable retries or a positive value for the maximum number of retry attempts.");
        }

        if (InitialDelayMs < 0)
        {
            throw new InvalidOperationException(
                $"InitialDelayMs cannot be negative. Current value: {InitialDelayMs}. " +
                "Use 0 for no delay or a positive value for delay in milliseconds.");
        }

        if (!Enum.IsDefined(typeof(RetryStrategy), Strategy))
        {
            throw new InvalidOperationException(
                $"Invalid retry strategy: {Strategy}. " +
                "Must be one of: Linear, ExponentialBackoff.");
        }
    }
}

using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Configuration options for the transaction system.
    /// </summary>
    /// <remarks>
    /// These options control the default behavior of the transaction system.
    /// Individual requests can override these defaults using attributes or interfaces.
    /// 
    /// Configuration example (appsettings.json):
    /// <code>
    /// {
    ///   "Relay": {
    ///     "Transactions": {
    ///       "DefaultTimeoutSeconds": 30,
    ///       "EnableMetrics": true,
    ///       "EnableDistributedTracing": true,
    ///       "EnableNestedTransactions": true,
    ///       "EnableSavepoints": true,
    ///       "RequireExplicitTransactionAttribute": true
    ///     }
    ///   }
    /// }
    /// </code>
    /// </remarks>
    public class TransactionOptions
    {
        /// <summary>
        /// Gets or sets the default timeout for transactions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This timeout is applied to all transactions unless overridden by a request-specific timeout
        /// using the <see cref="TransactionAttribute.TimeoutSeconds"/> property.
        /// </para>
        /// <para>
        /// The default value is 30 seconds. To disable timeout enforcement, set this to:
        /// <list type="bullet">
        /// <item><description><see cref="TimeSpan.Zero"/> - No timeout</description></item>
        /// <item><description><see cref="System.Threading.Timeout.InfiniteTimeSpan"/> - Infinite timeout</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// When timeout is enabled, transactions that exceed the timeout duration will be automatically
        /// rolled back and a <see cref="TransactionTimeoutException"/> will be thrown.
        /// </para>
        /// <para>
        /// Note: There is no default isolation level. Each transactional request MUST explicitly specify
        /// its isolation level through the TransactionAttribute.
        /// </para>
        /// </remarks>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets a value indicating whether transaction metrics collection is enabled.
        /// </summary>
        /// <remarks>
        /// When enabled, the transaction system collects metrics such as transaction count, duration,
        /// success rate, and failure rate. These metrics can be exposed through health check endpoints
        /// or exported to monitoring systems. Default is true.
        /// </remarks>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether distributed tracing is enabled.
        /// </summary>
        /// <remarks>
        /// When enabled, the transaction system creates distributed tracing spans for transaction operations
        /// using OpenTelemetry. This allows transaction operations to be traced across service boundaries.
        /// Default is true.
        /// </remarks>
        public bool EnableDistributedTracing { get; set; } = true;

        /// <summary>
        /// Gets or sets the default retry policy for transient transaction failures.
        /// </summary>
        /// <remarks>
        /// When specified, this retry policy is applied to all transactions unless overridden by a
        /// request-specific retry policy. If null, no automatic retry is performed. Default is null.
        /// 
        /// Example:
        /// <code>
        /// DefaultRetryPolicy = new TransactionRetryPolicy
        /// {
        ///     MaxRetries = 3,
        ///     InitialDelay = TimeSpan.FromMilliseconds(100),
        ///     Strategy = RetryStrategy.ExponentialBackoff
        /// };
        /// </code>
        /// </remarks>
        public TransactionRetryPolicy? DefaultRetryPolicy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether nested transactions are enabled.
        /// </summary>
        /// <remarks>
        /// When enabled, nested transactional requests will reuse the existing transaction instead of
        /// creating a new one. When disabled, nested transactional requests will throw an exception.
        /// Default is true.
        /// </remarks>
        public bool EnableNestedTransactions { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether savepoint support is enabled.
        /// </summary>
        /// <remarks>
        /// When enabled, transactions can create savepoints for partial rollback operations.
        /// When disabled, attempts to create savepoints will throw an exception.
        /// Default is true.
        /// 
        /// Note: Savepoint support also depends on database capabilities. Some databases may not
        /// support savepoints even when this option is enabled.
        /// </remarks>
        public bool EnableSavepoints { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether the TransactionAttribute is required on all ITransactionalRequest implementations.
        /// </summary>
        /// <remarks>
        /// When true (default), the transaction system will throw an exception at runtime if a request
        /// implementing ITransactionalRequest does not have the TransactionAttribute with an explicit isolation level.
        /// This ensures that all transactional requests have explicit transaction configuration.
        /// 
        /// When false, the system will allow transactional requests without the attribute, but this is NOT recommended
        /// as it can lead to implicit transaction behavior and configuration issues.
        /// 
        /// Default is true (recommended).
        /// </remarks>
        public bool RequireExplicitTransactionAttribute { get; set; } = true;
    }
}

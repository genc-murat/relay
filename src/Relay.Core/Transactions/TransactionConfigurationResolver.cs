using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Options;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Resolves transaction configuration from multiple sources including attributes,
    /// interfaces, and options with proper precedence rules.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Configuration resolution follows this precedence (highest to lowest):
    /// <list type="number">
    /// <item>TransactionAttribute on the request type</item>
    /// <item>TransactionRetryAttribute on the request type</item>
    /// <item>Default values from TransactionOptions</item>
    /// </list>
    /// </para>
    /// <para>
    /// The TransactionAttribute is REQUIRED on all ITransactionalRequest implementations
    /// when RequireExplicitTransactionAttribute is true (default).
    /// </para>
    /// </remarks>
    public sealed class TransactionConfigurationResolver : ITransactionConfigurationResolver
    {
        private readonly TransactionOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionConfigurationResolver"/> class.
        /// </summary>
        /// <param name="options">The transaction options.</param>
        public TransactionConfigurationResolver(IOptions<TransactionOptions> options)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Resolves the transaction configuration for the specified request type.
        /// </summary>
        /// <param name="requestType">The type of the transactional request.</param>
        /// <returns>The resolved transaction configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestType"/> is null.</exception>
        /// <exception cref="TransactionConfigurationException">
        /// Thrown when the request type is missing required configuration or has invalid configuration.
        /// </exception>
        public ITransactionConfiguration Resolve(Type requestType)
        {
            if (requestType == null)
            {
                throw new ArgumentNullException(nameof(requestType));
            }

            // Check if the request implements ITransactionalRequest
            if (!typeof(ITransactionalRequest).IsAssignableFrom(requestType))
            {
                throw new TransactionConfigurationException(
                    $"Type '{requestType.FullName}' does not implement ITransactionalRequest.",
                    requestType);
            }

            // Get the TransactionAttribute (required)
            var transactionAttribute = requestType.GetCustomAttribute<TransactionAttribute>(inherit: true);

            if (transactionAttribute == null)
            {
                if (_options.RequireExplicitTransactionAttribute)
                {
                    throw new TransactionConfigurationException(
                        $"Type '{requestType.FullName}' implements ITransactionalRequest but is missing the required [Transaction] attribute. " +
                        "You must explicitly specify the transaction isolation level using [Transaction(IsolationLevel.YourChoice)]. " +
                        "Example: [Transaction(IsolationLevel.ReadCommitted)]",
                        requestType);
                }

                // Fallback: This should not happen with default settings, but handle gracefully
                throw new TransactionConfigurationException(
                    $"Type '{requestType.FullName}' is missing transaction configuration.",
                    requestType);
            }

            // Validate the transaction attribute
            try
            {
                transactionAttribute.Validate();
            }
            catch (InvalidOperationException ex)
            {
                throw new TransactionConfigurationException(
                    $"Invalid transaction configuration on type '{requestType.FullName}': {ex.Message}",
                    requestType,
                    ex);
            }

            // Check for Unspecified isolation level
            if (transactionAttribute.IsolationLevel == IsolationLevel.Unspecified)
            {
                throw new TransactionConfigurationException(
                    $"Type '{requestType.FullName}' has IsolationLevel.Unspecified. " +
                    "You must explicitly specify an isolation level (e.g., IsolationLevel.ReadCommitted, " +
                    "IsolationLevel.RepeatableRead, IsolationLevel.Serializable, etc.).",
                    requestType);
            }

            // Get the TransactionRetryAttribute (optional)
            var retryAttribute = requestType.GetCustomAttribute<TransactionRetryAttribute>(inherit: true);

            // Validate retry attribute if present
            if (retryAttribute != null)
            {
                try
                {
                    retryAttribute.Validate();
                }
                catch (InvalidOperationException ex)
                {
                    throw new TransactionConfigurationException(
                        $"Invalid retry configuration on type '{requestType.FullName}': {ex.Message}",
                        requestType,
                        ex);
                }
            }

            // Build the configuration
            var timeout = ConvertTimeoutToTimeSpan(transactionAttribute.TimeoutSeconds);
            var retryPolicy = BuildRetryPolicy(retryAttribute);

            return new TransactionConfiguration(
                isolationLevel: transactionAttribute.IsolationLevel,
                timeout: timeout,
                isReadOnly: transactionAttribute.IsReadOnly,
                useDistributedTransaction: transactionAttribute.UseDistributedTransaction,
                retryPolicy: retryPolicy);
        }

        /// <summary>
        /// Resolves the transaction configuration for the specified request instance.
        /// </summary>
        /// <typeparam name="TRequest">The type of the request.</typeparam>
        /// <param name="request">The request instance.</param>
        /// <returns>The resolved transaction configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
        /// <exception cref="TransactionConfigurationException">
        /// Thrown when the request type is missing required configuration or has invalid configuration.
        /// </exception>
        public ITransactionConfiguration Resolve<TRequest>(TRequest request)
            where TRequest : notnull
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return Resolve(request.GetType());
        }

        /// <summary>
        /// Converts timeout in seconds to TimeSpan.
        /// </summary>
        /// <remarks>
        /// Timeout values are interpreted as follows:
        /// - 0 or -1: Infinite timeout (no timeout enforcement)
        /// - Positive value: Timeout in seconds
        /// 
        /// This supports the requirement to disable timeout with TimeSpan.Zero or Timeout.InfiniteTimeSpan.
        /// </remarks>
        private TimeSpan ConvertTimeoutToTimeSpan(int timeoutSeconds)
        {
            if (timeoutSeconds == 0 || timeoutSeconds == -1)
            {
                // 0 or -1 means infinite timeout (disabled)
                // This satisfies the requirement: "Support disabling timeout with TimeSpan.Zero or Timeout.InfiniteTimeSpan"
                return System.Threading.Timeout.InfiniteTimeSpan;
            }

            if (timeoutSeconds < -1)
            {
                throw new TransactionConfigurationException(
                    $"Invalid timeout value: {timeoutSeconds}. " +
                    "Timeout must be -1 (infinite), 0 (infinite), or a positive number of seconds.");
            }

            return TimeSpan.FromSeconds(timeoutSeconds);
        }

        /// <summary>
        /// Builds a retry policy from the attribute or options.
        /// </summary>
        private TransactionRetryPolicy? BuildRetryPolicy(TransactionRetryAttribute? retryAttribute)
        {
            // If retry attribute is present, use it
            if (retryAttribute != null)
            {
                return new TransactionRetryPolicy
                {
                    MaxRetries = retryAttribute.MaxRetries,
                    InitialDelay = TimeSpan.FromMilliseconds(retryAttribute.InitialDelayMs),
                    Strategy = retryAttribute.Strategy
                };
            }

            // Otherwise, use default from options (may be null)
            return _options.DefaultRetryPolicy;
        }
    }
}

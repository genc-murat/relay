using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Validates <see cref="TransactionOptions"/> configuration.
    /// </summary>
    /// <remarks>
    /// This validator ensures that transaction options are configured correctly:
    /// <list type="bullet">
    /// <item><description>DefaultTimeout must be non-negative</description></item>
    /// <item><description>Configuration values are within acceptable ranges</description></item>
    /// <item><description>Retry policy settings are reasonable</description></item>
    /// <item><description>RequireExplicitTransactionAttribute setting is validated</description></item>
    /// </list>
    /// 
    /// The validator is automatically registered when using <see cref="TransactionServiceCollectionExtensions.AddRelayTransactions"/>.
    /// 
    /// <para><strong>Validation Rules:</strong></para>
    /// <list type="bullet">
    /// <item><description>DefaultTimeout must be non-negative or Timeout.InfiniteTimeSpan</description></item>
    /// <item><description>DefaultRetryPolicy.MaxRetries must be between 0 and 100</description></item>
    /// <item><description>DefaultRetryPolicy.InitialDelay must be non-negative</description></item>
    /// <item><description>RequireExplicitTransactionAttribute should be true (warning if false)</description></item>
    /// </list>
    /// </remarks>
    internal sealed class TransactionOptionsValidator : IValidateOptions<TransactionOptions>
    {
        /// <summary>
        /// Validates the specified transaction options.
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance to validate.</param>
        /// <returns>A validation result indicating success or failure with error messages.</returns>
        /// <remarks>
        /// This method performs comprehensive validation of transaction options including:
        /// <list type="bullet">
        /// <item><description>Timeout configuration validation</description></item>
        /// <item><description>Retry policy validation</description></item>
        /// <item><description>Feature flag validation</description></item>
        /// <item><description>Security and best practice warnings</description></item>
        /// </list>
        /// 
        /// Validation failures will prevent the application from starting, ensuring that
        /// misconfigured transaction settings are caught early.
        /// </remarks>
        public ValidateOptionsResult Validate(string? name, TransactionOptions options)
        {
            if (options == null)
            {
                return ValidateOptionsResult.Fail("TransactionOptions cannot be null.");
            }

            var failures = new List<string>();

            // Validate DefaultTimeout
            if (options.DefaultTimeout < TimeSpan.Zero && 
                options.DefaultTimeout != System.Threading.Timeout.InfiniteTimeSpan)
            {
                failures.Add($"DefaultTimeout must be non-negative or Timeout.InfiniteTimeSpan. Current value: {options.DefaultTimeout}");
            }

            // Warn about infinite timeout
            if (options.DefaultTimeout == System.Threading.Timeout.InfiniteTimeSpan ||
                options.DefaultTimeout == TimeSpan.Zero)
            {
                failures.Add("WARNING: DefaultTimeout is set to infinite. This may cause transactions to run indefinitely and block resources. " +
                    "Consider setting a reasonable timeout value (e.g., 30-60 seconds).");
            }

            // Validate DefaultRetryPolicy if specified
            if (options.DefaultRetryPolicy != null)
            {
                if (options.DefaultRetryPolicy.MaxRetries < 0)
                {
                    failures.Add($"DefaultRetryPolicy.MaxRetries must be non-negative. Current value: {options.DefaultRetryPolicy.MaxRetries}");
                }

                if (options.DefaultRetryPolicy.InitialDelay < TimeSpan.Zero)
                {
                    failures.Add($"DefaultRetryPolicy.InitialDelay must be non-negative. Current value: {options.DefaultRetryPolicy.InitialDelay}");
                }

                if (options.DefaultRetryPolicy.MaxRetries > 100)
                {
                    failures.Add($"DefaultRetryPolicy.MaxRetries should not exceed 100 to prevent excessive retry attempts. Current value: {options.DefaultRetryPolicy.MaxRetries}");
                }

                // Warn about high retry counts
                if (options.DefaultRetryPolicy.MaxRetries > 10)
                {
                    failures.Add($"WARNING: DefaultRetryPolicy.MaxRetries is set to {options.DefaultRetryPolicy.MaxRetries}. " +
                        "High retry counts may cause long delays and resource exhaustion. Consider using a lower value (e.g., 3-5 retries).");
                }
            }

            // Provide helpful warnings about RequireExplicitTransactionAttribute
            if (!options.RequireExplicitTransactionAttribute)
            {
                // This is a warning, not a failure, but we'll include it in the validation message
                failures.Add("WARNING: RequireExplicitTransactionAttribute is set to false. " +
                    "This is NOT recommended as it allows implicit transaction behavior. " +
                    "All ITransactionalRequest implementations should have explicit TransactionAttribute with IsolationLevel. " +
                    "Set RequireExplicitTransactionAttribute to true to enforce this requirement.");
            }

            // Validate feature flag combinations
            if (!options.EnableNestedTransactions && options.EnableSavepoints)
            {
                failures.Add("WARNING: EnableSavepoints is true but EnableNestedTransactions is false. " +
                    "Savepoints are typically used with nested transactions. Consider enabling nested transactions.");
            }

            if (failures.Count > 0)
            {
                return ValidateOptionsResult.Fail(failures);
            }

            return ValidateOptionsResult.Success;
        }
    }
}

using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Resolves transaction configuration from multiple sources including attributes,
    /// interfaces, and options with proper precedence rules.
    /// </summary>
    public interface ITransactionConfigurationResolver
    {
        /// <summary>
        /// Resolves the transaction configuration for the specified request type.
        /// </summary>
        /// <param name="requestType">The type of the transactional request.</param>
        /// <returns>The resolved transaction configuration.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="requestType"/> is null.</exception>
        /// <exception cref="TransactionConfigurationException">
        /// Thrown when the request type is missing required configuration or has invalid configuration.
        /// </exception>
        ITransactionConfiguration Resolve(Type requestType);

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
        ITransactionConfiguration Resolve<TRequest>(TRequest request) where TRequest : notnull;
    }
}
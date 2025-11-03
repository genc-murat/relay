using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Defines a contract for detecting transient errors that should trigger transaction retry.
    /// </summary>
    /// <remarks>
    /// Implementations of this interface determine whether a given exception represents a transient
    /// failure that can be resolved by retrying the operation. Transient errors typically include:
    /// <list type="bullet">
    /// <item><description>Deadlock exceptions</description></item>
    /// <item><description>Connection timeout exceptions</description></item>
    /// <item><description>Transient network errors</description></item>
    /// <item><description>Database unavailable errors</description></item>
    /// </list>
    /// </remarks>
    public interface ITransientErrorDetector
    {
        /// <summary>
        /// Determines whether the specified exception represents a transient error.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>True if the exception represents a transient error that should be retried; otherwise, false.</returns>
        bool IsTransient(Exception exception);
    }
}

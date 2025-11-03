using System;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// A transient error detector that uses a custom predicate function.
    /// </summary>
    /// <remarks>
    /// This detector allows you to provide a custom function to determine whether an exception
    /// represents a transient error. This is useful for application-specific or database-specific
    /// transient error detection logic.
    /// 
    /// Example usage:
    /// <code>
    /// var detector = new CustomTransientErrorDetector(exception =>
    /// {
    ///     // SQL Server specific error codes
    ///     if (exception is SqlException sqlEx)
    ///         return sqlEx.Number == 1205; // Deadlock
    ///     
    ///     return exception is TimeoutException;
    /// });
    /// </code>
    /// </remarks>
    internal class CustomTransientErrorDetector : ITransientErrorDetector
    {
        private readonly Func<Exception, bool> _predicate;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTransientErrorDetector"/> class.
        /// </summary>
        /// <param name="predicate">The predicate function that determines whether an exception is transient.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is null.</exception>
        public CustomTransientErrorDetector(Func<Exception, bool> predicate)
        {
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <summary>
        /// Determines whether the specified exception represents a transient error.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>True if the exception represents a transient error; otherwise, false.</returns>
        public bool IsTransient(Exception exception)
        {
            if (exception == null)
                return false;

            return _predicate(exception);
        }
    }
}

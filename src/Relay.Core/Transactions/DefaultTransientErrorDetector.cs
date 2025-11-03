using System;
using System.Data.Common;

namespace Relay.Core.Transactions
{
    /// <summary>
    /// Default implementation of transient error detection for common database errors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This detector identifies common transient database errors by examining exception messages
    /// and types. It uses a conservative approach that should work with most database providers.
    /// </para>
    /// <para>
    /// Detected transient error patterns include:
    /// <list type="bullet">
    /// <item><description>Deadlock exceptions</description></item>
    /// <item><description>Connection timeout exceptions</description></item>
    /// <item><description>Transient network errors</description></item>
    /// <item><description>Database unavailable errors</description></item>
    /// <item><description>Lock timeout errors</description></item>
    /// <item><description>Transaction rollback errors (in some cases)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For production use with specific database providers, consider implementing a provider-specific
    /// detector that uses error codes or exception types specific to your database provider.
    /// For example, SQL Server has specific error numbers for transient errors, and Entity Framework
    /// provides execution strategies with built-in transient error detection.
    /// </para>
    /// </remarks>
    public class DefaultTransientErrorDetector : ITransientErrorDetector
    {
        private static readonly string[] TransientErrorPatterns = new[]
        {
            "deadlock",
            "timeout",
            "connection",
            "network",
            "unavailable",
            "transient",
            "temporary",
            "lock",
            "could not open",
            "transport-level",
            "communication link",
            "broken pipe",
            "connection reset",
            "connection refused",
            "host not found",
            "no route to host"
        };

        /// <summary>
        /// Determines whether the specified exception represents a transient error.
        /// </summary>
        /// <param name="exception">The exception to evaluate.</param>
        /// <returns>True if the exception represents a transient error; otherwise, false.</returns>
        /// <remarks>
        /// This method checks the exception message for common transient error patterns and
        /// recursively checks inner exceptions. It also recognizes common exception types
        /// that typically indicate transient failures.
        /// </remarks>
        public bool IsTransient(Exception exception)
        {
            if (exception == null)
                return false;

            // Check for common transient exception types
            if (IsTransientExceptionType(exception))
                return true;

            // Check exception message for transient error patterns
            var message = exception.Message?.ToLowerInvariant() ?? string.Empty;
            foreach (var pattern in TransientErrorPatterns)
            {
                if (message.Contains(pattern))
                    return true;
            }

            // Check inner exception recursively
            if (exception.InnerException != null)
                return IsTransient(exception.InnerException);

            return false;
        }

        /// <summary>
        /// Checks if the exception type is commonly associated with transient errors.
        /// </summary>
        /// <param name="exception">The exception to check.</param>
        /// <returns>True if the exception type indicates a transient error; otherwise, false.</returns>
        private static bool IsTransientExceptionType(Exception exception)
        {
            // TimeoutException is almost always transient
            if (exception is TimeoutException)
                return true;

            // DbException with specific characteristics
            if (exception is DbException dbException)
            {
                // Check for common transient error codes (this is database-agnostic)
                // Specific implementations should override this for provider-specific codes
                return IsTransientDbException(dbException);
            }

            // InvalidOperationException can sometimes indicate transient connection issues
            if (exception is InvalidOperationException invalidOp)
            {
                var message = invalidOp.Message?.ToLowerInvariant() ?? string.Empty;
                return message.Contains("connection") || message.Contains("timeout");
            }

            return false;
        }

        /// <summary>
        /// Checks if a DbException represents a transient error.
        /// </summary>
        /// <param name="dbException">The database exception to check.</param>
        /// <returns>True if the exception represents a transient error; otherwise, false.</returns>
        /// <remarks>
        /// This is a basic implementation. For production use, consider implementing
        /// provider-specific detection using error codes:
        /// - SQL Server: Error numbers like 1205 (deadlock), 40197, 40501, 40613, etc.
        /// - PostgreSQL: Error codes like 40001 (serialization failure), 40P01 (deadlock), etc.
        /// - MySQL: Error codes like 1205 (lock wait timeout), 1213 (deadlock), etc.
        /// </remarks>
        private static bool IsTransientDbException(DbException dbException)
        {
            // This is a conservative implementation that relies on message patterns
            // Provider-specific implementations should check error codes
            var message = dbException.Message?.ToLowerInvariant() ?? string.Empty;
            
            return message.Contains("deadlock") ||
                   message.Contains("timeout") ||
                   message.Contains("connection") ||
                   message.Contains("network") ||
                   message.Contains("unavailable");
        }
    }
}

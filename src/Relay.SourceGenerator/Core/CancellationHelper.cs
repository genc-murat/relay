using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator.Core
{
    /// <summary>
    /// Helper class for handling cancellation tokens in source generator operations.
    /// Provides utilities for periodic cancellation checks and graceful cancellation handling.
    /// </summary>
    public static class CancellationHelper
    {
        /// <summary>
        /// Checks if cancellation has been requested and throws OperationCanceledException if so.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to check</param>
        /// <exception cref="OperationCanceledException">Thrown when cancellation is requested</exception>
        public static void ThrowIfCancellationRequested(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Executes an action with cancellation support, catching and handling OperationCanceledException.
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <param name="diagnosticReporter">Optional diagnostic reporter for logging cancellation</param>
        /// <returns>True if completed successfully, false if cancelled</returns>
        public static bool ExecuteWithCancellation(
            Action action,
            CancellationToken cancellationToken,
            IDiagnosticReporter? diagnosticReporter = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                action();
                return true;
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected, log if reporter is provided
                if (diagnosticReporter != null)
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.Info,
                        Location.None,
                        "Source generation was cancelled");
                    diagnosticReporter.ReportDiagnostic(diagnostic);
                }
                return false;
            }
        }

        /// <summary>
        /// Executes a function with cancellation support, catching and handling OperationCanceledException.
        /// </summary>
        /// <typeparam name="T">The return type of the function</typeparam>
        /// <param name="func">The function to execute</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <param name="defaultValue">The default value to return if cancelled</param>
        /// <param name="diagnosticReporter">Optional diagnostic reporter for logging cancellation</param>
        /// <returns>The function result if completed, or defaultValue if cancelled</returns>
        public static T ExecuteWithCancellation<T>(
            Func<T> func,
            CancellationToken cancellationToken,
            T defaultValue,
            IDiagnosticReporter? diagnosticReporter = null)
        {
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            try
            {
                return func();
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected, log if reporter is provided
                if (diagnosticReporter != null)
                {
                    var diagnostic = Diagnostic.Create(
                        DiagnosticDescriptors.Info,
                        Location.None,
                        "Source generation was cancelled");
                    diagnosticReporter.ReportDiagnostic(diagnostic);
                }
                return defaultValue;
            }
        }

        /// <summary>
        /// Checks for cancellation periodically during long-running operations.
        /// Call this method inside loops or long-running operations.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to check</param>
        /// <param name="iterationCount">Current iteration count (for periodic checking)</param>
        /// <param name="checkInterval">How often to check (default: every 100 iterations)</param>
        /// <returns>True if should continue, false if cancelled</returns>
        public static bool CheckCancellationPeriodically(
            CancellationToken cancellationToken,
            int iterationCount,
            int checkInterval = 100)
        {
            // Check every N iterations to reduce overhead
            if (iterationCount % checkInterval == 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Wraps an operation with try-catch for OperationCanceledException,
        /// ensuring graceful handling of cancellation.
        /// </summary>
        /// <param name="operation">The operation to execute</param>
        /// <param name="operationName">Name of the operation for error reporting</param>
        /// <param name="diagnosticReporter">Diagnostic reporter for logging</param>
        /// <returns>True if completed successfully, false if cancelled or failed</returns>
        public static bool TryExecuteWithCancellation(
            Action operation,
            string operationName,
            IDiagnosticReporter diagnosticReporter)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (diagnosticReporter == null)
                throw new ArgumentNullException(nameof(diagnosticReporter));

            try
            {
                operation();
                return true;
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected, don't report as error
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.Info,
                    Location.None,
                    $"Operation '{operationName}' was cancelled");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }
            catch (Exception ex)
            {
                // Unexpected error
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.GeneratorError,
                    Location.None,
                    $"Error in operation '{operationName}': {ex.Message}");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }
        }

        /// <summary>
        /// Creates a linked cancellation token source that combines multiple cancellation tokens.
        /// Useful when you need to respect both a user-provided token and an internal timeout.
        /// </summary>
        /// <param name="tokens">Cancellation tokens to link</param>
        /// <returns>A linked cancellation token source</returns>
        public static CancellationTokenSource CreateLinkedTokenSource(params CancellationToken[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                return new CancellationTokenSource();
            }

            return CancellationTokenSource.CreateLinkedTokenSource(tokens);
        }

        /// <summary>
        /// Checks if a cancellation token is in a cancelled state.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to check</param>
        /// <returns>True if cancelled, false otherwise</returns>
        public static bool IsCancelled(CancellationToken cancellationToken)
        {
            return cancellationToken.IsCancellationRequested;
        }

        /// <summary>
        /// Safely disposes a cancellation token source.
        /// </summary>
        /// <param name="cts">The cancellation token source to dispose</param>
        public static void SafeDispose(CancellationTokenSource? cts)
        {
            try
            {
                cts?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
}

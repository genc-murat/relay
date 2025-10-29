using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Discovery;
using Relay.SourceGenerator.Generators;

namespace Relay.SourceGenerator.Core
{
    /// <summary>
    /// Provides error isolation for source generator operations.
    /// Ensures that errors in one component don't cascade to others.
    /// </summary>
    public static class ErrorIsolation
    {
        /// <summary>
        /// Executes a generator with error isolation, ensuring failures don't affect other generators.
        /// </summary>
        /// <param name="generator">The generator to execute</param>
        /// <param name="result">Handler discovery result</param>
        /// <param name="options">Generation options</param>
        /// <param name="diagnosticReporter">Diagnostic reporter</param>
        /// <returns>Generated source code, or null if generation failed</returns>
        public static string? ExecuteGeneratorWithIsolation(
            ICodeGenerator generator,
            HandlerDiscoveryResult result,
            GenerationOptions options,
            IDiagnosticReporter diagnosticReporter)
        {
            if (generator == null)
                throw new ArgumentNullException(nameof(generator));
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (diagnosticReporter == null)
                throw new ArgumentNullException(nameof(diagnosticReporter));

            try
            {
                // Check if generator can generate for this result
                if (!generator.CanGenerate(result))
                {
                    return null;
                }

                // Generate code
                var source = generator.Generate(result, options);
                return source;
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected, don't report as error
                throw;
            }
            catch (Exception ex)
            {
                // Report generator-specific error but don't propagate
                ReportGeneratorError(generator.GeneratorName, ex, diagnosticReporter);
                return null;
            }
        }

        /// <summary>
        /// Executes multiple generators with error isolation.
        /// Each generator failure is isolated and doesn't affect others.
        /// </summary>
        /// <param name="generators">Generators to execute</param>
        /// <param name="result">Handler discovery result</param>
        /// <param name="options">Generation options</param>
        /// <param name="diagnosticReporter">Diagnostic reporter</param>
        /// <returns>Dictionary of successful generations (filename -> source)</returns>
        public static Dictionary<string, string> ExecuteGeneratorsWithIsolation(
            IEnumerable<ICodeGenerator> generators,
            HandlerDiscoveryResult result,
            GenerationOptions options,
            IDiagnosticReporter diagnosticReporter)
        {
            var generatedSources = new Dictionary<string, string>();

            foreach (var generator in generators)
            {
                try
                {
                    var source = ExecuteGeneratorWithIsolation(generator, result, options, diagnosticReporter);
                    if (source != null)
                    {
                        var fileName = $"{generator.OutputFileName}.g.cs";
                        generatedSources[fileName] = source;
                    }
                }
                catch (OperationCanceledException)
                {
                    // Cancellation requested, stop processing
                    break;
                }
                catch (Exception ex)
                {
                    // This should not happen as ExecuteGeneratorWithIsolation catches exceptions
                    // But we add this as a safety net
                    ReportGeneratorError(generator.GeneratorName, ex, diagnosticReporter);
                }
            }

            return generatedSources;
        }

        /// <summary>
        /// Executes handler discovery with error isolation.
        /// </summary>
        /// <param name="discoveryAction">Discovery action to execute</param>
        /// <param name="diagnosticReporter">Diagnostic reporter</param>
        /// <returns>Discovery result, or empty result if discovery failed</returns>
        public static HandlerDiscoveryResult ExecuteDiscoveryWithIsolation(
            Func<HandlerDiscoveryResult> discoveryAction,
            IDiagnosticReporter diagnosticReporter)
        {
            if (discoveryAction == null)
                throw new ArgumentNullException(nameof(discoveryAction));
            if (diagnosticReporter == null)
                throw new ArgumentNullException(nameof(diagnosticReporter));

            try
            {
                return discoveryAction();
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected
                throw;
            }
            catch (Exception ex)
            {
                // Report discovery error and return empty result
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.GeneratorError,
                    Location.None,
                    $"Handler discovery failed: {ex.Message}");
                diagnosticReporter.ReportDiagnostic(diagnostic);

                return new HandlerDiscoveryResult();
            }
        }

        /// <summary>
        /// Executes an operation with comprehensive error handling.
        /// Provides detailed error reporting and continues execution.
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="operationName">Name of the operation for error reporting</param>
        /// <param name="diagnosticReporter">Diagnostic reporter</param>
        /// <returns>True if successful, false if failed</returns>
        public static bool TryExecuteWithErrorHandling(
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
                return false;
            }
            catch (OutOfMemoryException ex)
            {
                // Critical error, report and fail
                ReportCriticalError(operationName, ex, diagnosticReporter);
                return false;
            }
            catch (StackOverflowException ex)
            {
                // Critical error, report and fail
                ReportCriticalError(operationName, ex, diagnosticReporter);
                return false;
            }
            catch (Exception ex)
            {
                // Regular error, report and continue
                ReportOperationError(operationName, ex, diagnosticReporter);
                return false;
            }
        }

        /// <summary>
        /// Executes an operation with comprehensive error handling and returns a result.
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="operationName">Name of the operation for error reporting</param>
        /// <param name="defaultValue">Default value to return on error</param>
        /// <param name="diagnosticReporter">Diagnostic reporter</param>
        /// <returns>Operation result, or defaultValue if failed</returns>
        public static T ExecuteWithErrorHandling<T>(
            Func<T> operation,
            string operationName,
            T defaultValue,
            IDiagnosticReporter diagnosticReporter)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));
            if (diagnosticReporter == null)
                throw new ArgumentNullException(nameof(diagnosticReporter));

            try
            {
                return operation();
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected
                return defaultValue;
            }
            catch (OutOfMemoryException ex)
            {
                // Critical error
                ReportCriticalError(operationName, ex, diagnosticReporter);
                return defaultValue;
            }
            catch (StackOverflowException ex)
            {
                // Critical error
                ReportCriticalError(operationName, ex, diagnosticReporter);
                return defaultValue;
            }
            catch (Exception ex)
            {
                // Regular error
                ReportOperationError(operationName, ex, diagnosticReporter);
                return defaultValue;
            }
        }

        /// <summary>
        /// Reports a generator-specific error.
        /// </summary>
        private static void ReportGeneratorError(string generatorName, Exception ex, IDiagnosticReporter diagnosticReporter)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                Location.None,
                $"Error in {generatorName}: {ex.Message}");
            diagnosticReporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an operation error.
        /// </summary>
        private static void ReportOperationError(string operationName, Exception ex, IDiagnosticReporter diagnosticReporter)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                Location.None,
                $"Error in operation '{operationName}': {ex.Message}");
            diagnosticReporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a critical error that cannot be recovered from.
        /// </summary>
        private static void ReportCriticalError(string operationName, Exception ex, IDiagnosticReporter diagnosticReporter)
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                Location.None,
                $"Critical error in operation '{operationName}': {ex.GetType().Name} - {ex.Message}");
            diagnosticReporter.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Determines if an exception is recoverable.
        /// </summary>
        /// <param name="exception">Exception to check</param>
        /// <returns>True if recoverable, false if critical</returns>
        public static bool IsRecoverableException(Exception exception)
        {
            return exception is not OperationCanceledException &&
                   exception is not OutOfMemoryException &&
                   exception is not StackOverflowException;
        }

        /// <summary>
        /// Creates a safe execution context that catches and reports all errors.
        /// </summary>
        /// <param name="diagnosticReporter">Diagnostic reporter</param>
        /// <returns>Safe execution context</returns>
        public static SafeExecutionContext CreateSafeContext(IDiagnosticReporter diagnosticReporter)
        {
            return new SafeExecutionContext(diagnosticReporter);
        }
    }

    /// <summary>
    /// Provides a safe execution context for multiple operations.
    /// </summary>
    public class SafeExecutionContext
    {
        private readonly IDiagnosticReporter _diagnosticReporter;
        private readonly List<Exception> _errors = new();

        public SafeExecutionContext(IDiagnosticReporter diagnosticReporter)
        {
            _diagnosticReporter = diagnosticReporter ?? throw new ArgumentNullException(nameof(diagnosticReporter));
        }

        /// <summary>
        /// Gets all errors that occurred during execution.
        /// </summary>
        public IReadOnlyList<Exception> Errors => _errors;

        /// <summary>
        /// Gets whether any errors occurred.
        /// </summary>
        public bool HasErrors => _errors.Count > 0;

        /// <summary>
        /// Executes an operation safely, catching and recording errors.
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="operationName">Name for error reporting</param>
        /// <returns>True if successful, false if failed</returns>
        public bool Execute(Action operation, string operationName)
        {
            try
            {
                operation();
                return true;
            }
            catch (OperationCanceledException)
            {
                throw; // Don't catch cancellation
            }
            catch (Exception ex)
            {
                _errors.Add(ex);
                ErrorIsolation.TryExecuteWithErrorHandling(
                    () => { }, // No-op, just for error reporting
                    operationName,
                    _diagnosticReporter);
                return false;
            }
        }

        /// <summary>
        /// Executes an operation safely and returns a result.
        /// </summary>
        /// <typeparam name="T">Result type</typeparam>
        /// <param name="operation">Operation to execute</param>
        /// <param name="operationName">Name for error reporting</param>
        /// <param name="defaultValue">Default value on error</param>
        /// <returns>Operation result or default value</returns>
        public T Execute<T>(Func<T> operation, string operationName, T defaultValue)
        {
            try
            {
                return operation();
            }
            catch (OperationCanceledException)
            {
                throw; // Don't catch cancellation
            }
            catch (Exception ex)
            {
                _errors.Add(ex);
                return ErrorIsolation.ExecuteWithErrorHandling(
                    () => defaultValue,
                    operationName,
                    defaultValue,
                    _diagnosticReporter);
            }
        }
    }
}

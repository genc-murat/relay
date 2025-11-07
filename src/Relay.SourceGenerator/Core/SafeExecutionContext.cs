using Relay.SourceGenerator.Diagnostics;
using System;
using System.Collections.Generic;

namespace Relay.SourceGenerator.Core;

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
    /// Executes an operation safely.
    /// </summary>
    /// <param name="operation">Operation to execute</param>
    /// <param name="operationName">Name for error reporting</param>
    /// <returns>True if successful, false if failed</returns>
    public bool Execute(Action operation, string operationName)
    {
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

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
            if (ErrorIsolation.IsRecoverableException(ex))
            {
                ErrorIsolation.ReportOperationError(operationName, ex, _diagnosticReporter);
            }
            else
            {
                ErrorIsolation.ReportCriticalError(operationName, ex, _diagnosticReporter);
            }
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
        if (operation == null)
            throw new ArgumentNullException(nameof(operation));

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
            if (ErrorIsolation.IsRecoverableException(ex))
            {
                ErrorIsolation.ReportOperationError(operationName, ex, _diagnosticReporter);
            }
            else
            {
                ErrorIsolation.ReportCriticalError(operationName, ex, _diagnosticReporter);
            }
            return defaultValue;
        }
    }
}

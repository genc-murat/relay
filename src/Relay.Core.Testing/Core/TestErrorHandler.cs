using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Testing;

/// <summary>
/// Handles errors and exceptions during test execution.
/// Provides retry mechanisms, error capture, and diagnostic information.
/// </summary>
public class TestErrorHandler
{
    private readonly List<TestError> _capturedErrors = new();
    private readonly TestRelayOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestErrorHandler"/> class.
    /// </summary>
    /// <param name="options">The test relay options.</param>
    public TestErrorHandler(TestRelayOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets all captured errors.
    /// </summary>
    public IReadOnlyList<TestError> CapturedErrors => _capturedErrors.AsReadOnly();

    /// <summary>
    /// Captures an error that occurred during test execution.
    /// </summary>
    /// <param name="error">The error to capture.</param>
    public void CaptureError(TestError error)
    {
        if (error == null)
            throw new ArgumentNullException(nameof(error));

        _capturedErrors.Add(error);

        if (_options.EnableDiagnosticLogging && _options.DiagnosticLogging.EnableConsoleLogging)
        {
            Console.WriteLine($"[ERROR] {error.Message} at {error.Source} ({error.Timestamp})");
        }
    }

    /// <summary>
    /// Captures an exception as a test error.
    /// </summary>
    /// <param name="exception">The exception to capture.</param>
    /// <param name="context">The context where the error occurred.</param>
    public void CaptureException(Exception exception, string context = null)
    {
        var error = new TestError
        {
            Message = exception.Message,
            Exception = exception,
            Source = context ?? "Unknown",
            Timestamp = DateTime.UtcNow,
            ErrorType = TestErrorType.Exception,
            StackTrace = exception.StackTrace
        };

        CaptureError(error);
    }

    /// <summary>
    /// Executes an action with retry logic.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="retryPolicy">The retry policy to use.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteWithRetryAsync(Func<Task> action, RetryPolicy retryPolicy = null)
    {
        retryPolicy ??= new RetryPolicy();

        var attempts = 0;
        Exception lastException = null;

        while (attempts < retryPolicy.MaxAttempts)
        {
            try
            {
                attempts++;
                await action();
                return; // Success
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (!retryPolicy.ShouldRetry(ex, attempts))
                {
                    // All retries exhausted or should not retry
                    if (attempts >= retryPolicy.MaxAttempts)
                    {
                        var retryError = new TestError
                        {
                            Message = $"Operation failed after {retryPolicy.MaxAttempts} attempts",
                            Exception = lastException,
                            Source = "RetryHandler",
                            Timestamp = DateTime.UtcNow,
                            ErrorType = TestErrorType.RetryExhausted,
                            RetryAttempts = attempts
                        };

                        CaptureError(retryError);
                    }
                    throw; // Don't retry
                }

                if (_options.EnableDiagnosticLogging && _options.DiagnosticLogging.EnableConsoleLogging)
                {
                    Console.WriteLine($"[RETRY] Attempt {attempts} failed: {ex.Message}. Retrying in {retryPolicy.Delay}...");
                }

                if (retryPolicy.Delay > TimeSpan.Zero)
                {
                    await Task.Delay(retryPolicy.Delay);
                }
            }
        }
    }

    /// <summary>
    /// Executes a function with retry logic and returns the result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="function">The function to execute.</param>
    /// <param name="retryPolicy">The retry policy to use.</param>
    /// <returns>The result of the function.</returns>
    public async Task<TResult> ExecuteWithRetryAsync<TResult>(Func<Task<TResult>> function, RetryPolicy retryPolicy = null)
    {
        retryPolicy ??= new RetryPolicy();

        var attempts = 0;
        Exception lastException = null;

        while (attempts < retryPolicy.MaxAttempts)
        {
            try
            {
                attempts++;
                return await function();
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (!retryPolicy.ShouldRetry(ex, attempts))
                {
                    // All retries exhausted or should not retry
                    if (attempts >= retryPolicy.MaxAttempts)
                    {
                        var retryError = new TestError
                        {
                            Message = $"Operation failed after {retryPolicy.MaxAttempts} attempts",
                            Exception = lastException,
                            Source = "RetryHandler",
                            Timestamp = DateTime.UtcNow,
                            ErrorType = TestErrorType.RetryExhausted,
                            RetryAttempts = attempts
                        };

                        CaptureError(retryError);
                    }
                    throw; // Don't retry
                }

                if (_options.EnableDiagnosticLogging && _options.DiagnosticLogging.EnableConsoleLogging)
                {
                    Console.WriteLine($"[RETRY] Attempt {attempts} failed: {ex.Message}. Retrying in {retryPolicy.Delay}...");
                }

                if (retryPolicy.Delay > TimeSpan.Zero)
                {
                    await Task.Delay(retryPolicy.Delay);
                }
            }
        }

        // This should never be reached due to the loop condition
        throw new InvalidOperationException("Unexpected end of retry loop");
    }

    /// <summary>
    /// Executes an action with timeout and error handling.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteWithTimeoutAsync(Func<Task> action, TimeSpan? timeout = null)
    {
        timeout ??= _options.DefaultTimeout;

        using var cts = new CancellationTokenSource(timeout.Value);
        try
        {
            await action().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            var timeoutError = new TestError
            {
                Message = $"Operation timed out after {timeout.Value.TotalSeconds} seconds",
                Source = "TimeoutHandler",
                Timestamp = DateTime.UtcNow,
                ErrorType = TestErrorType.Timeout
            };

            CaptureError(timeoutError);
            throw new TimeoutException(timeoutError.Message);
        }
        catch (Exception ex)
        {
            CaptureException(ex, "TimeoutHandler");
            throw;
        }
    }

    /// <summary>
    /// Executes a function with timeout and error handling.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="function">The function to execute.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The result of the function.</returns>
    public async Task<TResult> ExecuteWithTimeoutAsync<TResult>(Func<Task<TResult>> function, TimeSpan? timeout = null)
    {
        timeout ??= _options.DefaultTimeout;

        using var cts = new CancellationTokenSource(timeout.Value);
        try
        {
            return await function().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            var timeoutError = new TestError
            {
                Message = $"Operation timed out after {timeout.Value.TotalSeconds} seconds",
                Source = "TimeoutHandler",
                Timestamp = DateTime.UtcNow,
                ErrorType = TestErrorType.Timeout
            };

            CaptureError(timeoutError);
            throw new TimeoutException(timeoutError.Message);
        }
        catch (Exception ex)
        {
            CaptureException(ex, "TimeoutHandler");
            throw;
        }
    }

    /// <summary>
    /// Clears all captured errors.
    /// </summary>
    public void ClearErrors()
    {
        _capturedErrors.Clear();
    }

    /// <summary>
    /// Gets diagnostic information about captured errors.
    /// </summary>
    /// <returns>A diagnostic report of errors.</returns>
    public ErrorDiagnosticReport GetDiagnosticReport()
    {
        return new ErrorDiagnosticReport
        {
            TotalErrors = _capturedErrors.Count,
            ErrorsByType = GroupErrorsByType(),
            ErrorsBySource = GroupErrorsBySource(),
            RecentErrors = GetRecentErrors(10),
            ErrorPatterns = AnalyzeErrorPatterns()
        };
    }

    private Dictionary<TestErrorType, int> GroupErrorsByType()
    {
        var groups = new Dictionary<TestErrorType, int>();
        foreach (var error in _capturedErrors)
        {
            if (!groups.ContainsKey(error.ErrorType))
                groups[error.ErrorType] = 0;
            groups[error.ErrorType]++;
        }
        return groups;
    }

    private Dictionary<string, int> GroupErrorsBySource()
    {
        var groups = new Dictionary<string, int>();
        foreach (var error in _capturedErrors)
        {
            var source = error.Source ?? "Unknown";
            if (!groups.ContainsKey(source))
                groups[source] = 0;
            groups[source]++;
        }
        return groups;
    }

    private List<TestError> GetRecentErrors(int count)
    {
        return _capturedErrors
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();
    }

    private List<ErrorPattern> AnalyzeErrorPatterns()
    {
        var patterns = new List<ErrorPattern>();

        // Simple pattern analysis - group by message similarity
        var messageGroups = _capturedErrors
            .GroupBy(e => GetMessagePattern(e.Message))
            .Where(g => g.Count() > 1)
            .Select(g => new ErrorPattern
            {
                Pattern = g.Key,
                Occurrences = g.Count(),
                FirstOccurrence = g.Min(e => e.Timestamp),
                LastOccurrence = g.Max(e => e.Timestamp),
                Sources = g.Select(e => e.Source).Distinct().ToList()
            })
            .OrderByDescending(p => p.Occurrences);

        patterns.AddRange(messageGroups);
        return patterns;
    }

    private string GetMessagePattern(string message)
    {
        if (string.IsNullOrEmpty(message))
            return "Empty";

        // Simple pattern extraction - remove specific values
        var result = System.Text.RegularExpressions.Regex.Replace(message, @"\d+", "{number}");

        // Replace GUIDs if present
        var guidMatch = System.Text.RegularExpressions.Regex.Match(result, @"[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}");
        if (guidMatch.Success)
        {
            result = result.Replace(guidMatch.Value, "{guid}");
        }

        return result;
    }
}

/// <summary>
/// Represents a test error.
/// </summary>
public class TestError
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the exception that caused the error.
    /// </summary>
    public Exception Exception { get; set; }

    /// <summary>
    /// Gets or sets the source of the error.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the type of error.
    /// </summary>
    public TestErrorType ErrorType { get; set; }

    /// <summary>
    /// Gets or sets the stack trace.
    /// </summary>
    public string StackTrace { get; set; }

    /// <summary>
    /// Gets or sets the number of retry attempts made.
    /// </summary>
    public int RetryAttempts { get; set; }
}

/// <summary>
/// Defines types of test errors.
/// </summary>
public enum TestErrorType
{
    /// <summary>
    /// General exception.
    /// </summary>
    Exception,

    /// <summary>
    /// Timeout error.
    /// </summary>
    Timeout,

    /// <summary>
    /// Assertion failure.
    /// </summary>
    Assertion,

    /// <summary>
    /// All retry attempts exhausted.
    /// </summary>
    RetryExhausted,

    /// <summary>
    /// Configuration error.
    /// </summary>
    Configuration,

    /// <summary>
    /// Resource error.
    /// </summary>
    Resource,

    /// <summary>
    /// Network error.
    /// </summary>
    Network
}

/// <summary>
/// Defines a retry policy for error handling.
/// </summary>
public class RetryPolicy
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the delay between retry attempts.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the function that determines whether to retry on a specific exception.
    /// </summary>
    public Func<Exception, int, bool> RetryCondition { get; set; }

    /// <summary>
    /// Gets or sets the backoff strategy for delays.
    /// </summary>
    public BackoffStrategy BackoffStrategy { get; set; } = BackoffStrategy.Fixed;

    /// <summary>
    /// Determines whether to retry on the given exception and attempt number.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="attemptNumber">The current attempt number.</param>
    /// <returns>True if the operation should be retried, false otherwise.</returns>
    public bool ShouldRetry(Exception exception, int attemptNumber)
    {
        if (attemptNumber >= MaxAttempts)
            return false;

        if (RetryCondition != null)
            return RetryCondition(exception, attemptNumber);

        // Default retry condition - retry on transient exceptions
        return exception is TimeoutException ||
               exception is System.Net.Http.HttpRequestException ||
               exception is InvalidOperationException;
    }

    /// <summary>
    /// Gets the delay for the specified attempt.
    /// </summary>
    /// <param name="attemptNumber">The attempt number.</param>
    /// <returns>The delay duration.</returns>
    public TimeSpan GetDelay(int attemptNumber)
    {
        switch (BackoffStrategy)
        {
            case BackoffStrategy.Linear:
                return TimeSpan.FromTicks(Delay.Ticks * attemptNumber);

            case BackoffStrategy.Exponential:
                return TimeSpan.FromTicks(Delay.Ticks * (long)Math.Pow(2, attemptNumber - 1));

            case BackoffStrategy.Fixed:
            default:
                return Delay;
        }
    }
}

/// <summary>
/// Defines backoff strategies for retry delays.
/// </summary>
public enum BackoffStrategy
{
    /// <summary>
    /// Fixed delay between retries.
    /// </summary>
    Fixed,

    /// <summary>
    /// Linear backoff - delay increases linearly.
    /// </summary>
    Linear,

    /// <summary>
    /// Exponential backoff - delay increases exponentially.
    /// </summary>
    Exponential
}

/// <summary>
/// Represents a diagnostic report of errors.
/// </summary>
public class ErrorDiagnosticReport
{
    /// <summary>
    /// Gets or sets the total number of errors.
    /// </summary>
    public int TotalErrors { get; set; }

    /// <summary>
    /// Gets or sets errors grouped by type.
    /// </summary>
    public Dictionary<TestErrorType, int> ErrorsByType { get; set; }

    /// <summary>
    /// Gets or sets errors grouped by source.
    /// </summary>
    public Dictionary<string, int> ErrorsBySource { get; set; }

    /// <summary>
    /// Gets or sets the most recent errors.
    /// </summary>
    public List<TestError> RecentErrors { get; set; }

    /// <summary>
    /// Gets or sets identified error patterns.
    /// </summary>
    public List<ErrorPattern> ErrorPatterns { get; set; }
}

/// <summary>
/// Represents an error pattern.
/// </summary>
public class ErrorPattern
{
    /// <summary>
    /// Gets or sets the error pattern.
    /// </summary>
    public string Pattern { get; set; }

    /// <summary>
    /// Gets or sets the number of occurrences.
    /// </summary>
    public int Occurrences { get; set; }

    /// <summary>
    /// Gets or sets the first occurrence timestamp.
    /// </summary>
    public DateTime FirstOccurrence { get; set; }

    /// <summary>
    /// Gets or sets the last occurrence timestamp.
    /// </summary>
    public DateTime LastOccurrence { get; set; }

    /// <summary>
    /// Gets or sets the sources where the pattern occurred.
    /// </summary>
    public List<string> Sources { get; set; }
}

/// <summary>
/// Extension methods for error handling.
/// </summary>
public static class ErrorHandlingExtensions
{
    /// <summary>
    /// Executes an action with error handling and retry.
    /// </summary>
    /// <param name="handler">The error handler.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="maxAttempts">The maximum number of attempts.</param>
    /// <param name="delay">The delay between attempts.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task WithRetry(this TestErrorHandler handler, Func<Task> action, int maxAttempts = 3, TimeSpan? delay = null)
    {
        var policy = new RetryPolicy
        {
            MaxAttempts = maxAttempts,
            Delay = delay ?? TimeSpan.FromSeconds(1)
        };

        return handler.ExecuteWithRetryAsync(action, policy);
    }

    /// <summary>
    /// Executes a function with error handling and retry.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="handler">The error handler.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="maxAttempts">The maximum number of attempts.</param>
    /// <param name="delay">The delay between attempts.</param>
    /// <returns>The result of the function.</returns>
    public static Task<TResult> WithRetry<TResult>(this TestErrorHandler handler, Func<Task<TResult>> function, int maxAttempts = 3, TimeSpan? delay = null)
    {
        var policy = new RetryPolicy
        {
            MaxAttempts = maxAttempts,
            Delay = delay ?? TimeSpan.FromSeconds(1)
        };

        return handler.ExecuteWithRetryAsync(function, policy);
    }

    /// <summary>
    /// Executes an action with timeout.
    /// </summary>
    /// <param name="handler">The error handler.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static Task WithTimeout(this TestErrorHandler handler, Func<Task> action, TimeSpan timeout)
    {
        return handler.ExecuteWithTimeoutAsync(action, timeout);
    }

    /// <summary>
    /// Executes a function with timeout.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="handler">The error handler.</param>
    /// <param name="function">The function to execute.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>The result of the function.</returns>
    public static Task<TResult> WithTimeout<TResult>(this TestErrorHandler handler, Func<Task<TResult>> function, TimeSpan timeout)
    {
        return handler.ExecuteWithTimeoutAsync(function, timeout);
    }
}
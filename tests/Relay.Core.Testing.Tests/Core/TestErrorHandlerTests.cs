using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class TestErrorHandlerTests
{
    private readonly TestRelayOptions _defaultOptions;

    public TestErrorHandlerTests()
    {
        _defaultOptions = new TestRelayOptions();
    }

    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        // Arrange & Act
        var handler = new TestErrorHandler(_defaultOptions);

        // Assert
        Assert.NotNull(handler);
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TestErrorHandler(null));
    }

    [Fact]
    public void CaptureError_WithValidError_AddsToCapturedErrors()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var error = new TestError
        {
            Message = "Test error",
            Source = "TestSource",
            Timestamp = DateTime.UtcNow,
            ErrorType = TestErrorType.Exception
        };

        // Act
        handler.CaptureError(error);

        // Assert
        Assert.Single(handler.CapturedErrors);
        Assert.Equal(error, handler.CapturedErrors[0]);
    }

    [Fact]
    public void CaptureError_WithNullError_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => handler.CaptureError(null));
    }

    [Fact]
    public void CaptureException_WithException_CreatesAndCapturesTestError()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var exception = new InvalidOperationException("Test exception");
        var context = "TestContext";

        // Act
        handler.CaptureException(exception, context);

        // Assert
        Assert.Single(handler.CapturedErrors);
        var capturedError = handler.CapturedErrors[0];
        Assert.Equal(exception.Message, capturedError.Message);
        Assert.Equal(exception, capturedError.Exception);
        Assert.Equal(context, capturedError.Source);
        Assert.Equal(TestErrorType.Exception, capturedError.ErrorType);
        Assert.Equal(exception.StackTrace, capturedError.StackTrace);
        Assert.True(capturedError.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void CaptureException_WithNullContext_UsesDefaultSource()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var exception = new InvalidOperationException("Test exception");

        // Act
        handler.CaptureException(exception);

        // Assert
        Assert.Single(handler.CapturedErrors);
        var capturedError = handler.CapturedErrors[0];
        Assert.Equal("Unknown", capturedError.Source);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ActionSucceedsOnFirstTry_DoesNotRetry()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var executionCount = 0;
        var retryPolicy = new RetryPolicy { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(10) };

        // Act
        await handler.ExecuteWithRetryAsync(() =>
        {
            executionCount++;
            return Task.CompletedTask;
        }, retryPolicy);

        // Assert
        Assert.Equal(1, executionCount);
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ActionFailsAndRetries_ExhaustsRetriesAndCapturesError()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var executionCount = 0;
        var retryPolicy = new RetryPolicy { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(10) };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.ExecuteWithRetryAsync(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Test failure");
            }, retryPolicy));

        // Assert
        Assert.Equal(3, executionCount);
        Assert.Single(handler.CapturedErrors);
        var capturedError = handler.CapturedErrors[0];
        Assert.Equal(TestErrorType.RetryExhausted, capturedError.ErrorType);
        Assert.Contains("failed after 3 attempts", capturedError.Message);
        Assert.Equal(3, capturedError.RetryAttempts);
        Assert.Equal(exception, exception);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_ActionFailsButShouldNotRetry_DoesNotRetry()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var executionCount = 0;
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(10),
            RetryCondition = (ex, attempt) => false // Never retry
        };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.ExecuteWithRetryAsync(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Test failure");
            }, retryPolicy));

        // Assert
        Assert.Equal(1, executionCount); // Only one attempt, no retry
        Assert.Empty(handler.CapturedErrors); // No retry exhausted error
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithDiagnosticLoggingEnabled_LogsRetryAttempts()
    {
        // Arrange
        var options = new TestRelayOptions
        {
            EnableDiagnosticLogging = true,
            DiagnosticLogging = { EnableConsoleLogging = true }
        };
        var handler = new TestErrorHandler(options);
        var executionCount = 0;
        var retryPolicy = new RetryPolicy { MaxAttempts = 2, Delay = TimeSpan.FromMilliseconds(10) };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.ExecuteWithRetryAsync(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Test failure");
            }, retryPolicy));

        // Assert
        Assert.Equal(2, executionCount);
        Assert.Single(handler.CapturedErrors);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithZeroDelay_RetriesWithoutDelay()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var executionCount = 0;
        var retryPolicy = new RetryPolicy { MaxAttempts = 2, Delay = TimeSpan.Zero };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.ExecuteWithRetryAsync(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Test failure");
            }, retryPolicy));

        // Assert
        Assert.Equal(2, executionCount);
        Assert.Single(handler.CapturedErrors);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_FunctionSucceedsOnFirstTry_ReturnsResult()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var executionCount = 0;
        var retryPolicy = new RetryPolicy { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(10) };
        var expectedResult = "Success";

        // Act
        var result = await handler.ExecuteWithRetryAsync(() =>
        {
            executionCount++;
            return Task.FromResult(expectedResult);
        }, retryPolicy);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(1, executionCount);
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_FunctionFailsAndRetries_ExhaustsRetriesAndCapturesError()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var executionCount = 0;
        var retryPolicy = new RetryPolicy { MaxAttempts = 3, Delay = TimeSpan.FromMilliseconds(10) };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.ExecuteWithRetryAsync<string>(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Test failure");
            }, retryPolicy));

        // Assert
        Assert.Equal(3, executionCount);
        Assert.Single(handler.CapturedErrors);
        var capturedError = handler.CapturedErrors[0];
        Assert.Equal(TestErrorType.RetryExhausted, capturedError.ErrorType);
        Assert.Contains("failed after 3 attempts", capturedError.Message);
        Assert.Equal(3, capturedError.RetryAttempts);
        Assert.Equal(exception, exception);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_GenericFunctionFailsButShouldNotRetry_DoesNotRetry()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var executionCount = 0;
        var retryPolicy = new RetryPolicy
        {
            MaxAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(10),
            RetryCondition = (ex, attempt) => false
        };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.ExecuteWithRetryAsync<string>(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Test failure");
            }, retryPolicy));

        // Assert
        Assert.Equal(1, executionCount);
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_GenericWithDiagnosticLoggingEnabled_LogsRetryAttempts()
    {
        // Arrange
        var options = new TestRelayOptions
        {
            EnableDiagnosticLogging = true,
            DiagnosticLogging = { EnableConsoleLogging = true }
        };
        var handler = new TestErrorHandler(options);
        var executionCount = 0;
        var retryPolicy = new RetryPolicy { MaxAttempts = 2, Delay = TimeSpan.FromMilliseconds(10) };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.ExecuteWithRetryAsync<string>(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Test failure");
            }, retryPolicy));

        // Assert
        Assert.Equal(2, executionCount);
        Assert.Single(handler.CapturedErrors);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_GenericWithZeroDelay_RetriesWithoutDelay()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var executionCount = 0;
        var retryPolicy = new RetryPolicy { MaxAttempts = 2, Delay = TimeSpan.Zero };

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.ExecuteWithRetryAsync<string>(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Test failure");
            }, retryPolicy));

        // Assert
        Assert.Equal(2, executionCount);
        Assert.Single(handler.CapturedErrors);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_ActionCompletesBeforeTimeout_DoesNotThrow()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        await handler.ExecuteWithTimeoutAsync(() => Task.Delay(100), timeout);

        // Assert
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_ActionTimesOut_CapturesTimeoutError()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var timeout = TimeSpan.FromMilliseconds(50);

        // Act
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            handler.ExecuteWithTimeoutAsync(() => Task.Delay(200), timeout));

        // Assert
        Assert.Single(handler.CapturedErrors);
        var capturedError = handler.CapturedErrors[0];
        Assert.Equal(TestErrorType.Timeout, capturedError.ErrorType);
        Assert.Contains("timed out after 0.05 seconds", capturedError.Message);
        Assert.Equal("TimeoutHandler", capturedError.Source);
        Assert.Equal(exception.Message, capturedError.Message);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_ActionThrowsException_CapturesException()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.ExecuteWithTimeoutAsync(() =>
            {
                throw new InvalidOperationException("Test exception");
            }, timeout));

        // Assert
        Assert.Single(handler.CapturedErrors);
        var capturedError = handler.CapturedErrors[0];
        Assert.Equal(TestErrorType.Exception, capturedError.ErrorType);
        Assert.Equal("Test exception", capturedError.Message);
        Assert.Equal("TimeoutHandler", capturedError.Source);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_FunctionCompletesBeforeTimeout_ReturnsResult()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var timeout = TimeSpan.FromSeconds(1);
        var expectedResult = 42;

        // Act
        var result = await handler.ExecuteWithTimeoutAsync(() =>
            Task.FromResult(expectedResult), timeout);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_FunctionTimesOut_CapturesTimeoutError()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var timeout = TimeSpan.FromMilliseconds(50);

        // Act
        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            handler.ExecuteWithTimeoutAsync<int>(() => Task.Delay(200).ContinueWith(_ => 42), timeout));

        // Assert
        Assert.Single(handler.CapturedErrors);
        var capturedError = handler.CapturedErrors[0];
        Assert.Equal(TestErrorType.Timeout, capturedError.ErrorType);
        Assert.Contains("timed out after 0.05 seconds", capturedError.Message);
        Assert.Equal("TimeoutHandler", capturedError.Source);
    }

    [Fact]
    public async Task ExecuteWithTimeoutAsync_FunctionThrowsException_CapturesException()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.ExecuteWithTimeoutAsync<int>(() =>
            {
                throw new InvalidOperationException("Test exception");
            }, timeout));

        // Assert
        Assert.Single(handler.CapturedErrors);
        var capturedError = handler.CapturedErrors[0];
        Assert.Equal(TestErrorType.Exception, capturedError.ErrorType);
        Assert.Equal("Test exception", capturedError.Message);
        Assert.Equal("TimeoutHandler", capturedError.Source);
    }

    [Fact]
    public void ClearErrors_RemovesAllCapturedErrors()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        handler.CaptureError(new TestError { Message = "Error 1" });
        handler.CaptureError(new TestError { Message = "Error 2" });

        // Act
        handler.ClearErrors();

        // Assert
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public void GetDiagnosticReport_WithNoErrors_ReturnsEmptyReport()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);

        // Act
        var report = handler.GetDiagnosticReport();

        // Assert
        Assert.Equal(0, report.TotalErrors);
        Assert.Empty(report.ErrorsByType);
        Assert.Empty(report.ErrorsBySource);
        Assert.Empty(report.RecentErrors);
        Assert.Empty(report.ErrorPatterns);
    }

    [Fact]
    public void GetDiagnosticReport_WithErrors_ReturnsCorrectReport()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var now = DateTime.UtcNow;

        handler.CaptureError(new TestError
        {
            Message = "Error 1",
            Source = "Source1",
            ErrorType = TestErrorType.Exception,
            Timestamp = now.AddMinutes(-5)
        });

        handler.CaptureError(new TestError
        {
            Message = "Error 2",
            Source = "Source1",
            ErrorType = TestErrorType.Exception,
            Timestamp = now.AddMinutes(-3)
        });

        handler.CaptureError(new TestError
        {
            Message = "Error 3",
            Source = "Source2",
            ErrorType = TestErrorType.Timeout,
            Timestamp = now.AddMinutes(-1)
        });

        // Act
        var report = handler.GetDiagnosticReport();

        // Assert
        Assert.Equal(3, report.TotalErrors);
        Assert.Equal(2, report.ErrorsByType[TestErrorType.Exception]);
        Assert.Equal(1, report.ErrorsByType[TestErrorType.Timeout]);
        Assert.Equal(2, report.ErrorsBySource["Source1"]);
        Assert.Equal(1, report.ErrorsBySource["Source2"]);
        Assert.Equal(3, report.RecentErrors.Count);
        Assert.Equal("Error 3", report.RecentErrors[0].Message); // Most recent first
    }

    [Fact]
    public void GetDiagnosticReport_AnalyzesErrorPatterns()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var now = DateTime.UtcNow;

        // Add similar errors to create patterns
        handler.CaptureError(new TestError
        {
            Message = "Connection failed to host 192.168.1.1",
            Source = "NetworkClient",
            ErrorType = TestErrorType.Network,
            Timestamp = now.AddMinutes(-10)
        });

        handler.CaptureError(new TestError
        {
            Message = "Connection failed to host 192.168.1.2",
            Source = "NetworkClient",
            ErrorType = TestErrorType.Network,
            Timestamp = now.AddMinutes(-8)
        });

        handler.CaptureError(new TestError
        {
            Message = "Connection failed to host 192.168.1.3",
            Source = "NetworkClient",
            ErrorType = TestErrorType.Network,
            Timestamp = now.AddMinutes(-6)
        });

        // Act
        var report = handler.GetDiagnosticReport();

        // Assert
        Assert.Single(report.ErrorPatterns);
        var pattern = report.ErrorPatterns[0];
        Assert.Equal("Connection failed to host {number}.{number}.{number}.{number}", pattern.Pattern);
        Assert.Equal(3, pattern.Occurrences);
        Assert.Equal("NetworkClient", pattern.Sources[0]);
    }

    [Fact]
    public void GetDiagnosticReport_WithUniqueErrors_NoPatternsDetected()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var now = DateTime.UtcNow;

        // Add unique errors (no patterns)
        handler.CaptureError(new TestError
        {
            Message = "Unique error one",
            Source = "Source1",
            ErrorType = TestErrorType.Exception,
            Timestamp = now.AddMinutes(-10)
        });

        handler.CaptureError(new TestError
        {
            Message = "Unique error two",
            Source = "Source2",
            ErrorType = TestErrorType.Exception,
            Timestamp = now.AddMinutes(-8)
        });

        // Act
        var report = handler.GetDiagnosticReport();

        // Assert
        Assert.Empty(report.ErrorPatterns); // No patterns since all messages are unique
    }

    [Fact]
    public void GetDiagnosticReport_AnalyzesPatternsWithEmptyMessagesAndGuids()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var now = DateTime.UtcNow;
        var guid = Guid.NewGuid().ToString();

        // Add errors with empty message and GUID
        handler.CaptureError(new TestError
        {
            Message = "",
            Source = "Source1",
            ErrorType = TestErrorType.Exception,
            Timestamp = now.AddMinutes(-10)
        });

        handler.CaptureError(new TestError
        {
            Message = "",
            Source = "Source1",
            ErrorType = TestErrorType.Exception,
            Timestamp = now.AddMinutes(-9)
        });

        handler.CaptureError(new TestError
        {
            Message = $"Error with GUID {guid}",
            Source = "Source2",
            ErrorType = TestErrorType.Exception,
            Timestamp = now.AddMinutes(-8)
        });

        handler.CaptureError(new TestError
        {
            Message = $"Error with GUID {guid}",
            Source = "Source2",
            ErrorType = TestErrorType.Exception,
            Timestamp = now.AddMinutes(-6)
        });

        // Act
        var report = handler.GetDiagnosticReport();

        // Assert
        Assert.Equal(2, report.ErrorPatterns.Count);
        var emptyPattern = report.ErrorPatterns.First(p => p.Pattern == "Empty");
        Assert.Equal(2, emptyPattern.Occurrences);

        var guidPattern = report.ErrorPatterns.First(p => p.Pattern.Contains("{guid}"));
        Assert.Equal(2, guidPattern.Occurrences);
    }

    [Fact]
    public async Task WithRetry_ExtensionMethod_WorksCorrectly()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var executionCount = 0;

        // Act
        await handler.WithRetry(() =>
        {
            executionCount++;
            if (executionCount < 2)
                throw new InvalidOperationException("Retry me");
            return Task.CompletedTask;
        }, maxAttempts: 3, delay: TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.Equal(2, executionCount);
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public async Task WithRetry_GenericExtensionMethod_WorksCorrectly()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);
        var executionCount = 0;

        // Act
        var result = await handler.WithRetry(() =>
        {
            executionCount++;
            if (executionCount < 2)
                throw new InvalidOperationException("Retry me");
            return Task.FromResult("Success");
        }, maxAttempts: 3, delay: TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(2, executionCount);
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public async Task WithTimeout_ExtensionMethod_WorksCorrectly()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);

        // Act
        await handler.WithTimeout(() => Task.Delay(100), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public async Task WithTimeout_GenericExtensionMethod_WorksCorrectly()
    {
        // Arrange
        var handler = new TestErrorHandler(_defaultOptions);

        // Act
        var result = await handler.WithTimeout(() => Task.FromResult(42), TimeSpan.FromSeconds(1));

        // Assert
        Assert.Equal(42, result);
        Assert.Empty(handler.CapturedErrors);
    }

    [Fact]
    public void RetryPolicy_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var policy = new RetryPolicy();

        // Assert
        Assert.Equal(3, policy.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(1), policy.Delay);
        Assert.Null(policy.RetryCondition);
        Assert.Equal(BackoffStrategy.Fixed, policy.BackoffStrategy);
    }

    [Fact]
    public void RetryPolicy_ShouldRetry_WithDefaultCondition_RetriesTransientExceptions()
    {
        // Arrange
        var policy = new RetryPolicy();

        // Act & Assert
        Assert.True(policy.ShouldRetry(new TimeoutException(), 1));
        Assert.True(policy.ShouldRetry(new System.Net.Http.HttpRequestException(), 1));
        Assert.True(policy.ShouldRetry(new InvalidOperationException(), 1));
        Assert.False(policy.ShouldRetry(new ArgumentException(), 1)); // Not transient
    }

    [Fact]
    public void RetryPolicy_ShouldRetry_WithCustomCondition_UsesCustomLogic()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            RetryCondition = (ex, attempt) => ex is ArgumentException
        };

        // Act & Assert
        Assert.True(policy.ShouldRetry(new ArgumentException(), 1));
        Assert.False(policy.ShouldRetry(new InvalidOperationException(), 1));
    }

    [Fact]
    public void RetryPolicy_ShouldRetry_WhenMaxAttemptsReached_ReturnsFalse()
    {
        // Arrange
        var policy = new RetryPolicy { MaxAttempts = 2 };

        // Act & Assert
        Assert.False(policy.ShouldRetry(new InvalidOperationException(), 2));
        Assert.False(policy.ShouldRetry(new InvalidOperationException(), 3));
    }

    [Fact]
    public void RetryPolicy_GetDelay_WithFixedStrategy_ReturnsConstantDelay()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            Delay = TimeSpan.FromSeconds(2),
            BackoffStrategy = BackoffStrategy.Fixed
        };

        // Act & Assert
        Assert.Equal(TimeSpan.FromSeconds(2), policy.GetDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(2), policy.GetDelay(2));
        Assert.Equal(TimeSpan.FromSeconds(2), policy.GetDelay(3));
    }

    [Fact]
    public void RetryPolicy_GetDelay_WithLinearStrategy_IncreasesLinearly()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            Delay = TimeSpan.FromSeconds(2),
            BackoffStrategy = BackoffStrategy.Linear
        };

        // Act & Assert
        Assert.Equal(TimeSpan.FromSeconds(2), policy.GetDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(4), policy.GetDelay(2));
        Assert.Equal(TimeSpan.FromSeconds(6), policy.GetDelay(3));
    }

    [Fact]
    public void RetryPolicy_GetDelay_WithExponentialStrategy_IncreasesExponentially()
    {
        // Arrange
        var policy = new RetryPolicy
        {
            Delay = TimeSpan.FromSeconds(2),
            BackoffStrategy = BackoffStrategy.Exponential
        };

        // Act & Assert
        Assert.Equal(TimeSpan.FromSeconds(2), policy.GetDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(4), policy.GetDelay(2));
        Assert.Equal(TimeSpan.FromSeconds(8), policy.GetDelay(3));
    }

    [Fact]
    public void TestError_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var exception = new Exception("Test");
        var error = new TestError
        {
            Message = "Test message",
            Exception = exception,
            Source = "Test source",
            Timestamp = timestamp,
            ErrorType = TestErrorType.Exception,
            StackTrace = "Test stack trace",
            RetryAttempts = 5
        };

        // Assert
        Assert.Equal("Test message", error.Message);
        Assert.Equal(exception, error.Exception);
        Assert.Equal("Test source", error.Source);
        Assert.Equal(timestamp, error.Timestamp);
        Assert.Equal(TestErrorType.Exception, error.ErrorType);
        Assert.Equal("Test stack trace", error.StackTrace);
        Assert.Equal(5, error.RetryAttempts);
    }

    [Fact]
    public void ErrorDiagnosticReport_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var report = new ErrorDiagnosticReport
        {
            TotalErrors = 10,
            ErrorsByType = new Dictionary<TestErrorType, int> { { TestErrorType.Exception, 8 } },
            ErrorsBySource = new Dictionary<string, int> { { "Source1", 6 } },
            RecentErrors = new List<TestError> { new TestError { Message = "Recent" } },
            ErrorPatterns = new List<ErrorPattern> { new ErrorPattern { Pattern = "Pattern1" } }
        };

        // Assert
        Assert.Equal(10, report.TotalErrors);
        Assert.Single(report.ErrorsByType);
        Assert.Single(report.ErrorsBySource);
        Assert.Single(report.RecentErrors);
        Assert.Single(report.ErrorPatterns);
    }

    [Fact]
    public void ErrorPattern_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var firstOccurrence = DateTime.UtcNow.AddHours(-1);
        var lastOccurrence = DateTime.UtcNow;
        var pattern = new ErrorPattern
        {
            Pattern = "Test pattern",
            Occurrences = 5,
            FirstOccurrence = firstOccurrence,
            LastOccurrence = lastOccurrence,
            Sources = new List<string> { "Source1", "Source2" }
        };

        // Assert
        Assert.Equal("Test pattern", pattern.Pattern);
        Assert.Equal(5, pattern.Occurrences);
        Assert.Equal(firstOccurrence, pattern.FirstOccurrence);
        Assert.Equal(lastOccurrence, pattern.LastOccurrence);
        Assert.Equal(2, pattern.Sources.Count);
    }
}
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Core;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Comprehensive tests for ErrorIsolation.ExecuteWithErrorHandling&lt;T&gt; method.
/// Covers all branches, edge cases, and exception scenarios.
/// </summary>
public class ErrorIsolationExecuteWithErrorHandlingTests
{
    private TestDiagnosticReporter _diagnosticReporter;

    public ErrorIsolationExecuteWithErrorHandlingTests()
    {
        _diagnosticReporter = new TestDiagnosticReporter();
    }

    #region Parameter Validation Tests

    [Fact]
    public void ExecuteWithErrorHandling_NullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        Func<string> operation = null;
        var defaultValue = "default";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            ErrorIsolation.ExecuteWithErrorHandling(operation, "TestOperation", defaultValue, _diagnosticReporter));
        
        Assert.Equal(nameof(operation), exception.ParamName);
    }

    [Fact]
    public void ExecuteWithErrorHandling_NullDiagnosticReporter_ThrowsArgumentNullException()
    {
        // Arrange
        Func<string> operation = () => "result";
        var defaultValue = "default";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            ErrorIsolation.ExecuteWithErrorHandling(operation, "TestOperation", defaultValue, null));
        
        Assert.Equal("diagnosticReporter", exception.ParamName);
    }

    #endregion

    #region Successful Execution Tests

    [Fact]
    public void ExecuteWithErrorHandling_SuccessfulStringOperation_ReturnsResult()
    {
        // Arrange
        Func<string> operation = () => "success";
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "TestOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal("success", result);
        Assert.Equal(0, _diagnosticReporter.Diagnostics.Count);
    }

    [Fact]
    public void ExecuteWithErrorHandling_SuccessfulIntegerOperation_ReturnsResult()
    {
        // Arrange
        Func<int> operation = () => 42;
        var defaultValue = 0;

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "TestOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(42, result);
        Assert.Equal(0, _diagnosticReporter.Diagnostics.Count);
    }

    [Fact]
    public void ExecuteWithErrorHandling_SuccessfulNullReturningOperation_ReturnsNull()
    {
        // Arrange
        Func<string> operation = () => null;
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "TestOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Null(result);
        Assert.Equal(0, _diagnosticReporter.Diagnostics.Count);
    }

    #endregion

    #region OperationCanceledException Tests

    [Fact]
    public void ExecuteWithErrorHandling_OperationCanceledException_ReturnsDefaultValue()
    {
        // Arrange
        Func<string> operation = () => throw new OperationCanceledException();
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "TestOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(0, _diagnosticReporter.Diagnostics.Count);
    }

    [Fact]
    public void ExecuteWithErrorHandling_OperationCanceledExceptionWithMessage_ReturnsDefaultValue()
    {
        // Arrange
        Func<string> operation = () => throw new OperationCanceledException("Operation was cancelled");
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "TestOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(0, _diagnosticReporter.Diagnostics.Count);
    }

    #endregion

    #region OutOfMemoryException Tests (Critical Errors)

    [Fact]
    public void ExecuteWithErrorHandling_OutOfMemoryException_ReturnsDefaultValueAndReportsCriticalError()
    {
        // Arrange
        Func<string> operation = () => throw new OutOfMemoryException("Out of memory");
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "MemoryOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.GeneratorError.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("An error occurred during source generation: Critical error in operation 'MemoryOperation': OutOfMemoryException - Out of memory", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteWithErrorHandling_OutOfMemoryExceptionWithNullMessage_ReturnsDefaultValueAndReportsCriticalError()
    {
        // Arrange
        Func<string> operation = () => throw new OutOfMemoryException(null);
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "MemoryOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Critical error in operation 'MemoryOperation': OutOfMemoryException - Exception of type 'System.OutOfMemoryException' was thrown.", diagnostic.GetMessage());
    }

    #endregion

    #region StackOverflowException Tests (Critical Errors)

    [Fact]
    public void ExecuteWithErrorHandling_StackOverflowException_ReturnsDefaultValueAndReportsCriticalError()
    {
        // Arrange
        Func<string> operation = () => throw new StackOverflowException("Stack overflow");
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "RecursiveOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.GeneratorError.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("An error occurred during source generation: Critical error in operation 'RecursiveOperation': StackOverflowException - Stack overflow", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteWithErrorHandling_StackOverflowExceptionWithNullMessage_ReturnsDefaultValueAndReportsCriticalError()
    {
        // Arrange
        Func<string> operation = () => throw new StackOverflowException(null);
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "RecursiveOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Critical error in operation 'RecursiveOperation': StackOverflowException - Exception of type 'System.StackOverflowException' was thrown.", diagnostic.GetMessage());
    }

    #endregion

    #region Regular Exception Tests

    [Fact]
    public void ExecuteWithErrorHandling_InvalidOperationException_ReturnsDefaultValueAndReportsError()
    {
        // Arrange
        Func<string> operation = () => throw new InvalidOperationException("Invalid operation");
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "InvalidOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.GeneratorError.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("An error occurred during source generation: Error in operation 'InvalidOperation': Invalid operation", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteWithErrorHandling_ArgumentException_ReturnsDefaultValueAndReportsError()
    {
        // Arrange
        Func<string> operation = () => throw new ArgumentException("Invalid argument");
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "ArgumentOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Error in operation 'ArgumentOperation': Invalid argument", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteWithErrorHandling_CustomException_ReturnsDefaultValueAndReportsError()
    {
        // Arrange
        Func<string> operation = () => throw new CustomTestException("Custom error occurred");
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "CustomOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Error in operation 'CustomOperation': Custom error occurred", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteWithErrorHandling_NestedException_ReturnsDefaultValueAndReportsError()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        Func<string> operation = () => throw new Exception("Outer error", innerException);
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "NestedOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Error in operation 'NestedOperation': Outer error", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteWithErrorHandling_EmptyMessageException_ReturnsDefaultValueAndReportsError()
    {
        // Arrange
        Func<string> operation = () => throw new Exception("");
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "EmptyMessageOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Error in operation 'EmptyMessageOperation': ", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteWithErrorHandling_NullMessageException_ReturnsDefaultValueAndReportsError()
    {
        // Arrange
        Func<string> operation = () => throw new Exception(null);
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "NullMessageOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Error in operation 'NullMessageOperation': Exception of type 'System.Exception' was thrown.", diagnostic.GetMessage());
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [Fact]
    public void ExecuteWithErrorHandling_OperationNameWithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var operationName = "Test-Operation_123!@#$%^&*()";
        Func<string> operation = () => throw new Exception("Test error");
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, operationName, defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal($"An error occurred during source generation: Error in operation '{operationName}': Test error", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteWithErrorHandling_EmptyOperationName_HandlesCorrectly()
    {
        // Arrange
        Func<string> operation = () => throw new Exception("Test error");
        var defaultValue = "default";

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Error in operation '': Test error", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteWithErrorHandling_DefaultValueIsNull_ReturnsNullOnError()
    {
        // Arrange
        Func<string> operation = () => throw new Exception("Error occurred");
        string defaultValue = null;

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "TestOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Null(result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
    }

    [Fact]
    public void ExecuteWithErrorHandling_MultipleCallsWithSameOperation_EachCallIsIsolated()
    {
        // Arrange
        var callCount = 0;
        Func<int> operation = () =>
        {
            callCount++;
            if (callCount == 1)
                throw new Exception("First call fails");
            return callCount;
        };
        var defaultValue = -1;

        // Act
        var result1 = ErrorIsolation.ExecuteWithErrorHandling(operation, "MultiCallOperation", defaultValue, _diagnosticReporter);
        var result2 = ErrorIsolation.ExecuteWithErrorHandling(operation, "MultiCallOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(defaultValue, result1); // First call fails
        Assert.Equal(2, result2); // Second call succeeds
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count); // Only one error reported
    }

    #endregion

    #region Type-Specific Tests

    [Fact]
    public void ExecuteWithErrorHandling_BooleanOperation_HandlesCorrectly()
    {
        // Arrange
        Func<bool> operation = () => true;
        var defaultValue = false;

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "BoolOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.True(result);
        Assert.Equal(0, _diagnosticReporter.Diagnostics.Count);
    }

    [Fact]
    public void ExecuteWithErrorHandling_BooleanOperationWithException_ReturnsFalseAndReportsError()
    {
        // Arrange
        Func<bool> operation = () => throw new Exception("Boolean operation failed");
        var defaultValue = false;

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "BoolOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.False(result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
    }

    [Fact]
    public void ExecuteWithErrorHandling_ValueTypeOperation_HandlesCorrectly()
    {
        // Arrange
        Func<DateTime> operation = () => DateTime.Now;
        var defaultValue = DateTime.MinValue;

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "DateTimeOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.NotEqual(DateTime.MinValue, result);
        Assert.Equal(0, _diagnosticReporter.Diagnostics.Count);
    }

    [Fact]
    public void ExecuteWithErrorHandling_ValueTypeOperationWithException_ReturnsDefaultValueAndReportsError()
    {
        // Arrange
        Func<DateTime> operation = () => throw new Exception("DateTime operation failed");
        var defaultValue = DateTime.MinValue;

        // Act
        var result = ErrorIsolation.ExecuteWithErrorHandling(operation, "DateTimeOperation", defaultValue, _diagnosticReporter);

        // Assert
        Assert.Equal(DateTime.MinValue, result);
        Assert.Equal(1, _diagnosticReporter.Diagnostics.Count);
    }

    #endregion

    #region Helper Classes

    private class CustomTestException : Exception
    {
        public CustomTestException(string message) : base(message) { }
        public CustomTestException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion
}
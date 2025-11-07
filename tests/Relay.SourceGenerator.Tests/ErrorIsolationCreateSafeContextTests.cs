using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Core;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Tests for ErrorIsolation.CreateSafeContext method and SafeExecutionContext class.
/// Verifies safe execution context creation and operation isolation.
/// </summary>
public class ErrorIsolationCreateSafeContextTests
{
    private TestDiagnosticReporter _diagnosticReporter;

    public ErrorIsolationCreateSafeContextTests()
    {
        _diagnosticReporter = new TestDiagnosticReporter();
    }

    #region CreateSafeContext Tests

    [Fact]
    public void CreateSafeContext_ValidDiagnosticReporter_ReturnsSafeExecutionContext()
    {
        // Arrange & Act
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Assert
        Assert.NotNull(context);
        Assert.False(context.HasErrors);
        Assert.Empty(context.Errors);
    }

    [Fact]
    public void CreateSafeContext_NullDiagnosticReporter_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ErrorIsolation.CreateSafeContext(null));
    }

    #endregion

    #region SafeExecutionContext Execute Action Tests

    [Fact]
    public void SafeExecutionContext_Execute_SuccessfulOperation_ReturnsTrue()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);
        var executed = false;

        // Act
        var result = context.Execute(() => executed = true, "TestOperation");

        // Assert
        Assert.True(result);
        Assert.True(executed);
        Assert.False(context.HasErrors);
        Assert.Empty(context.Errors);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void SafeExecutionContext_Execute_InvalidOperationException_ReturnsFalseAndReportsError()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var result = context.Execute(() => throw new InvalidOperationException("Invalid operation"), "TestOperation");

        // Assert
        Assert.False(result);
        Assert.True(context.HasErrors);
        Assert.Single(context.Errors);
        Assert.IsType<InvalidOperationException>(context.Errors[0]);
        Assert.Single(_diagnosticReporter.Diagnostics);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.GeneratorError.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("An error occurred during source generation: Error in operation 'TestOperation': Invalid operation", diagnostic.GetMessage());
    }

    [Fact]
    public void SafeExecutionContext_Execute_CriticalException_ReturnsFalseAndReportsCriticalError()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var result = context.Execute(() => throw new OutOfMemoryException("Out of memory"), "CriticalOperation");

        // Assert
        Assert.False(result);
        Assert.True(context.HasErrors);
        Assert.Single(context.Errors);
        Assert.IsType<OutOfMemoryException>(context.Errors[0]);
        Assert.Single(_diagnosticReporter.Diagnostics);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.GeneratorError.Id, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("An error occurred during source generation: Critical error in operation 'CriticalOperation': OutOfMemoryException - Out of memory", diagnostic.GetMessage());
    }

    [Fact]
    public void SafeExecutionContext_Execute_OperationCanceledException_PropagatesException()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act & Assert
        Assert.Throws<OperationCanceledException>(() =>
            context.Execute(() => throw new OperationCanceledException("Cancelled"), "CancelledOperation"));
        
        Assert.False(context.HasErrors);
        Assert.Empty(context.Errors);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void SafeExecutionContext_Execute_MultipleOperations_IsolatesErrors()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var result1 = context.Execute(() => { }, "SuccessOperation");
        var result2 = context.Execute(() => throw new InvalidOperationException("Error 1"), "ErrorOperation1");
        var result3 = context.Execute(() => { }, "SuccessOperation2");
        var result4 = context.Execute(() => throw new ArgumentException("Error 2"), "ErrorOperation2");

        // Assert
        Assert.True(result1);
        Assert.False(result2);
        Assert.True(result3);
        Assert.False(result4);
        
        Assert.True(context.HasErrors);
        Assert.Equal(2, context.Errors.Count);
        Assert.Equal(2, _diagnosticReporter.Diagnostics.Count);
    }

    [Fact]
    public void SafeExecutionContext_Execute_NullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            context.Execute(null, "TestOperation"));
    }

    [Fact]
    public void SafeExecutionContext_Execute_NullOperationName_HandlesCorrectly()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var result = context.Execute(() => throw new Exception("Test error"), null);

        // Assert
        Assert.False(result);
        Assert.True(context.HasErrors);
        Assert.Single(context.Errors);
        Assert.Single(_diagnosticReporter.Diagnostics);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Error in operation '': Test error", diagnostic.GetMessage());
    }

    #endregion

    #region SafeExecutionContext Execute Func Tests

    [Fact]
    public void SafeExecutionContext_ExecuteFunc_SuccessfulOperation_ReturnsResult()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var result = context.Execute(() => "success", "TestOperation", "default");

        // Assert
        Assert.Equal("success", result);
        Assert.False(context.HasErrors);
        Assert.Empty(context.Errors);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void SafeExecutionContext_ExecuteFunc_InvalidOperationException_ReturnsDefaultAndReportsError()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var result = context.Execute(() => throw new InvalidOperationException("Invalid operation"), "TestOperation", "default");

        // Assert
        Assert.Equal("default", result);
        Assert.True(context.HasErrors);
        Assert.Single(context.Errors);
        Assert.IsType<InvalidOperationException>(context.Errors[0]);
        Assert.Single(_diagnosticReporter.Diagnostics);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Error in operation 'TestOperation': Invalid operation", diagnostic.GetMessage());
    }

    [Fact]
    public void SafeExecutionContext_ExecuteFunc_CriticalException_ReturnsDefaultAndReportsCriticalError()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var result = context.Execute(() => throw new StackOverflowException("Stack overflow"), "CriticalOperation", 42);

        // Assert
        Assert.Equal(42, result);
        Assert.True(context.HasErrors);
        Assert.Single(context.Errors);
        Assert.IsType<StackOverflowException>(context.Errors[0]);
        Assert.Single(_diagnosticReporter.Diagnostics);
        
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Critical error in operation 'CriticalOperation': StackOverflowException - Stack overflow", diagnostic.GetMessage());
    }

    [Fact]
    public void SafeExecutionContext_ExecuteFunc_OperationCanceledException_PropagatesException()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act & Assert
        Assert.Throws<OperationCanceledException>(() =>
            context.Execute(() => throw new OperationCanceledException("Cancelled"), "CancelledOperation", "default"));
        
        Assert.False(context.HasErrors);
        Assert.Empty(context.Errors);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void SafeExecutionContext_ExecuteFunc_MultipleOperations_IsolatesErrors()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var result1 = context.Execute(() => 1, "SuccessOperation", 0);
        var result2 = context.Execute(() => throw new InvalidOperationException("Error 1"), "ErrorOperation1", 0);
        var result3 = context.Execute(() => 3, "SuccessOperation2", 0);
        var result4 = context.Execute(() => throw new ArgumentException("Error 2"), "ErrorOperation2", 0);

        // Assert
        Assert.Equal(1, result1);
        Assert.Equal(0, result2);
        Assert.Equal(3, result3);
        Assert.Equal(0, result4);
        
        Assert.True(context.HasErrors);
        Assert.Equal(2, context.Errors.Count);
        Assert.Equal(2, _diagnosticReporter.Diagnostics.Count);
    }

    [Fact]
    public void SafeExecutionContext_ExecuteFunc_NullOperation_ThrowsArgumentNullException()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            context.Execute((Func<string>)null, "TestOperation", "default"));
    }

    [Fact]
    public void SafeExecutionContext_ExecuteFunc_NullDefaultValue_HandlesCorrectly()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var result = context.Execute<string?>(() => throw new Exception("Test error"), "TestOperation", null);

        // Assert
        Assert.Null(result);
        Assert.True(context.HasErrors);
        Assert.Single(context.Errors);
        Assert.Single(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void SafeExecutionContext_ExecuteFunc_ValueTypeWithNullDefaultValue_HandlesCorrectly()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var result = context.Execute(() => throw new Exception("Test error"), "TestOperation", null as int?);

        // Assert
        Assert.Null(result);
        Assert.True(context.HasErrors);
        Assert.Single(context.Errors);
        Assert.Single(_diagnosticReporter.Diagnostics);
    }

    #endregion

    #region Error Collection Tests

    [Fact]
    public void SafeExecutionContext_ErrorsProperty_ReadOnlyList()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        var errors = context.Errors;

        // Assert
        Assert.NotNull(errors);
        Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<Exception>>(errors);
    }

    [Fact]
    public void SafeExecutionContext_ErrorsCollection_ReflectsActualErrors()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act
        context.Execute(() => throw new Exception("Error 1"), "Operation1");
        context.Execute(() => throw new Exception("Error 2"), "Operation2");

        // Assert
        Assert.Equal(2, context.Errors.Count);
        Assert.Equal("Error 1", context.Errors[0].Message);
        Assert.Equal("Error 2", context.Errors[1].Message);
    }

    [Fact]
    public void SafeExecutionContext_HasErrors_AccuratelyReflectsErrorState()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);

        // Act & Assert - Initially no errors
        Assert.False(context.HasErrors);

        // Act - Add an error
        context.Execute(() => throw new Exception("Test error"), "TestOperation");

        // Assert - Now has errors
        Assert.True(context.HasErrors);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SafeExecutionContext_Integration_MixedOperations_CorrectIsolation()
    {
        // Arrange
        var context = ErrorIsolation.CreateSafeContext(_diagnosticReporter);
        var results = new List<string>();

        // Act
        var success1 = context.Execute(() => results.Add("success1"), "Success1");
        var fail1 = context.Execute(() => throw new InvalidOperationException("Fail1"), "Fail1");
        var success2 = context.Execute(() => results.Add("success2"), "Success2");
        var fail2 = context.Execute(() => throw new OutOfMemoryException("Fail2"), "Fail2");
        var success3 = context.Execute(() => results.Add("success3"), "Success3");

        // Assert
        Assert.True(success1);
        Assert.False(fail1);
        Assert.True(success2);
        Assert.False(fail2);
        Assert.True(success3);

        Assert.Equal(3, results.Count);
        Assert.Contains("success1", results);
        Assert.Contains("success2", results);
        Assert.Contains("success3", results);

        Assert.True(context.HasErrors);
        Assert.Equal(2, context.Errors.Count);
        Assert.Equal(2, _diagnosticReporter.Diagnostics.Count);
    }

    #endregion
}
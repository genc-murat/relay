using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Core;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator.Tests;

/// <summary>
/// Comprehensive tests for ErrorIsolation.ExecuteDiscoveryWithIsolation method.
/// Covers all branches, cases, and throws.
/// </summary>
public class ErrorIsolationExecuteDiscoveryWithIsolationTests
{
    private readonly TestDiagnosticReporter _diagnosticReporter = new();

    private class TestDiagnosticReporter : IDiagnosticReporter
    {
        public List<Diagnostic> Diagnostics { get; } = new();

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            Diagnostics.Add(diagnostic);
        }
    }

    #region Parameter Validation Tests

    [Fact]
    public void ExecuteDiscoveryWithIsolation_NullDiscoveryAction_ThrowsArgumentNullException()
    {
        // Arrange
        var reporter = new TestDiagnosticReporter();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            ErrorIsolation.ExecuteDiscoveryWithIsolation(null!, reporter));
        
        Assert.Equal("discoveryAction", exception.ParamName);
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_NullDiagnosticReporter_ThrowsArgumentNullException()
    {
        // Arrange
        Func<HandlerDiscoveryResult> discoveryAction = () => new HandlerDiscoveryResult();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, null!));
        
        Assert.Equal("diagnosticReporter", exception.ParamName);
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_BothParametersNull_ThrowsArgumentNullExceptionForDiscoveryAction()
    {
        // Act & Assert - First parameter check happens first
        var exception = Assert.Throws<ArgumentNullException>(() =>
            ErrorIsolation.ExecuteDiscoveryWithIsolation(null!, null!));
        
        Assert.Equal("discoveryAction", exception.ParamName);
    }

    #endregion

    #region Successful Discovery Tests

    [Fact]
    public void ExecuteDiscoveryWithIsolation_SuccessfulDiscovery_ReturnsResult()
    {
        // Arrange
        var expectedResult = new HandlerDiscoveryResult();
        Func<HandlerDiscoveryResult> discoveryAction = () => expectedResult;

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.Same(expectedResult, result);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_DiscoveryReturnsEmptyResult_ReturnsEmptyResult()
    {
        // Arrange
        var expectedResult = new HandlerDiscoveryResult();
        Func<HandlerDiscoveryResult> discoveryAction = () => expectedResult;

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Handlers);
        Assert.Empty(result.NotificationHandlers);
        Assert.Empty(result.PipelineBehaviors);
        Assert.Empty(result.StreamHandlers);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_DiscoveryReturnsResultWithData_ReturnsCorrectData()
    {
        // Arrange
        var expectedResult = new HandlerDiscoveryResult();
        Func<HandlerDiscoveryResult> discoveryAction = () => expectedResult;

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.Same(expectedResult, result);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_MultipleSuccessfulCalls_EachCallIsIsolated()
    {
        // Arrange
        var callCount = 0;
        Func<HandlerDiscoveryResult> discoveryAction = () =>
        {
            callCount++;
            return new HandlerDiscoveryResult();
        };

        // Act
        var result1 = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);
        var result2 = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.Equal(2, callCount);
        Assert.NotSame(result1, result2);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    #endregion

    #region OperationCanceledException Tests

    [Fact]
    public void ExecuteDiscoveryWithIsolation_OperationCanceledException_Throws()
    {
        // Arrange
        var cancellationToken = new System.Threading.CancellationToken(true);
        Func<HandlerDiscoveryResult> discoveryAction = () => 
            throw new OperationCanceledException("Operation was canceled", cancellationToken);

        // Act & Assert
        var exception = Assert.Throws<OperationCanceledException>(() =>
            ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter));
        
        Assert.Equal("Operation was canceled", exception.Message);
        Assert.Equal(cancellationToken, exception.CancellationToken);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_OperationCanceledExceptionWithoutMessage_Throws()
    {
        // Arrange
        Func<HandlerDiscoveryResult> discoveryAction = () => 
            throw new OperationCanceledException();

        // Act & Assert
        Assert.Throws<OperationCanceledException>(() =>
            ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter));
        
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_OperationCanceledExceptionWithInnerException_Throws()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        Func<HandlerDiscoveryResult> discoveryAction = () => 
            throw new OperationCanceledException("Operation canceled", innerException);

        // Act & Assert
        var exception = Assert.Throws<OperationCanceledException>(() =>
            ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter));
        
        Assert.Equal("Operation canceled", exception.Message);
        Assert.Same(innerException, exception.InnerException);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_OperationCanceledExceptionWithToken_Throws()
    {
        // Arrange
        var cancellationToken = new System.Threading.CancellationToken(false);
        Func<HandlerDiscoveryResult> discoveryAction = () => 
            throw new OperationCanceledException("Operation canceled", cancellationToken);

        // Act & Assert
        var exception = Assert.Throws<OperationCanceledException>(() =>
            ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter));
        
        Assert.Equal("Operation canceled", exception.Message);
        Assert.Equal(cancellationToken, exception.CancellationToken);
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    #endregion

    #region General Exception Handling Tests

    [Fact]
    public void ExecuteDiscoveryWithIsolation_GeneralException_ReturnsEmptyResultAndReportsError()
    {
        // Arrange
        var testException = new InvalidOperationException("Discovery failed");
        Func<HandlerDiscoveryResult> discoveryAction = () => throw testException;

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Handlers);
        Assert.Empty(result.NotificationHandlers);
        Assert.Empty(result.PipelineBehaviors);
        Assert.Empty(result.StreamHandlers);
        
        Assert.Single(_diagnosticReporter.Diagnostics);
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Handler discovery failed: Discovery failed", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_NullMessageException_ReturnsEmptyResultAndReportsError()
    {
        // Arrange
        Func<HandlerDiscoveryResult> discoveryAction = () => throw new Exception(null);

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Handlers);
        Assert.Empty(result.NotificationHandlers);
        Assert.Empty(result.PipelineBehaviors);
        Assert.Empty(result.StreamHandlers);
        
        Assert.Single(_diagnosticReporter.Diagnostics);
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal("An error occurred during source generation: Handler discovery failed: Exception of type 'System.Exception' was thrown.", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_NestedException_ReturnsEmptyResultAndReportsError()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var outerException = new Exception("Outer error", innerException);
        Func<HandlerDiscoveryResult> discoveryAction = () => throw outerException;

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Handlers);
        Assert.Empty(result.NotificationHandlers);
        Assert.Empty(result.PipelineBehaviors);
        Assert.Empty(result.StreamHandlers);
        
        Assert.Single(_diagnosticReporter.Diagnostics);
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal(DiagnosticDescriptors.GeneratorError.Id, diagnostic.Id);
        Assert.Equal("An error occurred during source generation: Handler discovery failed: Outer error", diagnostic.GetMessage());
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
    }

    #endregion

    #region Diagnostic Reporting Tests

    [Fact]
    public void ExecuteDiscoveryWithIsolation_ExceptionWithSpecialCharactersInMessage_ReportsCorrectly()
    {
        // Arrange
        var specialMessage = "Error with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";
        Func<HandlerDiscoveryResult> discoveryAction = () => throw new Exception(specialMessage);

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Handlers);
        Assert.Empty(result.NotificationHandlers);
        Assert.Empty(result.PipelineBehaviors);
        Assert.Empty(result.StreamHandlers);
        
        Assert.Single(_diagnosticReporter.Diagnostics);
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal($"An error occurred during source generation: Handler discovery failed: {specialMessage}", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_ExceptionWithUnicodeMessage_ReportsCorrectly()
    {
        // Arrange
        var unicodeMessage = "Hata mesajı: üğışöç 中文 العربية";
        Func<HandlerDiscoveryResult> discoveryAction = () => throw new Exception(unicodeMessage);

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Handlers);
        Assert.Empty(result.NotificationHandlers);
        Assert.Empty(result.PipelineBehaviors);
        Assert.Empty(result.StreamHandlers);
        
        Assert.Single(_diagnosticReporter.Diagnostics);
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal($"An error occurred during source generation: Handler discovery failed: {unicodeMessage}", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_ExceptionWithVeryLongMessage_ReportsCorrectly()
    {
        // Arrange
        var longMessage = new string('A', 10000);
        Func<HandlerDiscoveryResult> discoveryAction = () => throw new Exception(longMessage);

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Handlers);
        Assert.Empty(result.NotificationHandlers);
        Assert.Empty(result.PipelineBehaviors);
        Assert.Empty(result.StreamHandlers);
        
        Assert.Single(_diagnosticReporter.Diagnostics);
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Equal($"An error occurred during source generation: Handler discovery failed: {longMessage}", diagnostic.GetMessage());
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public void ExecuteDiscoveryWithIsolation_MultipleConcurrentCalls_HandlesCorrectly()
    {
        // Arrange
        var tasks = new System.Threading.Tasks.Task[10];
        var results = new HandlerDiscoveryResult[10];

        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            Func<HandlerDiscoveryResult> discoveryAction = () => new HandlerDiscoveryResult();
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                results[index] = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);
            });
        }

        // Act
        System.Threading.Tasks.Task.WaitAll(tasks);

        // Assert
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.Empty(result.Handlers);
            Assert.Empty(result.NotificationHandlers);
            Assert.Empty(result.PipelineBehaviors);
            Assert.Empty(result.StreamHandlers);
        }
        Assert.Empty(_diagnosticReporter.Diagnostics);
    }

    #endregion

    #region Non-Recoverable Exception Tests (Else Block Coverage)

    [Fact]
    public void ExecuteDiscoveryWithIsolation_NonRecoverableException_ReportsCriticalErrorAndReturnsEmptyResult()
    {
        // Arrange
        var testException = new OutOfMemoryException("Out of memory during discovery");
        Func<HandlerDiscoveryResult> discoveryAction = () => throw testException;

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Handlers);
        Assert.Empty(result.NotificationHandlers);
        Assert.Empty(result.PipelineBehaviors);
        Assert.Empty(result.StreamHandlers);
        
        // Should have one critical error diagnostic
        Assert.Single(_diagnosticReporter.Diagnostics);
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Contains("Critical error in operation 'Handler discovery'", diagnostic.GetMessage());
        Assert.Contains("OutOfMemoryException", diagnostic.GetMessage());
        Assert.Contains("Out of memory during discovery", diagnostic.GetMessage());
    }

    [Fact]
    public void ExecuteDiscoveryWithIsolation_StackOverflowException_ReportsCriticalErrorAndReturnsEmptyResult()
    {
        // Arrange
        var testException = new StackOverflowException("Stack overflow during discovery");
        Func<HandlerDiscoveryResult> discoveryAction = () => throw testException;

        // Act
        var result = ErrorIsolation.ExecuteDiscoveryWithIsolation(discoveryAction, _diagnosticReporter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Handlers);
        Assert.Empty(result.NotificationHandlers);
        Assert.Empty(result.PipelineBehaviors);
        Assert.Empty(result.StreamHandlers);
        
        // Should have one critical error diagnostic
        Assert.Single(_diagnosticReporter.Diagnostics);
        var diagnostic = _diagnosticReporter.Diagnostics[0];
        Assert.Contains("Critical error in operation 'Handler discovery'", diagnostic.GetMessage());
        Assert.Contains("StackOverflowException", diagnostic.GetMessage());
        Assert.Contains("Stack overflow during discovery", diagnostic.GetMessage());
    }

    #endregion
}

/// <summary>
/// Custom test exception for testing exception handling.
/// </summary>
public class CustomTestException : Exception
{
    public CustomTestException(string message) : base(message) { }
    public CustomTestException(string message, Exception innerException) : base(message, innerException) { }
}
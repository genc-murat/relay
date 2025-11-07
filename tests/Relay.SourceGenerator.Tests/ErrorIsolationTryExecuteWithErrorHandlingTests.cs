using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Core;
using Relay.SourceGenerator.Diagnostics;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Comprehensive tests for ErrorIsolation.TryExecuteWithErrorHandling method.
    /// Covers all branches, cases, and throws.
    /// </summary>
    public class ErrorIsolationTryExecuteWithErrorHandlingTests
    {
        #region Parameter Validation Tests (ArgumentNullException branches)

        [Fact]
        public void TryExecuteWithErrorHandling_NullOperation_ThrowsArgumentNullException()
        {
            // Arrange
            var reporter = new TestDiagnosticReporter();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                ErrorIsolation.TryExecuteWithErrorHandling(null, "TestOperation", reporter));
            
            Assert.Equal("operation", exception.ParamName);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_NullDiagnosticReporter_ThrowsArgumentNullException()
        {
            // Arrange
            var operationExecuted = false;
            Action operation = () => operationExecuted = true;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                ErrorIsolation.TryExecuteWithErrorHandling(operation, "TestOperation", null));
            
            Assert.Equal("diagnosticReporter", exception.ParamName);
            
            // Verify operation was not executed
            Assert.False(operationExecuted);
        }

        #endregion

        #region Successful Execution Tests

        [Fact]
        public void TryExecuteWithErrorHandling_SuccessfulOperation_ReturnsTrue()
        {
            // Arrange
            var operationExecuted = false;
            Action operation = () => operationExecuted = true;
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "TestOperation", reporter);

            // Assert
            Assert.True(result);
            Assert.True(operationExecuted);
            Assert.Empty(reporter.Diagnostics);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_OperationWithComplexLogic_ReturnsTrue()
        {
            // Arrange
            var counter = 0;
            Action operation = () => 
            {
                counter++;
                // Simulate some complex operation
                for (int i = 0; i < 10; i++)
                {
                    counter += i;
                }
            };
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "ComplexOperation", reporter);

            // Assert
            Assert.True(result);
            Assert.Equal(46, counter); // 1 + sum(0..9) = 1 + 45 = 46
            Assert.Empty(reporter.Diagnostics);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_OperationDoesNothing_ReturnsTrue()
        {
            // Arrange
            Action operation = () => { /* Do nothing */ };
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "NoOpOperation", reporter);

            // Assert
            Assert.True(result);
            Assert.Empty(reporter.Diagnostics);
        }

        #endregion

        #region OperationCanceledException Tests

        [Fact]
        public void TryExecuteWithErrorHandling_OperationCanceledException_ReturnsFalse()
        {
            // Arrange
            Action operation = () => throw new OperationCanceledException();
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "CancelledOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Empty(reporter.Diagnostics); // Should not report cancellation as error
        }

        [Fact]
        public void TryExecuteWithErrorHandling_OperationCanceledExceptionWithMessage_ReturnsFalse()
        {
            // Arrange
            Action operation = () => throw new OperationCanceledException("Operation was cancelled");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "CancelledOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Empty(reporter.Diagnostics); // Should not report cancellation as error
        }

        [Fact]
        public void TryExecuteWithErrorHandling_OperationCanceledExceptionWithToken_ReturnsFalse()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            Action operation = () => throw new OperationCanceledException(cts.Token);
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "CancelledOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Empty(reporter.Diagnostics); // Should not report cancellation as error
        }

        [Fact]
        public void TryExecuteWithErrorHandling_OperationCanceledExceptionWithInnerException_ReturnsFalse()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner operation failed");
            Action operation = () => throw new OperationCanceledException("Cancelled", innerException);
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "CancelledOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Empty(reporter.Diagnostics); // Should not report cancellation as error
        }

        #endregion

        #region Critical Exception Tests - OutOfMemoryException

        [Fact]
        public void TryExecuteWithErrorHandling_OutOfMemoryException_ReturnsFalseAndReportsCriticalError()
        {
            // Arrange
            Action operation = () => throw new OutOfMemoryException("Out of memory");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "MemoryOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Critical error in operation 'MemoryOperation'", message);
            Assert.Contains("OutOfMemoryException", message);
            Assert.Contains("Out of memory", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_OutOfMemoryExceptionWithEmptyMessage_ReturnsFalseAndReportsCriticalError()
        {
            // Arrange
            Action operation = () => throw new OutOfMemoryException("");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "MemoryOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            Assert.Contains("Critical error in operation 'MemoryOperation'", reporter.Diagnostics[0].GetMessage());
            Assert.Contains("OutOfMemoryException", reporter.Diagnostics[0].GetMessage());
        }

        [Fact]
        public void TryExecuteWithErrorHandling_OutOfMemoryExceptionWithNullMessage_ReturnsFalseAndReportsCriticalError()
        {
            // Arrange
            Action operation = () => throw new OutOfMemoryException(null);
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "MemoryOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Critical error in operation 'MemoryOperation'", message);
            Assert.Contains("OutOfMemoryException", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_OutOfMemoryExceptionWithInnerException_ReturnsFalseAndReportsCriticalError()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner memory error");
            Action operation = () => throw new OutOfMemoryException("Memory error", innerException);
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "MemoryOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Critical error in operation 'MemoryOperation'", message);
            Assert.Contains("OutOfMemoryException", message);
            Assert.Contains("Memory error", message);
        }

        #endregion

        #region Critical Exception Tests - StackOverflowException

        [Fact]
        public void TryExecuteWithErrorHandling_StackOverflowException_ReturnsFalseAndReportsCriticalError()
        {
            // Arrange
            Action operation = () => throw new StackOverflowException("Stack overflow");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "RecursiveOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Critical error in operation 'RecursiveOperation'", message);
            Assert.Contains("StackOverflowException", message);
            Assert.Contains("Stack overflow", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_StackOverflowExceptionWithEmptyMessage_ReturnsFalseAndReportsCriticalError()
        {
            // Arrange
            Action operation = () => throw new StackOverflowException("");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "RecursiveOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Critical error in operation 'RecursiveOperation'", message);
            Assert.Contains("StackOverflowException", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_StackOverflowExceptionWithNullMessage_ReturnsFalseAndReportsCriticalError()
        {
            // Arrange
            Action operation = () => throw new StackOverflowException(null);
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "RecursiveOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Critical error in operation 'RecursiveOperation'", message);
            Assert.Contains("StackOverflowException", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_StackOverflowExceptionWithInnerException_ReturnsFalseAndReportsCriticalError()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner recursion error");
            Action operation = () => throw new StackOverflowException("Recursion error", innerException);
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "RecursiveOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Critical error in operation 'RecursiveOperation'", message);
            Assert.Contains("StackOverflowException", message);
            Assert.Contains("Recursion error", message);
        }

        #endregion

        #region Regular Exception Tests

        [Fact]
        public void TryExecuteWithErrorHandling_InvalidOperationException_ReturnsFalseAndReportsError()
        {
            // Arrange
            Action operation = () => throw new InvalidOperationException("Invalid operation");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "InvalidOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Error in operation 'InvalidOperation'", message);
            Assert.Contains("Invalid operation", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_ArgumentException_ReturnsFalseAndReportsError()
        {
            // Arrange
            Action operation = () => throw new ArgumentException("Invalid argument");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "ArgumentOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Error in operation 'ArgumentOperation'", message);
            Assert.Contains("Invalid argument", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_NullReferenceException_ReturnsFalseAndReportsError()
        {
            // Arrange
            Action operation = () => throw new NullReferenceException("Null reference");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "NullOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Error in operation 'NullOperation'", message);
            Assert.Contains("Null reference", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_CustomException_ReturnsFalseAndReportsError()
        {
            // Arrange
            Action operation = () => throw new CustomOperationException("Custom error");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "CustomOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Error in operation 'CustomOperation'", message);
            Assert.Contains("Custom error", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_NestedException_ReturnsFalseAndReportsError()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner error");
            Action operation = () => throw new ApplicationException("Outer error", innerException);
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "NestedOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Error in operation 'NestedOperation'", message);
            Assert.Contains("Outer error", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_EmptyMessageException_ReturnsFalseAndReportsError()
        {
            // Arrange
            Action operation = () => throw new Exception("");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "EmptyMessageOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Error in operation 'EmptyMessageOperation'", message);
        }

        [Fact]
        public void TryExecuteWithErrorHandling_NullMessageException_ReturnsFalseAndReportsError()
        {
            // Arrange
            Action operation = () => throw new Exception(null);
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "NullMessageOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Error in operation 'NullMessageOperation'", message);
        }

        #endregion

        #region Different Exception Types Test

        [Fact]
        public void TryExecuteWithErrorHandling_VariousExceptionTypes_AllHandledCorrectly()
        {
            // Test different exception types to ensure all are caught and handled
            var exceptionTypes = new Exception[]
            {
                new InvalidOperationException(),
                new ArgumentException(),
                new ArgumentNullException(),
                new ArgumentOutOfRangeException(),
                new NotSupportedException(),
                new NotImplementedException(),
                new TimeoutException(),
                new ApplicationException(),
                new FormatException(),
                new OverflowException(),
                new DivideByZeroException(),
                new IndexOutOfRangeException(),
                new KeyNotFoundException(),
                new ObjectDisposedException("TestObject"),
                new UnauthorizedAccessException(),
                new System.IO.FileNotFoundException(),
                new System.Net.WebException(),
                new System.Text.DecoderFallbackException()
            };

            foreach (var exception in exceptionTypes)
            {
                // Arrange
                Action operation = () => throw exception;
                var reporter = new TestDiagnosticReporter();

                // Act
                var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "TestOperation", reporter);

                // Assert
                Assert.False(result, $"Failed for exception type: {exception.GetType().Name}");
                Assert.Equal(1, reporter.Diagnostics.Count);
                Assert.Contains("An error occurred during source generation: Error in operation 'TestOperation'", reporter.Diagnostics[0].GetMessage());
                
                // Reset for next iteration
                reporter.Diagnostics.Clear();
            }
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void TryExecuteWithErrorHandling_OperationNameWithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            Action operation = () => throw new InvalidOperationException("Test error");
            var reporter = new TestDiagnosticReporter();
            var operationName = "Operation with special chars: \n\r\t\"'\\ and unicode: \u00E9 \u03A9";

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, operationName, reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            Assert.Contains($"An error occurred during source generation: Error in operation '{operationName}'", reporter.Diagnostics[0].GetMessage());
        }

        [Fact]
        public void TryExecuteWithErrorHandling_EmptyOperationName_HandlesCorrectly()
        {
            // Arrange
            Action operation = () => throw new InvalidOperationException("Test error");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            Assert.Contains("An error occurred during source generation: Error in operation ''", reporter.Diagnostics[0].GetMessage());
        }

        [Fact]
        public void TryExecuteWithErrorHandling_MultipleCallsWithSameOperation_EachCallIsIsolated()
        {
            // Arrange
            var callCount = 0;
            Action operation = () => 
            {
                callCount++;
                if (callCount == 1)
                    return; // Success
                else
                    throw new InvalidOperationException("Second call failed");
            };
            var reporter = new TestDiagnosticReporter();

            // Act
            var result1 = ErrorIsolation.TryExecuteWithErrorHandling(operation, "MultiCallOperation", reporter);
            var result2 = ErrorIsolation.TryExecuteWithErrorHandling(operation, "MultiCallOperation", reporter);

            // Assert
            Assert.True(result1);
            Assert.False(result2);
            Assert.Single(reporter.Diagnostics); // Only second call reports error
        }

        [Fact]
        public void TryExecuteWithErrorHandling_OperationThrowsAndRecovers_ReturnsFalse()
        {
            // Arrange
            Action operation = () => throw new InvalidOperationException("Operation failed");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "FailingOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            Assert.Contains("An error occurred during source generation: Error in operation 'FailingOperation'", reporter.Diagnostics[0].GetMessage());
        }

        [Fact]
        public void TryExecuteWithErrorHandling_OperationThatThrowsCriticalError_ReturnsFalseAndReportsCritical()
        {
            // Arrange
            Action operation = () => throw new OutOfMemoryException("Critical memory error");
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(operation, "CriticalOperation", reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("An error occurred during source generation: Critical error in operation 'CriticalOperation'", message);
            Assert.Contains("OutOfMemoryException", message);
        }

        #endregion

        #region Test Helper Classes

        private class CustomOperationException : Exception
        {
            public CustomOperationException(string message) : base(message)
            {
            }

            public CustomOperationException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        private class TestDiagnosticReporter : IDiagnosticReporter
        {
            public List<Diagnostic> Diagnostics { get; } = new();

            public void ReportDiagnostic(Diagnostic diagnostic)
            {
                Diagnostics.Add(diagnostic);
            }
        }

        #endregion
    }
}
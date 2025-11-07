using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Core;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Generators;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Comprehensive tests for ErrorIsolation.ExecuteGeneratorWithIsolation method.
    /// Covers all branches, cases, and throws.
    /// </summary>
    public class ErrorIsolationExecuteGeneratorWithIsolationTests
    {
        #region Parameter Validation Tests (ArgumentNullException branches)

        [Fact]
        public void ExecuteGeneratorWithIsolation_NullGenerator_ThrowsArgumentNullException()
        {
            // Arrange
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                ErrorIsolation.ExecuteGeneratorWithIsolation(null, result, options, reporter));
            
            Assert.Equal("generator", exception.ParamName);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_NullResult_ThrowsArgumentNullException()
        {
            // Arrange
            var generator = new TestCodeGenerator();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                ErrorIsolation.ExecuteGeneratorWithIsolation(generator, null, options, reporter));
            
            Assert.Equal("result", exception.ParamName);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_NullOptions_ThrowsArgumentNullException()
        {
            // Arrange
            var generator = new TestCodeGenerator();
            var result = new HandlerDiscoveryResult();
            var reporter = new TestDiagnosticReporter();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, null, reporter));
            
            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_NullDiagnosticReporter_ThrowsArgumentNullException()
        {
            // Arrange
            var generator = new TestCodeGenerator();
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, null));
            
            Assert.Equal("diagnosticReporter", exception.ParamName);
        }

        #endregion

        #region CanGenerate Returns False Tests

        [Fact]
        public void ExecuteGeneratorWithIsolation_CanGenerateReturnsFalse_ReturnsNull()
        {
            // Arrange
            var generator = new TestCodeGenerator { CanGenerateResult = false };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source);
            Assert.Empty(reporter.Diagnostics);
            Assert.False(generator.GenerateCalled);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_CanGenerateReturnsFalse_DoesNotCallGenerate()
        {
            // Arrange
            var generator = new TestCodeGenerator { CanGenerateResult = false };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.False(generator.GenerateCalled);
        }

        #endregion

        #region Successful Generation Tests

        [Fact]
        public void ExecuteGeneratorWithIsolation_SuccessfulGeneration_ReturnsSource()
        {
            // Arrange
            var expectedSource = "generated source code";
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                GenerateResult = expectedSource
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Equal(expectedSource, source);
            Assert.Empty(reporter.Diagnostics);
            Assert.True(generator.GenerateCalled);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_SuccessfulGeneration_CallsGenerateWithCorrectParameters()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                GenerateResult = "source"
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.True(generator.GenerateCalled);
            Assert.Same(result, generator.LastResult);
            Assert.Same(options, generator.LastOptions);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_SuccessfulGenerationWithEmptySource_ReturnsEmptyString()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                GenerateResult = ""
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Equal("", source);
            Assert.Empty(reporter.Diagnostics);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_SuccessfulGenerationWithComplexSource_ReturnsComplexSource()
        {
            // Arrange
            var complexSource = @"using System;
namespace Generated {
    public class TestClass {
        public void Method() {
            // Complex logic here
        }
    }
}";
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                GenerateResult = complexSource
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Equal(complexSource, source);
            Assert.Empty(reporter.Diagnostics);
        }

        #endregion

        #region OperationCanceledException Tests

        [Fact]
        public void ExecuteGeneratorWithIsolation_OperationCanceledException_Throws()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new OperationCanceledException()
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act & Assert
            var exception = Assert.Throws<OperationCanceledException>(() =>
                ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter));
            
            Assert.Empty(reporter.Diagnostics);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_OperationCanceledExceptionWithMessage_Throws()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new OperationCanceledException("Operation was cancelled")
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act & Assert
            var exception = Assert.Throws<OperationCanceledException>(() =>
                ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter));
            
            Assert.Equal("Operation was cancelled", exception.Message);
            Assert.Empty(reporter.Diagnostics);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_OperationCanceledExceptionWithToken_Throws()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new OperationCanceledException(cts.Token)
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act & Assert
            var exception = Assert.Throws<OperationCanceledException>(() =>
                ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter));
            
            Assert.Equal(cts.Token, exception.CancellationToken);
            Assert.Empty(reporter.Diagnostics);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_OperationCanceledExceptionWithInnerException_Throws()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner operation failed");
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new OperationCanceledException("Cancelled", innerException)
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act & Assert
            var exception = Assert.Throws<OperationCanceledException>(() =>
                ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter));
            
            Assert.Equal("Cancelled", exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Empty(reporter.Diagnostics);
        }

        #endregion

        #region General Exception Handling Tests

        [Fact]
        public void ExecuteGeneratorWithIsolation_GeneralException_ReturnsNullAndReportsError()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new InvalidOperationException("Generation failed")
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("Error in TestGenerator", message);
            Assert.Contains("Generation failed", message);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_ArgumentException_ReturnsNullAndReportsError()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new ArgumentException("Invalid argument")
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("Error in TestGenerator", message);
            Assert.Contains("Invalid argument", message);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_NullReferenceException_ReturnsNullAndReportsError()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new NullReferenceException("Null reference")
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("Error in TestGenerator", message);
            Assert.Contains("Null reference", message);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_CustomException_ReturnsNullAndReportsError()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new CustomGeneratorException("Custom generator error")
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("Error in TestGenerator", message);
            Assert.Contains("Custom generator error", message);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_NestedException_ReturnsNullAndReportsError()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner generation error");
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new ApplicationException("Outer generation error", innerException)
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("Error in TestGenerator", message);
            Assert.Contains("Outer generation error", message);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_EmptyMessageException_ReturnsNullAndReportsError()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new Exception("")
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("Error in TestGenerator", message);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_NullMessageException_ReturnsNullAndReportsError()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowException = new Exception(null)
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("Error in TestGenerator", message);
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_VariousExceptionTypes_AllHandledCorrectly()
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
                var generator = new TestCodeGenerator 
                { 
                    CanGenerateResult = true,
                    ThrowException = exception
                };
                var result = new HandlerDiscoveryResult();
                var options = new GenerationOptions();
                var reporter = new TestDiagnosticReporter();

                // Act
                var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

                // Assert
                Assert.Null(source);
                Assert.True(source == null, $"Failed for exception type: {exception.GetType().Name}");
                Assert.Equal(1, reporter.Diagnostics.Count);
                Assert.Contains("Error in TestGenerator", reporter.Diagnostics[0].GetMessage());
                
                // Reset for next iteration
                reporter.Diagnostics.Clear();
            }
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void ExecuteGeneratorWithIsolation_GeneratorNameWithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                GeneratorNameValue = "Generator with special chars: \n\r\t\"'\\ and unicode: \u00E9 \u03A9",
                CanGenerateResult = true,
                ThrowException = new InvalidOperationException("Test error")
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source);
            Assert.Single(reporter.Diagnostics);
            Assert.Contains($"Error in {generator.GeneratorNameValue}", reporter.Diagnostics[0].GetMessage());
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_MultipleCallsWithSameGenerator_EachCallIsIsolated()
        {
            // Arrange
            var callCount = 0;
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                GenerateResult = "source"
            };
            generator.GenerateAction = () => 
            {
                callCount++;
                if (callCount == 1)
                    return "source1";
                else
                    throw new InvalidOperationException("Second call failed");
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source1 = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);
            var source2 = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Equal("source1", source1);
            Assert.Null(source2);
            Assert.Single(reporter.Diagnostics); // Only second call reports error
        }

        [Fact]
        public void ExecuteGeneratorWithIsolation_CanGenerateThrowsException_ReturnsNullAndReportsError()
        {
            // Arrange
            var generator = new TestCodeGenerator 
            { 
                CanGenerateResult = true,
                ThrowInCanGenerate = new InvalidOperationException("CanGenerate failed")
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source);
            Assert.Single(reporter.Diagnostics);
            var message = reporter.Diagnostics[0].GetMessage();
            Assert.Contains("Error in TestGenerator", message);
            Assert.Contains("CanGenerate failed", message);
        }

        #endregion

        #region Test Helper Classes

        private class TestCodeGenerator : ICodeGenerator
        {
            public string GeneratorNameValue { get; set; } = "TestGenerator";
            public string OutputFileNameValue { get; set; } = "TestOutput";
            public int PriorityValue { get; set; } = 100;
            
            public bool CanGenerateResult { get; set; } = true;
            public string GenerateResult { get; set; } = "generated source";
            public Exception? ThrowException { get; set; }
            public Exception? ThrowInCanGenerate { get; set; }
            
            public bool GenerateCalled { get; private set; }
            public bool CanGenerateCalled { get; private set; }
            public HandlerDiscoveryResult? LastResult { get; private set; }
            public GenerationOptions? LastOptions { get; private set; }
            
            public Func<string>? GenerateAction { get; set; }

            public string GeneratorName => GeneratorNameValue;
            public string OutputFileName => OutputFileNameValue;
            public int Priority => PriorityValue;

            public bool CanGenerate(HandlerDiscoveryResult result)
            {
                CanGenerateCalled = true;
                if (ThrowInCanGenerate != null)
                    throw ThrowInCanGenerate;
                return CanGenerateResult;
            }

            public string Generate(HandlerDiscoveryResult result, GenerationOptions options)
            {
                GenerateCalled = true;
                LastResult = result;
                LastOptions = options;
                
                if (ThrowException != null)
                    throw ThrowException;
                
                return GenerateAction?.Invoke() ?? GenerateResult;
            }
        }

        private class CustomGeneratorException : Exception
        {
            public CustomGeneratorException(string message) : base(message)
            {
            }

            public CustomGeneratorException(string message, Exception innerException) : base(message, innerException)
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
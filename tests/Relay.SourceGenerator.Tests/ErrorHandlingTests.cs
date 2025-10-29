using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Core;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Discovery;
using Relay.SourceGenerator.Generators;
using Relay.SourceGenerator.Validation;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Tests for error handling and resilience features.
    /// Covers exception handling, cancellation, and fallback mechanisms.
    /// </summary>
    public class ErrorHandlingTests
    {
        #region Exception Handling Tests

        [Fact]
        public void ErrorIsolation_ExecuteGeneratorWithIsolation_CatchesException()
        {
            // Arrange
            var generator = new ThrowingGenerator();
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var source = ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter);

            // Assert
            Assert.Null(source); // Should return null on error
            Assert.Single(reporter.Diagnostics); // Should report error
        }

        [Fact]
        public void ErrorIsolation_ExecuteGeneratorWithIsolation_PropagatesCancellation()
        {
            // Arrange
            var generator = new CancellingGenerator();
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(() =>
                ErrorIsolation.ExecuteGeneratorWithIsolation(generator, result, options, reporter));
        }

        [Fact]
        public void ErrorIsolation_ExecuteMultipleGenerators_IsolatesErrors()
        {
            // Arrange
            var generators = new List<ICodeGenerator>
            {
                new ThrowingGenerator(),
                new SuccessfulGenerator(),
                new ThrowingGenerator()
            };
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var reporter = new TestDiagnosticReporter();

            // Act
            var sources = ErrorIsolation.ExecuteGeneratorsWithIsolation(generators, result, options, reporter);

            // Assert
            Assert.Single(sources); // Only successful generator should produce output
            Assert.Equal(2, reporter.Diagnostics.Count); // Two errors should be reported
        }

        [Fact]
        public void ErrorIsolation_TryExecuteWithErrorHandling_HandlesException()
        {
            // Arrange
            var reporter = new TestDiagnosticReporter();
            var executed = false;

            // Act
            var result = ErrorIsolation.TryExecuteWithErrorHandling(
                () => throw new InvalidOperationException("Test error"),
                "TestOperation",
                reporter);

            // Assert
            Assert.False(result);
            Assert.Single(reporter.Diagnostics);
        }

        [Fact]
        public void ErrorIsolation_IsRecoverableException_IdentifiesRecoverableErrors()
        {
            // Arrange & Act & Assert
            Assert.True(ErrorIsolation.IsRecoverableException(new InvalidOperationException()));
            Assert.True(ErrorIsolation.IsRecoverableException(new ArgumentException()));
            Assert.False(ErrorIsolation.IsRecoverableException(new OperationCanceledException()));
            Assert.False(ErrorIsolation.IsRecoverableException(new OutOfMemoryException()));
        }

        [Fact]
        public void SafeExecutionContext_Execute_CatchesAndRecordsErrors()
        {
            // Arrange
            var reporter = new TestDiagnosticReporter();
            var context = ErrorIsolation.CreateSafeContext(reporter);

            // Act
            var result1 = context.Execute(() => throw new InvalidOperationException("Error 1"), "Op1");
            var result2 = context.Execute(() => { }, "Op2"); // Success
            var result3 = context.Execute(() => throw new ArgumentException("Error 2"), "Op3");

            // Assert
            Assert.False(result1);
            Assert.True(result2);
            Assert.False(result3);
            Assert.Equal(2, context.Errors.Count);
            Assert.True(context.HasErrors);
        }

        #endregion

        #region Cancellation Tests

        [Fact]
        public void CancellationHelper_ThrowIfCancellationRequested_ThrowsWhenCancelled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(() =>
                CancellationHelper.ThrowIfCancellationRequested(cts.Token));
        }

        [Fact]
        public void CancellationHelper_ExecuteWithCancellation_ReturnsFalseWhenCancelled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = CancellationHelper.ExecuteWithCancellation(
                () => { throw new OperationCanceledException(); },
                cts.Token,
                diagnosticReporter: reporter);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancellationHelper_CheckCancellationPeriodically_DetectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var result = CancellationHelper.CheckCancellationPeriodically(cts.Token, 100);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void CancellationHelper_CheckCancellationPeriodically_ContinuesWhenNotCancelled()
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act
            var result = CancellationHelper.CheckCancellationPeriodically(cts.Token, 100);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void CancellationHelper_IsCancelled_DetectsCancelledState()
        {
            // Arrange
            var cts = new CancellationTokenSource();

            // Act & Assert
            Assert.False(CancellationHelper.IsCancelled(cts.Token));
            cts.Cancel();
            Assert.True(CancellationHelper.IsCancelled(cts.Token));
        }

        #endregion

        #region Fallback Mechanism Tests

        [Fact]
        public void FallbackGenerator_GenerateBasicAddRelay_ProducesValidCode()
        {
            // Arrange
            var options = new GenerationOptions
            {
                EnableNullableContext = true,
                IncludeDocumentation = true
            };

            // Act
            var source = FallbackGenerator.GenerateBasicAddRelay(options);

            // Assert
            Assert.NotNull(source);
            Assert.Contains("AddRelay", source);
            Assert.Contains("#nullable enable", source);
            Assert.Contains("/// <summary>", source);
        }

        [Fact]
        public void FallbackGenerator_GenerateFallbackDispatcher_ProducesValidCode()
        {
            // Arrange
            var options = new GenerationOptions
            {
                EnableNullableContext = true,
                IncludeDocumentation = true
            };

            // Act
            var source = FallbackGenerator.GenerateFallbackDispatcher(options, "Test error");

            // Assert
            Assert.NotNull(source);
            Assert.Contains("GeneratedRequestDispatcher", source);
            Assert.Contains("Test error", source);
            Assert.Contains("FallbackRequestDispatcher", source);
        }

        [Fact]
        public void FallbackGenerator_GenerateMinimalHandlerRegistry_ProducesValidCode()
        {
            // Arrange
            var options = new GenerationOptions
            {
                EnableNullableContext = true,
                IncludeDocumentation = true
            };

            // Act
            var source = FallbackGenerator.GenerateMinimalHandlerRegistry(options);

            // Assert
            Assert.NotNull(source);
            Assert.Contains("HandlerRegistry", source);
            Assert.Contains("RequestHandlers", source);
            Assert.Contains("NotificationHandlers", source);
        }

        [Fact]
        public void FallbackGenerator_ShouldUseFallback_IdentifiesRecoverableErrors()
        {
            // Arrange & Act & Assert
            Assert.True(FallbackGenerator.ShouldUseFallback(new InvalidOperationException()));
            Assert.True(FallbackGenerator.ShouldUseFallback(new ArgumentException()));
            Assert.False(FallbackGenerator.ShouldUseFallback(new OperationCanceledException()));
            Assert.False(FallbackGenerator.ShouldUseFallback(new OutOfMemoryException()));
        }

        [Fact]
        public void FallbackGenerator_GenerateCompleteFallback_ProducesAllFiles()
        {
            // Arrange
            var options = new GenerationOptions
            {
                EnableOptimizedDispatcher = true
            };
            var reporter = new TestDiagnosticReporter();

            // Act
            var sources = FallbackGenerator.GenerateCompleteFallback(options, reporter);

            // Assert
            Assert.Equal(3, sources.Count);
            Assert.Contains("RelayRegistration.g.cs", sources.Keys);
            Assert.Contains("HandlerRegistry.g.cs", sources.Keys);
            Assert.Contains("OptimizedRequestDispatcher.g.cs", sources.Keys);
        }

        #endregion

        #region Input Validation Tests

        [Fact]
        public void InputValidator_ValidateHandlerClass_RejectsNullHandlerClass()
        {
            // Arrange
            var reporter = new TestDiagnosticReporter();

            // Act
            var result = InputValidator.ValidateHandlerClass(null, reporter);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void InputValidator_ValidateGenerationOptions_ClampsInvalidValues()
        {
            // Arrange
            var options = new GenerationOptions
            {
                MaxDegreeOfParallelism = 100 // Too high
            };

            // Act
            var result = InputValidator.ValidateGenerationOptions(options);

            // Assert
            Assert.True(result);
            Assert.True(options.MaxDegreeOfParallelism <= 64);
        }

        [Fact]
        public void InputValidator_IsNullOrEmpty_DetectsEmptyCollections()
        {
            // Arrange & Act & Assert
            Assert.True(InputValidator.IsNullOrEmpty<string>(null));
            Assert.True(InputValidator.IsNullOrEmpty(new List<string>()));
            Assert.False(InputValidator.IsNullOrEmpty(new List<string> { "item" }));
        }

        [Fact]
        public void InputValidator_SafeCount_HandlesNullCollections()
        {
            // Arrange & Act & Assert
            Assert.Equal(0, InputValidator.SafeCount<string>(null));
            Assert.Equal(0, InputValidator.SafeCount(new List<string>()));
            Assert.Equal(2, InputValidator.SafeCount(new List<string> { "a", "b" }));
        }

        [Fact]
        public void InputValidator_ValidateString_RejectsNullOrWhitespace()
        {
            // Arrange & Act & Assert
            Assert.False(InputValidator.ValidateString(null, "test"));
            Assert.False(InputValidator.ValidateString("", "test"));
            Assert.False(InputValidator.ValidateString("   ", "test"));
            Assert.True(InputValidator.ValidateString("valid", "test"));
        }

        #endregion

        #region Test Helpers

        private class ThrowingGenerator : ICodeGenerator
        {
            public string GeneratorName => "ThrowingGenerator";
            public string OutputFileName => "Throwing";
            public int Priority => 10;

            public bool CanGenerate(HandlerDiscoveryResult result) => true;

            public string Generate(HandlerDiscoveryResult result, GenerationOptions options)
            {
                throw new InvalidOperationException("Test exception");
            }
        }

        private class CancellingGenerator : ICodeGenerator
        {
            public string GeneratorName => "CancellingGenerator";
            public string OutputFileName => "Cancelling";
            public int Priority => 10;

            public bool CanGenerate(HandlerDiscoveryResult result) => true;

            public string Generate(HandlerDiscoveryResult result, GenerationOptions options)
            {
                throw new OperationCanceledException();
            }
        }

        private class SuccessfulGenerator : ICodeGenerator
        {
            public string GeneratorName => "SuccessfulGenerator";
            public string OutputFileName => "Successful";
            public int Priority => 10;

            public bool CanGenerate(HandlerDiscoveryResult result) => true;

            public string Generate(HandlerDiscoveryResult result, GenerationOptions options)
            {
                return "// Generated code";
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

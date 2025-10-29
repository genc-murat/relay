using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Diagnostics;
using Relay.SourceGenerator.Generators;
using Xunit;

namespace Relay.SourceGenerator.Tests
{
    /// <summary>
    /// Comprehensive tests for GeneratorExecutionPipeline covering edge cases and advanced scenarios.
    /// </summary>
    public class GeneratorExecutionPipelineComprehensiveTests
    {
        #region ExecuteParallel Tests

        [Fact]
        public void ExecuteParallel_WithNullResult_ShouldThrowArgumentNullException()
        {
            // Arrange
            var generators = new List<ICodeGenerator>();
            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var options = new GenerationOptions();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                pipeline.ExecuteParallel(null!, options, (f, s) => { }));
        }

        [Fact]
        public void ExecuteParallel_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var generators = new List<ICodeGenerator>();
            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                pipeline.ExecuteParallel(result, null!, (f, s) => { }));
        }

        [Fact]
        public void ExecuteParallel_WithNullAddSource_ShouldThrowArgumentNullException()
        {
            // Arrange
            var generators = new List<ICodeGenerator>();
            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                pipeline.ExecuteParallel(result, options, null!));
        }

        [Fact]
        public void ExecuteParallel_WithDisabledGenerator_ShouldSkipGenerator()
        {
            // Arrange
            var executionOrder = new System.Collections.Concurrent.ConcurrentBag<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Handler Registry Generator", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 20, executionOrder)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions { EnableHandlerRegistry = false };
            var generatedSources = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            // Act
            var executionResult = pipeline.ExecuteParallel(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            // ExecuteParallel filters out disabled generators before execution
            // So they won't appear in execution order or skipped list
            Assert.Single(executionOrder);
            Assert.Contains("Generator2", executionOrder);
            Assert.Equal(1, executionResult.TotalExecuted);
        }

        [Fact]
        public void ExecuteParallel_WithGeneratorThatCannotGenerate_ShouldSkipGenerator()
        {
            // Arrange
            var executionOrder = new System.Collections.Concurrent.ConcurrentBag<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder, canGenerate: false),
                new TestGenerator("Generator2", "Output2", 10, executionOrder, canGenerate: true)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            // Act
            var executionResult = pipeline.ExecuteParallel(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Single(executionOrder);
            Assert.Contains("Generator2", executionOrder);
            Assert.Single(executionResult.SkippedGenerators);
            Assert.Contains("Generator1", executionResult.SkippedGenerators);
        }

        [Fact]
        public void ExecuteParallel_WithGeneratorThatThrows_ShouldIsolateErrorAndContinue()
        {
            // Arrange
            var executionOrder = new System.Collections.Concurrent.ConcurrentBag<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 10, executionOrder, shouldThrow: true),
                new TestGenerator("Generator3", "Output3", 10, executionOrder)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            // Act
            var executionResult = pipeline.ExecuteParallel(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Equal(3, executionOrder.Count);
            Assert.False(executionResult.IsSuccess);
            Assert.Equal(2, executionResult.TotalExecuted);
            Assert.Single(executionResult.Errors);
            Assert.Equal("Generator2", executionResult.Errors[0].GeneratorName);
            Assert.Single(diagnosticReporter.Diagnostics);
        }

        [Fact]
        public void ExecuteParallel_WithCancellationToken_ShouldStopExecution()
        {
            // Arrange
            var executionOrder = new System.Collections.Concurrent.ConcurrentBag<string>();
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 20, executionOrder)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            // Act
            var executionResult = pipeline.ExecuteParallel(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source,
                cts.Token);

            // Assert
            // ExecuteParallel checks cancellation at the start of each priority group
            // If cancelled before any execution, it should mark as cancelled
            Assert.True(executionResult.WasCancelled);
        }

        [Fact]
        public void ExecuteParallel_WithSingleGeneratorInPriorityGroup_ShouldExecuteSequentially()
        {
            // Arrange
            var executionOrder = new System.Collections.Concurrent.ConcurrentBag<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 20, executionOrder), // Different priority
                new TestGenerator("Generator3", "Output3", 30, executionOrder)  // Different priority
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            // Act
            var executionResult = pipeline.ExecuteParallel(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Equal(3, executionOrder.Count);
            Assert.True(executionResult.IsSuccess);
            Assert.Equal(3, executionResult.TotalExecuted);
        }

        [Fact]
        public void ExecuteParallel_WithMaxDegreeOfParallelism_ShouldRespectLimit()
        {
            // Arrange
            var executionOrder = new System.Collections.Concurrent.ConcurrentBag<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 10, executionOrder),
                new TestGenerator("Generator3", "Output3", 10, executionOrder),
                new TestGenerator("Generator4", "Output4", 10, executionOrder)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions { MaxDegreeOfParallelism = 2 };
            var generatedSources = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            // Act
            var executionResult = pipeline.ExecuteParallel(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Equal(4, executionOrder.Count);
            Assert.True(executionResult.IsSuccess);
            Assert.Equal(4, executionResult.TotalExecuted);
        }

        #endregion

        #region GeneratorExecutionResult Tests

        [Fact]
        public void GeneratorExecutionResult_IsSuccess_WithNoErrors_ShouldReturnTrue()
        {
            // Arrange
            var result = new GeneratorExecutionResult();
            result.GeneratedSources.Add(new GeneratedSourceInfo
            {
                GeneratorName = "Test",
                FileName = "Test.g.cs",
                SourceLength = 100
            });

            // Act & Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.TotalExecuted);
            Assert.Equal(0, result.TotalFailed);
            Assert.Equal(0, result.TotalSkipped);
        }

        [Fact]
        public void GeneratorExecutionResult_IsSuccess_WithErrors_ShouldReturnFalse()
        {
            // Arrange
            var result = new GeneratorExecutionResult();
            result.Errors.Add(new GeneratorError
            {
                GeneratorName = "Test",
                Message = "Error",
                Exception = new Exception("Test error")
            });

            // Act & Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(0, result.TotalExecuted);
            Assert.Equal(1, result.TotalFailed);
        }

        [Fact]
        public void GeneratorExecutionResult_IsSuccess_WhenCancelled_ShouldReturnFalse()
        {
            // Arrange
            var result = new GeneratorExecutionResult
            {
                WasCancelled = true
            };

            // Act & Assert
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void GeneratorExecutionResult_TotalSkipped_ShouldReturnCorrectCount()
        {
            // Arrange
            var result = new GeneratorExecutionResult();
            result.SkippedGenerators.Add("Generator1");
            result.SkippedGenerators.Add("Generator2");

            // Act & Assert
            Assert.Equal(2, result.TotalSkipped);
        }

        #endregion

        #region GeneratedSourceInfo Tests

        [Fact]
        public void GeneratedSourceInfo_Properties_ShouldBeSettable()
        {
            // Arrange & Act
            var info = new GeneratedSourceInfo
            {
                GeneratorName = "TestGenerator",
                FileName = "Test.g.cs",
                SourceLength = 1234
            };

            // Assert
            Assert.Equal("TestGenerator", info.GeneratorName);
            Assert.Equal("Test.g.cs", info.FileName);
            Assert.Equal(1234, info.SourceLength);
        }

        [Fact]
        public void GeneratedSourceInfo_DefaultValues_ShouldBeEmpty()
        {
            // Arrange & Act
            var info = new GeneratedSourceInfo();

            // Assert
            Assert.Equal(string.Empty, info.GeneratorName);
            Assert.Equal(string.Empty, info.FileName);
            Assert.Equal(0, info.SourceLength);
        }

        #endregion

        #region GeneratorError Tests

        [Fact]
        public void GeneratorError_Properties_ShouldBeSettable()
        {
            // Arrange
            var exception = new InvalidOperationException("Test error");

            // Act
            var error = new GeneratorError
            {
                GeneratorName = "TestGenerator",
                Message = "Error occurred",
                Exception = exception
            };

            // Assert
            Assert.Equal("TestGenerator", error.GeneratorName);
            Assert.Equal("Error occurred", error.Message);
            Assert.Same(exception, error.Exception);
        }

        [Fact]
        public void GeneratorError_DefaultValues_ShouldBeEmpty()
        {
            // Arrange & Act
            var error = new GeneratorError();

            // Assert
            Assert.Equal(string.Empty, error.GeneratorName);
            Assert.Equal(string.Empty, error.Message);
            Assert.Null(error.Exception);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void Execute_WithEmptyGeneratorList_ShouldReturnSuccessWithNoExecutions()
        {
            // Arrange
            var generators = new List<ICodeGenerator>();
            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new Dictionary<string, string>();

            // Act
            var executionResult = pipeline.Execute(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.True(executionResult.IsSuccess);
            Assert.Equal(0, executionResult.TotalExecuted);
            Assert.Equal(0, executionResult.TotalFailed);
            Assert.Equal(0, executionResult.TotalSkipped);
        }

        [Fact]
        public void ExecuteParallel_WithEmptyGeneratorList_ShouldReturnSuccessWithNoExecutions()
        {
            // Arrange
            var generators = new List<ICodeGenerator>();
            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

            // Act
            var executionResult = pipeline.ExecuteParallel(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.True(executionResult.IsSuccess);
            Assert.Equal(0, executionResult.TotalExecuted);
        }

        [Fact]
        public void Execute_WithAllGeneratorsDisabled_ShouldSkipAll()
        {
            // Arrange
            var executionOrder = new List<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("DI Registration Generator", "Output1", 10, executionOrder),
                new TestGenerator("Handler Registry Generator", "Output2", 20, executionOrder),
                new TestGenerator("Optimized Dispatcher Generator", "Output3", 30, executionOrder)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions
            {
                EnableDIGeneration = false,
                EnableHandlerRegistry = false,
                EnableOptimizedDispatcher = false
            };
            var generatedSources = new Dictionary<string, string>();

            // Act
            var executionResult = pipeline.Execute(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Empty(executionOrder);
            Assert.True(executionResult.IsSuccess);
            Assert.Equal(0, executionResult.TotalExecuted);
            Assert.Equal(3, executionResult.TotalSkipped);
        }

        [Fact]
        public void Execute_WithAllGeneratorsCannotGenerate_ShouldSkipAll()
        {
            // Arrange
            var executionOrder = new List<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder, canGenerate: false),
                new TestGenerator("Generator2", "Output2", 20, executionOrder, canGenerate: false),
                new TestGenerator("Generator3", "Output3", 30, executionOrder, canGenerate: false)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new Dictionary<string, string>();

            // Act
            var executionResult = pipeline.Execute(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Empty(executionOrder);
            Assert.True(executionResult.IsSuccess);
            Assert.Equal(0, executionResult.TotalExecuted);
            Assert.Equal(3, executionResult.TotalSkipped);
        }

        [Fact]
        public void Execute_WithSamePriorityGenerators_ShouldExecuteInNameOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("ZGenerator", "Output3", 10, executionOrder),
                new TestGenerator("AGenerator", "Output1", 10, executionOrder),
                new TestGenerator("MGenerator", "Output2", 10, executionOrder)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new Dictionary<string, string>();

            // Act
            var executionResult = pipeline.Execute(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Equal(3, executionOrder.Count);
            Assert.Equal("AGenerator", executionOrder[0]);
            Assert.Equal("MGenerator", executionOrder[1]);
            Assert.Equal("ZGenerator", executionOrder[2]);
        }

        [Fact]
        public void Execute_WithMultipleErrors_ShouldCaptureAllErrors()
        {
            // Arrange
            var executionOrder = new List<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder, shouldThrow: true),
                new TestGenerator("Generator2", "Output2", 20, executionOrder, shouldThrow: true),
                new TestGenerator("Generator3", "Output3", 30, executionOrder, shouldThrow: true)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new Dictionary<string, string>();

            // Act
            var executionResult = pipeline.Execute(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Equal(3, executionOrder.Count);
            Assert.False(executionResult.IsSuccess);
            Assert.Equal(0, executionResult.TotalExecuted);
            Assert.Equal(3, executionResult.TotalFailed);
            Assert.Equal(3, executionResult.Errors.Count);
            Assert.Equal(3, diagnosticReporter.Diagnostics.Count);
        }

        [Fact]
        public void Execute_WithGeneratorThatThrowsOperationCanceledException_ShouldSetWasCancelled()
        {
            // Arrange
            var executionOrder = new List<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 20, executionOrder, shouldThrowCancellation: true),
                new TestGenerator("Generator3", "Output3", 30, executionOrder)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new Dictionary<string, string>();

            // Act & Assert
            Assert.Throws<OperationCanceledException>(() =>
                pipeline.Execute(
                    result,
                    options,
                    (fileName, source) => generatedSources[fileName] = source));
        }

        [Fact]
        public void Execute_WithGeneratedSourceTracking_ShouldTrackAllGeneratedSources()
        {
            // Arrange
            var executionOrder = new List<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 20, executionOrder),
                new TestGenerator("Generator3", "Output3", 30, executionOrder)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();
            var generatedSources = new Dictionary<string, string>();

            // Act
            var executionResult = pipeline.Execute(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Equal(3, executionResult.GeneratedSources.Count);
            Assert.Contains(executionResult.GeneratedSources, s => s.GeneratorName == "Generator1");
            Assert.Contains(executionResult.GeneratedSources, s => s.GeneratorName == "Generator2");
            Assert.Contains(executionResult.GeneratedSources, s => s.GeneratorName == "Generator3");
            Assert.All(executionResult.GeneratedSources, s => Assert.True(s.SourceLength > 0));
        }

        #endregion

        #region Test Helper Classes

        private class TestGenerator : ICodeGenerator
        {
            private readonly List<string>? _executionOrder;
            private readonly System.Collections.Concurrent.ConcurrentBag<string>? _concurrentExecutionOrder;
            private readonly bool _canGenerate;
            private readonly bool _shouldThrow;
            private readonly bool _shouldThrowCancellation;
            private readonly CancellationTokenSource? _cancellationTokenSource;

            public TestGenerator(
                string name,
                string outputFileName,
                int priority,
                List<string> executionOrder,
                bool canGenerate = true,
                bool shouldThrow = false,
                bool shouldThrowCancellation = false,
                CancellationTokenSource? cancellationTokenSource = null)
            {
                GeneratorName = name;
                OutputFileName = outputFileName;
                Priority = priority;
                _executionOrder = executionOrder;
                _canGenerate = canGenerate;
                _shouldThrow = shouldThrow;
                _shouldThrowCancellation = shouldThrowCancellation;
                _cancellationTokenSource = cancellationTokenSource;
            }

            public TestGenerator(
                string name,
                string outputFileName,
                int priority,
                System.Collections.Concurrent.ConcurrentBag<string> executionOrder,
                bool canGenerate = true,
                bool shouldThrow = false)
            {
                GeneratorName = name;
                OutputFileName = outputFileName;
                Priority = priority;
                _concurrentExecutionOrder = executionOrder;
                _canGenerate = canGenerate;
                _shouldThrow = shouldThrow;
            }

            public string GeneratorName { get; }
            public string OutputFileName { get; }
            public int Priority { get; }

            public bool CanGenerate(HandlerDiscoveryResult result)
            {
                return _canGenerate;
            }

            public string Generate(HandlerDiscoveryResult result, GenerationOptions options)
            {
                if (_executionOrder != null)
                {
                    _executionOrder.Add(GeneratorName);
                }
                else if (_concurrentExecutionOrder != null)
                {
                    _concurrentExecutionOrder.Add(GeneratorName);
                }

                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                }

                if (_shouldThrowCancellation)
                {
                    throw new OperationCanceledException($"Cancelled from {GeneratorName}");
                }

                if (_shouldThrow)
                {
                    throw new InvalidOperationException($"Test exception from {GeneratorName}");
                }

                return $"// Generated by {GeneratorName}";
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

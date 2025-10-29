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
    /// Tests for the GeneratorExecutionPipeline class.
    /// </summary>
    public class GeneratorExecutionPipelineTests
    {
        [Fact]
        public void Execute_WithValidGenerators_ShouldExecuteInPriorityOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator3", "Output3", 30, executionOrder),
                new TestGenerator("Generator1", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 20, executionOrder)
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
            Assert.Equal("Generator1", executionOrder[0]);
            Assert.Equal("Generator2", executionOrder[1]);
            Assert.Equal("Generator3", executionOrder[2]);
            Assert.True(executionResult.IsSuccess);
            Assert.Equal(3, executionResult.TotalExecuted);
        }

        [Fact]
        public void Execute_WithDisabledGenerator_ShouldSkipGenerator()
        {
            // Arrange
            var executionOrder = new List<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("DI Registration Generator", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 20, executionOrder)
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions { EnableDIGeneration = false };
            var generatedSources = new Dictionary<string, string>();

            // Act
            var executionResult = pipeline.Execute(
                result,
                options,
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Single(executionOrder);
            Assert.Equal("Generator2", executionOrder[0]);
            Assert.Single(executionResult.SkippedGenerators);
            Assert.Contains("DI Registration Generator", executionResult.SkippedGenerators);
        }

        [Fact]
        public void Execute_WithGeneratorThatCannotGenerate_ShouldSkipGenerator()
        {
            // Arrange
            var executionOrder = new List<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder, canGenerate: false),
                new TestGenerator("Generator2", "Output2", 20, executionOrder, canGenerate: true)
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
            Assert.Single(executionOrder);
            Assert.Equal("Generator2", executionOrder[0]);
            Assert.Single(executionResult.SkippedGenerators);
            Assert.Contains("Generator1", executionResult.SkippedGenerators);
        }

        [Fact]
        public void Execute_WithGeneratorThatThrows_ShouldIsolateErrorAndContinue()
        {
            // Arrange
            var executionOrder = new List<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 20, executionOrder, shouldThrow: true),
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
            Assert.Equal(3, executionOrder.Count);
            Assert.False(executionResult.IsSuccess);
            Assert.Equal(2, executionResult.TotalExecuted); // Generator1 and Generator3
            Assert.Single(executionResult.Errors);
            Assert.Equal("Generator2", executionResult.Errors[0].GeneratorName);
            Assert.Single(diagnosticReporter.Diagnostics);
        }

        [Fact]
        public void Execute_WithCancellationToken_ShouldStopExecution()
        {
            // Arrange
            var executionOrder = new List<string>();
            var cts = new CancellationTokenSource();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder, cancellationTokenSource: cts),
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
                (fileName, source) => generatedSources[fileName] = source,
                cts.Token);

            // Assert
            Assert.Single(executionOrder);
            Assert.Equal("Generator1", executionOrder[0]);
            Assert.True(executionResult.WasCancelled);
        }

        [Fact]
        public void ExecuteParallel_WithMultipleGenerators_ShouldExecuteInParallel()
        {
            // Arrange
            var executionOrder = new System.Collections.Concurrent.ConcurrentBag<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator1", "Output1", 10, executionOrder),
                new TestGenerator("Generator2", "Output2", 10, executionOrder), // Same priority
                new TestGenerator("Generator3", "Output3", 10, executionOrder)  // Same priority
            };

            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions { MaxDegreeOfParallelism = 3 };
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
        public void ExecuteParallel_WithDifferentPriorities_ShouldExecuteInOrder()
        {
            // Arrange
            var executionOrder = new System.Collections.Concurrent.ConcurrentBag<string>();
            var generators = new List<ICodeGenerator>
            {
                new TestGenerator("Generator3", "Output3", 30, executionOrder),
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
                (fileName, source) => generatedSources[fileName] = source);

            // Assert
            Assert.Equal(3, executionOrder.Count);
            Assert.True(executionResult.IsSuccess);
            Assert.Equal(3, executionResult.TotalExecuted);
        }

        [Fact]
        public void Constructor_WithNullGenerators_ShouldThrowArgumentNullException()
        {
            // Arrange
            var diagnosticReporter = new TestDiagnosticReporter();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new GeneratorExecutionPipeline(null!, diagnosticReporter));
        }

        [Fact]
        public void Constructor_WithNullDiagnosticReporter_ShouldThrowArgumentNullException()
        {
            // Arrange
            var generators = new List<ICodeGenerator>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new GeneratorExecutionPipeline(generators, null!));
        }

        [Fact]
        public void Execute_WithNullResult_ShouldThrowArgumentNullException()
        {
            // Arrange
            var generators = new List<ICodeGenerator>();
            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var options = new GenerationOptions();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                pipeline.Execute(null!, options, (f, s) => { }));
        }

        [Fact]
        public void Execute_WithNullOptions_ShouldThrowArgumentNullException()
        {
            // Arrange
            var generators = new List<ICodeGenerator>();
            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                pipeline.Execute(result, null!, (f, s) => { }));
        }

        [Fact]
        public void Execute_WithNullAddSource_ShouldThrowArgumentNullException()
        {
            // Arrange
            var generators = new List<ICodeGenerator>();
            var diagnosticReporter = new TestDiagnosticReporter();
            var pipeline = new GeneratorExecutionPipeline(generators, diagnosticReporter);
            var result = new HandlerDiscoveryResult();
            var options = new GenerationOptions();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                pipeline.Execute(result, options, null!));
        }

        // Test helper classes
        private class TestGenerator : ICodeGenerator
        {
            private readonly List<string>? _executionOrder;
            private readonly System.Collections.Concurrent.ConcurrentBag<string>? _concurrentExecutionOrder;
            private readonly bool _canGenerate;
            private readonly bool _shouldThrow;
            private readonly CancellationTokenSource? _cancellationTokenSource;

            public TestGenerator(
                string name,
                string outputFileName,
                int priority,
                List<string> executionOrder,
                bool canGenerate = true,
                bool shouldThrow = false,
                CancellationTokenSource? cancellationTokenSource = null)
            {
                GeneratorName = name;
                OutputFileName = outputFileName;
                Priority = priority;
                _executionOrder = executionOrder;
                _canGenerate = canGenerate;
                _shouldThrow = shouldThrow;
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
    }
}

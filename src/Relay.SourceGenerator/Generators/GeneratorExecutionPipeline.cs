using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator.Generators
{
    /// <summary>
    /// Orchestrates the execution of multiple code generators with error isolation and priority-based ordering.
    /// Implements the Chain of Responsibility pattern for generator execution.
    /// </summary>
    public class GeneratorExecutionPipeline
    {
        private readonly List<ICodeGenerator> _generators;
        private readonly IDiagnosticReporter _diagnosticReporter;

        /// <summary>
        /// Initializes a new instance of the GeneratorExecutionPipeline class.
        /// </summary>
        /// <param name="generators">The collection of generators to execute</param>
        /// <param name="diagnosticReporter">The diagnostic reporter for error reporting</param>
        public GeneratorExecutionPipeline(
            IEnumerable<ICodeGenerator> generators,
            IDiagnosticReporter diagnosticReporter)
        {
            _generators = generators?.ToList() ?? throw new ArgumentNullException(nameof(generators));
            _diagnosticReporter = diagnosticReporter ?? throw new ArgumentNullException(nameof(diagnosticReporter));
        }

        /// <summary>
        /// Executes all generators in priority order with error isolation.
        /// </summary>
        /// <param name="result">The handler discovery result</param>
        /// <param name="options">Generation options</param>
        /// <param name="addSource">Callback to add generated source to the compilation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Execution result containing generated sources and any errors</returns>
        public GeneratorExecutionResult Execute(
            HandlerDiscoveryResult result,
            GenerationOptions options,
            Action<string, string> addSource,
            CancellationToken cancellationToken = default)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (addSource == null)
                throw new ArgumentNullException(nameof(addSource));

            var executionResult = new GeneratorExecutionResult();

            // Sort generators by priority (lower numbers execute first)
            var sortedGenerators = _generators
                .OrderBy(g => g.Priority)
                .ThenBy(g => g.GeneratorName)
                .ToList();

            // Execute each generator with error isolation
            foreach (var generator in sortedGenerators)
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    executionResult.WasCancelled = true;
                    break;
                }

                // Check if generator is enabled
                if (!options.IsGeneratorEnabled(generator.GeneratorName))
                {
                    executionResult.SkippedGenerators.Add(generator.GeneratorName);
                    continue;
                }

                // Execute generator with error isolation
                ExecuteGenerator(generator, result, options, addSource, executionResult, cancellationToken);
            }

            return executionResult;
        }

        /// <summary>
        /// Executes all generators in parallel where possible, with error isolation.
        /// </summary>
        /// <param name="result">The handler discovery result</param>
        /// <param name="options">Generation options</param>
        /// <param name="addSource">Callback to add generated source to the compilation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Execution result containing generated sources and any errors</returns>
        public GeneratorExecutionResult ExecuteParallel(
            HandlerDiscoveryResult result,
            GenerationOptions options,
            Action<string, string> addSource,
            CancellationToken cancellationToken = default)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (addSource == null)
                throw new ArgumentNullException(nameof(addSource));

            var executionResult = new GeneratorExecutionResult();

            // Group generators by priority
            var generatorGroups = _generators
                .Where(g => options.IsGeneratorEnabled(g.GeneratorName))
                .GroupBy(g => g.Priority)
                .OrderBy(g => g.Key)
                .ToList();

            // Execute each priority group sequentially, but generators within a group in parallel
            foreach (var group in generatorGroups)
            {
                // Check for cancellation
                if (cancellationToken.IsCancellationRequested)
                {
                    executionResult.WasCancelled = true;
                    break;
                }

                var generators = group.ToList();

                // If only one generator in this priority group, execute sequentially
                if (generators.Count == 1)
                {
                    ExecuteGenerator(generators[0], result, options, addSource, executionResult, cancellationToken);
                }
                else
                {
                    // Execute generators in parallel within the same priority group
                    ExecuteGeneratorsInParallel(generators, result, options, addSource, executionResult, cancellationToken);
                }
            }

            return executionResult;
        }

        private void ExecuteGenerator(
            ICodeGenerator generator,
            HandlerDiscoveryResult result,
            GenerationOptions options,
            Action<string, string> addSource,
            GeneratorExecutionResult executionResult,
            CancellationToken cancellationToken)
        {
            try
            {
                // Check if generator can generate code for this result
                if (!generator.CanGenerate(result))
                {
                    executionResult.SkippedGenerators.Add(generator.GeneratorName);
                    return;
                }

                // Generate code
                var source = generator.Generate(result, options);

                // Add source to compilation
                var fileName = $"{generator.OutputFileName}.g.cs";
                addSource(fileName, source);

                // Track successful generation
                executionResult.GeneratedSources.Add(new GeneratedSourceInfo
                {
                    GeneratorName = generator.GeneratorName,
                    FileName = fileName,
                    SourceLength = source.Length
                });
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected, don't report as error
                executionResult.WasCancelled = true;
                throw;
            }
            catch (Exception ex)
            {
                // Report generator-specific error but continue with other generators
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.GeneratorError,
                    Location.None,
                    $"Error in {generator.GeneratorName}: {ex.Message}");

                _diagnosticReporter.ReportDiagnostic(diagnostic);

                executionResult.Errors.Add(new GeneratorError
                {
                    GeneratorName = generator.GeneratorName,
                    Exception = ex,
                    Message = ex.Message
                });
            }
        }

        private void ExecuteGeneratorsInParallel(
            List<ICodeGenerator> generators,
            HandlerDiscoveryResult result,
            GenerationOptions options,
            Action<string, string> addSource,
            GeneratorExecutionResult executionResult,
            CancellationToken cancellationToken)
        {
            // Use thread-safe collections for parallel execution
            var generatedSources = new System.Collections.Concurrent.ConcurrentBag<GeneratedSourceInfo>();
            var errors = new System.Collections.Concurrent.ConcurrentBag<GeneratorError>();
            var skipped = new System.Collections.Concurrent.ConcurrentBag<string>();

            // Execute generators in parallel
            Parallel.ForEach(generators,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = options.MaxDegreeOfParallelism,
                    CancellationToken = cancellationToken
                },
                generator =>
                {
                    try
                    {
                        // Check if generator can generate code for this result
                        if (!generator.CanGenerate(result))
                        {
                            skipped.Add(generator.GeneratorName);
                            return;
                        }

                        // Generate code
                        var source = generator.Generate(result, options);

                        // Add source to compilation (thread-safe)
                        var fileName = $"{generator.OutputFileName}.g.cs";
                        lock (addSource)
                        {
                            addSource(fileName, source);
                        }

                        // Track successful generation
                        generatedSources.Add(new GeneratedSourceInfo
                        {
                            GeneratorName = generator.GeneratorName,
                            FileName = fileName,
                            SourceLength = source.Length
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancellation is expected, don't report as error
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // Report generator-specific error but continue with other generators
                        var diagnostic = Diagnostic.Create(
                            DiagnosticDescriptors.GeneratorError,
                            Location.None,
                            $"Error in {generator.GeneratorName}: {ex.Message}");

                        _diagnosticReporter.ReportDiagnostic(diagnostic);

                        errors.Add(new GeneratorError
                        {
                            GeneratorName = generator.GeneratorName,
                            Exception = ex,
                            Message = ex.Message
                        });
                    }
                });

            // Aggregate results
            foreach (var source in generatedSources)
            {
                executionResult.GeneratedSources.Add(source);
            }

            foreach (var error in errors)
            {
                executionResult.Errors.Add(error);
            }

            foreach (var skip in skipped)
            {
                executionResult.SkippedGenerators.Add(skip);
            }
        }
    }

    /// <summary>
    /// Result of generator execution pipeline.
    /// </summary>
    public class GeneratorExecutionResult
    {
        /// <summary>
        /// Gets the list of successfully generated sources.
        /// </summary>
        public List<GeneratedSourceInfo> GeneratedSources { get; } = new();

        /// <summary>
        /// Gets the list of errors that occurred during generation.
        /// </summary>
        public List<GeneratorError> Errors { get; } = new();

        /// <summary>
        /// Gets the list of skipped generators (either disabled or couldn't generate).
        /// </summary>
        public List<string> SkippedGenerators { get; } = new();

        /// <summary>
        /// Gets or sets whether the execution was cancelled.
        /// </summary>
        public bool WasCancelled { get; set; }

        /// <summary>
        /// Gets whether the execution was successful (no errors).
        /// </summary>
        public bool IsSuccess => Errors.Count == 0 && !WasCancelled;

        /// <summary>
        /// Gets the total number of generators that were executed.
        /// </summary>
        public int TotalExecuted => GeneratedSources.Count;

        /// <summary>
        /// Gets the total number of generators that failed.
        /// </summary>
        public int TotalFailed => Errors.Count;

        /// <summary>
        /// Gets the total number of generators that were skipped.
        /// </summary>
        public int TotalSkipped => SkippedGenerators.Count;
    }

    /// <summary>
    /// Information about a generated source file.
    /// </summary>
    public class GeneratedSourceInfo
    {
        /// <summary>
        /// Gets or sets the name of the generator that produced this source.
        /// </summary>
        public string GeneratorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file name of the generated source.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the length of the generated source code.
        /// </summary>
        public int SourceLength { get; set; }
    }

    /// <summary>
    /// Information about a generator error.
    /// </summary>
    public class GeneratorError
    {
        /// <summary>
        /// Gets or sets the name of the generator that failed.
        /// </summary>
        public string GeneratorName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the exception that occurred.
        /// </summary>
        public Exception? Exception { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}

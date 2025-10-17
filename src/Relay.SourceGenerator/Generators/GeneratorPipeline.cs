using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator.Generators
{
    /// <summary>
    /// Orchestrates the execution of multiple code generators in a prioritized pipeline.
    /// Implements the Chain of Responsibility pattern for ordered generator execution.
    /// </summary>
    public class GeneratorPipeline
    {
        private readonly IReadOnlyList<ICodeGenerator> _generators;

        /// <summary>
        /// Creates a new GeneratorPipeline with the specified generators.
        /// </summary>
        /// <param name="generators">Collection of generators to execute</param>
        /// <exception cref="ArgumentNullException">Thrown when generators is null</exception>
        public GeneratorPipeline(IEnumerable<ICodeGenerator> generators)
        {
            if (generators == null)
                throw new ArgumentNullException(nameof(generators));

            _generators = generators.ToList();
        }

        /// <summary>
        /// Executes all applicable generators in priority order.
        /// </summary>
        /// <param name="context">The source production context for adding generated code</param>
        /// <param name="result">The handler discovery result containing handlers and metadata</param>
        /// <param name="options">Generation options to apply</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
        public void Execute(
            SourceProductionContext context,
            HandlerDiscoveryResult result,
            GenerationOptions options)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Filter generators that can handle this result, are enabled in options, and sort by priority
            var applicableGenerators = _generators
                .Where(g => g.CanGenerate(result) && options.IsGeneratorEnabled(g.GeneratorName))
                .OrderBy(g => g.Priority)
                .ToList();

            // Execute each generator in priority order
            foreach (var generator in applicableGenerators)
            {
                try
                {
                    var source = generator.Generate(result, options);

                    if (!string.IsNullOrWhiteSpace(source))
                    {
                        context.AddSource($"{generator.OutputFileName}.g.cs", source);
                    }
                }
                catch (Exception ex)
                {
                    // Report diagnostic for generator failure
                    var diagnostic = Diagnostic.Create(
                        new DiagnosticDescriptor(
                            id: "RELAY9999",
                            title: "Code Generator Error",
                            messageFormat: "Generator '{0}' failed: {1}",
                            category: "Relay.SourceGenerator",
                            defaultSeverity: DiagnosticSeverity.Error,
                            isEnabledByDefault: true),
                        Location.None,
                        generator.GeneratorName,
                        ex.Message);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        /// <summary>
        /// Gets the number of registered generators in the pipeline.
        /// </summary>
        public int GeneratorCount => _generators.Count;

        /// <summary>
        /// Gets all registered generators.
        /// </summary>
        public IReadOnlyList<ICodeGenerator> Generators => _generators;
    }
}

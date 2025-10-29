using System;

namespace Relay.SourceGenerator.Generators
{
    /// <summary>
    /// Interface for all code generators in the Relay source generator pipeline.
    /// Implements the Strategy pattern to allow flexible and extensible code generation.
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// Gets the unique name of this generator.
        /// </summary>
        string GeneratorName { get; }

        /// <summary>
        /// Gets the output file name (without extension).
        /// </summary>
        string OutputFileName { get; }

        /// <summary>
        /// Gets the priority of this generator.
        /// Lower numbers execute first. Default is 100.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Determines whether this generator can generate code for the given discovery result.
        /// </summary>
        /// <param name="result">The handler discovery result</param>
        /// <returns>True if the generator can generate code, false otherwise</returns>
        bool CanGenerate(HandlerDiscoveryResult result);

        /// <summary>
        /// Generates source code for the given discovery result.
        /// </summary>
        /// <param name="result">The handler discovery result</param>
        /// <param name="options">Generation options</param>
        /// <returns>The generated source code</returns>
        string Generate(HandlerDiscoveryResult result, GenerationOptions options);
    }

    /// <summary>
    /// Options for code generation.
    /// </summary>
    public class GenerationOptions
    {
        /// <summary>
        /// Gets or sets whether to include debug information in generated code.
        /// </summary>
        public bool IncludeDebugInfo { get; set; }

        /// <summary>
        /// Gets or sets whether to include XML documentation comments.
        /// </summary>
        public bool IncludeDocumentation { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable nullable reference types.
        /// </summary>
        public bool EnableNullableContext { get; set; } = true;

        /// <summary>
        /// Gets or sets the custom namespace for generated code.
        /// </summary>
        public string? CustomNamespace { get; set; }

        /// <summary>
        /// Gets or sets whether to use aggressive inlining.
        /// </summary>
        public bool UseAggressiveInlining { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable aggressive inlining (alias for UseAggressiveInlining).
        /// Can be controlled via MSBuild property: RelayEnableAggressiveInlining
        /// </summary>
        public bool EnableAggressiveInlining
        {
            get => UseAggressiveInlining;
            set => UseAggressiveInlining = value;
        }

        /// <summary>
        /// Gets or sets the assembly name for the generated code.
        /// </summary>
        public string AssemblyName { get; set; } = "Relay.Generated";

        /// <summary>
        /// Gets or sets whether to enable performance optimizations.
        /// Can be controlled via MSBuild property: RelayEnablePerformanceOptimizations
        /// </summary>
        public bool EnablePerformanceOptimizations { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism for handler discovery.
        /// Can be controlled via MSBuild property: RelayMaxDegreeOfParallelism
        /// </summary>
        public int MaxDegreeOfParallelism { get; set; } = 4;

        /// <summary>
        /// Gets or sets whether to enable keyed services support (.NET 8+).
        /// Can be controlled via MSBuild property: RelayEnableKeyedServices
        /// </summary>
        public bool EnableKeyedServices { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable DI Registration generator.
        /// Can be controlled via MSBuild property: RelayEnableDIGeneration
        /// </summary>
        public bool EnableDIGeneration { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable Handler Registry generator.
        /// Can be controlled via MSBuild property: RelayEnableHandlerRegistry
        /// </summary>
        public bool EnableHandlerRegistry { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable Optimized Dispatcher generator.
        /// Can be controlled via MSBuild property: RelayEnableOptimizedDispatcher
        /// </summary>
        public bool EnableOptimizedDispatcher { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable Notification Dispatcher generator.
        /// Can be controlled via MSBuild property: RelayEnableNotificationDispatcher
        /// </summary>
        public bool EnableNotificationDispatcher { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable Pipeline Registry generator.
        /// Can be controlled via MSBuild property: RelayEnablePipelineRegistry
        /// </summary>
        public bool EnablePipelineRegistry { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable Endpoint Metadata generator.
        /// Can be controlled via MSBuild property: RelayEnableEndpointMetadata
        /// </summary>
        public bool EnableEndpointMetadata { get; set; } = true;

        /// <summary>
        /// Default generation options.
        /// </summary>
        public static GenerationOptions Default => new GenerationOptions();

        /// <summary>
        /// Determines if a specific generator is enabled based on its name.
        /// </summary>
        /// <param name="generatorName">The name of the generator to check</param>
        /// <returns>True if the generator is enabled, false otherwise</returns>
        public bool IsGeneratorEnabled(string generatorName)
        {
            return generatorName switch
            {
                "DI Registration Generator" => EnableDIGeneration,
                "Handler Registry Generator" => EnableHandlerRegistry,
                "Optimized Dispatcher Generator" => EnableOptimizedDispatcher,
                "Notification Dispatcher Generator" => EnableNotificationDispatcher,
                "Pipeline Registry Generator" => EnablePipelineRegistry,
                "Endpoint Metadata Generator" => EnableEndpointMetadata,
                _ => true // Unknown generators are enabled by default
            };
        }
    }
}

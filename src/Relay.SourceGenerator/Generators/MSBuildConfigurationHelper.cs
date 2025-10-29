using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Relay.SourceGenerator.Diagnostics;

namespace Relay.SourceGenerator.Generators
{
    /// <summary>
    /// Helper class for reading MSBuild configuration properties.
    /// </summary>
    public static class MSBuildConfigurationHelper
    {
        /// <summary>
        /// Creates GenerationOptions from AnalyzerConfigOptions (MSBuild properties).
        /// </summary>
        /// <param name="options">The analyzer config options from the compilation</param>
        /// <returns>GenerationOptions populated with MSBuild properties</returns>
        public static GenerationOptions CreateFromMSBuildProperties(AnalyzerConfigOptionsProvider options)
        {
            var globalOptions = options.GlobalOptions;

            return new GenerationOptions
            {
                // General options
                IncludeDebugInfo = GetBoolProperty(globalOptions, "build_property.RelayIncludeDebugInfo", false),
                IncludeDocumentation = GetBoolProperty(globalOptions, "build_property.RelayIncludeDocumentation", true),
                EnableNullableContext = GetBoolProperty(globalOptions, "build_property.RelayEnableNullableContext", true),
                UseAggressiveInlining = GetBoolProperty(globalOptions, "build_property.RelayUseAggressiveInlining", true),
                CustomNamespace = GetStringProperty(globalOptions, "build_property.RelayCustomNamespace"),
                AssemblyName = GetStringProperty(globalOptions, "build_property.AssemblyName") ?? "Relay.Generated",

                // Generator enable/disable flags
                EnableDIGeneration = GetBoolProperty(globalOptions, "build_property.RelayEnableDIGeneration", true),
                EnableHandlerRegistry = GetBoolProperty(globalOptions, "build_property.RelayEnableHandlerRegistry", true),
                EnableOptimizedDispatcher = GetBoolProperty(globalOptions, "build_property.RelayEnableOptimizedDispatcher", true),
                EnableNotificationDispatcher = GetBoolProperty(globalOptions, "build_property.RelayEnableNotificationDispatcher", true),
                EnablePipelineRegistry = GetBoolProperty(globalOptions, "build_property.RelayEnablePipelineRegistry", true),
                EnableEndpointMetadata = GetBoolProperty(globalOptions, "build_property.RelayEnableEndpointMetadata", true)
            };
        }

        /// <summary>
        /// Reads a boolean property from MSBuild configuration.
        /// </summary>
        private static bool GetBoolProperty(AnalyzerConfigOptions options, string propertyName, bool defaultValue)
        {
            if (options.TryGetValue(propertyName, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                if (bool.TryParse(value, out var result))
                {
                    return result;
                }

                // Also support "yes"/"no" and "1"/"0"
                if (string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) || value == "1")
                    return true;

                if (string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) || value == "0")
                    return false;
            }

            return defaultValue;
        }

        /// <summary>
        /// Reads a string property from MSBuild configuration.
        /// </summary>
        private static string? GetStringProperty(AnalyzerConfigOptions options, string propertyName)
        {
            if (options.TryGetValue(propertyName, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Creates diagnostic severity configuration from MSBuild properties and .editorconfig.
        /// </summary>
        /// <param name="options">The analyzer config options provider</param>
        /// <returns>Configured diagnostic severity settings</returns>
        public static DiagnosticSeverityConfiguration CreateDiagnosticConfiguration(AnalyzerConfigOptionsProvider options)
        {
            return DiagnosticSeverityConfiguration.CreateFromOptions(options);
        }
    }
}

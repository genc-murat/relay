using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Relay.SourceGenerator.Diagnostics
{
    /// <summary>
    /// Provides configuration support for diagnostic severity overrides via .editorconfig and MSBuild properties.
    /// </summary>
    public class DiagnosticSeverityConfiguration
    {
        private readonly Dictionary<string, DiagnosticSeverity> _severityOverrides = new();
        private readonly HashSet<string> _suppressedDiagnostics = new();

        /// <summary>
        /// Creates a diagnostic severity configuration from analyzer config options.
        /// </summary>
        /// <param name="options">The analyzer config options provider</param>
        /// <returns>A configured DiagnosticSeverityConfiguration instance</returns>
        public static DiagnosticSeverityConfiguration CreateFromOptions(AnalyzerConfigOptionsProvider options)
        {
            var config = new DiagnosticSeverityConfiguration();
            var globalOptions = options.GlobalOptions;

            // Read MSBuild properties for diagnostic severity overrides
            // Format: build_property.RelayDiagnosticSeverity_RELAY_GEN_XXX = error|warning|info|none
            foreach (var diagnosticId in GetAllDiagnosticIds())
            {
                var propertyName = $"build_property.RelayDiagnosticSeverity_{diagnosticId}";
                if (globalOptions.TryGetValue(propertyName, out var severityValue) && 
                    !string.IsNullOrWhiteSpace(severityValue))
                {
                    var severity = ParseSeverity(severityValue);
                    if (severity.HasValue)
                    {
                        if (severity.Value == DiagnosticSeverity.Hidden)
                        {
                            config._suppressedDiagnostics.Add(diagnosticId);
                        }
                        else
                        {
                            config._severityOverrides[diagnosticId] = severity.Value;
                        }
                    }
                }
            }

            // Read suppression list
            // Format: build_property.RelaySuppressDiagnostics = RELAY_GEN_001,RELAY_GEN_002
            if (globalOptions.TryGetValue("build_property.RelaySuppressDiagnostics", out var suppressList) &&
                !string.IsNullOrWhiteSpace(suppressList))
            {
                var diagnosticIds = suppressList.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var id in diagnosticIds)
                {
                    var trimmedId = id.Trim();
                    if (!string.IsNullOrEmpty(trimmedId))
                    {
                        config._suppressedDiagnostics.Add(trimmedId);
                    }
                }
            }

            return config;
        }

        /// <summary>
        /// Applies severity configuration to a diagnostic descriptor.
        /// </summary>
        /// <param name="descriptor">The original diagnostic descriptor</param>
        /// <returns>A new descriptor with configured severity, or the original if no override exists</returns>
        public DiagnosticDescriptor ApplyConfiguration(DiagnosticDescriptor descriptor)
        {
            if (descriptor == null)
                throw new ArgumentNullException(nameof(descriptor));

            // Check if diagnostic is suppressed
            if (_suppressedDiagnostics.Contains(descriptor.Id))
            {
                return new DiagnosticDescriptor(
                    descriptor.Id,
                    descriptor.Title.ToString(),
                    descriptor.MessageFormat.ToString(),
                    descriptor.Category,
                    DiagnosticSeverity.Hidden,
                    isEnabledByDefault: false,
                    descriptor.Description?.ToString(),
                    descriptor.HelpLinkUri,
                    descriptor.CustomTags.ToArray());
            }

            // Check if severity override exists
            if (_severityOverrides.TryGetValue(descriptor.Id, out var overrideSeverity))
            {
                return new DiagnosticDescriptor(
                    descriptor.Id,
                    descriptor.Title.ToString(),
                    descriptor.MessageFormat.ToString(),
                    descriptor.Category,
                    overrideSeverity,
                    descriptor.IsEnabledByDefault,
                    descriptor.Description?.ToString(),
                    descriptor.HelpLinkUri,
                    descriptor.CustomTags.ToArray());
            }

            return descriptor;
        }

        /// <summary>
        /// Checks if a diagnostic is suppressed.
        /// </summary>
        /// <param name="diagnosticId">The diagnostic ID to check</param>
        /// <returns>True if the diagnostic is suppressed, false otherwise</returns>
        public bool IsSuppressed(string diagnosticId)
        {
            return _suppressedDiagnostics.Contains(diagnosticId);
        }

        /// <summary>
        /// Gets the configured severity for a diagnostic, if any.
        /// </summary>
        /// <param name="diagnosticId">The diagnostic ID</param>
        /// <returns>The configured severity, or null if no override exists</returns>
        public DiagnosticSeverity? GetConfiguredSeverity(string diagnosticId)
        {
            return _severityOverrides.TryGetValue(diagnosticId, out var severity) ? severity : null;
        }

        private static DiagnosticSeverity? ParseSeverity(string value)
        {
            return value.ToLowerInvariant() switch
            {
                "error" => DiagnosticSeverity.Error,
                "warning" => DiagnosticSeverity.Warning,
                "info" => DiagnosticSeverity.Info,
                "hidden" => DiagnosticSeverity.Hidden,
                "none" => DiagnosticSeverity.Hidden,
                _ => null
            };
        }

        private static IEnumerable<string> GetAllDiagnosticIds()
        {
            // Return all known diagnostic IDs
            yield return "RELAY_GEN_001";
            yield return "RELAY_GEN_002";
            yield return "RELAY_GEN_003";
            yield return "RELAY_GEN_004";
            yield return "RELAY_GEN_005";
            yield return "RELAY_GEN_101";
            yield return "RELAY_GEN_102";
            yield return "RELAY_GEN_104";
            yield return "RELAY_GEN_105";
            yield return "RELAY_GEN_106";
            yield return "RELAY_GEN_107";
            yield return "RELAY_GEN_108";
            yield return "RELAY_GEN_109";
            yield return "RELAY_GEN_201";
            yield return "RELAY_GEN_202";
            yield return "RELAY_GEN_203";
            yield return "RELAY_GEN_204";
            yield return "RELAY_GEN_205";
            yield return "RELAY_GEN_206";
            yield return "RELAY_GEN_207";
            yield return "RELAY_GEN_208";
            yield return "RELAY_GEN_209";
            yield return "RELAY_GEN_210";
            yield return "RELAY_GEN_211";
            yield return "RELAY_GEN_212";
            yield return "RELAY_GEN_213";
            yield return "RELAY_GEN_214";
            yield return "RELAY_GEN_215";
            yield return "RELAY_GEN_216";
        }
    }
}

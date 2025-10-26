using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Relay.SourceGenerator.Validators
{
    /// <summary>
    /// Validates for duplicate handler registrations.
    /// </summary>
    internal static class DuplicateHandlerValidator
    {
        /// <summary>
        /// Minimum recommended priority value to avoid warnings.
        /// </summary>
        private const int MinRecommendedPriority = -1000;

        /// <summary>
        /// Maximum recommended priority value to avoid warnings.
        /// </summary>
        private const int MaxRecommendedPriority = 1000;

        /// <summary>
        /// Validates for duplicate handler registrations across the compilation.
        /// </summary>
        public static void ValidateDuplicateHandlers(
            CompilationAnalysisContext context,
            HandlerRegistry handlerRegistry)
        {
            // Group handlers by request type
            var handlerGroups = handlerRegistry.Handlers
                .GroupBy(h => h.RequestType, SymbolEqualityComparer.Default);

            foreach (var group in handlerGroups)
            {
                if (group.Key == null) continue;
                var handlers = group.ToList();
                var requestTypeName = group.Key.ToDisplayString();

                // Check for unnamed duplicate handlers
                var unnamedHandlers = handlers.Where(h => string.IsNullOrWhiteSpace(h.Name) || h.Name == "default").ToList();
                if (unnamedHandlers.Count > 1)
                {
                    var handlerLocations = string.Join(", ", unnamedHandlers.Select(h =>
                        $"{h.MethodSymbol.ContainingType.Name}.{h.MethodName}"));

                    foreach (var handler in unnamedHandlers)
                    {
                        ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.DuplicateHandler,
                            handler.Location,
                            requestTypeName,
                            handlerLocations);
                    }
                }

                // Check for named handler conflicts
                var namedHandlers = handlers.Where(h => !string.IsNullOrWhiteSpace(h.Name) && h.Name != "default")
                    .GroupBy(h => h.Name);

                foreach (var namedGroup in namedHandlers)
                {
                    if (namedGroup.Count() > 1)
                    {
                        var conflictingHandlers = string.Join(", ", namedGroup.Select(h =>
                            $"{h.MethodSymbol.ContainingType.Name}.{h.MethodName}"));

                        foreach (var handler in namedGroup)
                        {
                            ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.NamedHandlerConflict,
                                handler.Location,
                                namedGroup.Key!,
                                requestTypeName);
                        }
                    }
                }

                // Check for mixed named and unnamed handlers (potential issue)
                if (unnamedHandlers.Count > 0 && handlers.Any(h => !string.IsNullOrWhiteSpace(h.Name) && h.Name != "default"))
                {
                    foreach (var unnamedHandler in unnamedHandlers)
                    {
                        ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.ConfigurationConflict,
                            unnamedHandler.Location,
                            $"Request type '{requestTypeName}' has both named and unnamed handlers. " +
                            "Consider using names for all handlers or removing names from all handlers for consistency.");
                    }
                }
            }

            // Additional validation: Check for handlers that might be unreachable
            ValidateHandlerReachability(context, handlerRegistry);
        }

        /// <summary>
        /// Validates handler reachability and potential issues.
        /// </summary>
        private static void ValidateHandlerReachability(
            CompilationAnalysisContext context,
            HandlerRegistry handlerRegistry)
        {
            // Group handlers by request type to check for potential issues
            var handlerGroups = handlerRegistry.Handlers
                .GroupBy(h => h.RequestType, SymbolEqualityComparer.Default);

            foreach (var group in handlerGroups)
            {
                var handlers = group.ToList();

                // Check for handlers with very low or very high priorities that might indicate issues
                foreach (var handler in handlers)
                {
                    if (handler.Priority < MinRecommendedPriority)
                    {
                        ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.PerformanceWarning,
                            handler.Location,
                            handler.MethodName,
                            "Very low priority value might indicate the handler will rarely be selected");
                    }
                    else if (handler.Priority > MaxRecommendedPriority)
                    {
                        ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.PerformanceWarning,
                            handler.Location,
                            handler.MethodName,
                            "Very high priority value might indicate over-prioritization");
                    }
                }

                // Check for potential naming conflicts with common patterns
                foreach (var handler in handlers.Where(h => !string.IsNullOrWhiteSpace(h.Name)))
                {
                    if (handler.Name!.Equals("main", System.StringComparison.OrdinalIgnoreCase))
                    {
                        ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.PerformanceWarning,
                            handler.Location,
                            handler.MethodName,
                            $"Handler name '{handler.Name}' might conflict with common naming patterns");
                    }
                }
            }
        }
    }
}

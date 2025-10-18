using Microsoft.CodeAnalysis;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Diagnostic descriptors for the Relay source generator.
    /// </summary>
    internal static class DiagnosticDescriptors
    {
        private const string Category = "Relay.Generator";

        // Debug and informational diagnostics
        public static readonly DiagnosticDescriptor Debug = new(
            "RELAY_DEBUG",
            "Relay Generator Debug Information",
            "{0}",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: false);

        public static readonly DiagnosticDescriptor Info = new(
            "RELAY_INFO",
            "Relay Generator Information",
            "{0}",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        // Error diagnostics
        public static readonly DiagnosticDescriptor GeneratorError = new(
            "RELAY_GEN_001",
            "Source Generator Error",
            "An error occurred during source generation: {0}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidHandlerSignature = new(
            "RELAY_GEN_002",
            "Invalid Handler Signature",
            "Invalid signature for {0} '{1}': {2}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateHandler = new(
            "RELAY_GEN_003",
            "Duplicate Handler Registration",
            "Multiple handlers found for request type '{0}': {1}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NamedHandlerConflict = new(
            "RELAY_GEN_005",
            "Named Handler Conflict",
            "Multiple handlers with the same name '{0}' found for request type '{1}'",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingRelayCoreReference = new(
            "RELAY_GEN_004",
            "Missing Relay.Core Reference",
            "The Relay.Core package must be referenced to use the Relay source generator",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        // Warning diagnostics
        public static readonly DiagnosticDescriptor UnusedHandler = new(
            "RELAY_GEN_101",
            "Unused Handler",
            "Handler method '{0}' is registered but may not be reachable",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PerformanceWarning = new(
            "RELAY_GEN_102",
            "Performance Warning",
            "Handler '{0}' may have performance implications: {1}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MissingConfigureAwait = new(
            "RELAY_GEN_104",
            "Missing ConfigureAwait(false)",
            "Await on a task without calling .ConfigureAwait(false) can lead to deadlocks. Consider using .ConfigureAwait(false).",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false);

        public static readonly DiagnosticDescriptor SyncOverAsync = new(
            "RELAY_GEN_105",
            "Sync-over-async detected",
            "Calling .Result or .Wait() on a task can lead to deadlocks. Use await instead.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false);

        // Configuration validation diagnostics
        public static readonly DiagnosticDescriptor DuplicatePipelineOrder = new(
            "RELAY_GEN_201",
            "Duplicate Pipeline Order",
            "Multiple pipelines found with order '{0}' in scope '{1}'. Pipeline orders must be unique within each scope.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidHandlerReturnType = new(
            "RELAY_GEN_202",
            "Invalid Handler Return Type",
            "Handler return type '{0}' is invalid. Expected '{1}' or Task<{1}> or ValueTask<{1}>.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidStreamHandlerReturnType = new(
            "RELAY_GEN_203",
            "Invalid Stream Handler Return Type",
            "Stream handler return type '{0}' is invalid. Expected IAsyncEnumerable<{1}>.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidNotificationHandlerReturnType = new(
            "RELAY_GEN_204",
            "Invalid Notification Handler Return Type",
            "Notification handler return type '{0}' is invalid. Expected Task or ValueTask.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor HandlerMissingRequestParameter = new(
            "RELAY_GEN_205",
            "Handler Missing Request Parameter",
            "Handler method '{0}' is missing the required request parameter.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor HandlerInvalidRequestParameter = new(
            "RELAY_GEN_206",
            "Handler Invalid Request Parameter Type",
            "Handler method request parameter type '{0}' does not match expected type '{1}'.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor HandlerMissingCancellationToken = new(
            "RELAY_GEN_207",
            "Handler Missing CancellationToken Parameter",
            "Handler method '{0}' should include a CancellationToken parameter for proper cancellation support.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NotificationHandlerMissingParameter = new(
            "RELAY_GEN_208",
            "Notification Handler Missing Parameter",
            "Notification handler method '{0}' is missing the required notification parameter.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidPriorityValue = new(
            "RELAY_GEN_209",
            "Invalid Priority Value",
            "Priority value '{0}' is invalid. Priority must be a valid integer value.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NoHandlersFound = new(
            "RELAY_GEN_210",
            "No Handlers Found",
            "No handlers or notification handlers were found. Add [Handle] or [Notification] attributes to methods.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ConfigurationConflict = new(
            "RELAY_GEN_211",
            "Configuration Conflict",
            "Configuration conflict detected: {0}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidPipelineScope = new(
            "RELAY_GEN_212",
            "Invalid Pipeline Scope",
            "Pipeline scope '{0}' is invalid for method '{1}'. Expected scope: {2}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PrivateHandler = new(
            "RELAY_GEN_106",
            "Private Handler",
            "Handler method '{0}' is private. Private handlers may not be discoverable by the dependency injection container.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InternalHandler = new(
            "RELAY_GEN_107",
            "Internal Handler",
            "Handler method '{0}' is internal. This is a valid accessibility, but consider making it public if it needs to be accessed from other assemblies.",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MultipleConstructors = new(
            "RELAY_GEN_108",
            "Multiple Constructors",
            "Handler class '{0}' has multiple constructors. The dependency injection container may not be able to resolve the correct one. Consider using a single constructor.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}

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
            isEnabledByDefault: true,
            description: "An error occurred during source generation. This typically indicates an internal error in the source generator itself.",
            customTags: new[] { "CompilationEnd", "NotConfigurable" });

        public static readonly DiagnosticDescriptor InvalidHandlerSignature = new(
            "RELAY_GEN_002",
            "Invalid Handler Signature",
            "Invalid signature for {0} '{1}': {2}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The handler signature is invalid and does not conform to expected patterns.");

        public static readonly DiagnosticDescriptor DuplicateHandler = new(
            "RELAY_GEN_003",
            "Duplicate Handler Registration",
            "Multiple handlers found for request type '{0}': {1}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Multiple handlers were found for the same request type, which is not allowed. Each request type should have at most one handler implementation.");

        public static readonly DiagnosticDescriptor NamedHandlerConflict = new(
            "RELAY_GEN_005",
            "Named Handler Conflict",
            "Multiple handlers with the same name '{0}' found for request type '{1}'",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Multiple handlers with the same name were found for the same request type, which creates a conflict in handler resolution.");

        public static readonly DiagnosticDescriptor MissingRelayCoreReference = new(
            "RELAY_GEN_004",
            "Missing Relay.Core Reference",
            "The Relay.Core package must be referenced to use the Relay source generator",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The Relay.Core package is not referenced in the project, which is required for the Relay source generator to function properly.");

        // Warning diagnostics
        public static readonly DiagnosticDescriptor UnusedHandler = new(
            "RELAY_GEN_101",
            "Unused Handler",
            "Handler method '{0}' is registered but may not be reachable",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A handler method has been registered but may not be reachable by the dependency injection container or may not be called by the application logic.");

        public static readonly DiagnosticDescriptor PerformanceWarning = new(
            "RELAY_GEN_102",
            "Performance Warning",
            "Handler '{0}' may have performance implications: {1}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The handler method may have performance implications that could affect the request processing throughput or latency.");

        public static readonly DiagnosticDescriptor MissingConfigureAwait = new(
            "RELAY_GEN_104",
            "Missing ConfigureAwait(false)",
            "Await on a task without calling .ConfigureAwait(false) can lead to deadlocks. Consider using .ConfigureAwait(false).",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: "The await expression is missing ConfigureAwait(false) which can lead to deadlocks in synchronous contexts. Consider using .ConfigureAwait(false) to prevent potential issues with sync-context switches.");

        public static readonly DiagnosticDescriptor SyncOverAsync = new(
            "RELAY_GEN_105",
            "Sync-over-async detected",
            "Calling .Result or .Wait() on a task can lead to deadlocks. Use await instead.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: "The code is using synchronous methods (.Result or .Wait()) on asynchronous operations, which can lead to deadlocks. Use await instead for proper asynchronous execution.");

        // Configuration validation diagnostics
        public static readonly DiagnosticDescriptor DuplicatePipelineOrder = new(
            "RELAY_GEN_201",
            "Duplicate Pipeline Order",
            "Multiple pipelines found with order '{0}' in scope '{1}'. Pipeline orders must be unique within each scope.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Multiple pipeline attributes have been assigned the same order value within a single scope, which causes ambiguity in pipeline execution order.");

        public static readonly DiagnosticDescriptor InvalidHandlerReturnType = new(
            "RELAY_GEN_202",
            "Invalid Handler Return Type",
            "Handler return type '{0}' is invalid. Expected '{1}' or Task<{1}> or ValueTask<{1}>.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The handler method has an invalid return type that does not conform to the expected patterns for the request type.");

        public static readonly DiagnosticDescriptor InvalidStreamHandlerReturnType = new(
            "RELAY_GEN_203",
            "Invalid Stream Handler Return Type",
            "Stream handler return type '{0}' is invalid. Expected IAsyncEnumerable<{1}>.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The stream handler method has an invalid return type that should be IAsyncEnumerable<T> for streaming responses.");

        public static readonly DiagnosticDescriptor InvalidNotificationHandlerReturnType = new(
            "RELAY_GEN_204",
            "Invalid Notification Handler Return Type",
            "Notification handler return type '{0}' is invalid. Expected Task or ValueTask.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The notification handler method has an invalid return type that should be Task or ValueTask for proper async execution.");

        public static readonly DiagnosticDescriptor HandlerMissingRequestParameter = new(
            "RELAY_GEN_205",
            "Handler Missing Request Parameter",
            "Handler method '{0}' is missing the required request parameter.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The handler method is missing the required request parameter that should match the IRequest interface implementation.");

        public static readonly DiagnosticDescriptor HandlerInvalidRequestParameter = new(
            "RELAY_GEN_206",
            "Handler Invalid Request Parameter Type",
            "Handler method request parameter type '{0}' does not match expected type '{1}'.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The handler method has a request parameter with a type that does not match the expected request type for the handler.");

        public static readonly DiagnosticDescriptor HandlerMissingCancellationToken = new(
            "RELAY_GEN_207",
            "Handler Missing CancellationToken Parameter",
            "Handler method '{0}' should include a CancellationToken parameter for proper cancellation support.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The handler method is missing an optional but recommended CancellationToken parameter for proper cancellation support during long-running operations.");

        public static readonly DiagnosticDescriptor NotificationHandlerMissingParameter = new(
            "RELAY_GEN_208",
            "Notification Handler Missing Parameter",
            "Notification handler method '{0}' is missing the required notification parameter.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The notification handler method is missing the required notification parameter that should match the INotification interface implementation.");

        public static readonly DiagnosticDescriptor InvalidPriorityValue = new(
            "RELAY_GEN_209",
            "Invalid Priority Value",
            "Priority value '{0}' is invalid. Priority must be a valid integer value.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The priority value specified for a handler is invalid and must be a valid integer value.");

        public static readonly DiagnosticDescriptor NoHandlersFound = new(
            "RELAY_GEN_210",
            "No Handlers Found",
            "No handlers or notification handlers were found. Add [Handle] or [Notification] attributes to methods.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "No handlers or notification handlers were found in the compilation. Add [Handle] or [Notification] attributes to handler methods to register them with the Relay framework.");

        public static readonly DiagnosticDescriptor ConfigurationConflict = new(
            "RELAY_GEN_211",
            "Configuration Conflict",
            "Configuration conflict detected: {0}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A configuration conflict was detected that prevents proper handler registration or pipeline setup. Check the configuration for conflicting values.");

        public static readonly DiagnosticDescriptor InvalidPipelineScope = new(
            "RELAY_GEN_212",
            "Invalid Pipeline Scope",
            "Pipeline scope '{0}' is invalid for method '{1}'. Expected scope: {2}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The pipeline scope specified for a method is invalid and does not match the expected scope for the handler type.");

        public static readonly DiagnosticDescriptor PrivateHandler = new(
            "RELAY_GEN_106",
            "Private Handler",
            "Handler method '{0}' is private. Private handlers may not be discoverable by the dependency injection container.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The handler method is marked as private, which can make it undiscoverable by the dependency injection container. Consider making the handler method public.");

        public static readonly DiagnosticDescriptor InternalHandler = new(
            "RELAY_GEN_107",
            "Internal Handler",
            "Handler method '{0}' is internal. This is a valid accessibility, but consider making it public if it needs to be accessed from other assemblies.",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "The handler method is marked as internal, which is valid but limits its accessibility. Consider making it public if it needs to be accessed from other assemblies.");

        public static readonly DiagnosticDescriptor MultipleConstructors = new(
            "RELAY_GEN_108",
            "Multiple Constructors",
            "Handler class '{0}' has multiple constructors. The dependency injection container may not be able to resolve the correct one. Consider using a single constructor.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The handler class has multiple constructors which can cause issues with dependency injection resolution. Consider using a single constructor with all required dependencies.");

        public static readonly DiagnosticDescriptor ConstructorValueTypeParameter = new(
            "RELAY_GEN_109",
            "Constructor Value Type Parameter",
            "Handler class '{0}' has a constructor with a value type parameter '{1}'. Value types are not typically registered as services and may cause issues with dependency injection.",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The handler class constructor has a value type parameter which is not typically registered as a service and may cause dependency injection issues.");

        // New diagnostic descriptors for improved error reporting
        public static readonly DiagnosticDescriptor InvalidConfigurationValue = new(
            "RELAY_GEN_213",
            "Invalid Configuration Value",
            "Configuration property '{0}' has invalid value '{1}'. {2}",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A configuration property has been set to an invalid value. Check the property value and ensure it meets the expected format and constraints.",
            helpLinkUri: "https://github.com/MrDave1999/Relay/wiki/RELAY_GEN_213");

        public static readonly DiagnosticDescriptor MissingRequiredAttribute = new(
            "RELAY_GEN_214",
            "Missing Required Attribute",
            "Handler method '{0}' is missing the required '{1}' attribute. Add the attribute to enable proper handler registration.",
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "A handler method is missing a required attribute for proper discovery and registration. Add the specified attribute to the method.",
            helpLinkUri: "https://github.com/MrDave1999/Relay/wiki/RELAY_GEN_214");

        public static readonly DiagnosticDescriptor ObsoleteHandlerPattern = new(
            "RELAY_GEN_215",
            "Obsolete Handler Pattern",
            "Handler '{0}' uses an obsolete pattern: {1}. Consider migrating to: {2}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The handler is using an obsolete or deprecated pattern. Consider updating to the recommended modern pattern for better performance and maintainability.",
            helpLinkUri: "https://github.com/MrDave1999/Relay/wiki/RELAY_GEN_215");

        public static readonly DiagnosticDescriptor PerformanceBottleneck = new(
            "RELAY_GEN_216",
            "Performance Bottleneck Detected",
            "Handler '{0}' may have a performance bottleneck: {1}. Suggestion: {2}",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A potential performance issue has been detected in the handler. Review the suggestion to optimize performance.",
            helpLinkUri: "https://github.com/MrDave1999/Relay/wiki/RELAY_GEN_216");
    }
}

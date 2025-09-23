using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Source generator for the Relay mediator framework.
    /// Generates handler registrations, dispatch logic, and DI container extensions.
    /// </summary>
    [Generator]
    public class RelaySourceGenerator : ISourceGenerator
    {
        private const string GeneratorName = "Relay.SourceGenerator";
        private const string GeneratorVersion = "1.0.0";

        public void Initialize(GeneratorInitializationContext context)
        {
            // Enable debugging support
            if (!Debugger.IsAttached)
            {
                // Uncomment the following line to debug the source generator
                // Debugger.Launch();
            }

            // Register for syntax notifications
            context.RegisterForSyntaxNotifications(() => new RelaySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            try
            {
                GeneratorLogger.LogDebug(context, "Starting Relay source generation");

                // Get the syntax receiver
                if (context.SyntaxReceiver is not RelaySyntaxReceiver receiver)
                {
                    GeneratorLogger.LogDebug(context, "No syntax receiver found");
                    return;
                }

                // Create compilation context
                var compilationContext = new RelayCompilationContext(context.Compilation, context.CancellationToken);

                // Check if Relay.Core is referenced
                if (!compilationContext.HasRelayCoreReference())
                {
                    GeneratorLogger.ReportDiagnostic(context, DiagnosticDescriptors.MissingRelayCoreReference, Location.None);
                    return;
                }

                GeneratorLogger.LogDebug(context, $"Found {receiver.CandidateMethods.Count} candidate methods");

                // Discover and validate handlers
                var discoveryEngine = new HandlerDiscoveryEngine(compilationContext);
                var diagnosticReporter = new GeneratorExecutionContextDiagnosticReporter(context);
                var discoveryResult = discoveryEngine.DiscoverHandlers(receiver.CandidateMethods, diagnosticReporter);

                GeneratorLogger.LogDebug(context, $"Discovered {discoveryResult.Handlers.Count} valid handlers");

                // Generate handler registry if we have valid handlers
                if (discoveryResult.Handlers.Count > 0)
                {
                    GenerateHandlerRegistry(context, compilationContext, discoveryResult);
                    GenerateOptimizedDispatcher(context, compilationContext, discoveryResult);
                    GenerateNotificationDispatcher(context, compilationContext, discoveryResult);
                    GeneratePipelineRegistry(context, compilationContext, discoveryResult);
                    GenerateEndpointMetadata(context, compilationContext, discoveryResult);
                    GenerateDIRegistrations(context, compilationContext, discoveryResult);
                    GenerateMarkerFile(context, compilationContext);
                }

                GeneratorLogger.LogInfo(context, $"Relay source generation completed successfully for assembly '{compilationContext.AssemblyName}'");
            }
            catch (Exception ex)
            {
                GeneratorLogger.LogError(context, ex);
            }
        }

        private void GenerateHandlerRegistry(GeneratorExecutionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var registryGenerator = new HandlerRegistryGenerator(compilationContext);
            var registrySource = registryGenerator.GenerateHandlerRegistry(discoveryResult);
            
            context.AddSource("HandlerRegistry.g.cs", registrySource);
            GeneratorLogger.LogDebug(context, "Generated handler registry");
        }

        private void GenerateOptimizedDispatcher(GeneratorExecutionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var optimizedGenerator = new OptimizedDispatcherGenerator(compilationContext);
            var optimizedSource = optimizedGenerator.GenerateOptimizedDispatcher(discoveryResult);
            
            context.AddSource("OptimizedDispatcher.g.cs", optimizedSource);
            GeneratorLogger.LogDebug(context, "Generated optimized dispatcher");
        }

        private void GenerateNotificationDispatcher(GeneratorExecutionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var notificationGenerator = new NotificationDispatcherGenerator(compilationContext);
            var notificationSource = notificationGenerator.GenerateNotificationDispatcher(discoveryResult);
            
            if (!string.IsNullOrEmpty(notificationSource))
            {
                context.AddSource("NotificationDispatcher.g.cs", notificationSource);
                GeneratorLogger.LogDebug(context, "Generated notification dispatcher");
            }
        }

        private void GeneratePipelineRegistry(GeneratorExecutionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var pipelineGenerator = new PipelineRegistryGenerator(compilationContext);
            var pipelineSource = pipelineGenerator.GeneratePipelineRegistry(discoveryResult);
            
            if (!string.IsNullOrEmpty(pipelineSource))
            {
                context.AddSource("PipelineRegistry.g.cs", pipelineSource);
                GeneratorLogger.LogDebug(context, "Generated pipeline registry");
            }
        }

        private void GenerateEndpointMetadata(GeneratorExecutionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var diagnosticReporter = new GeneratorExecutionContextDiagnosticReporter(context);
            var endpointGenerator = new EndpointMetadataGenerator(compilationContext.Compilation, diagnosticReporter);
            var endpointSource = endpointGenerator.GenerateEndpointMetadata(discoveryResult.Handlers);
            
            if (!string.IsNullOrEmpty(endpointSource))
            {
                context.AddSource("EndpointMetadata.g.cs", endpointSource);
                GeneratorLogger.LogDebug(context, "Generated endpoint metadata");
            }
        }

        private void GenerateDIRegistrations(GeneratorExecutionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var diGenerator = new DIRegistrationGenerator(compilationContext);
            var diSource = diGenerator.GenerateDIRegistrations(discoveryResult);
            
            context.AddSource("DIRegistrations.g.cs", diSource);
            GeneratorLogger.LogDebug(context, "Generated DI registrations");
        }

        private void GenerateMarkerFile(GeneratorExecutionContext context, RelayCompilationContext compilationContext)
        {
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("// <auto-generated />");
            sourceBuilder.AppendLine($"// Generated by {GeneratorName} v{GeneratorVersion}");
            sourceBuilder.AppendLine($"// Generation time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace Relay.Generated");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Marker class to verify source generator execution.");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    internal static class RelayGeneratorMarker");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine($"        public const string GeneratorName = \"{GeneratorName}\";");
            sourceBuilder.AppendLine($"        public const string GeneratorVersion = \"{GeneratorVersion}\";");
            sourceBuilder.AppendLine($"        public const string AssemblyName = \"{compilationContext.AssemblyName}\";");
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            context.AddSource("RelayGeneratorMarker.g.cs", sourceBuilder.ToString());
        }


    }

    /// <summary>
    /// Syntax receiver for collecting attributed methods during compilation.
    /// </summary>
    public class RelaySyntaxReceiver : ISyntaxReceiver
    {
        public List<MethodDeclarationSyntax> CandidateMethods { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Look for methods with attributes that might be Relay handlers
            if (syntaxNode is MethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.AttributeLists.Count > 0)
            {
                // Check if any attribute might be a Relay attribute
                var hasRelayAttribute = methodDeclaration.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(attr => IsRelayAttribute(attr.Name.ToString()));

                if (hasRelayAttribute)
                {
                    CandidateMethods.Add(methodDeclaration);
                }
            }
        }

        private static bool IsRelayAttribute(string attributeName)
        {
            // Check for known Relay attributes (without the "Attribute" suffix)
            return attributeName is "Handle" or "HandleAttribute" or
                   "Notification" or "NotificationAttribute" or
                   "Pipeline" or "PipelineAttribute" or
                   "ExposeAsEndpoint" or "ExposeAsEndpointAttribute";
        }
    }

    /// <summary>
    /// Compilation context for the Relay source generator.
    /// Provides access to compilation information and helper methods.
    /// </summary>
    public class RelayCompilationContext
    {
        public Compilation Compilation { get; }
        public System.Threading.CancellationToken CancellationToken { get; }
        public string AssemblyName { get; }

        public RelayCompilationContext(Compilation compilation, System.Threading.CancellationToken cancellationToken)
        {
            Compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            CancellationToken = cancellationToken;
            AssemblyName = compilation.AssemblyName ?? "Unknown";
        }

        /// <summary>
        /// Gets the semantic model for a syntax tree.
        /// </summary>
        public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
        {
            return Compilation.GetSemanticModel(syntaxTree);
        }

        /// <summary>
        /// Finds a type by its full name.
        /// </summary>
        public INamedTypeSymbol? FindType(string fullTypeName)
        {
            return Compilation.GetTypeByMetadataName(fullTypeName);
        }

        /// <summary>
        /// Checks if the compilation references the Relay.Core assembly.
        /// </summary>
        public bool HasRelayCoreReference()
        {
            // Allow the generator project itself to compile without Relay.Core
            if (string.Equals(AssemblyName, "Relay.SourceGenerator", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (Compilation.ReferencedAssemblyNames
                .Any(name => name.Name.Equals("Relay.Core", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Fallback: detect by presence of known Relay.Core types in the compilation
            // Works in test scenarios using metadata references or embedded stubs
            var knownTypeNames = new[]
            {
                "Relay.Core.IRelay",
                "Relay.Core.IRequest`1",
                // Attributes may be missing in some contexts; interfaces are sufficient to detect reference
                "Relay.Core.INotification"
            };

            foreach (var typeName in knownTypeNames)
            {
                if (Compilation.GetTypeByMetadataName(typeName) is not null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
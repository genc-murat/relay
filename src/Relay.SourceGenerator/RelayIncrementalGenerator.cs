using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Modern incremental source generator for the Relay mediator framework.
    /// Generates handler registrations, dispatch logic, and DI container extensions.
    /// </summary>
    [Generator]
    public class RelayIncrementalGenerator : IIncrementalGenerator
    {
        private const string GeneratorName = "Relay.IncrementalGenerator";
        private const string GeneratorVersion = "1.0.0";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Enable debugging support
            if (!Debugger.IsAttached)
            {
                // Uncomment the following line to debug the source generator
                // Debugger.Launch();
            }

            // Create pipeline for method declarations with attributes
            var methodDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsCandidateMethodSyntax(s),
                    transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null);

            // Create pipeline for compilation data
            var compilationAndMethods = context.CompilationProvider.Combine(methodDeclarations.Collect());

            // Register source output
            context.RegisterSourceOutput(compilationAndMethods, Execute);
        }

        private static bool IsCandidateMethodSyntax(SyntaxNode node)
        {
            return node is MethodDeclarationSyntax { AttributeLists.Count: > 0 } method &&
                   method.AttributeLists
                       .SelectMany(al => al.Attributes)
                       .Any(attr => IsRelayAttribute(attr.Name.ToString()));
        }

        private static MethodDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Additional semantic filtering can be done here if needed
            foreach (var attributeList in methodDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol)
                    {
                        var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                        var fullName = attributeContainingTypeSymbol.ToDisplayString();

                        if (IsRelayAttributeFullName(fullName))
                        {
                            return methodDeclaration;
                        }
                    }
                }
            }

            return null;
        }

        private static void Execute(SourceProductionContext context, (Compilation Left, ImmutableArray<MethodDeclarationSyntax?> Right) source)
        {
            try
            {
                var (compilation, methods) = source;
                
                // Create compilation context
                var compilationContext = new RelayCompilationContext(compilation, CancellationToken.None);

                // Check if Relay.Core is referenced
                if (!compilationContext.HasRelayCoreReference())
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.MissingRelayCoreReference,
                            Location.None));
                    return;
                }

                var candidateMethods = methods.Where(m => m != null).ToList();

                if (candidateMethods.Count == 0)
                {
                    return; // No methods found, nothing to generate
                }

                // Discover and validate handlers
                var discoveryEngine = new HandlerDiscoveryEngine(compilationContext);
                var diagnosticReporter = new SourceOutputDiagnosticReporter(context);
                var discoveryResult = discoveryEngine.DiscoverHandlers(candidateMethods, diagnosticReporter);

                // Generate marker file and other content only if we have valid handlers
                if (discoveryResult.Handlers.Count > 0)
                {
                    GenerateMarkerFile(context, compilationContext);
                    GenerateHandlerRegistry(context, compilationContext, discoveryResult);
                    GenerateOptimizedDispatcher(context, compilationContext, discoveryResult);
                    GenerateNotificationDispatcher(context, compilationContext, discoveryResult);
                    GeneratePipelineRegistry(context, compilationContext, discoveryResult);
                    GenerateEndpointMetadata(context, compilationContext, discoveryResult);
                    GenerateDIRegistrations(context, compilationContext, discoveryResult);
                    GeneratePerformanceOptimizations(context, compilationContext, discoveryResult);
                }
            }
            catch (Exception ex)
            {
                // Report exception as diagnostic
                var descriptor = new DiagnosticDescriptor(
                    "RELAY_GEN_ERROR",
                    "Generator Exception",
                    "An exception occurred during source generation: {0}",
                    "Generator",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true);

                context.ReportDiagnostic(
                    Diagnostic.Create(descriptor, Location.None, ex.ToString()));
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

        private static bool IsRelayAttributeFullName(string fullName)
        {
            return fullName.Contains("Relay.Core.HandleAttribute") ||
                   fullName.Contains("Relay.Core.NotificationAttribute") ||
                   fullName.Contains("Relay.Core.PipelineAttribute") ||
                   fullName.Contains("Relay.Core.ExposeAsEndpointAttribute") ||
                   fullName.Contains("HandleAttribute") ||
                   fullName.Contains("NotificationAttribute") ||
                   fullName.Contains("PipelineAttribute") ||
                   fullName.Contains("ExposeAsEndpointAttribute");
        }

        #region Generation Methods

        private static void GenerateHandlerRegistry(SourceProductionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var registryGenerator = new HandlerRegistryGenerator(compilationContext);
            var registrySource = registryGenerator.GenerateHandlerRegistry(discoveryResult);

            context.AddSource("HandlerRegistry.g.cs", registrySource);
        }

        private static void GenerateOptimizedDispatcher(SourceProductionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var optimizedGenerator = new OptimizedDispatcherGenerator(compilationContext);
            var optimizedSource = optimizedGenerator.GenerateOptimizedDispatcher(discoveryResult);

            context.AddSource("OptimizedDispatcher.g.cs", optimizedSource);
        }

        private static void GenerateNotificationDispatcher(SourceProductionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var notificationGenerator = new NotificationDispatcherGenerator(compilationContext);
            var notificationSource = notificationGenerator.GenerateNotificationDispatcher(discoveryResult);

            if (!string.IsNullOrWhiteSpace(notificationSource))
            {
                context.AddSource("NotificationDispatcher.g.cs", notificationSource);
            }
        }

        private static void GeneratePipelineRegistry(SourceProductionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var pipelineGenerator = new PipelineRegistryGenerator(compilationContext);
            var pipelineSource = pipelineGenerator.GeneratePipelineRegistry(discoveryResult);

            if (!string.IsNullOrWhiteSpace(pipelineSource))
            {
                context.AddSource("PipelineRegistry.g.cs", pipelineSource);
            }
        }

        private static void GenerateEndpointMetadata(SourceProductionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var diagnosticReporter = new SourceProductionContextDiagnosticReporter(context);
            var endpointGenerator = new EndpointMetadataGenerator(compilationContext.Compilation, diagnosticReporter);
            var endpointSource = endpointGenerator.GenerateEndpointMetadata(discoveryResult.Handlers);

            if (!string.IsNullOrWhiteSpace(endpointSource))
            {
                context.AddSource("EndpointMetadata.g.cs", endpointSource);
            }
        }

        private static void GenerateDIRegistrations(SourceProductionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var diGenerator = new DIRegistrationGenerator(compilationContext);
            var diSource = diGenerator.GenerateDIRegistrations(discoveryResult);

            context.AddSource("DIRegistrations.g.cs", diSource);
        }

        private static void GeneratePerformanceOptimizations(SourceProductionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("// <auto-generated />");
            sourceBuilder.AppendLine("// Enhanced performance optimizations for Relay Incremental Generator");
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Collections.Concurrent;");
            sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
            sourceBuilder.AppendLine("using System.Threading;");
            sourceBuilder.AppendLine("using System.Threading.Tasks;");
            sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"namespace {compilationContext.AssemblyName}.Generated");
            sourceBuilder.AppendLine("{");

            GeneratePerformanceCacheClass(sourceBuilder, discoveryResult);
            GenerateUltraFastDispatcher(sourceBuilder, discoveryResult);
            GenerateCompiledRelay(sourceBuilder, discoveryResult);

            sourceBuilder.AppendLine("}");

            context.AddSource("RelayPerformanceOptimizations.g.cs", sourceBuilder.ToString());
        }

        private static void GeneratePerformanceCacheClass(StringBuilder sourceBuilder, HandlerDiscoveryResult discoveryResult)
        {
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Ultra high-performance handler cache for Relay optimizations");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public static class RelayPerformanceCache");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine("        private static readonly ConcurrentDictionary<Type, object?> _handlerCache = new();");
            sourceBuilder.AppendLine("        private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> _factoryCache = new();");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]");
            sourceBuilder.AppendLine("        public static T GetOrCreateHandler<T>(IServiceProvider serviceProvider) where T : class");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            var type = typeof(T);");
            sourceBuilder.AppendLine("            if (_handlerCache.TryGetValue(type, out var cached) && cached is T handler)");
            sourceBuilder.AppendLine("                return handler;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("            var serviceHandler = serviceProvider.GetRequiredService<T>();");
            sourceBuilder.AppendLine("            _handlerCache.TryAdd(type, serviceHandler);");
            sourceBuilder.AppendLine("            return serviceHandler;");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine();
        }

        private static void GenerateUltraFastDispatcher(StringBuilder sourceBuilder, HandlerDiscoveryResult discoveryResult)
        {
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Ultra-fast compile-time generated dispatcher");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public static class UltraFastDispatcher");
            sourceBuilder.AppendLine("    {");

            // Generate specific dispatch methods for each discovered handler
            foreach (var handler in discoveryResult.Handlers.Take(10)) // Limit to prevent huge files
            {
                var requestType = handler.RequestType?.Name ?? "UnknownRequest";
                var responseType = handler.ResponseType?.Name ?? "UnknownResponse";
                var handlerType = handler.HandlerType?.Name ?? "UnknownHandler";
                var methodName = handler.MethodName ?? "Handle";

                sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]");
                sourceBuilder.AppendLine($"        public static ValueTask<{responseType}> Dispatch{requestType}(");
                sourceBuilder.AppendLine($"            {requestType} request, {handlerType} handler, CancellationToken cancellationToken)");
                sourceBuilder.AppendLine($"        {{");
                sourceBuilder.AppendLine($"            return handler.{methodName}(request, cancellationToken);");
                sourceBuilder.AppendLine($"        }}");
                sourceBuilder.AppendLine();
            }

            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine();
        }

        private static void GenerateCompiledRelay(StringBuilder sourceBuilder, HandlerDiscoveryResult discoveryResult)
        {
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Compile-time optimized relay implementation");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public sealed class CompiledRelay : Relay.Core.IRelay");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine("        private readonly IServiceProvider _serviceProvider;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("        public CompiledRelay(IServiceProvider serviceProvider)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine("        public ValueTask<TResponse> SendAsync<TResponse>(Relay.Core.IRequest<TResponse> request, CancellationToken cancellationToken = default)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            ArgumentNullException.ThrowIfNull(request);");
            sourceBuilder.AppendLine("            // Fast-path dispatch implementation would go here");
            sourceBuilder.AppendLine("            throw new NotImplementedException(\"Ultra-fast dispatch coming soon\");");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("        public ValueTask SendAsync(Relay.Core.IRequest request, CancellationToken cancellationToken = default)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            throw new NotImplementedException(\"Void requests not implemented in compiled relay\");");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("        public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(Relay.Core.IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            throw new NotImplementedException(\"Streaming not implemented in compiled relay\");");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("        public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)");
            sourceBuilder.AppendLine("            where TNotification : Relay.Core.INotification");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            ArgumentNullException.ThrowIfNull(notification);");
            sourceBuilder.AppendLine("            return ValueTask.CompletedTask;");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine("    }");
        }

        private static void GenerateMarkerFile(SourceProductionContext context, RelayCompilationContext compilationContext)
        {
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("// <auto-generated />");
            sourceBuilder.AppendLine($"// Generated by {GeneratorName} v{GeneratorVersion}");
            sourceBuilder.AppendLine($"// Generation time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("namespace Relay.Generated");
            sourceBuilder.AppendLine("{");
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Marker class to verify incremental source generator execution.");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    internal static class RelayIncrementalGeneratorMarker");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine($"        public const string GeneratorName = \"{GeneratorName}\";");
            sourceBuilder.AppendLine($"        public const string GeneratorVersion = \"{GeneratorVersion}\";");
            sourceBuilder.AppendLine($"        public const string AssemblyName = \"{compilationContext.AssemblyName}\";");
            sourceBuilder.AppendLine("        public const bool IncrementalGeneration = true;");
            sourceBuilder.AppendLine("        public const bool OptimizationsEnabled = true;");
            sourceBuilder.AppendLine("        public const bool ValueTaskOptimized = true;");
            sourceBuilder.AppendLine("        public const bool DirectCallsEnabled = true;");
            sourceBuilder.AppendLine("    }");
            sourceBuilder.AppendLine("}");

            context.AddSource("RelayGeneratorMarker.g.cs", sourceBuilder.ToString());
        }

        #endregion
    }

    /// <summary>
    /// Diagnostic reporter adapter for SourceProductionContext.
    /// </summary>
    public class SourceProductionContextDiagnosticReporter : IDiagnosticReporter
    {
        private readonly SourceProductionContext _context;

        public SourceProductionContextDiagnosticReporter(SourceProductionContext context)
        {
            _context = context;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
            _context.ReportDiagnostic(diagnostic);
        }
    }
}
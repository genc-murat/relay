using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

                    // Enhanced performance optimizations
                    GeneratePerformanceOptimizations(context, compilationContext, discoveryResult);

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

            if (!string.IsNullOrWhiteSpace(notificationSource))
            {
                context.AddSource("NotificationDispatcher.g.cs", notificationSource);
                GeneratorLogger.LogDebug(context, "Generated notification dispatcher");
            }
        }

        private void GeneratePipelineRegistry(GeneratorExecutionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var pipelineGenerator = new PipelineRegistryGenerator(compilationContext);
            var pipelineSource = pipelineGenerator.GeneratePipelineRegistry(discoveryResult);

            if (!string.IsNullOrWhiteSpace(pipelineSource))
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

            if (!string.IsNullOrWhiteSpace(endpointSource))
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

        private void GeneratePerformanceOptimizations(GeneratorExecutionContext context, RelayCompilationContext compilationContext, HandlerDiscoveryResult discoveryResult)
        {
            var sourceBuilder = new StringBuilder();
            sourceBuilder.AppendLine("// <auto-generated />");
            sourceBuilder.AppendLine("// Enhanced performance optimizations for Relay Source Generator");
            sourceBuilder.AppendLine("using System;");
            sourceBuilder.AppendLine("using System.Collections.Concurrent;");
            sourceBuilder.AppendLine("using System.Runtime.CompilerServices;");
            sourceBuilder.AppendLine("using System.Threading;");
            sourceBuilder.AppendLine("using System.Threading.Tasks;");
            sourceBuilder.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine($"namespace {compilationContext.AssemblyName}.Generated");
            sourceBuilder.AppendLine("{");

            // Enhanced performance cache with multiple optimization strategies
            sourceBuilder.AppendLine("    /// <summary>");
            sourceBuilder.AppendLine("    /// Ultra high-performance handler cache for Relay optimizations");
            sourceBuilder.AppendLine("    /// </summary>");
            sourceBuilder.AppendLine("    public static class RelayPerformanceCache");
            sourceBuilder.AppendLine("    {");
            sourceBuilder.AppendLine("        private static readonly ConcurrentDictionary<Type, object?> _handlerCache = new();");
            sourceBuilder.AppendLine("        private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> _factoryCache = new();");
            sourceBuilder.AppendLine();

            // Pre-compiled exception tasks for better error handling performance
            sourceBuilder.AppendLine("        // Pre-allocated exception tasks for common scenarios");
            sourceBuilder.AppendLine("        private static readonly ValueTask<object> _notFoundExceptionTask =");
            sourceBuilder.AppendLine("            ValueTask.FromException<object>(new InvalidOperationException(\"Handler not found\"));");
            sourceBuilder.AppendLine();

            // Ultra-fast handler resolution with factory caching
            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine("        /// Ultra-fast handler resolution with factory caching");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]");
            sourceBuilder.AppendLine("        public static T GetOrCreateHandler<T>(IServiceProvider serviceProvider) where T : class");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            var type = typeof(T);");
            sourceBuilder.AppendLine("            if (_handlerCache.TryGetValue(type, out var cached) && cached is T handler)");
            sourceBuilder.AppendLine("                return handler;");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("            // Use factory if available for better performance");
            sourceBuilder.AppendLine("            if (_factoryCache.TryGetValue(type, out var factory))");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine("                var newHandler = (T)factory(serviceProvider);");
            sourceBuilder.AppendLine("                _handlerCache.TryAdd(type, newHandler);");
            sourceBuilder.AppendLine("                return newHandler;");
            sourceBuilder.AppendLine("            }");
            sourceBuilder.AppendLine();
            sourceBuilder.AppendLine("            // Fallback to service provider");
            sourceBuilder.AppendLine("            var serviceHandler = serviceProvider.GetRequiredService<T>();");
            sourceBuilder.AppendLine("            _handlerCache.TryAdd(type, serviceHandler);");
            sourceBuilder.AppendLine("            return serviceHandler;");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();

            // Batch processing optimization
            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine("        /// Batch handler warming for startup performance");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        public static void WarmUpHandlers(IServiceProvider serviceProvider, params Type[] handlerTypes)");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            Parallel.ForEach(handlerTypes, handlerType =>");
            sourceBuilder.AppendLine("            {");
            sourceBuilder.AppendLine("                try");
            sourceBuilder.AppendLine("                {");
            sourceBuilder.AppendLine("                    var handler = serviceProvider.GetService(handlerType);");
            sourceBuilder.AppendLine("                    if (handler != null)");
            sourceBuilder.AppendLine("                        _handlerCache.TryAdd(handlerType, handler);");
            sourceBuilder.AppendLine("                }");
            sourceBuilder.AppendLine("                catch { /* Ignore warming failures */ }");
            sourceBuilder.AppendLine("            });");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();

            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine("        /// Registers a factory for ultra-fast handler creation");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        public static void RegisterFactory<T>(Func<IServiceProvider, T> factory) where T : class");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            _factoryCache.TryAdd(typeof(T), sp => factory(sp));");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();

            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine("        /// Clears all caches (useful for testing scenarios)");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        public static void ClearCaches()");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            _handlerCache.Clear();");
            sourceBuilder.AppendLine("            _factoryCache.Clear();");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine();

            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine("        /// Gets cache statistics for monitoring");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        public static (int HandlerCount, int FactoryCount) GetCacheStats()");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            return (_handlerCache.Count, _factoryCache.Count);");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine("    }");

            // Generate ultra-fast compile-time dispatchers for discovered handlers
            sourceBuilder.AppendLine();
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

                sourceBuilder.AppendLine($"        /// <summary>");
                sourceBuilder.AppendLine($"        /// Ultra-fast dispatch for {requestType}");
                sourceBuilder.AppendLine($"        /// </summary>");
                sourceBuilder.AppendLine($"        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]");
                sourceBuilder.AppendLine($"        public static ValueTask<{responseType}> Dispatch{requestType}(");
                sourceBuilder.AppendLine($"            {requestType} request, {handlerType} handler, CancellationToken cancellationToken)");
                sourceBuilder.AppendLine($"        {{");
                sourceBuilder.AppendLine($"            // Direct method call - zero overhead");
                sourceBuilder.AppendLine($"            return handler.{methodName}(request, cancellationToken);");
                sourceBuilder.AppendLine($"        }}");
                sourceBuilder.AppendLine();
            }

            // Generate a switch-based ultra dispatcher
            sourceBuilder.AppendLine("        /// <summary>");
            sourceBuilder.AppendLine("        /// Switch-based ultra-fast dispatch with no reflection");
            sourceBuilder.AppendLine("        /// </summary>");
            sourceBuilder.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sourceBuilder.AppendLine("        public static async ValueTask<TResponse> DispatchUltraFast<TRequest, TResponse>(");
            sourceBuilder.AppendLine("            TRequest request, IServiceProvider serviceProvider, CancellationToken cancellationToken)");
            sourceBuilder.AppendLine("            where TRequest : class, Relay.Core.IRequest<TResponse>");
            sourceBuilder.AppendLine("        {");
            sourceBuilder.AppendLine("            // Ultra-fast type-based switching");
            sourceBuilder.AppendLine("            var requestType = typeof(TRequest);");
            sourceBuilder.AppendLine("            return requestType.Name switch");
            sourceBuilder.AppendLine("            {");

            foreach (var handler in discoveryResult.Handlers.Take(5))
            {
                var requestType = handler.RequestType?.Name ?? "UnknownRequest";
                var handlerType = handler.HandlerType?.Name ?? "UnknownHandler";
                sourceBuilder.AppendLine($"                \"{requestType}\" => Unsafe.As<TResponse>((object)await Dispatch{requestType}(");
                sourceBuilder.AppendLine($"                    Unsafe.As<{requestType}>((object)request),");
                sourceBuilder.AppendLine($"                    serviceProvider.GetRequiredService<{handlerType}>(),");
                sourceBuilder.AppendLine($"                    cancellationToken)),");
            }

            sourceBuilder.AppendLine("                _ => throw new InvalidOperationException($\"Unknown request type: {requestType.Name}\")");
            sourceBuilder.AppendLine("            };");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine("    }");

            // Generate specialized relay implementation
            sourceBuilder.AppendLine();
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
            sourceBuilder.AppendLine("            return UltraFastDispatcher.DispatchUltraFast<Relay.Core.IRequest<TResponse>, TResponse>(request, _serviceProvider, cancellationToken);");
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
            sourceBuilder.AppendLine("            return ValueTask.CompletedTask; // Simplified implementation");
            sourceBuilder.AppendLine("        }");
            sourceBuilder.AppendLine("    }");

            sourceBuilder.AppendLine("}");

            context.AddSource("RelayPerformanceOptimizations.g.cs", sourceBuilder.ToString());
            GeneratorLogger.LogDebug(context, "Generated enhanced performance optimizations");
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
            sourceBuilder.AppendLine("        public const bool OptimizationsEnabled = true;");
            sourceBuilder.AppendLine("        public const bool ValueTaskOptimized = true;");
            sourceBuilder.AppendLine("        public const bool DirectCallsEnabled = true;");
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
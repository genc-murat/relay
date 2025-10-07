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

            // Create pipeline for class declarations that implement handler interfaces
            var handlerClasses = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsCandidateHandlerClass(s),
                    transform: static (ctx, _) => GetSemanticHandlerInfo(ctx))
                .Where(static h => h is not null);

            // Create pipeline for methods with Relay attributes (for missing reference detection)
            var relayAttributeMethods = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsMethodWithRelayAttribute(s),
                    transform: static (ctx, _) => ctx.Node)
                .Where(static m => m is not null);

            // Create pipeline for compilation data
            var compilationAndHandlers = context.CompilationProvider.Combine(handlerClasses.Collect());
            var compilationAndAttributeMethods = context.CompilationProvider.Combine(relayAttributeMethods.Collect());

            // Register source output
            context.RegisterSourceOutput(compilationAndHandlers, Execute);
            
            // Register diagnostic output for missing reference detection
            context.RegisterSourceOutput(compilationAndAttributeMethods, CheckForMissingRelayCoreReference);
        }

        private static bool IsCandidateHandlerClass(SyntaxNode node)
        {
            // Look for class declarations that might implement handler interfaces
            if (node is ClassDeclarationSyntax classDecl)
            {
                // Check if class has base list (implements interfaces)
                if (classDecl.BaseList?.Types.Count > 0)
                {
                    return classDecl.BaseList.Types
                        .Any(baseType => IsHandlerInterface(baseType.Type.ToString()));
                }
            }
            return false;
        }

        private static bool IsMethodWithRelayAttribute(SyntaxNode node)
        {
            if (node is MethodDeclarationSyntax method)
            {
                return method.AttributeLists
                    .SelectMany(al => al.Attributes)
                    .Any(attr => IsRelayAttributeName(attr.Name.ToString()));
            }
            return false;
        }

        private static bool IsRelayAttributeName(string attributeName)
        {
            // Remove the "Attribute" suffix if present
            var name = attributeName.EndsWith("Attribute") 
                ? attributeName.Substring(0, attributeName.Length - 9) 
                : attributeName;

            return name switch
            {
                "Handle" => true,
                "Notification" => true,
                "Pipeline" => true,
                "ExposeAsEndpoint" => true,
                _ => false
            };
        }

        private static void CheckForMissingRelayCoreReference(SourceProductionContext context, (Compilation Left, ImmutableArray<SyntaxNode?> Right) source)
        {
            var (compilation, attributeMethods) = source;
            
            // If there are methods with Relay attributes, check if Relay.Core is referenced
            if (attributeMethods.Length > 0)
            {
                var hasRelayCoreReference = compilation.ReferencedAssemblyNames
                    .Any(name => name.Name.Equals("Relay.Core", StringComparison.OrdinalIgnoreCase));

                if (!hasRelayCoreReference)
                {
                    // Report missing Relay.Core reference
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.MissingRelayCoreReference,
                            Location.None));
                }
            }
        }

        private static bool IsHandlerInterface(string typeName)
        {
            return typeName.Contains("IRequestHandler") ||
                   typeName.Contains("INotificationHandler") ||
                   typeName.Contains("IStreamHandler");
        }

        private static HandlerClassInfo? GetSemanticHandlerInfo(GeneratorSyntaxContext context)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            
            if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                return null;

            var handlerInfo = new HandlerClassInfo
            {
                ClassDeclaration = classDeclaration,
                ClassSymbol = classSymbol,
                ImplementedInterfaces = new List<HandlerInterfaceInfo>()
            };

            // Analyze implemented interfaces
            foreach (var interfaceSymbol in classSymbol.AllInterfaces)
            {
                var interfaceName = interfaceSymbol.ToDisplayString();
                
                if (IsRequestHandlerInterface(interfaceSymbol))
                {
                    var genericArgs = interfaceSymbol.TypeArguments;
                    handlerInfo.ImplementedInterfaces.Add(new HandlerInterfaceInfo
                    {
                        InterfaceType = HandlerType.Request,
                        InterfaceSymbol = interfaceSymbol,
                        RequestType = genericArgs.Length > 0 ? genericArgs[0] : null,
                        ResponseType = genericArgs.Length > 1 ? genericArgs[1] : null
                    });
                }
                else if (IsNotificationHandlerInterface(interfaceSymbol))
                {
                    var genericArgs = interfaceSymbol.TypeArguments;
                    handlerInfo.ImplementedInterfaces.Add(new HandlerInterfaceInfo
                    {
                        InterfaceType = HandlerType.Notification,
                        InterfaceSymbol = interfaceSymbol,
                        RequestType = genericArgs.Length > 0 ? genericArgs[0] : null
                    });
                }
                else if (IsStreamHandlerInterface(interfaceSymbol))
                {
                    var genericArgs = interfaceSymbol.TypeArguments;
                    handlerInfo.ImplementedInterfaces.Add(new HandlerInterfaceInfo
                    {
                        InterfaceType = HandlerType.Stream,
                        InterfaceSymbol = interfaceSymbol,
                        RequestType = genericArgs.Length > 0 ? genericArgs[0] : null,
                        ResponseType = genericArgs.Length > 1 ? genericArgs[1] : null
                    });
                }
            }

            return handlerInfo.ImplementedInterfaces.Count > 0 ? handlerInfo : null;
        }

        private static bool IsRequestHandlerInterface(INamedTypeSymbol interfaceSymbol)
        {
            var fullName = interfaceSymbol.ToDisplayString();
            return fullName.StartsWith("Relay.Core.Contracts.Handlers.IRequestHandler<") ||
                   (interfaceSymbol.Name == "IRequestHandler" && interfaceSymbol.ContainingNamespace?.ToDisplayString() == "Relay.Core.Contracts.Handlers");
        }

        private static bool IsNotificationHandlerInterface(INamedTypeSymbol interfaceSymbol)
        {
            var fullName = interfaceSymbol.ToDisplayString();
            return fullName.StartsWith("Relay.Core.Contracts.Handlers.INotificationHandler<") ||
                   (interfaceSymbol.Name == "INotificationHandler" && interfaceSymbol.ContainingNamespace?.ToDisplayString() == "Relay.Core.Contracts.Handlers");
        }

        private static bool IsStreamHandlerInterface(INamedTypeSymbol interfaceSymbol)
        {
            var fullName = interfaceSymbol.ToDisplayString();
            return fullName.StartsWith("Relay.Core.Contracts.Handlers.IStreamHandler<") ||
                   (interfaceSymbol.Name == "IStreamHandler" && interfaceSymbol.ContainingNamespace?.ToDisplayString() == "Relay.Core.Contracts.Handlers");
        }

        private static void Execute(SourceProductionContext context, (Compilation Left, ImmutableArray<HandlerClassInfo?> Right) source)
        {
            try
            {
                var (compilation, handlerClasses) = source;
                
                var validHandlers = handlerClasses.Where(h => h != null).ToList();
                
                if (validHandlers.Count == 0)
                {
                    // No handlers found, generate basic AddRelay method
                    GenerateBasicAddRelayMethod(context);
                    return;
                }

                // Generate DI registration
                GenerateDIRegistrations(context, validHandlers);

                // Generate optimized dispatchers
                GenerateOptimizedDispatchers(context, validHandlers);

            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.GeneratorError,
                        Location.None,
                        $"Source generator error: {ex.Message}"));
            }
        }

        private static void GenerateBasicAddRelayMethod(SourceProductionContext context)
        {
            var source = GenerateBasicAddRelaySource();
            context.AddSource("RelayRegistration.g.cs", source);
        }

        private static void GenerateDIRegistrations(SourceProductionContext context, List<HandlerClassInfo?> handlerClasses)
        {
            var source = GenerateDIRegistrationSource(handlerClasses);
            context.AddSource("RelayRegistration.g.cs", source);
        }

        private static void GenerateOptimizedDispatchers(SourceProductionContext context, List<HandlerClassInfo?> handlerClasses)
        {
            // Generate optimized request dispatcher
            var requestDispatcherSource = GenerateOptimizedRequestDispatcher(handlerClasses);
            context.AddSource("OptimizedRequestDispatcher.g.cs", requestDispatcherSource);
        }

        private static string GenerateBasicAddRelaySource()
        {
            return @"// <auto-generated />
// Generated by Relay.SourceGenerator - No handlers found

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Relay.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for registering Relay services with the DI container.
    /// Generated when no handlers are found.
    /// </summary>
    public static class GeneratedRelayExtensions
    {
        /// <summary>
        /// Registers basic Relay services with the DI container (no handlers found).
        /// </summary>
        /// <param name=""services"">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddRelayGenerated(this IServiceCollection services)
        {
            // Register core Relay services
            services.TryAddTransient<Relay.Core.Contracts.Core.IRelay, Relay.Core.Implementation.Core.RelayImplementation>();
            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.IRequestDispatcher, Relay.Core.Implementation.Fallback.FallbackRequestDispatcher>();
            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.IStreamDispatcher, Relay.Core.Implementation.Dispatchers.StreamDispatcher>();
            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.INotificationDispatcher, Relay.Core.Implementation.Dispatchers.NotificationDispatcher>();

            return services;
        }
    }
}";
        }

        private static string GenerateDIRegistrationSource(List<HandlerClassInfo?> handlerClasses)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("// Generated by Relay.SourceGenerator");
            sb.AppendLine($"// Generation time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection.Extensions;");
            sb.AppendLine("using Relay.Core;");
            sb.AppendLine();
            sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Extension methods for registering Relay services with the DI container.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static partial class RelayServiceCollectionExtensions");
            sb.AppendLine("    {");
            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Registers all Relay handlers and services with the DI container.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        /// <param name=\"services\">The service collection.</param>");
            sb.AppendLine("        /// <returns>The service collection for chaining.</returns>");
            sb.AppendLine("        public static IServiceCollection AddRelay(this IServiceCollection services)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Register core Relay services");
            sb.AppendLine("            services.TryAddTransient<Relay.Core.Contracts.Core.IRelay, Relay.Core.Implementation.Core.RelayImplementation>();");
            sb.AppendLine("            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.IRequestDispatcher, Relay.Core.Implementation.Fallback.FallbackRequestDispatcher>();");
            sb.AppendLine("            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.IStreamDispatcher, Relay.Core.Implementation.Dispatchers.StreamDispatcher>();");
            sb.AppendLine("            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.INotificationDispatcher, Relay.Core.Implementation.Dispatchers.NotificationDispatcher>();");
            sb.AppendLine();
            sb.AppendLine("            // Register all discovered handlers");

            foreach (var handlerClass in handlerClasses.Where(h => h != null))
            {
                var className = handlerClass!.ClassSymbol.ToDisplayString();
                
                foreach (var interfaceInfo in handlerClass.ImplementedInterfaces)
                {
                    var interfaceTypeName = interfaceInfo.InterfaceSymbol.ToDisplayString();
                    sb.AppendLine($"            services.AddTransient<{interfaceTypeName}, {className}>();");
                }
            }

            sb.AppendLine();
            sb.AppendLine("            return services;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string GenerateOptimizedRequestDispatcher(List<HandlerClassInfo?> handlerClasses)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("// Generated by Relay.SourceGenerator - Optimized Request Dispatcher");
            sb.AppendLine();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine("using System.Threading;");
            sb.AppendLine("using System.Threading.Tasks;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using Relay.Core;");
            sb.AppendLine();
            sb.AppendLine("namespace Relay.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// High-performance generated request dispatcher");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public class GeneratedRequestDispatcher : BaseRequestDispatcher");
            sb.AppendLine("    {");
            sb.AppendLine("        public GeneratedRequestDispatcher(IServiceProvider serviceProvider) : base(serviceProvider) { }");
            sb.AppendLine();
            sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine("        public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)");
            sb.AppendLine("        {");
            sb.AppendLine("            return request switch");
            sb.AppendLine("            {");

            // Generate switch cases for each request type
            var requestHandlers = handlerClasses
                .Where(h => h != null)
                .SelectMany(h => h!.ImplementedInterfaces)
                .Where(i => i.InterfaceType == HandlerType.Request)
                .GroupBy(i => i.RequestType?.ToDisplayString())
                .ToList();

            foreach (var group in requestHandlers)
            {
                var requestType = group.Key;
                if (string.IsNullOrEmpty(requestType)) continue;

                var handlerInterface = group.First();
                var responseType = handlerInterface.ResponseType?.ToDisplayString() ?? "object";
                
                sb.AppendLine($"                {requestType} req when request is {requestType} => ");
                sb.AppendLine($"                    (ValueTask<TResponse>)(object)ServiceProvider.GetRequiredService<IRequestHandler<{requestType}, {responseType}>>().HandleAsync(req, cancellationToken),");
            }

            sb.AppendLine("                _ => ValueTask.FromException<TResponse>(new HandlerNotFoundException(request.GetType().Name))");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)");
            sb.AppendLine("        {");
            sb.AppendLine("            throw new NotImplementedException(\"Void requests not yet supported in generated dispatcher\");");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)");
            sb.AppendLine("        {");
            sb.AppendLine("            throw new NotImplementedException(\"Named handlers not yet supported in generated dispatcher\");");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public override ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)");
            sb.AppendLine("        {");
            sb.AppendLine("            throw new NotImplementedException(\"Named handlers not yet supported in generated dispatcher\");");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }

    // Supporting classes
    public class HandlerClassInfo
    {
        public ClassDeclarationSyntax? ClassDeclaration { get; set; }
        public INamedTypeSymbol? ClassSymbol { get; set; }
        public List<HandlerInterfaceInfo> ImplementedInterfaces { get; set; } = new();
    }

    public class HandlerInterfaceInfo
    {
        public HandlerType InterfaceType { get; set; }
        public INamedTypeSymbol? InterfaceSymbol { get; set; }
        public ITypeSymbol? RequestType { get; set; }
        public ITypeSymbol? ResponseType { get; set; }
    }

    public enum HandlerType
    {
        Request,
        Notification,
        Stream
    }
}
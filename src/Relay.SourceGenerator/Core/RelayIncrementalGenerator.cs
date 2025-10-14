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

        // StringBuilder pool to reduce allocations
        [ThreadStatic]
        private static StringBuilder? t_cachedStringBuilder;

        private static StringBuilder GetStringBuilder()
        {
            var sb = t_cachedStringBuilder;
            if (sb != null)
            {
                t_cachedStringBuilder = null;
                sb.Clear();
                return sb;
            }
            return new StringBuilder(1024); // Start with reasonable capacity
        }

        private static void ReturnStringBuilder(StringBuilder sb)
        {
            if (sb.Capacity <= 4096) // Don't cache if too large
            {
                t_cachedStringBuilder = sb;
            }
        }

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
            if (node is not ClassDeclarationSyntax classDecl)
                return false;

            // Early exit: Check if class has base list (implements interfaces)
            var baseList = classDecl.BaseList;
            if (baseList == null || baseList.Types.Count == 0)
                return false;

            // Optimized: avoid LINQ for hot path
            foreach (var baseType in baseList.Types)
            {
                if (IsHandlerInterface(baseType.Type.ToString()))
                    return true;
            }

            return false;
        }

        private static bool IsMethodWithRelayAttribute(SyntaxNode node)
        {
            if (node is not MethodDeclarationSyntax method)
                return false;

            // Early exit if no attributes
            if (method.AttributeLists.Count == 0)
                return false;

            // Optimized: avoid LINQ overhead
            foreach (var attributeList in method.AttributeLists)
            {
                foreach (var attr in attributeList.Attributes)
                {
                    if (IsRelayAttributeName(attr.Name.ToString()))
                        return true;
                }
            }

            return false;
        }

        private static bool IsRelayAttributeName(string attributeName)
        {
            if (string.IsNullOrEmpty(attributeName))
                return false;

            // Remove the "Attribute" suffix if present
            var name = attributeName.EndsWith("Attribute") 
                ? attributeName.Substring(0, attributeName.Length - 9) 
                : attributeName;

            // Optimized switch statement - most common cases first
            switch (name)
            {
                case "Handle":
                case "Notification":
                case "Pipeline":
                case "ExposeAsEndpoint":
                    return true;
                default:
                    return false;
            }
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
            var sb = GetStringBuilder();
            
            try
            {
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
                sb.AppendLine("            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.IRequestDispatcher, Relay.Generated.GeneratedRequestDispatcher>();");
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
                        sb.Append("            services.AddTransient<");
                        sb.Append(interfaceTypeName);
                        sb.Append(", ");
                        sb.Append(className);
                        sb.AppendLine(">();");
                    }
                }

                sb.AppendLine();
                sb.AppendLine("            return services;");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");

                return sb.ToString();
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }

        private static string GenerateOptimizedRequestDispatcher(List<HandlerClassInfo?> handlerClasses)
        {
            var sb = GetStringBuilder();
            
            try
            {
                sb.AppendLine("// <auto-generated />");
                sb.AppendLine("// Generated by Relay.SourceGenerator - Optimized Request Dispatcher");
                sb.AppendLine();
                sb.AppendLine("#nullable disable");
                sb.AppendLine();
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Runtime.CompilerServices;");
                sb.AppendLine("using System.Threading;");
                sb.AppendLine("using System.Threading.Tasks;");
                sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
                sb.AppendLine("using Relay.Core;");
                sb.AppendLine("using Relay.Core.Contracts;");
                sb.AppendLine("using Relay.Core.Contracts.Requests;");
                sb.AppendLine("using Relay.Core.Contracts.Handlers;");
                sb.AppendLine("using Relay.Core.Contracts.Dispatchers;");
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

                // Generate switch cases for requests with a response
                var responseRequestHandlers = handlerClasses
                    .Where(h => h != null)
                    .SelectMany(h => h!.ImplementedInterfaces)
                    .Where(i => i.InterfaceType == HandlerType.Request && i.ResponseType != null)
                    .GroupBy(i => i.RequestType?.ToDisplayString())
                    .ToList();

                foreach (var group in responseRequestHandlers)
                {
                    var requestType = group.Key;
                    if (string.IsNullOrEmpty(requestType)) continue;

                    var handlerInterface = group.First();
                    var responseType = handlerInterface.ResponseType?.ToDisplayString();
                    if (string.IsNullOrEmpty(responseType)) continue;
                    
                    sb.Append("                ");
                    sb.Append(requestType);
                    sb.Append(" req when request is ");
                    sb.Append(requestType);
                    sb.AppendLine(" => ");
                    sb.Append("                    (ValueTask<TResponse>)(object)ServiceProvider.GetRequiredService<IRequestHandler<");
                    sb.Append(requestType);
                    sb.Append(", ");
                    sb.Append(responseType);
                    sb.AppendLine(">>().HandleAsync(req, cancellationToken),");
                }

                sb.AppendLine("                _ => ValueTask.FromException<TResponse>(new HandlerNotFoundException(request.GetType().Name))");
                sb.AppendLine("            };");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        public override ValueTask DispatchAsync(IRequest request, CancellationToken cancellationToken)");
                sb.AppendLine("        {");
                sb.AppendLine("            return request switch");
                sb.AppendLine("            {");

                // Generate switch cases for void requests
                var voidRequestHandlers = handlerClasses
                    .Where(h => h != null)
                    .SelectMany(h => h!.ImplementedInterfaces)
                    .Where(i => i.InterfaceType == HandlerType.Request && i.ResponseType == null)
                    .GroupBy(i => i.RequestType?.ToDisplayString())
                    .ToList();

                foreach (var group in voidRequestHandlers)
                {
                    var requestType = group.Key;
                    if (string.IsNullOrEmpty(requestType)) continue;

                    sb.Append("                ");
                    sb.Append(requestType);
                    sb.Append(" req => ServiceProvider.GetRequiredService<IRequestHandler<");
                    sb.Append(requestType);
                    sb.AppendLine(">>().HandleAsync(req, cancellationToken),");
                }

                sb.AppendLine("                _ => ValueTask.FromException(new HandlerNotFoundException(request.GetType().Name))");
                sb.AppendLine("            };");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        public override ValueTask<TResponse> DispatchAsync<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)");
                sb.AppendLine("        {");
                sb.AppendLine("            ValidateRequest(request);");
                sb.AppendLine("            ValidateHandlerName(handlerName);");
                sb.AppendLine();
                sb.AppendLine("            return DispatchNamedRequestWithResponse(request, handlerName, cancellationToken);");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        public override ValueTask DispatchAsync(IRequest request, string handlerName, CancellationToken cancellationToken)");
                sb.AppendLine("        {");
                sb.AppendLine("            ValidateRequest(request);");
                sb.AppendLine("            ValidateHandlerName(handlerName);");
                sb.AppendLine();
                sb.AppendLine("            return DispatchNamedRequestVoid(request, handlerName, cancellationToken);");
                sb.AppendLine("        }");
                sb.AppendLine();

                // Generate helper methods for named handler dispatch
                sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.AppendLine("        private ValueTask<TResponse> DispatchNamedRequestWithResponse<TResponse>(IRequest<TResponse> request, string handlerName, CancellationToken cancellationToken)");
                sb.AppendLine("        {");
                sb.AppendLine("            return request switch");
                sb.AppendLine("            {");

                // Generate switch cases for named requests with a response
                foreach (var group in responseRequestHandlers)
                {
                    var requestType = group.Key;
                    if (string.IsNullOrEmpty(requestType)) continue;

                    var handlerInterface = group.First();
                    var responseType = handlerInterface.ResponseType?.ToDisplayString();
                    if (string.IsNullOrEmpty(responseType)) continue;

                    sb.Append("                ");
                    sb.Append(requestType);
                    sb.Append(" req when request is ");
                    sb.Append(requestType);
                    sb.AppendLine(" => ");
                    sb.Append("                    (ValueTask<TResponse>)(object)DispatchNamedHandlerWithResponse<");
                    sb.Append(requestType);
                    sb.Append(", ");
                    sb.Append(responseType);
                    sb.Append(">(req, handlerName, typeof(");
                    sb.Append(requestType);
                    sb.Append("), typeof(");
                    // Strip nullable annotation for typeof
                    sb.Append(responseType.TrimEnd('?'));
                    sb.AppendLine("), cancellationToken),");
                }

                sb.AppendLine("                _ => ValueTask.FromException<TResponse>(CreateHandlerNotFoundException(request.GetType(), handlerName))");
                sb.AppendLine("            };");
                sb.AppendLine("        }");
                sb.AppendLine();

                sb.AppendLine("        [MethodImpl(MethodImplOptions.AggressiveInlining)]");
                sb.AppendLine("        private ValueTask DispatchNamedRequestVoid(IRequest request, string handlerName, CancellationToken cancellationToken)");
                sb.AppendLine("        {");
                sb.AppendLine("            return request switch");
                sb.AppendLine("            {");

                // Generate switch cases for named void requests
                foreach (var group in voidRequestHandlers)
                {
                    var requestType = group.Key;
                    if (string.IsNullOrEmpty(requestType)) continue;

                    sb.Append("                ");
                    sb.Append(requestType);
                    sb.Append(" req => DispatchNamedHandlerVoid<");
                    sb.Append(requestType);
                    sb.Append(">(req, handlerName, typeof(");
                    sb.Append(requestType);
                    sb.AppendLine("), cancellationToken),");
                }

                sb.AppendLine("                _ => ValueTask.FromException(CreateHandlerNotFoundException(request.GetType(), handlerName))");
                sb.AppendLine("            };");
                sb.AppendLine("        }");
                sb.AppendLine();

                // Generate generic named handler resolution methods
                sb.AppendLine("        private async ValueTask<TResponse> DispatchNamedHandlerWithResponse<TRequest, TResponse>(");
                sb.AppendLine("            TRequest request, string handlerName, Type requestType, Type responseType, CancellationToken cancellationToken)");
                sb.AppendLine("            where TRequest : IRequest<TResponse>");
                sb.AppendLine("        {");
                sb.AppendLine("            // Try to get keyed service first (for .NET 8+ named services)");
                sb.AppendLine("            var handlerInterfaceType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);");
                sb.AppendLine("            ");
                sb.AppendLine("            // Attempt keyed service resolution");
                sb.AppendLine("            var keyedProvider = ServiceProvider.GetService<Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider>();");
                sb.AppendLine("            if (keyedProvider != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                var keyedHandler = keyedProvider.GetKeyedService(handlerInterfaceType, handlerName);");
                sb.AppendLine("                if (keyedHandler != null)");
                sb.AppendLine("                {");
                sb.AppendLine("                    var handler = (IRequestHandler<TRequest, TResponse>)keyedHandler;");
                sb.AppendLine("                    return await handler.HandleAsync(request, cancellationToken);");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            // Fallback: throw handler not found exception");
                sb.AppendLine("            throw CreateHandlerNotFoundException(requestType, handlerName);");
                sb.AppendLine("        }");
                sb.AppendLine();

                sb.AppendLine("        private async ValueTask DispatchNamedHandlerVoid<TRequest>(");
                sb.AppendLine("            TRequest request, string handlerName, Type requestType, CancellationToken cancellationToken)");
                sb.AppendLine("            where TRequest : IRequest");
                sb.AppendLine("        {");
                sb.AppendLine("            // Try to get keyed service first (for .NET 8+ named services)");
                sb.AppendLine("            var handlerInterfaceType = typeof(IRequestHandler<>).MakeGenericType(requestType);");
                sb.AppendLine("            ");
                sb.AppendLine("            // Attempt keyed service resolution");
                sb.AppendLine("            var keyedProvider = ServiceProvider.GetService<Microsoft.Extensions.DependencyInjection.IKeyedServiceProvider>();");
                sb.AppendLine("            if (keyedProvider != null)");
                sb.AppendLine("            {");
                sb.AppendLine("                var keyedHandler = keyedProvider.GetKeyedService(handlerInterfaceType, handlerName);");
                sb.AppendLine("                if (keyedHandler != null)");
                sb.AppendLine("                {");
                sb.AppendLine("                    var handler = (IRequestHandler<TRequest>)keyedHandler;");
                sb.AppendLine("                    await handler.HandleAsync(request, cancellationToken);");
                sb.AppendLine("                    return;");
                sb.AppendLine("                }");
                sb.AppendLine("            }");
                sb.AppendLine();
                sb.AppendLine("            // Fallback: throw handler not found exception");
                sb.AppendLine("            throw CreateHandlerNotFoundException(requestType, handlerName);");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");

                return sb.ToString();
            }
            finally
            {
                ReturnStringBuilder(sb);
            }
        }
    }
}
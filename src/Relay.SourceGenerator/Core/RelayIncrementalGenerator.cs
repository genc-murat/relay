using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Relay.SourceGenerator.Core;

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

    // Pool configuration for modern code generation workloads
    private const int DefaultStringBuilderCapacity = 1024;      // 1KB initial
    private const int MaxPooledCapacity = 16 * 1024;             // 16KB maximum

    // Test hook to force exception for coverage
    [ThreadStatic]
    internal static bool TestForceException;

    private static StringBuilder GetStringBuilder()
    {
        var sb = t_cachedStringBuilder;
        if (sb != null)
        {
            t_cachedStringBuilder = null;
            sb.Clear();
            return sb;
        }
        return new StringBuilder(DefaultStringBuilderCapacity);
    }

    private static void ReturnStringBuilder(StringBuilder sb)
    {
        // Cache StringBuilder if it's within reasonable size limits
        // Modern generated code often exceeds 4KB, so we allow up to 16KB
        if (sb.Capacity <= MaxPooledCapacity)
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

        // Create configuration pipeline with value-based equality for incremental caching
        var configuration = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) => ParseConfiguration(options))
            .WithComparer(Configuration.RelayConfigurationComparer.Instance);

        // Create pipeline for class declarations that implement handler interfaces
        // Use value-based equality comparer for efficient incremental caching
        var handlerClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateHandlerClass(s),
                transform: static (ctx, _) => GetSemanticHandlerInfo(ctx))
            .Where(static h => h is not null)
            .WithComparer(HandlerClassInfoComparer.Instance);

        // Create pipeline for methods with Relay attributes (for missing reference detection)
        var relayAttributeMethods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsMethodWithRelayAttribute(s),
                transform: static (ctx, _) => ctx.Node)
            .Where(static m => m != null);

        // Combine pipelines: compilation + handlers + configuration
        var compilationAndHandlers = context.CompilationProvider
            .Combine(handlerClasses.Collect())
            .Combine(configuration);
        
        var compilationAndAttributeMethods = context.CompilationProvider.Combine(relayAttributeMethods.Collect());

        // Register source output with configuration
        context.RegisterSourceOutput(compilationAndHandlers, Execute);
        
        // Register diagnostic output for missing reference detection
        context.RegisterSourceOutput(compilationAndAttributeMethods, CheckForMissingRelayCoreReference);
    }

    internal static Configuration.RelayConfiguration ParseConfiguration(AnalyzerConfigOptionsProvider options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var generationOptions = Generators.MSBuildConfigurationHelper.CreateFromMSBuildProperties(options);

        return new Configuration.RelayConfiguration
        {
            Options = generationOptions,
            CreatedAt = DateTime.UtcNow
        };
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

    internal static bool IsRelayAttributeName(string attributeName)
    {
        if (string.IsNullOrEmpty(attributeName))
            return false;

        // Remove the "Attribute" suffix if present
        var name = attributeName.EndsWith("Attribute") 
            ? attributeName.Substring(0, attributeName.Length - 9) 
            : attributeName;

        // Optimized switch statement - most common cases first
        return name switch
        {
            "Handle" or "Notification" or "Pipeline" or "ExposeAsEndpoint" => true,
            _ => false,
        };
    }

    private static void CheckForMissingRelayCoreReference(SourceProductionContext context, (Compilation Left, ImmutableArray<SyntaxNode> Right) source)
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
                var reporter = new SourceOutputDiagnosticReporter(context);
                reporter.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MissingRelayCoreReference,
                        Location.None));
            }
        }
    }

    internal static bool IsHandlerInterface(string typeName)
    {
        return typeName.Contains("IRequestHandler") ||
               typeName.Contains("INotificationHandler") ||
               typeName.Contains("IStreamHandler");
    }

    internal static HandlerClassInfo? GetSemanticHandlerInfo(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
            return null;

        var implementedInterfaces = new List<HandlerInterfaceInfo>();

        // Analyze implemented interfaces
        foreach (var interfaceSymbol in classSymbol.AllInterfaces)
        {
            if (IsRequestHandlerInterface(interfaceSymbol))
            {
                var genericArgs = interfaceSymbol.TypeArguments;
                implementedInterfaces.Add(new HandlerInterfaceInfo
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
                implementedInterfaces.Add(new HandlerInterfaceInfo
                {
                    InterfaceType = HandlerType.Notification,
                    InterfaceSymbol = interfaceSymbol,
                    RequestType = genericArgs.Length > 0 ? genericArgs[0] : null
                });
            }
            else if (IsStreamHandlerInterface(interfaceSymbol))
            {
                var genericArgs = interfaceSymbol.TypeArguments;
                implementedInterfaces.Add(new HandlerInterfaceInfo
                {
                    InterfaceType = HandlerType.Stream,
                    InterfaceSymbol = interfaceSymbol,
                    RequestType = genericArgs.Length > 0 ? genericArgs[0] : null,
                    ResponseType = genericArgs.Length > 1 ? genericArgs[1] : null
                });
            }
        }

        if (implementedInterfaces.Count == 0)
            return null;

        return new HandlerClassInfo
        {
            ClassDeclaration = classDeclaration,
            ClassSymbol = classSymbol,
            ImplementedInterfaces = implementedInterfaces
        };
    }

    // Test helper method to allow direct testing of the logic that processes classSymbol.AllInterfaces
    internal static HandlerClassInfo? ProcessClassSymbol(INamedTypeSymbol classSymbol, ClassDeclarationSyntax classDeclaration)
    {
        var implementedInterfaces = new List<HandlerInterfaceInfo>();

        // Analyze implemented interfaces - this is the key foreach loop that processes classSymbol.AllInterfaces
        foreach (var interfaceSymbol in classSymbol.AllInterfaces)
        {
            if (IsRequestHandlerInterface(interfaceSymbol))
            {
                var genericArgs = interfaceSymbol.TypeArguments;
                implementedInterfaces.Add(new HandlerInterfaceInfo
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
                implementedInterfaces.Add(new HandlerInterfaceInfo
                {
                    InterfaceType = HandlerType.Notification,
                    InterfaceSymbol = interfaceSymbol,
                    RequestType = genericArgs.Length > 0 ? genericArgs[0] : null
                });
            }
            else if (IsStreamHandlerInterface(interfaceSymbol))
            {
                var genericArgs = interfaceSymbol.TypeArguments;
                implementedInterfaces.Add(new HandlerInterfaceInfo
                {
                    InterfaceType = HandlerType.Stream,
                    InterfaceSymbol = interfaceSymbol,
                    RequestType = genericArgs.Length > 0 ? genericArgs[0] : null,
                    ResponseType = genericArgs.Length > 1 ? genericArgs[1] : null
                });
            }
        }

        if (implementedInterfaces.Count == 0)
            return null;

        return new HandlerClassInfo
        {
            ClassDeclaration = classDeclaration,
            ClassSymbol = classSymbol,
            ImplementedInterfaces = implementedInterfaces
        };
    }

    internal static bool IsRequestHandlerInterface(INamedTypeSymbol interfaceSymbol)
    {
        var fullName = interfaceSymbol.ToDisplayString();
        return fullName.StartsWith("Relay.Core.Contracts.Handlers.IRequestHandler<") ||
               (interfaceSymbol.Name == "IRequestHandler" && interfaceSymbol.ContainingNamespace?.ToDisplayString() == "Relay.Core.Contracts.Handlers");
    }

    internal static bool IsNotificationHandlerInterface(INamedTypeSymbol interfaceSymbol)
    {
        var fullName = interfaceSymbol.ToDisplayString();
        return fullName.StartsWith("Relay.Core.Contracts.Handlers.INotificationHandler<") ||
               (interfaceSymbol.Name == "INotificationHandler" && interfaceSymbol.ContainingNamespace?.ToDisplayString() == "Relay.Core.Contracts.Handlers");
    }

    internal static bool IsStreamHandlerInterface(INamedTypeSymbol interfaceSymbol)
    {
        var fullName = interfaceSymbol.ToDisplayString();
        return fullName.StartsWith("Relay.Core.Contracts.Handlers.IStreamHandler<") ||
               (interfaceSymbol.Name == "IStreamHandler" && interfaceSymbol.ContainingNamespace?.ToDisplayString() == "Relay.Core.Contracts.Handlers");
    }

    internal static void Execute(SourceProductionContext context, ((Compilation Left, ImmutableArray<HandlerClassInfo?> Right) Left, Configuration.RelayConfiguration Right) source)
    {
        try
        {
            var ((compilation, handlerClasses), configuration) = source;
            var options = configuration.Options;
            
            var validHandlers = handlerClasses.Where(h => h != null).ToList();
            
            if (validHandlers.Count == 0)
            {
                // No handlers found, generate basic AddRelay method if DI generation is enabled
                if (options.EnableDIGeneration)
                {
                    GenerateBasicAddRelayMethod(context);
                }
                return;
            }

            // Generate DI registration if enabled
            if (options.EnableDIGeneration)
            {
                GenerateDIRegistrations(context, validHandlers, options);
            }

            // Generate optimized dispatchers if enabled
            if (options.EnableOptimizedDispatcher)
            {
                GenerateOptimizedDispatchers(context, validHandlers, options);
            }

        }
        catch (Exception ex)
        {
            var reporter = new SourceOutputDiagnosticReporter(context);
            reporter.ReportDiagnostic(
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

    private static void GenerateDIRegistrations(SourceProductionContext context, List<HandlerClassInfo?> handlerClasses, Generators.GenerationOptions options)
    {
        var validHandlers = handlerClasses.Where(h => h != null).Cast<HandlerClassInfo>().ToList();
        var source = GenerateDIRegistrationSource(validHandlers, options);
        context.AddSource("RelayRegistration.g.cs", source);
    }

    private static void GenerateOptimizedDispatchers(SourceProductionContext context, List<HandlerClassInfo?> handlerClasses, Generators.GenerationOptions options)
    {
        var validHandlers = handlerClasses.Where(h => h != null).Cast<HandlerClassInfo>().ToList();
        // Generate optimized request dispatcher
        var requestDispatcherSource = GenerateOptimizedRequestDispatcher(validHandlers, options);
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

    private static string GenerateDIRegistrationSource(List<HandlerClassInfo> handlerClasses, Generators.GenerationOptions options)
    {
        // Test hook to force exception for coverage
        if (TestForceException)
        {
            throw new InvalidOperationException("Test exception");
        }

        var sb = GetStringBuilder();
        
        try
        {
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("// Generated by Relay.SourceGenerator");
            sb.AppendLine($"// Generation time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine();
            
            if (options.EnableNullableContext)
            {
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
            }
            
            sb.AppendLine("using System;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
            sb.AppendLine("using Microsoft.Extensions.DependencyInjection.Extensions;");
            sb.AppendLine("using Relay.Core;");
            sb.AppendLine();
            sb.AppendLine("namespace Microsoft.Extensions.DependencyInjection");
            sb.AppendLine("{");
            
            if (options.IncludeDocumentation)
            {
                sb.AppendLine("    /// <summary>");
                sb.AppendLine("    /// Extension methods for registering Relay services with the DI container.");
                sb.AppendLine("    /// </summary>");
            }
            
            sb.AppendLine("    public static partial class RelayServiceCollectionExtensions");
            sb.AppendLine("    {");
            
            if (options.IncludeDocumentation)
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine("        /// Registers all Relay handlers and services with the DI container.");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine("        /// <param name=\"services\">The service collection.</param>");
                sb.AppendLine("        /// <returns>The service collection for chaining.</returns>");
            }
            
            sb.AppendLine("        public static IServiceCollection AddRelay(this IServiceCollection services)");
            sb.AppendLine("        {");
            sb.AppendLine("            // Register core Relay services");
            sb.AppendLine("            services.TryAddTransient<Relay.Core.Contracts.Core.IRelay, Relay.Core.Implementation.Core.RelayImplementation>();");
            
            if (options.EnableOptimizedDispatcher)
            {
                sb.AppendLine("            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.IRequestDispatcher, Relay.Generated.GeneratedRequestDispatcher>();");
            }
            else
            {
                sb.AppendLine("            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.IRequestDispatcher, Relay.Core.Implementation.Fallback.FallbackRequestDispatcher>();");
            }
            
            sb.AppendLine("            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.IStreamDispatcher, Relay.Core.Implementation.Dispatchers.StreamDispatcher>();");
            sb.AppendLine("            services.TryAddTransient<Relay.Core.Contracts.Dispatchers.INotificationDispatcher, Relay.Core.Implementation.Dispatchers.NotificationDispatcher>();");
            sb.AppendLine();
            sb.AppendLine("            // Register all discovered handlers");

            foreach (var handlerClass in handlerClasses)
            {
                var className = handlerClass.ClassSymbol.ToDisplayString();

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

    private static string GenerateOptimizedRequestDispatcher(List<HandlerClassInfo> handlerClasses, Generators.GenerationOptions options)
    {
        var sb = GetStringBuilder();

        try
        {
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("// Generated by Relay.SourceGenerator - Optimized Request Dispatcher");
            sb.AppendLine();
            
            if (options.EnableNullableContext)
            {
                sb.AppendLine("#nullable enable");
            }
            
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
                .SelectMany(h => h.ImplementedInterfaces)
                .Where(i => i.InterfaceType == HandlerType.Request && i.ResponseType != null && i.RequestType != null)
                .GroupBy(i => i.RequestType!.ToDisplayString())
                .ToList();

            foreach (var group in responseRequestHandlers)
            {
                var requestType = group.Key;
                var handlerInterface = group.First();
                var responseType = handlerInterface.ResponseType!.ToDisplayString();

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
                .SelectMany(h => h.ImplementedInterfaces)
                .Where(i => i.InterfaceType == HandlerType.Request && i.ResponseType == null && i.RequestType != null)
                .GroupBy(i => i.RequestType!.ToDisplayString())
                .ToList();

            foreach (var group in voidRequestHandlers)
            {
                var requestType = group.Key;

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
                var handlerInterface = group.First();
                var responseType = handlerInterface.ResponseType!.ToDisplayString();

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
                sb.Append(responseType!.TrimEnd('?'));
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
            sb.AppendLine("            where TResponse : notnull");
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
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Roslyn analyzer for Relay framework that provides compile-time validation
    /// of handler signatures, attribute usage, and configuration issues.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RelayAnalyzer : DiagnosticAnalyzer
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
        /// Gets the supported diagnostics for this analyzer.
        /// </summary>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                // Handler signature validation
                DiagnosticDescriptors.InvalidHandlerSignature,
                DiagnosticDescriptors.InvalidHandlerReturnType,
                DiagnosticDescriptors.InvalidStreamHandlerReturnType,
                DiagnosticDescriptors.InvalidNotificationHandlerReturnType,
                DiagnosticDescriptors.HandlerMissingRequestParameter,
                DiagnosticDescriptors.HandlerInvalidRequestParameter,
                DiagnosticDescriptors.HandlerMissingCancellationToken,
                DiagnosticDescriptors.NotificationHandlerMissingParameter,

                // Duplicate handler detection
                DiagnosticDescriptors.DuplicateHandler,
                DiagnosticDescriptors.NamedHandlerConflict,

                // Configuration validation
                DiagnosticDescriptors.InvalidPriorityValue,
                DiagnosticDescriptors.ConfigurationConflict,
                DiagnosticDescriptors.InvalidPipelineScope,
                DiagnosticDescriptors.DuplicatePipelineOrder,

                // Usage warnings
                DiagnosticDescriptors.UnusedHandler,
                DiagnosticDescriptors.PerformanceWarning
            );

        /// <summary>
        /// Initializes the analyzer by registering analysis actions.
        /// </summary>
        /// <param name="context">The analysis context.</param>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register syntax node actions for method declarations with Relay attributes
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);

            // Register compilation action for cross-method validation (duplicate handlers)
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        /// <summary>
        /// Analyzes method declarations for Relay attribute usage and signature validation.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            try
            {
                if (context.Node is not MethodDeclarationSyntax methodDeclaration)
                    return;

                var semanticModel = context.SemanticModel;
                var methodSymbol = TryGetDeclaredSymbol(semanticModel, methodDeclaration);

                if (methodSymbol == null)
                    return;

                // Check for Relay attributes
                var handleAttribute = GetAttribute(methodSymbol, "Relay.Core.HandleAttribute");
                var notificationAttribute = GetAttribute(methodSymbol, "Relay.Core.NotificationAttribute");
                var pipelineAttribute = GetAttribute(methodSymbol, "Relay.Core.PipelineAttribute");

                // Validate handler methods with [Handle] attribute
                if (handleAttribute != null)
                {
                    ValidateHandlerMethod(context, methodDeclaration, methodSymbol, handleAttribute);
                }

                // Validate notification handler methods with [Notification] attribute
                if (notificationAttribute != null)
                {
                    ValidateNotificationHandlerMethod(context, methodDeclaration, methodSymbol, notificationAttribute);
                }

                // Validate pipeline methods with [Pipeline] attribute
                if (pipelineAttribute != null)
                {
                    ValidatePipelineMethod(context, methodDeclaration, methodSymbol, pipelineAttribute);
                }
            }
            catch (OperationCanceledException)
            {
                // Analysis was cancelled, propagate the cancellation
                throw;
            }
            catch (Exception ex)
            {
                // Report analyzer error but don't crash the analysis
                ReportAnalyzerError(context, methodDeclaration: context.Node as MethodDeclarationSyntax, ex);
            }
        }

        /// <summary>
        /// Analyzes the entire compilation for cross-method validation like duplicate handlers.
        /// </summary>
        /// <param name="context">The compilation analysis context.</param>
        private static void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            try
            {
                var compilation = context.Compilation;
                var handlerRegistry = new HandlerRegistry();
                var pipelineRegistry = new List<PipelineInfo>();

                // Collect all handler and pipeline methods across the compilation
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    try
                    {
                        var semanticModel = TryGetSemanticModel(compilation, syntaxTree);
                        if (semanticModel == null)
                            continue;

                        var root = syntaxTree.GetRoot(context.CancellationToken);

                        var methodDeclarations = root.DescendantNodes()
                            .OfType<MethodDeclarationSyntax>();

                        foreach (var methodDeclaration in methodDeclarations)
                        {
                            var methodSymbol = TryGetDeclaredSymbol(semanticModel, methodDeclaration);
                            if (methodSymbol == null) continue;

                            var handleAttribute = GetAttribute(methodSymbol, "Relay.Core.HandleAttribute");
                            if (handleAttribute != null)
                            {
                                handlerRegistry.AddHandler(methodSymbol, handleAttribute, methodDeclaration);
                            }

                            var pipelineAttribute = GetAttribute(methodSymbol, "Relay.Core.PipelineAttribute");
                            if (pipelineAttribute != null)
                            {
                                CollectPipelineInfo(pipelineRegistry, methodSymbol, pipelineAttribute, methodDeclaration);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Analysis was cancelled, propagate the cancellation
                        throw;
                    }
                    catch (Exception)
                    {
                        // Skip this syntax tree if there's an error processing it
                        // Continue analyzing other syntax trees
                        continue;
                    }
                }

                // Validate for duplicate handlers
                ValidateDuplicateHandlers(context, handlerRegistry);

                // Validate for duplicate pipeline orders
                ValidateDuplicatePipelineOrders(context, pipelineRegistry);
            }
            catch (OperationCanceledException)
            {
                // Analysis was cancelled, propagate the cancellation
                throw;
            }
            catch (Exception ex)
            {
                // Report analyzer error but don't crash the compilation
                ReportAnalyzerError(context, ex);
            }
        }

        /// <summary>
        /// Validates a handler method signature and configuration.
        /// </summary>
        private static void ValidateHandlerMethod(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            AttributeData handleAttribute)
        {
            // Validate method signature
            ValidateHandlerSignature(context, methodDeclaration, methodSymbol);

            // Validate attribute parameters
            ValidateHandleAttributeParameters(context, methodDeclaration, handleAttribute);
        }

        /// <summary>
        /// Validates a notification handler method signature and configuration.
        /// </summary>
        private static void ValidateNotificationHandlerMethod(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            AttributeData notificationAttribute)
        {
            // Validate method signature for notifications
            ValidateNotificationHandlerSignature(context, methodDeclaration, methodSymbol);

            // Validate attribute parameters
            ValidateNotificationAttributeParameters(context, methodDeclaration, notificationAttribute);
        }

        /// <summary>
        /// Validates a pipeline method signature and configuration.
        /// </summary>
        private static void ValidatePipelineMethod(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            AttributeData pipelineAttribute)
        {
            // Validate pipeline method signature
            ValidatePipelineSignature(context, methodDeclaration, methodSymbol);

            // Validate attribute parameters
            ValidatePipelineAttributeParameters(context, methodDeclaration, pipelineAttribute);
        }

        /// <summary>
        /// Gets an attribute from a method symbol by its full name.
        /// </summary>
        private static AttributeData? GetAttribute(IMethodSymbol methodSymbol, string attributeFullName)
        {
            try
            {
                return methodSymbol.GetAttributes()
                    .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == attributeFullName);
            }
            catch (Exception)
            {
                // Symbol resolution can fail in incomplete or malformed code
                // Return null to gracefully handle the error
                return null;
            }
        }

        /// <summary>
        /// Safely gets the declared symbol for a method declaration.
        /// </summary>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="methodDeclaration">The method declaration syntax.</param>
        /// <returns>The method symbol, or null if resolution fails.</returns>
        private static IMethodSymbol? TryGetDeclaredSymbol(
            SemanticModel semanticModel,
            MethodDeclarationSyntax methodDeclaration)
        {
            try
            {
                return semanticModel.GetDeclaredSymbol(methodDeclaration);
            }
            catch (Exception)
            {
                // Symbol resolution can fail in incomplete or malformed code
                return null;
            }
        }

        /// <summary>
        /// Safely gets the semantic model for a syntax tree.
        /// </summary>
        /// <param name="compilation">The compilation.</param>
        /// <param name="syntaxTree">The syntax tree.</param>
        /// <returns>The semantic model, or null if creation fails.</returns>
        private static SemanticModel? TryGetSemanticModel(
            Compilation compilation,
            SyntaxTree syntaxTree)
        {
            try
            {
                return compilation.GetSemanticModel(syntaxTree);
            }
            catch (Exception)
            {
                // Semantic model creation can fail for invalid syntax trees
                return null;
            }
        }

        /// <summary>
        /// Validates handler method signatures for proper async patterns and parameter types.
        /// </summary>
        private static void ValidateHandlerSignature(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var parameters = methodSymbol.Parameters;

            // Handler must have at least one parameter (the request)
            if (parameters.Length == 0)
            {
                ReportDiagnostic(context, DiagnosticDescriptors.HandlerMissingRequestParameter,
                    methodDeclaration.Identifier.GetLocation(), methodSymbol.Name);
                return;
            }

            // First parameter should be the request type
            var requestParameter = parameters[0];
            var requestType = requestParameter.Type;

            // Validate that the request parameter implements IRequest or IRequest<T>
            if (!IsValidRequestType(requestType))
            {
                ReportDiagnostic(context, DiagnosticDescriptors.HandlerInvalidRequestParameter,
                    methodDeclaration.ParameterList.Parameters[0].GetLocation(),
                    requestType.ToDisplayString(),
                    "IRequest or IRequest<TResponse>");
                return;
            }

            // Validate return type matches request interface
            ValidateHandlerReturnType(context, methodDeclaration, methodSymbol, requestType);

            // Validate parameter order and types
            ValidateHandlerParameterOrder(context, methodDeclaration, methodSymbol);

            // Check for CancellationToken parameter (warning if missing)
            if (!HasCancellationTokenParameter(parameters))
            {
                ReportDiagnostic(context, DiagnosticDescriptors.HandlerMissingCancellationToken,
                    methodDeclaration.Identifier.GetLocation(), methodSymbol.Name);
            }

            // Validate async signature patterns
            ValidateAsyncSignaturePattern(context, methodDeclaration, methodSymbol);
        }

        /// <summary>
        /// Validates notification handler method signatures.
        /// </summary>
        private static void ValidateNotificationHandlerSignature(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var parameters = methodSymbol.Parameters;

            // Notification handler must have at least one parameter (the notification)
            if (parameters.Length == 0)
            {
                ReportDiagnostic(context, DiagnosticDescriptors.NotificationHandlerMissingParameter,
                    methodDeclaration.Identifier.GetLocation(), methodSymbol.Name);
                return;
            }

            // First parameter should be the notification type
            var notificationParameter = parameters[0];
            var notificationType = notificationParameter.Type;

            // Validate that the notification parameter implements INotification
            if (!IsValidNotificationType(notificationType))
            {
                ReportDiagnostic(context, DiagnosticDescriptors.HandlerInvalidRequestParameter,
                    methodDeclaration.ParameterList.Parameters[0].GetLocation(),
                    notificationType.ToDisplayString(),
                    "INotification");
                return;
            }

            // Validate return type (should be Task or ValueTask)
            var returnType = methodSymbol.ReturnType;
            if (!IsValidNotificationReturnType(returnType))
            {
                ReportDiagnostic(context, DiagnosticDescriptors.InvalidNotificationHandlerReturnType,
                    methodDeclaration.ReturnType.GetLocation(), returnType.ToDisplayString());
            }

            // Validate parameter order and types
            ValidateNotificationHandlerParameterOrder(context, methodDeclaration, methodSymbol);

            // Check for CancellationToken parameter (warning if missing)
            if (!HasCancellationTokenParameter(parameters))
            {
                ReportDiagnostic(context, DiagnosticDescriptors.HandlerMissingCancellationToken,
                    methodDeclaration.Identifier.GetLocation(), methodSymbol.Name);
            }

            // Validate async signature patterns
            ValidateAsyncSignaturePattern(context, methodDeclaration, methodSymbol);
        }

        /// <summary>
        /// Validates pipeline method signatures.
        /// Pipeline methods can have 2 patterns:
        /// 1. Generic pipeline: (TContext context, CancellationToken cancellationToken) - for cross-cutting concerns
        /// 2. IPipelineBehavior: (TRequest request, RequestHandlerDelegate&lt;TResponse&gt; next, CancellationToken cancellationToken)
        /// </summary>
        private static void ValidatePipelineSignature(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var parameters = methodSymbol.Parameters;

            // Pipeline methods must have at least 2 parameters: (context/request, cancellationToken)
            // or 3 parameters: (request, next delegate, cancellationToken)
            if (parameters.Length < 2)
            {
                ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                    methodDeclaration.Identifier.GetLocation(),
                    methodSymbol.Name,
                    $"Pipeline methods must have at least 2 parameters. Found {parameters.Length} parameters");
                return;
            }

            // Check if this is an IPipelineBehavior pattern (3 parameters with middle delegate)
            if (parameters.Length == 3)
            {
                // Validate second parameter is a delegate type
                var nextParam = parameters[1];
                if (IsValidPipelineDelegate(nextParam.Type))
                {
                    // This is an IPipelineBehavior pattern - validate delegate pattern
                    ValidateIPipelineBehaviorPattern(context, methodDeclaration, methodSymbol, parameters);
                    return;
                }
            }

            // Otherwise, validate as generic pipeline pattern
            ValidateGenericPipelinePattern(context, methodDeclaration, methodSymbol, parameters);
        }

        /// <summary>
        /// Validates IPipelineBehavior pattern: (TRequest request, RequestHandlerDelegate&lt;TResponse&gt; next, CancellationToken cancellationToken)
        /// </summary>
        private static void ValidateIPipelineBehaviorPattern(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            ImmutableArray<IParameterSymbol> parameters)
        {
            // Validate second parameter is a delegate type
            var nextParam = parameters[1];
            if (!IsValidPipelineDelegate(nextParam.Type))
            {
                ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                    methodDeclaration.ParameterList.Parameters[1].GetLocation(),
                    methodSymbol.Name,
                    $"Second parameter must be a delegate type (RequestHandlerDelegate<TResponse> or Func<ValueTask<TResponse>>). Found: {nextParam.Type.ToDisplayString()}");
            }

            // Validate third parameter is CancellationToken
            var cancellationTokenParam = parameters[2];
            if (cancellationTokenParam.Type.Name != "CancellationToken")
            {
                ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                    methodDeclaration.ParameterList.Parameters[2].GetLocation(),
                    methodSymbol.Name,
                    $"Third parameter must be CancellationToken. Found: {cancellationTokenParam.Type.ToDisplayString()}");
            }

            // Validate return type matches delegate return type
            var returnType = methodSymbol.ReturnType;
            if (!IsValidPipelineReturnType(returnType))
            {
                ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                    methodDeclaration.ReturnType.GetLocation(),
                    methodSymbol.Name,
                    $"Pipeline methods must return Task<TResponse>, ValueTask<TResponse>, or IAsyncEnumerable<TResponse>. Found: {returnType.ToDisplayString()}");
            }
        }

        /// <summary>
        /// Validates generic pipeline pattern: (TContext context, CancellationToken cancellationToken)
        /// </summary>
        private static void ValidateGenericPipelinePattern(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            ImmutableArray<IParameterSymbol> parameters)
        {
            // Last parameter should be CancellationToken
            var lastParam = parameters[parameters.Length - 1];
            if (lastParam.Type.Name != "CancellationToken")
            {
                ReportDiagnostic(context, DiagnosticDescriptors.HandlerMissingCancellationToken,
                    methodDeclaration.Identifier.GetLocation(),
                    methodSymbol.Name);
            }

            // No strict return type requirement for generic pipelines (can be void, Task, etc.)
            // Do not validate async pattern for generic pipelines as they can be void
        }

        /// <summary>
        /// Checks if a type is a valid pipeline delegate type.
        /// </summary>
        private static bool IsValidPipelineDelegate(ITypeSymbol type)
        {
            // Check for RequestHandlerDelegate<TResponse> or StreamHandlerDelegate<TResponse>
            if (type is INamedTypeSymbol namedType)
            {
                var typeName = namedType.Name;

                // RequestHandlerDelegate<TResponse>
                if (typeName == "RequestHandlerDelegate" && namedType.TypeArguments.Length == 1)
                    return true;

                // StreamHandlerDelegate<TResponse>
                if (typeName == "StreamHandlerDelegate" && namedType.TypeArguments.Length == 1)
                    return true;

                // Func<ValueTask<TResponse>> or Func<Task<TResponse>>
                if (typeName == "Func" && namedType.TypeArguments.Length == 1)
                {
                    var returnType = namedType.TypeArguments[0];
                    if (returnType is INamedTypeSymbol returnNamedType)
                    {
                        if (returnNamedType.Name == "ValueTask" || returnNamedType.Name == "Task")
                            return true;
                    }
                }

                // Func<IAsyncEnumerable<TResponse>> for stream pipelines
                if (typeName == "Func" && namedType.TypeArguments.Length == 1)
                {
                    var returnType = namedType.TypeArguments[0];
                    if (returnType is INamedTypeSymbol returnNamedType)
                    {
                        if (returnNamedType.Name == "IAsyncEnumerable")
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a return type is valid for pipeline methods.
        /// </summary>
        private static bool IsValidPipelineReturnType(ITypeSymbol returnType)
        {
            if (returnType is INamedTypeSymbol namedReturnType)
            {
                // Task<TResponse> or ValueTask<TResponse>
                if ((namedReturnType.Name == "Task" || namedReturnType.Name == "ValueTask")
                    && namedReturnType.TypeArguments.Length == 1)
                    return true;

                // IAsyncEnumerable<TResponse> for stream pipelines
                if (namedReturnType.Name == "IAsyncEnumerable" && namedReturnType.TypeArguments.Length == 1)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Validates handler return types match the request interface expectations.
        /// </summary>
        private static void ValidateHandlerReturnType(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            ITypeSymbol requestType)
        {
            var returnType = methodSymbol.ReturnType;
            var requestInterfaces = requestType.AllInterfaces;

            // Check if request implements IStreamRequest<TResponse>
            var streamRequestInterface = requestInterfaces
                .FirstOrDefault(i => i.Name == "IStreamRequest" && i.TypeArguments.Length == 1);

            if (streamRequestInterface != null)
            {
                // Stream request - validate return type is IAsyncEnumerable<TResponse>
                var expectedResponseType = streamRequestInterface.TypeArguments[0];
                if (!IsValidStreamHandlerReturnType(returnType, expectedResponseType))
                {
                    ReportDiagnostic(context, DiagnosticDescriptors.InvalidStreamHandlerReturnType,
                        methodDeclaration.ReturnType.GetLocation(),
                        returnType.ToDisplayString(),
                        expectedResponseType.ToDisplayString());
                }
                return;
            }

            // Check if request implements IRequest<TResponse>
            var genericRequestInterface = requestInterfaces
                .FirstOrDefault(i => i.Name == "IRequest" && i.TypeArguments.Length == 1);

            if (genericRequestInterface != null)
            {
                // Request returns a response - validate return type
                var expectedResponseType = genericRequestInterface.TypeArguments[0];
                if (!IsValidHandlerReturnType(returnType, expectedResponseType))
                {
                    ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerReturnType,
                        methodDeclaration.ReturnType.GetLocation(),
                        returnType.ToDisplayString(),
                        expectedResponseType.ToDisplayString());
                }
            }
            else
            {
                // Check if request implements IRequest (void)
                var voidRequestInterface = requestInterfaces
                    .FirstOrDefault(i => i.Name == "IRequest" && i.TypeArguments.Length == 0);

                if (voidRequestInterface != null)
                {
                    // Request doesn't return a response - should return Task or ValueTask
                    if (!IsValidVoidHandlerReturnType(returnType))
                    {
                        ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerReturnType,
                            methodDeclaration.ReturnType.GetLocation(),
                            returnType.ToDisplayString(),
                            "Task or ValueTask");
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a return type is valid for handlers that return a response.
        /// </summary>
        private static bool IsValidHandlerReturnType(ITypeSymbol returnType, ITypeSymbol expectedResponseType)
        {
            // Valid return types: TResponse, Task<TResponse>, ValueTask<TResponse>
            if (SymbolEqualityComparer.Default.Equals(returnType, expectedResponseType))
                return true;

            if (returnType is INamedTypeSymbol namedReturnType)
            {
                if (namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 1)
                {
                    return SymbolEqualityComparer.Default.Equals(namedReturnType.TypeArguments[0], expectedResponseType);
                }

                if (namedReturnType.Name == "ValueTask" && namedReturnType.TypeArguments.Length == 1)
                {
                    return SymbolEqualityComparer.Default.Equals(namedReturnType.TypeArguments[0], expectedResponseType);
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a return type is valid for handlers that don't return a response.
        /// </summary>
        private static bool IsValidVoidHandlerReturnType(ITypeSymbol returnType)
        {
            // Valid return types: Task, ValueTask (without generic parameters)
            if (returnType is INamedTypeSymbol namedReturnType)
            {
                return (namedReturnType.Name == "Task" && namedReturnType.TypeArguments.Length == 0) ||
                       (namedReturnType.Name == "ValueTask" && namedReturnType.TypeArguments.Length == 0);
            }

            return returnType.Name == "Task" || returnType.Name == "ValueTask";
        }

        /// <summary>
        /// Checks if a return type is valid for stream handlers.
        /// </summary>
        private static bool IsValidStreamHandlerReturnType(ITypeSymbol returnType, ITypeSymbol expectedResponseType)
        {
            // Valid return type: IAsyncEnumerable<TResponse>
            if (returnType is INamedTypeSymbol namedReturnType)
            {
                if (namedReturnType.Name == "IAsyncEnumerable" && namedReturnType.TypeArguments.Length == 1)
                {
                    return SymbolEqualityComparer.Default.Equals(namedReturnType.TypeArguments[0], expectedResponseType);
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a return type is valid for notification handlers.
        /// </summary>
        private static bool IsValidNotificationReturnType(ITypeSymbol returnType)
        {
            // Valid return types: Task, ValueTask
            return returnType.Name == "Task" || returnType.Name == "ValueTask";
        }

        /// <summary>
        /// Checks if a type is a valid request type (implements IRequest or IRequest<T>).
        /// </summary>
        private static bool IsValidRequestType(ITypeSymbol type)
        {
            var interfaces = type.AllInterfaces;

            // Check for IRequest interface
            if (interfaces.Any(i => i.Name == "IRequest" && i.TypeArguments.Length == 0))
                return true;

            // Check for IRequest<T> interface
            if (interfaces.Any(i => i.Name == "IRequest" && i.TypeArguments.Length == 1))
                return true;

            // Check for IStreamRequest<T> interface
            if (interfaces.Any(i => i.Name == "IStreamRequest" && i.TypeArguments.Length == 1))
                return true;

            return false;
        }

        /// <summary>
        /// Checks if a type is a valid notification type (implements INotification).
        /// </summary>
        private static bool IsValidNotificationType(ITypeSymbol type)
        {
            var interfaces = type.AllInterfaces;
            return interfaces.Any(i => i.Name == "INotification" && i.TypeArguments.Length == 0);
        }

        /// <summary>
        /// Validates parameter order and types for handler or notification handler methods.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        /// <param name="methodDeclaration">The method declaration syntax.</param>
        /// <param name="methodSymbol">The method symbol.</param>
        /// <param name="validationContext">Context containing type validator and expected type description.</param>
        private static void ValidateParameterOrder(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol,
            ParameterValidationContext validationContext)
        {
            var parameters = methodSymbol.Parameters;

            // First parameter must be the request/notification
            if (parameters.Length > 0)
            {
                var firstParam = parameters[0];
                if (!validationContext.TypeValidator(firstParam.Type))
                {
                    ReportDiagnostic(context, DiagnosticDescriptors.HandlerInvalidRequestParameter,
                        methodDeclaration.ParameterList.Parameters[0].GetLocation(),
                        firstParam.Type.ToDisplayString(),
                        validationContext.ExpectedTypeDescription);
                }
            }

            // If CancellationToken is present, it should be the last parameter
            var cancellationTokenIndex = -1;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Type.Name == "CancellationToken")
                {
                    cancellationTokenIndex = i;
                    break;
                }
            }

            if (cancellationTokenIndex >= 0 && cancellationTokenIndex != parameters.Length - 1)
            {
                ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                    methodDeclaration.ParameterList.Parameters[cancellationTokenIndex].GetLocation(),
                    methodSymbol.Name,
                    "CancellationToken parameter should be the last parameter");
            }

            // Validate that there are no unexpected parameter types
            for (int i = 1; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (param.Type.Name != "CancellationToken")
                {
                    // For now, only allow CancellationToken as additional parameters
                    // Future versions might allow dependency injection parameters
                    ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                        methodDeclaration.ParameterList.Parameters[i].GetLocation(),
                        methodSymbol.Name,
                        $"Unexpected parameter type '{param.Type.ToDisplayString()}'. Only CancellationToken is allowed as additional parameter");
                }
            }
        }

        /// <summary>
        /// Validates the parameter order and types for handler methods.
        /// </summary>
        private static void ValidateHandlerParameterOrder(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var validationContext = new ParameterValidationContext(
                IsValidRequestType,
                "IRequest or IRequest<TResponse>"
            );

            ValidateParameterOrder(context, methodDeclaration, methodSymbol, validationContext);
        }

        /// <summary>
        /// Validates async signature patterns for handler methods.
        /// </summary>
        private static void ValidateAsyncSignaturePattern(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var returnType = methodSymbol.ReturnType;

            // Check if method is marked as async but returns wrong type
            var isAsync = methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));

            if (isAsync)
            {
                // Async methods should return Task or ValueTask (with or without generic parameter)
                if (returnType.Name != "Task" && returnType.Name != "ValueTask")
                {
                    ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                        methodDeclaration.ReturnType.GetLocation(),
                        methodSymbol.Name,
                        "Async handler methods must return Task, Task<T>, ValueTask, or ValueTask<T>");
                }
            }
            else
            {
                // Non-async methods should still return Task/ValueTask for proper async handling
                if (returnType.Name != "Task" && returnType.Name != "ValueTask")
                {
                    ReportDiagnostic(context, DiagnosticDescriptors.PerformanceWarning,
                        methodDeclaration.ReturnType.GetLocation(),
                        methodSymbol.Name,
                        "Handler methods should return Task or ValueTask for optimal async performance");
                }
            }
        }

        /// <summary>
        /// Checks if the method parameters include a CancellationToken.
        /// </summary>
        private static bool HasCancellationTokenParameter(ImmutableArray<IParameterSymbol> parameters)
        {
            return parameters.Any(p => p.Type.Name == "CancellationToken");
        }

        /// <summary>
        /// Validates the parameter order and types for notification handler methods.
        /// </summary>
        private static void ValidateNotificationHandlerParameterOrder(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            IMethodSymbol methodSymbol)
        {
            var validationContext = new ParameterValidationContext(
                IsValidNotificationType,
                "INotification"
            );

            ValidateParameterOrder(context, methodDeclaration, methodSymbol, validationContext);
        }

        /// <summary>
        /// Validates Handle attribute parameters.
        /// </summary>
        private static void ValidateHandleAttributeParameters(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            AttributeData handleAttribute)
        {
            // Validate Priority parameter if present
            var priorityArg = handleAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Priority");

            if (priorityArg.Key != null && priorityArg.Value.Value is not int)
            {
                ReportDiagnostic(context, DiagnosticDescriptors.InvalidPriorityValue,
                    methodDeclaration.AttributeLists.First().GetLocation(),
                    priorityArg.Value.Value?.ToString() ?? "null");
            }
        }

        /// <summary>
        /// Validates Notification attribute parameters.
        /// </summary>
        private static void ValidateNotificationAttributeParameters(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            AttributeData notificationAttribute)
        {
            // Validate Priority parameter if present
            var priorityArg = notificationAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Priority");

            if (priorityArg.Key != null && priorityArg.Value.Value is not int)
            {
                ReportDiagnostic(context, DiagnosticDescriptors.InvalidPriorityValue,
                    methodDeclaration.AttributeLists.First().GetLocation(),
                    priorityArg.Value.Value?.ToString() ?? "null");
            }
        }

        /// <summary>
        /// Validates Pipeline attribute parameters.
        /// </summary>
        private static void ValidatePipelineAttributeParameters(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax methodDeclaration,
            AttributeData pipelineAttribute)
        {
            // Validate Order parameter if present
            var orderArg = pipelineAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Order");

            if (orderArg.Key != null && orderArg.Value.Value is not int)
            {
                ReportDiagnostic(context, DiagnosticDescriptors.InvalidPriorityValue,
                    methodDeclaration.AttributeLists.First().GetLocation(),
                    orderArg.Value.Value?.ToString() ?? "null");
            }

            // Validate Scope parameter if present
            var scopeArg = pipelineAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Scope");

            if (scopeArg.Key != null && scopeArg.Value.Value is int scopeValue)
            {
                // Validate scope value is within PipelineScope enum range (0-3)
                if (scopeValue < 0 || scopeValue > 3)
                {
                    ReportDiagnostic(context, DiagnosticDescriptors.InvalidPipelineScope,
                        methodDeclaration.AttributeLists.First().GetLocation(),
                        scopeValue.ToString(),
                        methodDeclaration.Identifier.Text,
                        "All, Requests, Streams, or Notifications");
                }
            }
        }

        /// <summary>
        /// Validates for duplicate handler registrations across the compilation.
        /// </summary>
        private static void ValidateDuplicateHandlers(
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
                var unnamedHandlers = handlers.Where(h => string.IsNullOrWhiteSpace(h.Name)).ToList();
                if (unnamedHandlers.Count > 1)
                {
                    var handlerLocations = string.Join(", ", unnamedHandlers.Select(h =>
                        $"{h.MethodSymbol.ContainingType.Name}.{h.MethodName}"));

                    foreach (var handler in unnamedHandlers)
                    {
                        ReportDiagnostic(context, DiagnosticDescriptors.DuplicateHandler,
                            handler.Location,
                            requestTypeName,
                            handlerLocations);
                    }
                }

                // Check for named handler conflicts
                var namedHandlers = handlers.Where(h => !string.IsNullOrWhiteSpace(h.Name))
                    .GroupBy(h => h.Name);

                foreach (var namedGroup in namedHandlers)
                {
                    if (namedGroup.Count() > 1)
                    {
                        var conflictingHandlers = string.Join(", ", namedGroup.Select(h =>
                            $"{h.MethodSymbol.ContainingType.Name}.{h.MethodName}"));

                        foreach (var handler in namedGroup)
                        {
                            ReportDiagnostic(context, DiagnosticDescriptors.NamedHandlerConflict,
                                handler.Location,
                                namedGroup.Key!,
                                requestTypeName);
                        }
                    }
                }

                // Check for mixed named and unnamed handlers (potential issue)
                if (unnamedHandlers.Count > 0 && handlers.Any(h => !string.IsNullOrWhiteSpace(h.Name)))
                {
                    foreach (var unnamedHandler in unnamedHandlers)
                    {
                        ReportDiagnostic(context, DiagnosticDescriptors.ConfigurationConflict,
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
                        ReportDiagnostic(context, DiagnosticDescriptors.PerformanceWarning,
                            handler.Location,
                            handler.MethodName,
                            "Very low priority value might indicate the handler will rarely be selected");
                    }
                    else if (handler.Priority > MaxRecommendedPriority)
                    {
                        ReportDiagnostic(context, DiagnosticDescriptors.PerformanceWarning,
                            handler.Location,
                            handler.MethodName,
                            "Very high priority value might indicate over-prioritization");
                    }
                }

                // Check for potential naming conflicts with common patterns
                foreach (var handler in handlers.Where(h => !string.IsNullOrWhiteSpace(h.Name)))
                {
                    if (handler.Name!.Equals("default", StringComparison.OrdinalIgnoreCase) ||
                        handler.Name.Equals("main", StringComparison.OrdinalIgnoreCase))
                    {
                        ReportDiagnostic(context, DiagnosticDescriptors.PerformanceWarning,
                            handler.Location,
                            handler.MethodName,
                            $"Handler name '{handler.Name}' might conflict with common naming patterns");
                    }
                }
            }
        }

        /// <summary>
        /// Reports a diagnostic to the analysis context.
        /// </summary>
        private static void ReportDiagnostic(
            SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor descriptor,
            Location location,
            params object[] messageArgs)
        {
            var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports a diagnostic to the compilation analysis context.
        /// </summary>
        private static void ReportDiagnostic(
            CompilationAnalysisContext context,
            DiagnosticDescriptor descriptor,
            Location location,
            params object[] messageArgs)
        {
            var diagnostic = Diagnostic.Create(descriptor, location, messageArgs);
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an analyzer error for a syntax node analysis context.
        /// </summary>
        /// <param name="context">The syntax node analysis context.</param>
        /// <param name="methodDeclaration">The method declaration that caused the error, if available.</param>
        /// <param name="exception">The exception that occurred.</param>
        private static void ReportAnalyzerError(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax? methodDeclaration,
            Exception exception)
        {
            var location = methodDeclaration?.Identifier.GetLocation() ?? Location.None;
            var methodName = methodDeclaration?.Identifier.Text ?? "unknown method";
            var errorMessage = $"An error occurred while analyzing '{methodName}': {exception.GetType().Name}";

            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                location,
                errorMessage);

            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Reports an analyzer error for a compilation analysis context.
        /// </summary>
        /// <param name="context">The compilation analysis context.</param>
        /// <param name="exception">The exception that occurred.</param>
        private static void ReportAnalyzerError(
            CompilationAnalysisContext context,
            Exception exception)
        {
            var errorMessage = $"An error occurred during compilation analysis: {exception.GetType().Name}";

            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                Location.None,
                errorMessage);

            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Collects pipeline information for duplicate order validation.
        /// </summary>
        private static void CollectPipelineInfo(
            List<PipelineInfo> pipelineRegistry,
            IMethodSymbol methodSymbol,
            AttributeData pipelineAttribute,
            MethodDeclarationSyntax methodDeclaration)
        {
            var order = 0;
            var scope = 0; // PipelineScope.All

            // Extract Order parameter
            var orderArg = pipelineAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Order");
            if (orderArg.Key != null && orderArg.Value.Value is int orderValue)
            {
                order = orderValue;
            }

            // Extract Scope parameter
            var scopeArg = pipelineAttribute.NamedArguments
                .FirstOrDefault(arg => arg.Key == "Scope");
            if (scopeArg.Key != null && scopeArg.Value.Value is int scopeValue)
            {
                scope = scopeValue;
            }

            pipelineRegistry.Add(new PipelineInfo
            {
                MethodName = methodSymbol.Name,
                Order = order,
                Scope = scope,
                Location = methodDeclaration.Identifier.GetLocation(),
                ContainingType = methodSymbol.ContainingType.ToDisplayString()
            });
        }

        /// <summary>
        /// Validates for duplicate pipeline orders within the same scope.
        /// Note: Multiple pipelines with the same order are allowed if they handle different request types.
        /// This validation only warns about exact duplicates (same containing type and same order).
        /// </summary>
        private static void ValidateDuplicatePipelineOrders(
            CompilationAnalysisContext context,
            List<PipelineInfo> pipelineRegistry)
        {
            // Group pipelines by scope and containing type
            var pipelineGroups = pipelineRegistry
                .GroupBy(p => new { p.Scope, p.ContainingType });

            foreach (var group in pipelineGroups)
            {
                // Find pipelines with duplicate orders within the same class and scope
                var duplicateOrders = group
                    .GroupBy(p => p.Order)
                    .Where(g => g.Count() > 1);

                foreach (var orderGroup in duplicateOrders)
                {
                    var scopeName = GetScopeName(group.Key.Scope);

                    // Only report if the pipelines are truly identical (same method names would indicate a real problem)
                    var distinctMethods = orderGroup.Select(p => p.MethodName).Distinct().Count();

                    // If all methods are different, it's OK - they handle different contexts
                    if (distinctMethods == orderGroup.Count())
                        continue;

                    // Report duplicate order only for identical method scenarios
                    foreach (var pipeline in orderGroup)
                    {
                        ReportDiagnostic(context, DiagnosticDescriptors.DuplicatePipelineOrder,
                            pipeline.Location,
                            orderGroup.Key.ToString(),
                            scopeName);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the display name for a pipeline scope value.
        /// </summary>
        private static string GetScopeName(int scope)
        {
            return scope switch
            {
                0 => "All",
                1 => "Requests",
                2 => "Streams",
                3 => "Notifications",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Information about a pipeline for duplicate order validation.
        /// </summary>
        private struct PipelineInfo
        {
            public string MethodName { get; set; }
            public int Order { get; set; }
            public int Scope { get; set; }
            public Location Location { get; set; }
            public string ContainingType { get; set; }
        }

        /// <summary>
        /// Context for parameter validation containing type checking delegate and expected type description.
        /// </summary>
        private sealed class ParameterValidationContext
        {
            public Func<ITypeSymbol, bool> TypeValidator { get; }
            public string ExpectedTypeDescription { get; }

            public ParameterValidationContext(Func<ITypeSymbol, bool> typeValidator, string expectedTypeDescription)
            {
                TypeValidator = typeValidator;
                ExpectedTypeDescription = expectedTypeDescription;
            }
        }
    }
}
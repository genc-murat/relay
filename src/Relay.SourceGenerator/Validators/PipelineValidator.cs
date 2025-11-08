using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Relay.SourceGenerator.Validators;

/// <summary>
/// Validates pipeline method signatures and configuration.
/// </summary>
internal static class PipelineValidator
{
    /// <summary>
    /// Validates a pipeline method signature and configuration.
    /// </summary>
    public static void ValidatePipelineMethod(
        SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclaration,
        IMethodSymbol methodSymbol,
        AttributeData pipelineAttribute)
    {
        // Validate pipeline method signature
        ValidatePipelineSignature(context, methodDeclaration, methodSymbol);

        // Validate attribute parameters
        AttributeValidator.ValidatePipelineAttributeParameters(context, methodDeclaration, pipelineAttribute);
    }

    /// <summary>
    /// Validates pipeline method signatures.
    /// Pipeline methods can have 2 patterns:
    /// 1. Generic pipeline: (TContext context, CancellationToken cancellationToken) - for cross-cutting concerns
    /// 2. IPipelineBehavior: (TRequest request, RequestHandlerDelegate&lt;TResponse&gt; next, CancellationToken cancellationToken)
    /// </summary>
    public static void ValidatePipelineSignature(
        SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclaration,
        IMethodSymbol methodSymbol)
    {
        var parameters = methodSymbol.Parameters;

        // Pipeline methods must have at least 2 parameters: (context/request, cancellationToken)
        // or 3 parameters: (request, next delegate, cancellationToken)
        if (parameters.Length < 2)
        {
            ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                methodDeclaration.Identifier.GetLocation(),
                "pipeline",
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
            ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                methodDeclaration.ParameterList.Parameters[1].GetLocation(),
                "pipeline",
                methodSymbol.Name,
                $"Second parameter must be a delegate type (RequestHandlerDelegate<TResponse> or Func<ValueTask<TResponse>>). Found: {nextParam.Type.ToDisplayString()}");
        }

        // Validate third parameter is CancellationToken
        var cancellationTokenParam = parameters[2];
        if (cancellationTokenParam.Type.Name != "CancellationToken")
        {
            ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                methodDeclaration.ParameterList.Parameters[2].GetLocation(),
                "pipeline",
                methodSymbol.Name,
                $"Third parameter must be CancellationToken. Found: {cancellationTokenParam.Type.ToDisplayString()}");
        }

        // Validate return type matches delegate return type
        var returnType = methodSymbol.ReturnType;
        if (!IsValidPipelineReturnType(returnType))
        {
            ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.InvalidHandlerSignature,
                methodDeclaration.ReturnType.GetLocation(),
                "pipeline",
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
            ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.HandlerMissingCancellationToken,
                methodDeclaration.Identifier.GetLocation(),
                methodSymbol.Name);
        }

        // No strict return type requirement for generic pipelines (can be void, Task, etc.)
        // Do not validate async pattern for generic pipelines as they can be void
    }

    /// <summary>
    /// Checks if a type is a valid pipeline delegate type.
    /// </summary>
    internal static bool IsValidPipelineDelegate(ITypeSymbol type)
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
    internal static bool IsValidPipelineReturnType(ITypeSymbol returnType)
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
    /// Collects pipeline information for duplicate order validation.
    /// </summary>
    public static void CollectPipelineInfo(
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
             ContainingType = GetFullTypeName(methodSymbol.ContainingType)
         });
    }

    private static string GetFullTypeName(INamedTypeSymbol typeSymbol)
    {
        var parts = new List<string>();
        var current = typeSymbol;
        while (current != null)
        {
            parts.Insert(0, current.Name);
            current = current.ContainingType;
        }

        var namespaceSymbol = typeSymbol.ContainingNamespace;
        if (namespaceSymbol != null && !namespaceSymbol.IsGlobalNamespace)
        {
            parts.Insert(0, namespaceSymbol.ToDisplayString());
        }

        var result = string.Join(".", parts);
        // Debug output
        System.Diagnostics.Debug.WriteLine($"Type: {typeSymbol.Name}, Namespace: {namespaceSymbol?.ToDisplayString() ?? "null"}, Result: {result}");
        return result;
    }

    /// <summary>
    /// Validates for duplicate pipeline orders within the same scope.
    /// Note: Multiple pipelines with the same order are allowed if they handle different request types.
    /// This validation only warns about exact duplicates (same containing type and same order).
    /// </summary>
    public static void ValidateDuplicatePipelineOrders(
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
                    ValidationHelper.ReportDiagnostic(context, DiagnosticDescriptors.DuplicatePipelineOrder,
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
}

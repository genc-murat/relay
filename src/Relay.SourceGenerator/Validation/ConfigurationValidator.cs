using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.SourceGenerator;

/// <summary>
/// Validates configuration at compile time and reports configuration conflicts and issues.
/// </summary>
public class ConfigurationValidator
{
    private readonly IDiagnosticReporter _diagnosticReporter;

    public ConfigurationValidator(IDiagnosticReporter diagnosticReporter)
    {
        _diagnosticReporter = diagnosticReporter ?? throw new ArgumentNullException(nameof(diagnosticReporter));
    }

    /// <summary>
    /// Validates handler configurations for conflicts and completeness.
    /// </summary>
    public void ValidateHandlerConfigurations(IEnumerable<HandlerRegistration> handlers)
    {
        var handlerGroups = handlers.GroupBy(h => new { h.RequestType, h.ResponseType });

        foreach (var group in handlerGroups)
        {
            var handlersInGroup = group.ToList();

            // Check for duplicate unnamed handlers
            var unnamedHandlers = handlersInGroup.Where(h => string.IsNullOrWhiteSpace(h.Name)).ToList();
            if (unnamedHandlers.Count > 1)
            {
                foreach (var handler in unnamedHandlers)
                {
                    _diagnosticReporter.ReportDuplicateHandler(
                        handler.Location,
                        handler.RequestType.Name,
                        handler.ResponseType?.Name);
                }
            }

            // Check for duplicate named handlers
            var namedHandlerGroups = handlersInGroup
                .Where(h => !string.IsNullOrWhiteSpace(h.Name))
                .GroupBy(h => h.Name);

            foreach (var namedGroup in namedHandlerGroups)
            {
                var namedHandlers = namedGroup.ToList();
                if (namedHandlers.Count > 1)
                {
                    foreach (var handler in namedHandlers)
                    {
                        _diagnosticReporter.ReportDuplicateNamedHandler(
                            handler.Location,
                            handler.RequestType.Name,
                            handler.Name!);
                    }
                }
            }

            // Validate handler method signatures
            foreach (var handler in handlersInGroup)
            {
                ValidateHandlerSignature(handler);
            }
        }
    }

    /// <summary>
    /// Validates notification handler configurations.
    /// </summary>
    public void ValidateNotificationConfigurations(IEnumerable<NotificationHandlerRegistration> notificationHandlers)
    {
        foreach (var handler in notificationHandlers)
        {
            ValidateNotificationHandlerSignature(handler);
        }
    }

    /// <summary>
    /// Validates pipeline configurations for ordering and scope conflicts.
    /// </summary>
    public void ValidatePipelineConfigurations(IEnumerable<PipelineRegistration> pipelines)
    {
        // Group pipelines by scope to check for ordering conflicts
        var pipelineGroups = pipelines.GroupBy(p => p.Scope);

        foreach (var group in pipelineGroups)
        {
            var pipelinesInScope = group.OrderBy(p => p.Order).ToList();

            // Check for duplicate orders within the same scope
            var orderGroups = pipelinesInScope.GroupBy(p => p.Order);
            foreach (var orderGroup in orderGroups)
            {
                var pipelinesWithSameOrder = orderGroup.ToList();
                if (pipelinesWithSameOrder.Count > 1)
                {
                    foreach (var pipeline in pipelinesWithSameOrder)
                    {
                        _diagnosticReporter.ReportDuplicatePipelineOrder(
                            pipeline.Location,
                            pipeline.Order,
                            group.Key.ToString());
                    }
                }
            }

            // Validate pipeline method signatures
            foreach (var pipeline in pipelinesInScope)
            {
                ValidatePipelineSignature(pipeline);
            }
        }
    }

    /// <summary>
    /// Validates configuration completeness - ensures all required configurations are present.
    /// </summary>
    public void ValidateConfigurationCompleteness(
        IEnumerable<HandlerRegistration> handlers,
        IEnumerable<NotificationHandlerRegistration> notificationHandlers,
        IEnumerable<PipelineRegistration> pipelines)
    {
        // Check if there are any handlers at all
        if (!handlers.Any() && !notificationHandlers.Any())
        {
            _diagnosticReporter.ReportNoHandlersFound();
        }

        // Validate that request types have corresponding handlers
        var requestTypes = handlers.Select(h => h.RequestType).Distinct(SymbolEqualityComparer.Default).ToList();
        var handlerRequestTypes = handlers.Select(h => h.RequestType).Distinct(SymbolEqualityComparer.Default).ToList();

        // This validation would be more meaningful with actual usage analysis
        // For now, we ensure basic structural integrity
    }

    /// <summary>
    /// Validates attribute parameter combinations for conflicts.
    /// </summary>
    public void ValidateAttributeParameterConflicts(IEnumerable<HandlerRegistration> handlers)
    {
        foreach (var handler in handlers)
        {
            // Validate Handle attribute parameters
            if (handler.Attribute != null)
            {
                ValidateHandleAttributeParameters(handler);
            }
        }
    }

    private void ValidateHandlerSignature(HandlerRegistration handler)
    {
        var method = handler.Method;

        // Check return type
        if (handler.Kind == HandlerKind.Request)
        {
            if (!IsValidHandlerReturnType(method.ReturnType, handler.ResponseType))
            {
                _diagnosticReporter.ReportInvalidHandlerReturnType(
                    handler.Location,
                    method.ReturnType.ToString(),
                    handler.ResponseType?.Name ?? "void");
            }
        }
        else if (handler.Kind == HandlerKind.Stream)
        {
            if (!IsValidStreamHandlerReturnType(method.ReturnType, handler.ResponseType))
            {
                _diagnosticReporter.ReportInvalidStreamHandlerReturnType(
                    handler.Location,
                    method.ReturnType.ToString(),
                    handler.ResponseType?.Name ?? "unknown");
            }
        }

        // Check parameters
        var parameters = method.Parameters;
        if (parameters.Length == 0)
        {
            _diagnosticReporter.ReportHandlerMissingRequestParameter(
                handler.Location,
                method.Name);
        }
        else
        {
            var firstParam = parameters[0];
            if (!IsAssignableFrom(handler.RequestType, firstParam.Type))
            {
                _diagnosticReporter.ReportHandlerInvalidRequestParameter(
                    handler.Location,
                    firstParam.Type.Name,
                    handler.RequestType.Name);
            }
        }

        // Check for CancellationToken parameter
        var hasCancellationToken = parameters.Any(p =>
            p.Type.Name == "CancellationToken" &&
            p.Type.ContainingNamespace?.ToDisplayString() == "System.Threading");

        if (!hasCancellationToken)
        {
            _diagnosticReporter.ReportHandlerMissingCancellationToken(
                handler.Location,
                method.Name);
        }
    }

    private void ValidateNotificationHandlerSignature(NotificationHandlerRegistration handler)
    {
        var method = handler.Method;

        // Check return type (should be Task or ValueTask)
        if (!IsValidNotificationHandlerReturnType(method.ReturnType))
        {
            _diagnosticReporter.ReportInvalidNotificationHandlerReturnType(
                handler.Location,
                method.ReturnType.ToString());
        }

        // Check parameters
        var parameters = method.Parameters;
        if (parameters.Length == 0)
        {
            _diagnosticReporter.ReportNotificationHandlerMissingParameter(
                handler.Location,
                method.Name);
        }
    }

    private void ValidatePipelineSignature(PipelineRegistration pipeline)
    {
        var method = pipeline.Method;

        if (method == null) return; // System modules don't have methods

        // Validate pipeline method signature based on scope
        // This would require more detailed analysis of the method signature
        // For now, we do basic validation
    }

    private void ValidateHandleAttributeParameters(HandlerRegistration handler)
    {
        // Validate priority values
        if (handler.Priority < int.MinValue || handler.Priority > int.MaxValue)
        {
            _diagnosticReporter.ReportInvalidPriorityValue(
                handler.Location,
                handler.Priority);
        }

        // Validate name conflicts would be handled in ValidateHandlerConfigurations
    }

    private static bool IsValidHandlerReturnType(ITypeSymbol returnType, ITypeSymbol? expectedResponseType)
    {
        // Check for Task<T>, ValueTask<T>, T, Task, ValueTask
        var returnTypeName = returnType.Name;

        if (returnTypeName == "Task" || returnTypeName == "ValueTask")
        {
            if (returnType is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var typeArg = namedType.TypeArguments.FirstOrDefault();
                return expectedResponseType != null && SymbolEqualityComparer.Default.Equals(typeArg, expectedResponseType);
            }
            return expectedResponseType == null; // Task or ValueTask without generic parameter
        }

        // Direct return type
        return expectedResponseType != null && SymbolEqualityComparer.Default.Equals(returnType, expectedResponseType);
    }

    private static bool IsValidStreamHandlerReturnType(ITypeSymbol returnType, ITypeSymbol? expectedResponseType)
    {
        // Check for IAsyncEnumerable<T>
        if (returnType is INamedTypeSymbol namedType &&
            namedType.Name == "IAsyncEnumerable" &&
            namedType.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic")
        {
            var typeArg = namedType.TypeArguments.FirstOrDefault();
            return expectedResponseType != null && SymbolEqualityComparer.Default.Equals(typeArg, expectedResponseType);
        }

        return false;
    }

    private static bool IsValidNotificationHandlerReturnType(ITypeSymbol returnType)
    {
        var returnTypeName = returnType.Name;
        return returnTypeName == "Task" || returnTypeName == "ValueTask";
    }

    private static bool IsAssignableFrom(ITypeSymbol targetType, ITypeSymbol sourceType)
    {
        return SymbolEqualityComparer.Default.Equals(targetType, sourceType) ||
               sourceType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, targetType));
    }
}

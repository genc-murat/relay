using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Discovers and validates handler methods in the compilation with parallel processing support.
    /// </summary>
    public class HandlerDiscoveryEngine
    {
        private readonly RelayCompilationContext _context;
        private readonly ConcurrentDictionary<IMethodSymbol, ITypeSymbol?> _responseTypeCache = new(SymbolEqualityComparer.Default);

        public HandlerDiscoveryEngine(RelayCompilationContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Discovers all valid handler methods from the candidate methods with parallel processing.
        /// </summary>
        public HandlerDiscoveryResult DiscoverHandlers(IEnumerable<MethodDeclarationSyntax?> candidateMethods, IDiagnosticReporter diagnosticReporter)
        {
            var result = new HandlerDiscoveryResult();
            var methodList = candidateMethods.Where(m => m != null).ToList();

            if (methodList.Count == 0)
                return result;

            // For small collections, use sequential processing to avoid overhead
            if (methodList.Count < 10)
            {
                ProcessMethodsSequentially(methodList, result, diagnosticReporter);
            }
            else
            {
                // For larger collections, use parallel processing
                ProcessMethodsInParallel(methodList, result, diagnosticReporter);
            }

            // Validate for duplicate handlers
            ValidateForDuplicates(result.Handlers, diagnosticReporter);

            return result;
        }

        private void ProcessMethodsSequentially(List<MethodDeclarationSyntax?> methods, HandlerDiscoveryResult result, IDiagnosticReporter diagnosticReporter)
        {
            foreach (var method in methods)
            {
                if (method == null) continue;

                _context.CancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var handlerInfo = AnalyzeHandlerMethod(method, diagnosticReporter);
                    if (handlerInfo != null)
                    {
                        result.Handlers.Add(handlerInfo);
                    }
                }
                catch (Exception ex)
                {
                    var diagnostic = Diagnostic.Create(DiagnosticDescriptors.GeneratorError, Location.None, $"Error analyzing handler method '{method.Identifier.ValueText}': {ex.Message}");
                    diagnosticReporter.ReportDiagnostic(diagnostic);
                }
            }
        }

        private void ProcessMethodsInParallel(List<MethodDeclarationSyntax?> methods, HandlerDiscoveryResult result, IDiagnosticReporter diagnosticReporter)
        {
            var handlers = new ConcurrentBag<HandlerInfo>();
            var diagnostics = new ConcurrentBag<Diagnostic>();

            // Use a reasonable degree of parallelism without Environment.ProcessorCount
            // Most machines have 4-16 cores, use 4 as a safe default
            Parallel.ForEach(methods,
                new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = _context.CancellationToken },
                method =>
                {
                    if (method == null) return;

                    try
                    {
                        var handlerInfo = AnalyzeHandlerMethod(method, diagnosticReporter);
                        if (handlerInfo != null)
                        {
                            handlers.Add(handlerInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.GeneratorError, Location.None, $"Error analyzing handler method '{method.Identifier.ValueText}': {ex.Message}");
                        diagnostics.Add(diagnostic);
                    }
                });

            // Add all handlers to result
            foreach (var handler in handlers)
            {
                result.Handlers.Add(handler);
            }

            // Report all diagnostics
            foreach (var diagnostic in diagnostics)
            {
                diagnosticReporter.ReportDiagnostic(diagnostic);
            }
        }

        private HandlerInfo? AnalyzeHandlerMethod(MethodDeclarationSyntax method, IDiagnosticReporter diagnosticReporter)
        {
            var semanticModel = _context.GetSemanticModel(method.SyntaxTree);
            var methodSymbol = semanticModel.GetDeclaredSymbol(method) as IMethodSymbol;

            if (methodSymbol == null)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.GeneratorError, Location.None, $"Could not get symbol for method '{method.Identifier.ValueText}'");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return null;
            }

            // Find Relay attributes
            var relayAttributes = GetRelayAttributes(methodSymbol);
            if (!relayAttributes.Any())
            {
                return null; // No Relay attributes found
            }

            var handlerInfo = new HandlerInfo
            {
                Method = method,
                MethodSymbol = methodSymbol,
                Attributes = relayAttributes,
                HandlerTypeSymbol = methodSymbol.ContainingType,
                MethodName = methodSymbol.Name,
                RequestTypeSymbol = methodSymbol.Parameters.FirstOrDefault()?.Type,
                ResponseTypeSymbol = GetResponseType(methodSymbol)
            };

            // Validate method signature based on attribute type
            foreach (var attribute in relayAttributes)
            {
                if (!ValidateHandlerSignature(handlerInfo, attribute, diagnosticReporter))
                {
                    return null; // Invalid signature
                }
            }

            return handlerInfo;
        }

        private ITypeSymbol? GetResponseType(IMethodSymbol methodSymbol)
        {
            // Use cache to avoid repeated type analysis
            return _responseTypeCache.GetOrAdd(methodSymbol, symbol =>
            {
                var returnType = symbol.ReturnType;
                
                if (returnType is INamedTypeSymbol namedType && namedType.IsGenericType)
                {
                    var genericDef = namedType.OriginalDefinition.ToDisplayString();
                    if (genericDef == "System.Threading.Tasks.Task<TResult>" ||
                        genericDef == "System.Threading.Tasks.ValueTask<TResult>" ||
                        genericDef == "System.Collections.Generic.IAsyncEnumerable<T>")
                    {
                        return namedType.TypeArguments.FirstOrDefault();
                    }
                }

                var returnTypeString = returnType.ToDisplayString();
                if (returnTypeString == "System.Threading.Tasks.Task" || returnTypeString == "System.Threading.Tasks.ValueTask")
                {
                    return _context.FindType("Relay.Core.Unit");
                }

                return returnType;
            });
        }

        private List<RelayAttributeInfo> GetRelayAttributes(IMethodSymbol methodSymbol)
        {
            var relayAttributes = new List<RelayAttributeInfo>();

            foreach (var attribute in methodSymbol.GetAttributes())
            {
                var attributeName = attribute.AttributeClass?.Name;
                if (attributeName == null) continue;

                var relayAttributeType = GetRelayAttributeType(attributeName);
                if (relayAttributeType != RelayAttributeType.None)
                {
                    relayAttributes.Add(new RelayAttributeInfo
                    {
                        Type = relayAttributeType,
                        AttributeData = attribute
                    });
                }
            }

            return relayAttributes;
        }

        private RelayAttributeType GetRelayAttributeType(string attributeName)
        {
            return attributeName switch
            {
                "HandleAttribute" or "Handle" => RelayAttributeType.Handle,
                "NotificationAttribute" or "Notification" => RelayAttributeType.Notification,
                "PipelineAttribute" or "Pipeline" => RelayAttributeType.Pipeline,
                "ExposeAsEndpointAttribute" or "ExposeAsEndpoint" => RelayAttributeType.ExposeAsEndpoint,
                _ => RelayAttributeType.None
            };
        }

        private bool ValidateHandlerSignature(HandlerInfo handlerInfo, RelayAttributeInfo attributeInfo, IDiagnosticReporter diagnosticReporter)
        {
            var method = handlerInfo.MethodSymbol;
            if (handlerInfo.Method == null || method == null) return false;
            var location = handlerInfo.Method.GetLocation();

            switch (attributeInfo.Type)
            {
                case RelayAttributeType.Handle:
                    return ValidateRequestHandlerSignature(method, location, diagnosticReporter);

                case RelayAttributeType.Notification:
                    return ValidateNotificationHandlerSignature(method, location, diagnosticReporter);

                case RelayAttributeType.Pipeline:
                    return ValidatePipelineHandlerSignature(method, location, diagnosticReporter);

                case RelayAttributeType.ExposeAsEndpoint:
                    return ValidateEndpointHandlerSignature(method, location, diagnosticReporter);

                default:
                    return false;
            }
        }

        private bool ValidateRequestHandlerSignature(IMethodSymbol method, Location location, IDiagnosticReporter diagnosticReporter)
        {
            // Request handlers should have:
            // - Exactly one parameter (the request)
            // - Return Task<T>, ValueTask<T>, Task, ValueTask, or T
            // - Be public or internal

            if (method.Parameters.Length != 1)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidHandlerSignature, location,
                    method.Name, "Request handlers must have exactly one parameter");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            if (!IsValidAccessibility(method))
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidHandlerSignature, location,
                    method.Name, "Request handlers must be public or internal");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            if (!IsValidReturnType(method.ReturnType))
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidHandlerSignature, location,
                    method.Name, "Request handlers must return Task<T>, ValueTask<T>, Task, ValueTask, or a concrete type");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            return true;
        }

        private bool ValidateNotificationHandlerSignature(IMethodSymbol method, Location location, IDiagnosticReporter diagnosticReporter)
        {
            // Notification handlers should have:
            // - Exactly one parameter (the notification)
            // - Return Task, ValueTask, or void
            // - Be public or internal

            if (method.Parameters.Length != 1)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidHandlerSignature, location,
                    method.Name, "Notification handlers must have exactly one parameter");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            if (!IsValidAccessibility(method))
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidHandlerSignature, location,
                    method.Name, "Notification handlers must be public or internal");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            if (!IsValidNotificationReturnType(method.ReturnType))
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidHandlerSignature, location,
                    method.Name, "Notification handlers must return Task, ValueTask, or void");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            return true;
        }

        private bool ValidatePipelineHandlerSignature(IMethodSymbol method, Location location, IDiagnosticReporter diagnosticReporter)
        {
            // Pipeline handlers should have:
            // - At least two parameters (request and next delegate)
            // - Return Task<T>, ValueTask<T>, IAsyncEnumerable<T>, etc.
            // - Be public or internal

            if (method.Parameters.Length < 2)
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidHandlerSignature, location,
                    method.Name, "Pipeline handlers must have at least two parameters (request and next delegate)");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            if (!IsValidAccessibility(method))
            {
                var diagnostic = Diagnostic.Create(DiagnosticDescriptors.InvalidHandlerSignature, location,
                    method.Name, "Pipeline handlers must be public or internal");
                diagnosticReporter.ReportDiagnostic(diagnostic);
                return false;
            }

            return true;
        }

        private bool ValidateEndpointHandlerSignature(IMethodSymbol method, Location location, IDiagnosticReporter diagnosticReporter)
        {
            // Endpoint handlers follow the same rules as request handlers
            return ValidateRequestHandlerSignature(method, location, diagnosticReporter);
        }

        private bool IsValidAccessibility(IMethodSymbol method)
        {
            return method.DeclaredAccessibility == Accessibility.Public ||
                   method.DeclaredAccessibility == Accessibility.Internal;
        }

        private bool IsValidReturnType(ITypeSymbol returnType)
        {
            var typeName = returnType.ToDisplayString();

            // Check for void (not allowed for request handlers)
            if (returnType.SpecialType == SpecialType.System_Void)
            {
                return false;
            }

            // Check for Task<T>, ValueTask<T>, Task, ValueTask
            if (typeName.StartsWith("System.Threading.Tasks.Task") ||
                typeName.StartsWith("System.Threading.Tasks.ValueTask"))
            {
                return true;
            }

            // Check for IAsyncEnumerable<T> (streaming handlers)
            if (typeName.StartsWith("System.Collections.Generic.IAsyncEnumerable"))
            {
                return true;
            }

            // Any other concrete type is valid
            return true;
        }

        private bool IsValidNotificationReturnType(ITypeSymbol returnType)
        {
            var typeName = returnType.ToDisplayString();

            // Check for void
            if (returnType.SpecialType == SpecialType.System_Void)
            {
                return true;
            }

            // Check for Task, ValueTask (without generic parameter)
            if (typeName == "System.Threading.Tasks.Task" ||
                typeName == "System.Threading.Tasks.ValueTask")
            {
                return true;
            }

            return false;
        }

        private void ValidateForDuplicates(List<HandlerInfo> handlers, IDiagnosticReporter diagnosticReporter)
        {
            var requestHandlers = handlers
                .Where(h => h.Attributes.Any(a => a.Type == RelayAttributeType.Handle))
                .ToList();

            // Group by request type and validate naming conflicts
            var groupedHandlers = requestHandlers
                .Where(h => h.MethodSymbol != null)
                .GroupBy(h => GetRequestType(h.MethodSymbol!))
                .ToList();

            foreach (var group in groupedHandlers)
            {
                var requestType = group.Key;
                var handlersForType = group.ToList();

                ValidateNamedHandlerConflicts(requestType, handlersForType, diagnosticReporter);
            }
        }

        private void ValidateNamedHandlerConflicts(string requestType, List<HandlerInfo> handlers, IDiagnosticReporter diagnosticReporter)
        {
            // Get handler names
            var handlersByName = new Dictionary<string, List<HandlerInfo>>();

            foreach (var handler in handlers)
            {
                var handlerName = GetHandlerName(handler);

                if (!handlersByName.ContainsKey(handlerName))
                {
                    handlersByName[handlerName] = new List<HandlerInfo>();
                }
                handlersByName[handlerName].Add(handler);
            }

            // Check for conflicts
            foreach (var kvp in handlersByName)
            {
                var handlerName = kvp.Key;
                var handlersWithName = kvp.Value;

                if (handlersWithName.Count > 1)
                {
                    // Multiple handlers with the same name for the same request type
                    foreach (var handler in handlersWithName)
                    {
                        if (handler.Method == null) continue;
                        var diagnostic = Diagnostic.Create(DiagnosticDescriptors.NamedHandlerConflict,
                            handler.Method.GetLocation(), handlerName, requestType);
                        diagnosticReporter.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private string GetHandlerName(HandlerInfo handler)
        {
            var handleAttribute = handler.Attributes.FirstOrDefault(a => a.Type == RelayAttributeType.Handle);
            if (handleAttribute?.AttributeData != null)
            {
                // Try to get the Name property from the attribute
                var nameArg = handleAttribute.AttributeData.NamedArguments
                    .FirstOrDefault(arg => arg.Key == "Name");

                if (nameArg.Value.Value is string name && !string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }

            return "default";
        }

        private string GetRequestType(IMethodSymbol method)
        {
            if (method.Parameters.Length > 0)
            {
                return method.Parameters[0].Type.ToDisplayString();
            }
            return "Unknown";
        }

        /// <summary>
        /// Clears all internal caches. Useful for testing.
        /// </summary>
        public void ClearCaches()
        {
            _responseTypeCache.Clear();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Relay.SourceGenerator.Validators;
using Relay.SourceGenerator.Core;

namespace Relay.SourceGenerator
{
    /// <summary>
    /// Roslyn analyzer for Relay framework that provides compile-time validation
    /// of handler signatures, attribute usage, and configuration issues.
    /// Acts as an orchestrator for specialized validators.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RelayAnalyzer : DiagnosticAnalyzer
    {
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
                DiagnosticDescriptors.PerformanceWarning,
                DiagnosticDescriptors.MissingConfigureAwait,
                DiagnosticDescriptors.SyncOverAsync,
                DiagnosticDescriptors.PrivateHandler,
                DiagnosticDescriptors.InternalHandler,
                DiagnosticDescriptors.MultipleConstructors,
                DiagnosticDescriptors.ConstructorValueTypeParameter
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
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration);

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
                var methodSymbol = ValidationHelper.TryGetDeclaredSymbol(semanticModel, methodDeclaration);

                if (methodSymbol == null)
                    return;

                // Check for Relay attributes
                var handleAttribute = ValidationHelper.GetAttribute(methodSymbol, AttributeNames.Handle);
                var notificationAttribute = ValidationHelper.GetAttribute(methodSymbol, AttributeNames.Notification);
                var pipelineAttribute = ValidationHelper.GetAttribute(methodSymbol, AttributeNames.Pipeline);

                // Validate handler methods with [Handle] attribute
                if (handleAttribute != null)
                {
                    HandlerSignatureValidator.ValidateHandlerMethod(context, methodDeclaration, methodSymbol, handleAttribute);
                }

                // Validate notification handler methods with [Notification] attribute
                if (notificationAttribute != null)
                {
                    NotificationHandlerValidator.ValidateNotificationHandlerMethod(context, methodDeclaration, methodSymbol, notificationAttribute);
                }

                // Validate pipeline methods with [Pipeline] attribute
                if (pipelineAttribute != null)
                {
                    PipelineValidator.ValidatePipelineMethod(context, methodDeclaration, methodSymbol, pipelineAttribute);
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
                var attributeCache = new Dictionary<IMethodSymbol, (AttributeData? handle, AttributeData? pipeline)>(SymbolEqualityComparer.Default);

                // Collect all handler and pipeline methods across the compilation
                foreach (var syntaxTree in compilation.SyntaxTrees)
                {
                    try
                    {
                        var semanticModel = ValidationHelper.TryGetSemanticModel(compilation, syntaxTree);
                        if (semanticModel == null)
                            continue;

                        var root = syntaxTree.GetRoot(context.CancellationToken);

                        var methodDeclarations = root.DescendantNodes()
                            .OfType<MethodDeclarationSyntax>();

                        foreach (var methodDeclaration in methodDeclarations)
                        {
                            var methodSymbol = ValidationHelper.TryGetDeclaredSymbol(semanticModel, methodDeclaration);
                            if (methodSymbol == null) continue;

                            if (!attributeCache.TryGetValue(methodSymbol, out var attributes))
                            {
                                attributes = (
                                    ValidationHelper.GetAttribute(methodSymbol, AttributeNames.Handle),
                                    ValidationHelper.GetAttribute(methodSymbol, AttributeNames.Pipeline)
                                );
                                attributeCache.Add(methodSymbol, attributes);
                            }

                            var (handleAttribute, pipelineAttribute) = attributes;

                            if (handleAttribute != null)
                            {
                                handlerRegistry.AddHandler(methodSymbol, handleAttribute, methodDeclaration);
                            }

                            if (pipelineAttribute != null)
                            {
                                PipelineValidator.CollectPipelineInfo(pipelineRegistry, methodSymbol, pipelineAttribute, methodDeclaration);
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
                DuplicateHandlerValidator.ValidateDuplicateHandlers(context, handlerRegistry);

                // Validate for duplicate pipeline orders
                PipelineValidator.ValidateDuplicatePipelineOrders(context, pipelineRegistry);

                // Validate attribute parameter conflicts for all handlers
                ValidateAttributeParameterConflicts(context, handlerRegistry);
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

#pragma warning disable RS1005 // ReportDiagnostic invoked with an unsupported DiagnosticDescriptor
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                location,
                errorMessage);

            context.ReportDiagnostic(diagnostic);
#pragma warning restore RS1005
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

#pragma warning disable RS1005 // ReportDiagnostic invoked with an unsupported DiagnosticDescriptor
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.GeneratorError,
                Location.None,
                errorMessage);

            context.ReportDiagnostic(diagnostic);
#pragma warning restore RS1005
        }

        /// <summary>
        /// Validates attribute parameter conflicts for all handlers.
        /// </summary>
        private static void ValidateAttributeParameterConflicts(
            CompilationAnalysisContext context,
            HandlerRegistry handlerRegistry)
        {
            try
            {
                // Convert AnalyzerHandlerInfo to HandlerRegistration for validation
                var handlerRegistrations = ConvertToHandlerRegistrations(handlerRegistry);

                // Create a diagnostic reporter for the compilation analysis context
                var diagnosticReporter = new CompilationAnalysisContextDiagnosticReporter(context);

                // Use ConfigurationValidator to validate attribute parameter conflicts
                var validator = new ConfigurationValidator(diagnosticReporter);
                validator.ValidateAttributeParameterConflicts(handlerRegistrations);
            }
            catch (Exception ex)
            {
                ReportAnalyzerError(context, ex);
            }
        }

        /// <summary>
        /// Converts AnalyzerHandlerInfo objects to HandlerRegistration objects for validation.
        /// </summary>
        private static IEnumerable<HandlerRegistration> ConvertToHandlerRegistrations(HandlerRegistry handlerRegistry)
        {
            return handlerRegistry.Handlers.Select(handler => new HandlerRegistration
            {
                RequestType = handler.RequestType,
                ResponseType = null, // We don't have response type info in AnalyzerHandlerInfo
                Method = handler.MethodSymbol,
                Name = handler.Name,
                Priority = handler.Priority,
                Kind = HandlerKind.Request, // Default to Request kind
                Location = handler.Location,
                Attribute = handler.Attribute
            }).ToList();
        }
    }

    /// <summary>
    /// Adapter for IDiagnosticReporter that uses CompilationAnalysisContext.
    /// </summary>
    internal class CompilationAnalysisContextDiagnosticReporter : IDiagnosticReporter
    {
        private readonly CompilationAnalysisContext _context;

        public CompilationAnalysisContextDiagnosticReporter(CompilationAnalysisContext context)
        {
            _context = context;
        }

        public void ReportDiagnostic(Diagnostic diagnostic)
        {
#pragma warning disable RS1005 // ReportDiagnostic invoked with an unsupported DiagnosticDescriptor
            _context.ReportDiagnostic(diagnostic);
#pragma warning restore RS1005
        }
    }
}

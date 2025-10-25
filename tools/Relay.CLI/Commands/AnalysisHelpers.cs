using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.CLI.Commands;

internal static class AnalysisHelpers
{
    internal static bool IsHandler(ClassDeclarationSyntax classDecl, string content) =>
        classDecl.Identifier.ValueText.EndsWith("Handler") ||
        content.Contains("[Handle]") ||
        classDecl.BaseList?.Types.Any(t => t.ToString().Contains("IRequestHandler")) == true;

    internal static bool IsRequest(TypeDeclarationSyntax typeDecl, string content) =>
        typeDecl.Identifier.ValueText.EndsWith("Request") ||
        typeDecl.Identifier.ValueText.EndsWith("Query") ||
        typeDecl.Identifier.ValueText.EndsWith("Command") ||
        typeDecl.BaseList?.Types.Any(t => t.ToString().Contains("IRequest")) == true;

    internal static bool HasAsyncMethods(ClassDeclarationSyntax classDecl) =>
        classDecl.Members.OfType<MethodDeclarationSyntax>()
            .Any(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)));

    internal static bool HasConstructorDependencies(ClassDeclarationSyntax classDecl) =>
        classDecl.Members.OfType<ConstructorDeclarationSyntax>()
            .Any(c => c.ParameterList.Parameters.Count > 0);

    internal static bool UsesValueTask(ClassDeclarationSyntax classDecl, string content) =>
        content.Contains("ValueTask");

    internal static bool UsesCancellationToken(ClassDeclarationSyntax classDecl, string content) =>
        content.Contains("CancellationToken");

    internal static bool HasLogging(ClassDeclarationSyntax classDecl, string content) =>
        content.Contains("ILogger") || content.Contains("_logger");

    internal static bool HasValidation(ClassDeclarationSyntax classDecl, string content) =>
        content.Contains("ValidationAttribute") || content.Contains("[Required]");

    internal static int GetMethodLineCount(ClassDeclarationSyntax classDecl) =>
        classDecl.Members.OfType<MethodDeclarationSyntax>()
            .Sum(m => m.GetText().Lines.Count);

    internal static bool HasResponseType(TypeDeclarationSyntax typeDecl, string content) =>
        typeDecl.BaseList?.Types.Any(t => t.ToString().Contains("IRequest<")) == true;

    internal static bool HasValidationAttributes(TypeDeclarationSyntax typeDecl, string content) =>
        content.Contains("[Required]") || content.Contains("[StringLength]") || content.Contains("ValidationAttribute");

    internal static int GetParameterCount(TypeDeclarationSyntax typeDecl) =>
        typeDecl is RecordDeclarationSyntax record ?
            record.ParameterList?.Parameters.Count ?? 0 :
            typeDecl.Members.OfType<PropertyDeclarationSyntax>().Count();

    internal static bool HasCachingAttributes(TypeDeclarationSyntax typeDecl, string content) =>
        typeDecl.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString().Contains("Cacheable") || a.Name.ToString().Contains("Cache")));

    internal static bool HasAuthorizationAttributes(TypeDeclarationSyntax typeDecl, string content) =>
        typeDecl.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString().Contains("Authorize") || a.Name.ToString().Contains("Authorize")));
}
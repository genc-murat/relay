using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.CLI.Refactoring;

/// <summary>
/// Ensures proper disposal of IDisposable objects using 'using' statements
/// </summary>
public class DisposablePatternRule : IRefactoringRule
{
    public string RuleName => "DisposablePattern";
    public string Description => "Ensure IDisposable objects are properly disposed using 'using' statements";
    public RefactoringCategory Category => RefactoringCategory.BestPractices;

    private static readonly HashSet<string> CommonDisposableTypes = new()
    {
        "Stream", "StreamReader", "StreamWriter", "FileStream", "MemoryStream",
        "HttpClient", "HttpResponseMessage", "SqlConnection", "SqlCommand",
        "DbConnection", "DbCommand", "BinaryReader", "BinaryWriter",
        "StringReader", "StringWriter", "TextReader", "TextWriter"
    };

    public async Task<IEnumerable<RefactoringSuggestion>> AnalyzeAsync(string filePath, SyntaxNode root, RefactoringOptions options)
    {
        var suggestions = new List<RefactoringSuggestion>();

        var localDeclarations = root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>();

        foreach (var declaration in localDeclarations)
        {
            // Skip if already in a using statement
            if (declaration.Parent is UsingStatementSyntax)
                continue;

            // Check if the declaration has a using modifier (C# 8+)
            if (declaration.UsingKeyword.IsKind(SyntaxKind.UsingKeyword))
                continue;

            foreach (var variable in declaration.Declaration.Variables)
            {
                var typeName = declaration.Declaration.Type.ToString();

                // Also check the initializer for new expressions (e.g., new FileStream(...))
                string actualType = typeName;
                if (variable.Initializer != null &&
                    variable.Initializer.Value is ObjectCreationExpressionSyntax objectCreation)
                {
                    actualType = objectCreation.Type.ToString();
                }

                // Check if it's a known disposable type
                if (IsDisposableType(typeName) || IsDisposableType(actualType))
                {
                    // Check if there's a Dispose() call or using statement later
                    var method = declaration.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
                    if (method != null)
                    {
                        var variableName = variable.Identifier.Text;
                        var hasDispose = method.DescendantNodes()
                            .OfType<InvocationExpressionSyntax>()
                            .Any(inv => inv.Expression.ToString().StartsWith($"{variableName}.Dispose"));

                        if (!hasDispose)
                        {
                            var suggestedCode = $"using var {variable};";

                            suggestions.Add(new RefactoringSuggestion
                            {
                                RuleName = RuleName,
                                Description = $"Use 'using' for disposable variable '{variableName}'",
                                Category = Category,
                                Severity = RefactoringSeverity.Warning,
                                FilePath = filePath,
                                LineNumber = GetLineNumber(root, declaration),
                                StartPosition = declaration.Span.Start,
                                EndPosition = declaration.Span.End,
                                OriginalCode = declaration.ToString(),
                                SuggestedCode = suggestedCode,
                                Rationale = $"{actualType} implements IDisposable and should be disposed. Use 'using' to ensure proper cleanup.",
                                Context = declaration
                            });
                        }
                    }
                }
            }
        }

        // Find try-finally blocks that could be simplified to using statements
        var tryStatements = root.DescendantNodes().OfType<TryStatementSyntax>();

        foreach (var tryStatement in tryStatements)
        {
            if (tryStatement.Finally != null)
            {
                var finallyBlock = tryStatement.Finally.Block;

                // Check if finally block contains only Dispose calls
                if (finallyBlock.Statements.Count == 1 &&
                    finallyBlock.Statements[0] is ExpressionStatementSyntax exprStatement &&
                    exprStatement.Expression is InvocationExpressionSyntax invocation)
                {
                    var methodName = invocation.Expression.ToString();

                    if (methodName.EndsWith(".Dispose") || methodName.EndsWith("?.Dispose"))
                    {
                        var variableName = methodName.Replace(".Dispose()", "").Replace("?.Dispose()", "");

                        suggestions.Add(new RefactoringSuggestion
                        {
                            RuleName = RuleName,
                            Description = $"Replace try-finally with 'using' statement for '{variableName}'",
                            Category = Category,
                            Severity = RefactoringSeverity.Suggestion,
                            FilePath = filePath,
                            LineNumber = GetLineNumber(root, tryStatement),
                            StartPosition = tryStatement.Span.Start,
                            EndPosition = tryStatement.Span.End,
                            OriginalCode = tryStatement.ToString(),
                            SuggestedCode = $"// Consider converting to 'using' statement",
                            Rationale = "Using statements are cleaner and less error-prone than try-finally for disposal.",
                            Context = tryStatement
                        });
                    }
                }
            }
        }

        return await Task.FromResult(suggestions);
    }

    public async Task<SyntaxNode> ApplyRefactoringAsync(SyntaxNode root, RefactoringSuggestion suggestion)
    {
        if (suggestion.Context is LocalDeclarationStatementSyntax declaration)
        {
            // Add 'using' keyword to the declaration
            var usingDeclaration = declaration
                .WithUsingKeyword(SyntaxFactory.Token(SyntaxKind.UsingKeyword)
                    .WithTrailingTrivia(SyntaxFactory.Space))
                .WithLeadingTrivia(declaration.GetLeadingTrivia())
                .WithTrailingTrivia(declaration.GetTrailingTrivia());

            root = root.ReplaceNode(declaration, usingDeclaration);
        }

        return await Task.FromResult(root);
    }

    private bool IsDisposableType(string typeName)
    {
        // Remove generic type parameters and nullable markers
        var cleanTypeName = typeName.Split('<')[0].TrimEnd('?').Trim();

        // Check if the type name contains any of the common disposable type names
        return CommonDisposableTypes.Any(dt => cleanTypeName.Contains(dt, StringComparison.OrdinalIgnoreCase) ||
                                               cleanTypeName.EndsWith(dt, StringComparison.OrdinalIgnoreCase));
    }

    private int GetLineNumber(SyntaxNode root, SyntaxNode node)
    {
        var lineSpan = root.SyntaxTree.GetLineSpan(node.Span);
        return lineSpan.StartLinePosition.Line + 1;
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.CLI.Refactoring;

/// <summary>
/// Converts string concatenation and String.Format to string interpolation
/// </summary>
public class StringInterpolationRule : IRefactoringRule
{
    public string RuleName => "StringInterpolation";
    public string Description => "Convert string concatenation and String.Format to string interpolation";
    public RefactoringCategory Category => RefactoringCategory.Readability;

    public async Task<IEnumerable<RefactoringSuggestion>> AnalyzeAsync(string filePath, SyntaxNode root, RefactoringOptions options)
    {
        var suggestions = new List<RefactoringSuggestion>();

        // Find String.Format calls
        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name.ToString() == "Format" &&
                memberAccess.Expression.ToString() == "String")
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Count >= 1 &&
                    args[0].Expression is LiteralExpressionSyntax formatString &&
                    formatString.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    var format = formatString.Token.ValueText;

                    // Simple case: String.Format("{0}", value)
                    if (args.Count >= 2)
                    {
                        var interpolated = ConvertToInterpolation(format, args.Skip(1).Select(a => a.Expression.ToString()).ToArray());

                        if (interpolated != null)
                        {
                            suggestions.Add(new RefactoringSuggestion
                            {
                                RuleName = RuleName,
                                Description = "Replace String.Format with string interpolation",
                                Category = Category,
                                Severity = RefactoringSeverity.Suggestion,
                                FilePath = filePath,
                                LineNumber = GetLineNumber(root, invocation),
                                StartPosition = invocation.Span.Start,
                                EndPosition = invocation.Span.End,
                                OriginalCode = invocation.ToString(),
                                SuggestedCode = interpolated,
                                Rationale = "String interpolation is more readable than String.Format.",
                                Context = invocation
                            });
                        }
                    }
                }
            }
        }

        // Find string concatenation with +
        var binaryExpressions = root.DescendantNodes().OfType<BinaryExpressionSyntax>();

        foreach (var binary in binaryExpressions)
        {
            if (binary.IsKind(SyntaxKind.AddExpression))
            {
                var parts = GetConcatenationParts(binary);

                // Only suggest if there are at least 3 parts (or 2 parts with variables)
                if (parts.Count >= 2 && parts.Any(p => !p.isLiteral))
                {
                    var interpolated = ConvertConcatenationToInterpolation(parts);

                    suggestions.Add(new RefactoringSuggestion
                    {
                        RuleName = RuleName,
                        Description = "Replace string concatenation with string interpolation",
                        Category = Category,
                        Severity = RefactoringSeverity.Suggestion,
                        FilePath = filePath,
                        LineNumber = GetLineNumber(root, binary),
                        StartPosition = binary.Span.Start,
                        EndPosition = binary.Span.End,
                        OriginalCode = binary.ToString(),
                        SuggestedCode = interpolated,
                        Rationale = "String interpolation is more readable than concatenation with +.",
                        Context = binary
                    });
                }
            }
        }

        return await Task.FromResult(suggestions);
    }

    public async Task<SyntaxNode> ApplyRefactoringAsync(SyntaxNode root, RefactoringSuggestion suggestion)
    {
        if (suggestion.Context is InvocationExpressionSyntax invocation)
        {
            var newExpression = SyntaxFactory.ParseExpression(suggestion.SuggestedCode)
                .WithLeadingTrivia(invocation.GetLeadingTrivia())
                .WithTrailingTrivia(invocation.GetTrailingTrivia());

            root = root.ReplaceNode(invocation, newExpression);
        }
        else if (suggestion.Context is BinaryExpressionSyntax binary)
        {
            var newExpression = SyntaxFactory.ParseExpression(suggestion.SuggestedCode)
                .WithLeadingTrivia(binary.GetLeadingTrivia())
                .WithTrailingTrivia(binary.GetTrailingTrivia());

            root = root.ReplaceNode(binary, newExpression);
        }

        return await Task.FromResult(root);
    }

    private string? ConvertToInterpolation(string format, string[] arguments)
    {
        try
        {
            var result = format;

            for (int i = 0; i < arguments.Length; i++)
            {
                result = result.Replace($"{{{i}}}", $"{{{arguments[i]}}}");
                result = result.Replace($"{{{i}:", $"{{{arguments[i]}:");
            }

            return $"$\"{result}\"";
        }
        catch
        {
            return null;
        }
    }

    private List<(string text, bool isLiteral)> GetConcatenationParts(BinaryExpressionSyntax binary)
    {
        var parts = new List<(string text, bool isLiteral)>();

        void CollectParts(ExpressionSyntax expr)
        {
            if (expr is BinaryExpressionSyntax bin && bin.IsKind(SyntaxKind.AddExpression))
            {
                CollectParts(bin.Left);
                CollectParts(bin.Right);
            }
            else if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                parts.Add((literal.Token.ValueText, true));
            }
            else
            {
                parts.Add((expr.ToString(), false));
            }
        }

        CollectParts(binary);
        return parts;
    }

    private string ConvertConcatenationToInterpolation(List<(string text, bool isLiteral)> parts)
    {
        var result = new System.Text.StringBuilder("$\"");

        foreach (var (text, isLiteral) in parts)
        {
            if (isLiteral)
            {
                result.Append(text);
            }
            else
            {
                result.Append($"{{{text}}}");
            }
        }

        result.Append("\"");
        return result.ToString();
    }

    private int GetLineNumber(SyntaxNode root, SyntaxNode node)
    {
        var lineSpan = root.SyntaxTree.GetLineSpan(node.Span);
        return lineSpan.StartLinePosition.Line + 1;
    }
}

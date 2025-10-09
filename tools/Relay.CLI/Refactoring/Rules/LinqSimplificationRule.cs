using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.CLI.Refactoring;

/// <summary>
/// Simplifies LINQ expressions for better readability
/// </summary>
public class LinqSimplificationRule : IRefactoringRule
{
    public string RuleName => "LinqSimplification";
    public string Description => "Simplify LINQ expressions to more readable forms";
    public RefactoringCategory Category => RefactoringCategory.Readability;

    public async Task<IEnumerable<RefactoringSuggestion>> AnalyzeAsync(string filePath, SyntaxNode root, RefactoringOptions options)
    {
        var suggestions = new List<RefactoringSuggestion>();

        var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.ToString();

                // Where().Any() => Any()
                if (methodName == "Any" &&
                    memberAccess.Expression is InvocationExpressionSyntax whereInvocation &&
                    whereInvocation.Expression is MemberAccessExpressionSyntax whereMember &&
                    whereMember.Name.ToString() == "Where")
                {
                    var wherePredicate = whereInvocation.ArgumentList.Arguments.FirstOrDefault();
                    if (wherePredicate != null)
                    {
                        var suggestedCode = $"{whereMember.Expression}.Any({wherePredicate})";

                        suggestions.Add(new RefactoringSuggestion
                        {
                            RuleName = RuleName,
                            Description = "Replace Where().Any() with Any(predicate)",
                            Category = Category,
                            Severity = RefactoringSeverity.Suggestion,
                            FilePath = filePath,
                            LineNumber = GetLineNumber(root, invocation),
                            StartPosition = invocation.Span.Start,
                            EndPosition = invocation.Span.End,
                            OriginalCode = invocation.ToString(),
                            SuggestedCode = suggestedCode,
                            Rationale = "Combining Where().Any() into Any(predicate) is more efficient and readable.",
                            Context = invocation
                        });
                    }
                }

                // Where().First() => First()
                if (methodName == "First" &&
                    memberAccess.Expression is InvocationExpressionSyntax whereInvocationFirst &&
                    whereInvocationFirst.Expression is MemberAccessExpressionSyntax whereMemberFirst &&
                    whereMemberFirst.Name.ToString() == "Where")
                {
                    var wherePredicate = whereInvocationFirst.ArgumentList.Arguments.FirstOrDefault();
                    if (wherePredicate != null)
                    {
                        var suggestedCode = $"{whereMemberFirst.Expression}.First({wherePredicate})";

                        suggestions.Add(new RefactoringSuggestion
                        {
                            RuleName = RuleName,
                            Description = "Replace Where().First() with First(predicate)",
                            Category = Category,
                            Severity = RefactoringSeverity.Suggestion,
                            FilePath = filePath,
                            LineNumber = GetLineNumber(root, invocation),
                            StartPosition = invocation.Span.Start,
                            EndPosition = invocation.Span.End,
                            OriginalCode = invocation.ToString(),
                            SuggestedCode = suggestedCode,
                            Rationale = "Combining Where().First() into First(predicate) is more efficient.",
                            Context = invocation
                        });
                    }
                }

                // Where().Count() => Count()
                if (methodName == "Count" &&
                    memberAccess.Expression is InvocationExpressionSyntax whereInvocationCount &&
                    whereInvocationCount.Expression is MemberAccessExpressionSyntax whereMemberCount &&
                    whereMemberCount.Name.ToString() == "Where")
                {
                    var wherePredicate = whereInvocationCount.ArgumentList.Arguments.FirstOrDefault();
                    if (wherePredicate != null)
                    {
                        var suggestedCode = $"{whereMemberCount.Expression}.Count({wherePredicate})";

                        suggestions.Add(new RefactoringSuggestion
                        {
                            RuleName = RuleName,
                            Description = "Replace Where().Count() with Count(predicate)",
                            Category = Category,
                            Severity = RefactoringSeverity.Suggestion,
                            FilePath = filePath,
                            LineNumber = GetLineNumber(root, invocation),
                            StartPosition = invocation.Span.Start,
                            EndPosition = invocation.Span.End,
                            OriginalCode = invocation.ToString(),
                            SuggestedCode = suggestedCode,
                            Rationale = "Combining Where().Count() into Count(predicate) is more efficient.",
                            Context = invocation
                        });
                    }
                }

                // Select(x => x) can be removed
                if (methodName == "Select" && invocation.ArgumentList.Arguments.Count == 1)
                {
                    var argument = invocation.ArgumentList.Arguments[0];
                    if (argument.Expression is SimpleLambdaExpressionSyntax lambda)
                    {
                        var parameter = lambda.Parameter.Identifier.Text;
                        if (lambda.Body is IdentifierNameSyntax identifier &&
                            identifier.Identifier.Text == parameter)
                        {
                            var suggestedCode = memberAccess.Expression.ToString();

                            suggestions.Add(new RefactoringSuggestion
                            {
                                RuleName = RuleName,
                                Description = "Remove unnecessary Select(x => x)",
                                Category = Category,
                                Severity = RefactoringSeverity.Suggestion,
                                FilePath = filePath,
                                LineNumber = GetLineNumber(root, invocation),
                                StartPosition = invocation.Span.Start,
                                EndPosition = invocation.Span.End,
                                OriginalCode = invocation.ToString(),
                                SuggestedCode = suggestedCode,
                                Rationale = "Select(x => x) is redundant and can be removed.",
                                Context = invocation
                            });
                        }
                    }
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

        return await Task.FromResult(root);
    }

    private int GetLineNumber(SyntaxNode root, SyntaxNode node)
    {
        var lineSpan = root.SyntaxTree.GetLineSpan(node.Span);
        return lineSpan.StartLinePosition.Line + 1;
    }
}

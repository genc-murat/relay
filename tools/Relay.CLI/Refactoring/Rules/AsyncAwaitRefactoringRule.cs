using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.CLI.Refactoring;

/// <summary>
/// Refactors synchronous methods to use async/await patterns
/// </summary>
public class AsyncAwaitRefactoringRule : IRefactoringRule
{
    public string RuleName => "AsyncAwaitRefactoring";
    public string Description => "Convert synchronous blocking calls to async/await";
    public RefactoringCategory Category => RefactoringCategory.AsyncAwait;

    public async Task<IEnumerable<RefactoringSuggestion>> AnalyzeAsync(string filePath, SyntaxNode root, RefactoringOptions options)
    {
        var suggestions = new List<RefactoringSuggestion>();

        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

        foreach (var method in methods)
        {
            // Check for .Result or .Wait() calls
            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();

            foreach (var invocation in invocations)
            {
                var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
                if (memberAccess == null) continue;

                var methodName = memberAccess.Name.ToString();

                if (methodName == "Wait" || methodName == "GetAwaiter")
                {
                    suggestions.Add(new RefactoringSuggestion
                    {
                        RuleName = RuleName,
                        Description = $"Replace blocking {methodName}() with await",
                        Category = Category,
                        Severity = RefactoringSeverity.Warning,
                        FilePath = filePath,
                        LineNumber = GetLineNumber(root, invocation),
                        StartPosition = invocation.Span.Start,
                        EndPosition = invocation.Span.End,
                        OriginalCode = invocation.ToString(),
                        SuggestedCode = $"await {memberAccess.Expression}",
                        Rationale = "Blocking on async operations can lead to deadlocks and poor performance. Use await instead.",
                        Context = invocation
                    });
                }
            }

            // Check for .Result property access
            var memberAccesses = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>();

            foreach (var memberAccess in memberAccesses)
            {
                if (memberAccess.Name.ToString() == "Result")
                {
                    // Check if this is accessing Task<T>.Result
                    var expressionType = memberAccess.Expression.ToString();

                    // More permissive check - look for task variables or async method calls
                    if (expressionType.Contains("Async") || expressionType.Contains("Task") ||
                        expressionType.Contains("task") || expressionType.EndsWith("Async()") ||
                        memberAccess.Expression is IdentifierNameSyntax)
                    {
                        suggestions.Add(new RefactoringSuggestion
                        {
                            RuleName = RuleName,
                            Description = "Replace .Result with await",
                            Category = Category,
                            Severity = RefactoringSeverity.Warning,
                            FilePath = filePath,
                            LineNumber = GetLineNumber(root, memberAccess),
                            StartPosition = memberAccess.Span.Start,
                            EndPosition = memberAccess.Span.End,
                            OriginalCode = memberAccess.ToString(),
                            SuggestedCode = $"await {memberAccess.Expression}",
                            Rationale = "Accessing .Result blocks the thread. Use await for better async performance.",
                            Context = memberAccess
                        });
                    }
                }
            }
        }

        return await Task.FromResult(suggestions);
    }

    public async Task<SyntaxNode> ApplyRefactoringAsync(SyntaxNode root, RefactoringSuggestion suggestion)
    {
        SyntaxNode? contextNode = null;
        MethodDeclarationSyntax? method = null;

        if (suggestion.Context is MemberAccessExpressionSyntax memberAccess)
        {
            // Replace .Result with await
            var awaitExpression = SyntaxFactory.AwaitExpression(memberAccess.Expression)
                .WithLeadingTrivia(memberAccess.GetLeadingTrivia())
                .WithTrailingTrivia(memberAccess.GetTrailingTrivia());

            root = root.ReplaceNode(memberAccess, awaitExpression);
            contextNode = memberAccess;
            method = contextNode.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        }
        else if (suggestion.Context is InvocationExpressionSyntax invocation)
        {
            // Replace .Wait() with await
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccessInv)
            {
                var awaitExpression = SyntaxFactory.AwaitExpression(memberAccessInv.Expression)
                    .WithLeadingTrivia(invocation.GetLeadingTrivia())
                    .WithTrailingTrivia(invocation.GetTrailingTrivia());

                root = root.ReplaceNode(invocation, awaitExpression);
                contextNode = invocation;
                method = contextNode.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            }
        }

        // Make the containing method async if needed
        if (method != null && !method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
        {
            // Find the updated method in the new root
            var updatedMethod = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .First(m => m.Identifier.Text == method.Identifier.Text);

            var asyncModifier = SyntaxFactory.Token(SyntaxKind.AsyncKeyword);
            var newModifiers = updatedMethod.Modifiers.Add(asyncModifier);
            var newMethod = updatedMethod.WithModifiers(newModifiers);

            // Update return type if needed
            var returnType = updatedMethod.ReturnType.ToString();
            if (!returnType.StartsWith("Task"))
            {
                var newReturnType = returnType == "void"
                    ? SyntaxFactory.ParseTypeName("Task")
                    : SyntaxFactory.ParseTypeName($"Task<{returnType}>");

                // Preserve the original return type's trivia to maintain spacing
                newReturnType = newReturnType
                    .WithLeadingTrivia(updatedMethod.ReturnType.GetLeadingTrivia())
                    .WithTrailingTrivia(updatedMethod.ReturnType.GetTrailingTrivia());

                newMethod = newMethod.WithReturnType(newReturnType);
            }

            root = root.ReplaceNode(updatedMethod, newMethod);
        }

        return await Task.FromResult(root);
    }

    private int GetLineNumber(SyntaxNode root, SyntaxNode node)
    {
        var lineSpan = root.SyntaxTree.GetLineSpan(node.Span);
        return lineSpan.StartLinePosition.Line + 1;
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.CLI.Refactoring;

/// <summary>
/// Refactors null checks to use modern C# null-coalescing and null-conditional operators
/// </summary>
public class NullCheckRefactoringRule : IRefactoringRule
{
    public string RuleName => "NullCheckRefactoring";
    public string Description => "Modernize null checks with null-coalescing (??) and null-conditional (?.) operators";
    public RefactoringCategory Category => RefactoringCategory.Modernization;

    public async Task<IEnumerable<RefactoringSuggestion>> AnalyzeAsync(string filePath, SyntaxNode root, RefactoringOptions options)
    {
        var suggestions = new List<RefactoringSuggestion>();

        // Find if statements that can be converted to null-coalescing
        var ifStatements = root.DescendantNodes().OfType<IfStatementSyntax>();

        foreach (var ifStatement in ifStatements)
        {
            // Pattern: if (x == null) { x = y; }
            if (ifStatement.Condition is BinaryExpressionSyntax condition &&
                condition.IsKind(SyntaxKind.EqualsExpression))
            {
                if (condition.Right.IsKind(SyntaxKind.NullLiteralExpression) ||
                    condition.Left.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    var variableName = condition.Left.IsKind(SyntaxKind.NullLiteralExpression)
                        ? condition.Right.ToString()
                        : condition.Left.ToString();

                    // Check if the statement body is an assignment
                    var statement = ifStatement.Statement;
                    if (statement is BlockSyntax block && block.Statements.Count == 1)
                    {
                        statement = block.Statements[0];
                    }

                    if (statement is ExpressionStatementSyntax expressionStatement &&
                        expressionStatement.Expression is AssignmentExpressionSyntax assignment &&
                        assignment.Left.ToString() == variableName)
                    {
                        var suggestedCode = $"{variableName} ??= {assignment.Right};";

                        suggestions.Add(new RefactoringSuggestion
                        {
                            RuleName = RuleName,
                            Description = $"Replace if-null check with ??= operator",
                            Category = Category,
                            Severity = RefactoringSeverity.Suggestion,
                            FilePath = filePath,
                            LineNumber = GetLineNumber(root, ifStatement),
                            StartPosition = ifStatement.Span.Start,
                            EndPosition = ifStatement.Span.End,
                            OriginalCode = ifStatement.ToString(),
                            SuggestedCode = suggestedCode,
                            Rationale = "Null-coalescing assignment operator is more concise and readable.",
                            Context = ifStatement
                        });
                    }
                }
            }

            // Pattern: if (x != null) { y = x.Property; }
            if (ifStatement.Condition is BinaryExpressionSyntax notNullCondition &&
                notNullCondition.IsKind(SyntaxKind.NotEqualsExpression))
            {
                if (notNullCondition.Right.IsKind(SyntaxKind.NullLiteralExpression) ||
                    notNullCondition.Left.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    var variableName = notNullCondition.Left.IsKind(SyntaxKind.NullLiteralExpression)
                        ? notNullCondition.Right.ToString()
                        : notNullCondition.Left.ToString();

                    var statement = ifStatement.Statement;
                    if (statement is BlockSyntax block && block.Statements.Count == 1)
                    {
                        statement = block.Statements[0];
                    }

                    if (statement is ExpressionStatementSyntax expressionStatement &&
                        expressionStatement.Expression is AssignmentExpressionSyntax assignment)
                    {
                        var rightSide = assignment.Right.ToString();

                        // Check if right side accesses the variable
                        if (rightSide.StartsWith(variableName + "."))
                        {
                            var propertyAccess = rightSide.Substring(variableName.Length + 1);
                            var suggestedCode = $"{assignment.Left} = {variableName}?.{propertyAccess};";

                            suggestions.Add(new RefactoringSuggestion
                            {
                                RuleName = RuleName,
                                Description = "Replace null check with null-conditional operator (?.)  ",
                                Category = Category,
                                Severity = RefactoringSeverity.Suggestion,
                                FilePath = filePath,
                                LineNumber = GetLineNumber(root, ifStatement),
                                StartPosition = ifStatement.Span.Start,
                                EndPosition = ifStatement.Span.End,
                                OriginalCode = ifStatement.ToString(),
                                SuggestedCode = suggestedCode,
                                Rationale = "Null-conditional operator is safer and more concise.",
                                Context = ifStatement
                            });
                        }
                    }
                }
            }
        }

        // Find ternary expressions that can be simplified: x == null ? y : x
        var conditionalExpressions = root.DescendantNodes().OfType<ConditionalExpressionSyntax>();

        foreach (var conditional in conditionalExpressions)
        {
            if (conditional.Condition is BinaryExpressionSyntax binaryExpr &&
                binaryExpr.IsKind(SyntaxKind.EqualsExpression))
            {
                if (binaryExpr.Right.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    var variable = binaryExpr.Left.ToString();
                    var whenFalse = conditional.WhenFalse.ToString();

                    if (variable == whenFalse)
                    {
                        var suggestedCode = $"{variable} ?? {conditional.WhenTrue}";

                        suggestions.Add(new RefactoringSuggestion
                        {
                            RuleName = RuleName,
                            Description = "Replace ternary null check with ?? operator",
                            Category = Category,
                            Severity = RefactoringSeverity.Suggestion,
                            FilePath = filePath,
                            LineNumber = GetLineNumber(root, conditional),
                            StartPosition = conditional.Span.Start,
                            EndPosition = conditional.Span.End,
                            OriginalCode = conditional.ToString(),
                            SuggestedCode = suggestedCode,
                            Rationale = "Null-coalescing operator is more readable than ternary for null checks.",
                            Context = conditional
                        });
                    }
                }
            }
        }

        return await Task.FromResult(suggestions);
    }

    public async Task<SyntaxNode> ApplyRefactoringAsync(SyntaxNode root, RefactoringSuggestion suggestion)
    {
        if (suggestion.Context is IfStatementSyntax ifStatement)
        {
            // Parse the suggested code and create a new statement
            var newStatement = SyntaxFactory.ParseStatement(suggestion.SuggestedCode)
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

            root = root.ReplaceNode(ifStatement, newStatement);
        }
        else if (suggestion.Context is ConditionalExpressionSyntax conditional)
        {
            var newExpression = SyntaxFactory.ParseExpression(suggestion.SuggestedCode)
                .WithLeadingTrivia(conditional.GetLeadingTrivia())
                .WithTrailingTrivia(conditional.GetTrailingTrivia());

            root = root.ReplaceNode(conditional, newExpression);
        }

        return await Task.FromResult(root);
    }

    private int GetLineNumber(SyntaxNode root, SyntaxNode node)
    {
        var lineSpan = root.SyntaxTree.GetLineSpan(node.Span);
        return lineSpan.StartLinePosition.Line + 1;
    }
}

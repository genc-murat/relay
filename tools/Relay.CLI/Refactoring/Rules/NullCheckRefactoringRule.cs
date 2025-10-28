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
                            Description = "Replace if-null check with ??= operator",
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

                    // Handle assignment statements
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
                                Description = "Replace null check with null-conditional operator (?)",
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

                    // Handle return statements
                    if (statement is ReturnStatementSyntax returnStatement &&
                        returnStatement.Expression != null)
                    {
                        var returnExpression = returnStatement.Expression.ToString();

                        // Check if return expression accesses the variable
                        if (returnExpression.StartsWith(variableName + "."))
                        {
                            var propertyAccess = returnExpression.Substring(variableName.Length + 1);
                            var suggestedCode = $"return {variableName}?.{propertyAccess};";

                            suggestions.Add(new RefactoringSuggestion
                            {
                                RuleName = RuleName,
                                Description = "Replace null check with null-conditional operator (?)",
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

            // NEW: Pattern for nested property access with multiple conditions
            // if (obj != null && obj.Prop != null) { result = obj.Prop.Value; }
            var nestedSuggestion = AnalyzeNestedPropertyAccess(ifStatement, filePath, root);
            if (nestedSuggestion != null)
            {
                suggestions.Add(nestedSuggestion);
            }

            // NEW: Pattern for extending existing null-conditional chains
            var chainSuggestion = AnalyzeNullConditionalChain(ifStatement, filePath, root);
            if (chainSuggestion != null)
            {
                suggestions.Add(chainSuggestion);
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

    /// <summary>
    /// Analyzes nested property access patterns like:
    /// if (obj != null && obj.Prop != null) { result = obj.Prop.Value; }
    /// → result = obj?.Prop?.Value;
    /// </summary>
    private RefactoringSuggestion? AnalyzeNestedPropertyAccess(IfStatementSyntax ifStatement, string filePath, SyntaxNode root)
    {
        // Check if condition is a logical AND expression
        if (ifStatement.Condition is BinaryExpressionSyntax logicalAnd &&
            logicalAnd.IsKind(SyntaxKind.LogicalAndExpression))
        {
            var conditions = ExtractNullCheckConditions(logicalAnd);
            var totalConditions = CountAllConditions(logicalAnd);

            // Only suggest refactoring if ALL conditions are null checks
            if (conditions.Count < 2 || conditions.Count != totalConditions) return null;

            // Get the statement body
            var statement = ifStatement.Statement;
            if (statement is BlockSyntax block && block.Statements.Count == 1)
            {
                statement = block.Statements[0];
            }

            // Handle assignment statements
            if (statement is ExpressionStatementSyntax expressionStatement &&
                expressionStatement.Expression is AssignmentExpressionSyntax assignment)
            {
                var rightSide = assignment.Right.ToString();

                // Check if the assignment uses nested property access matching our conditions
                var nestedAccess = BuildNestedNullConditionalAccess(conditions, rightSide);
                if (nestedAccess != null)
                {
                    var suggestedCode = $"{assignment.Left} = {nestedAccess};";

                    return new RefactoringSuggestion
                    {
                        RuleName = RuleName,
                        Description = "Replace nested null checks with null-conditional operator chain (?.)",
                        Category = Category,
                        Severity = RefactoringSeverity.Suggestion,
                        FilePath = filePath,
                        LineNumber = GetLineNumber(root, ifStatement),
                        StartPosition = ifStatement.Span.Start,
                        EndPosition = ifStatement.Span.End,
                        OriginalCode = ifStatement.ToString(),
                        SuggestedCode = suggestedCode,
                        Rationale = "Null-conditional operator chains are safer and more concise than multiple null checks.",
                        Context = ifStatement
                    };
                }
            }

            // Handle return statements
            if (statement is ReturnStatementSyntax returnStatement &&
                returnStatement.Expression != null)
            {
                var returnExpression = returnStatement.Expression.ToString();

                // Check if the return expression uses nested property access matching our conditions
                var nestedAccess = BuildNestedNullConditionalAccess(conditions, returnExpression);
                if (nestedAccess != null)
                {
                    var suggestedCode = $"return {nestedAccess};";

                    return new RefactoringSuggestion
                    {
                        RuleName = RuleName,
                        Description = "Replace nested null checks with null-conditional operator chain (?.)",
                        Category = Category,
                        Severity = RefactoringSeverity.Suggestion,
                        FilePath = filePath,
                        LineNumber = GetLineNumber(root, ifStatement),
                        StartPosition = ifStatement.Span.Start,
                        EndPosition = ifStatement.Span.End,
                        OriginalCode = ifStatement.ToString(),
                        SuggestedCode = suggestedCode,
                        Rationale = "Null-conditional operator chains are safer and more concise than multiple null checks.",
                        Context = ifStatement
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Analyzes opportunities to extend existing null-conditional chains.
    /// Example: if (obj?.Prop != null) { result = obj?.Prop?.Value; }
    /// → result = obj?.Prop?.Value; (remove the if check)
    /// </summary>
    private RefactoringSuggestion? AnalyzeNullConditionalChain(IfStatementSyntax ifStatement, string filePath, SyntaxNode root)
    {
        // Check for pattern: if (obj?.Prop != null) { result = obj?.Prop?.Value; }
        if (ifStatement.Condition is BinaryExpressionSyntax condition &&
            condition.IsKind(SyntaxKind.NotEqualsExpression) &&
            condition.Right.IsKind(SyntaxKind.NullLiteralExpression))
        {
            var checkedExpression = condition.Left.ToString();

            // Only proceed if the condition actually contains null-conditional operators
            if (!checkedExpression.Contains("?."))
            {
                return null;
            }

            // Get the statement body
            var statement = ifStatement.Statement;
            if (statement is BlockSyntax block && block.Statements.Count == 1)
            {
                statement = block.Statements[0];
            }

            // Handle return statements
            if (statement is ReturnStatementSyntax returnStatement &&
                returnStatement.Expression != null)
            {
                var rightSide = returnStatement.Expression.ToString();

                // Check if the return expression already uses null-conditional and extends the checked expression
                if (rightSide.StartsWith(checkedExpression) && rightSide.Length > checkedExpression.Length)
                {
                    // This is a case where we can remove the redundant null check
                    var suggestedCode = $"return {rightSide};";

                    return new RefactoringSuggestion
                    {
                        RuleName = RuleName,
                        Description = "Remove redundant null check when null-conditional operator is already used",
                        Category = Category,
                        Severity = RefactoringSeverity.Suggestion,
                        FilePath = filePath,
                        LineNumber = GetLineNumber(root, ifStatement),
                        StartPosition = ifStatement.Span.Start,
                        EndPosition = ifStatement.Span.End,
                        OriginalCode = ifStatement.ToString(),
                        SuggestedCode = suggestedCode,
                        Rationale = "Null-conditional operators already handle null checks, making explicit checks redundant.",
                        Context = ifStatement
                    };
                }
            }

            // Handle assignment statements
            if (statement is ExpressionStatementSyntax expressionStatement &&
                expressionStatement.Expression is AssignmentExpressionSyntax assignment)
            {
                var rightSide = assignment.Right.ToString();

                // Check if the assignment already uses null-conditional and extends the checked expression
                if (rightSide.StartsWith(checkedExpression) && rightSide.Length > checkedExpression.Length)
                {
                    // This is a case where we can remove the redundant null check
                    var suggestedCode = $"{assignment.Left} = {rightSide};";

                    return new RefactoringSuggestion
                    {
                        RuleName = RuleName,
                        Description = "Remove redundant null check when null-conditional operator is already used",
                        Category = Category,
                        Severity = RefactoringSeverity.Suggestion,
                        FilePath = filePath,
                        LineNumber = GetLineNumber(root, ifStatement),
                        StartPosition = ifStatement.Span.Start,
                        EndPosition = ifStatement.Span.End,
                        OriginalCode = ifStatement.ToString(),
                        SuggestedCode = suggestedCode,
                        Rationale = "Null-conditional operators already handle null checks, making explicit checks redundant.",
                        Context = ifStatement
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts individual null check conditions from a logical AND expression
    /// </summary>
    private List<string> ExtractNullCheckConditions(BinaryExpressionSyntax logicalAnd)
    {
        var conditions = new List<string>();

        void ExtractConditions(ExpressionSyntax expr)
        {
            // Handle parentheses
            if (expr is ParenthesizedExpressionSyntax parenthesized)
            {
                ExtractConditions(parenthesized.Expression);
                return;
            }

            if (expr is BinaryExpressionSyntax binary)
            {
                if (binary.IsKind(SyntaxKind.LogicalAndExpression))
                {
                    ExtractConditions(binary.Left);
                    ExtractConditions(binary.Right);
                }
                else if (binary.IsKind(SyntaxKind.NotEqualsExpression) &&
                          binary.Right.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    conditions.Add(binary.Left.ToString());
                }
            }
        }

        ExtractConditions(logicalAnd);
        return conditions;
    }

    /// <summary>
    /// Counts all conditions in a logical AND expression
    /// </summary>
    private int CountAllConditions(BinaryExpressionSyntax logicalAnd)
    {
        var count = 0;

        void CountConditions(ExpressionSyntax expr)
        {
            // Handle parentheses
            if (expr is ParenthesizedExpressionSyntax parenthesized)
            {
                CountConditions(parenthesized.Expression);
                return;
            }

            if (expr is BinaryExpressionSyntax binary)
            {
                if (binary.IsKind(SyntaxKind.LogicalAndExpression))
                {
                    CountConditions(binary.Left);
                    CountConditions(binary.Right);
                }
                else
                {
                    count++;
                }
            }
            else
            {
                // Count non-binary expressions (like identifiers, literals, etc.)
                count++;
            }
        }

        CountConditions(logicalAnd);
        return count;
    }



    /// <summary>
    /// Builds a null-conditional access chain from conditions and target expression
    /// </summary>
    private string? BuildNestedNullConditionalAccess(List<string> conditions, string targetExpression)
    {
        if (conditions.Count == 0) return null;

        // Find the root variable
        var rootVariable = conditions[0].Split('.')[0];

        // Check if target expression starts with our root variable
        if (!targetExpression.StartsWith(rootVariable + ".")) return null;

        // Build the null-conditional chain
        var parts = targetExpression.Split('.');
        if (parts.Length < 2) return null;

        var result = new List<string> { parts[0] };

        for (int i = 1; i < parts.Length; i++)
        {
            var parentPath = string.Join(".", parts.Take(i));

            // Check if we have a null check for the parent path
            var hasCheck = conditions.Any(c => c == parentPath);
            if (hasCheck)
            {
                result.Add($"?.{parts[i]}");
            }
            else
            {
                result.Add($".{parts[i]}");
            }
        }

        return string.Join("", result);
    }

    public async Task<SyntaxNode> ApplyRefactoringAsync(SyntaxNode root, RefactoringSuggestion suggestion)
    {
        if (suggestion.Context is IfStatementSyntax ifStatement)
        {
            // For nested property access and chain extensions, we need to handle differently
            if (suggestion.Description.Contains("nested null checks") ||
                suggestion.Description.Contains("redundant null check"))
            {
                // Parse the suggested code and create a new statement
                var newStatement = SyntaxFactory.ParseStatement(suggestion.SuggestedCode)
                    .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

                root = root.ReplaceNode(ifStatement, newStatement);
            }
            else
            {
                // Original logic for simple null checks
                var newStatement = SyntaxFactory.ParseStatement(suggestion.SuggestedCode)
                    .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

                root = root.ReplaceNode(ifStatement, newStatement);
            }
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

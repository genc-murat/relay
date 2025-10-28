using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Relay.CLI.Refactoring;

/// <summary>
/// Refactors code to use modern C# pattern matching features
/// </summary>
public class PatternMatchingRefactoringRule : IRefactoringRule
{
    public string RuleName => "PatternMatchingRefactoring";
    public string Description => "Modernize code with C# pattern matching (switch expressions, is patterns, property patterns)";
    public RefactoringCategory Category => RefactoringCategory.Modernization;

    public async Task<IEnumerable<RefactoringSuggestion>> AnalyzeAsync(string filePath, SyntaxNode root, RefactoringOptions options)
    {
        var suggestions = new List<RefactoringSuggestion>();

        // Analyze for switch expression opportunities
        suggestions.AddRange(AnalyzeSwitchExpressions(root, filePath));

        // Analyze for is pattern opportunities
        suggestions.AddRange(AnalyzeIsPatterns(root, filePath));

        // Analyze for property pattern opportunities
        suggestions.AddRange(AnalyzePropertyPatterns(root, filePath));

        return await Task.FromResult(suggestions);
    }

    private IEnumerable<RefactoringSuggestion> AnalyzeSwitchExpressions(SyntaxNode root, string filePath)
    {
        var suggestions = new List<RefactoringSuggestion>();

        // Find if-else chains that can be converted to switch expressions
        var ifStatements = root.DescendantNodes().OfType<IfStatementSyntax>();

        foreach (var ifStatement in ifStatements)
        {
            if (CanConvertToSwitchExpression(ifStatement, out var switchExpression))
            {
                suggestions.Add(new RefactoringSuggestion
                {
                    RuleName = RuleName,
                    Description = "Convert if-else chain to switch expression",
                    Category = Category,
                    Severity = RefactoringSeverity.Suggestion,
                    FilePath = filePath,
                    LineNumber = GetLineNumber(root, ifStatement),
                    StartPosition = ifStatement.Span.Start,
                    EndPosition = ifStatement.Span.End,
                    OriginalCode = ifStatement.ToString(),
                    SuggestedCode = switchExpression,
                    Rationale = "Switch expressions are more concise and readable than if-else chains for multiple conditions."
                });
            }
        }

        // Find switch statements that can be converted to switch expressions
        var switchStatements = root.DescendantNodes().OfType<SwitchStatementSyntax>();

        foreach (var switchStatement in switchStatements)
        {
            if (CanConvertSwitchStatementToExpression(switchStatement, out var switchExpression))
            {
                suggestions.Add(new RefactoringSuggestion
                {
                    RuleName = RuleName,
                    Description = "Convert switch statement to switch expression",
                    Category = Category,
                    Severity = RefactoringSeverity.Suggestion,
                    FilePath = filePath,
                    LineNumber = GetLineNumber(root, switchStatement),
                    StartPosition = switchStatement.Span.Start,
                    EndPosition = switchStatement.Span.End,
                    OriginalCode = switchStatement.ToString(),
                    SuggestedCode = switchExpression,
                    Rationale = "Switch expressions are more concise than switch statements."
                });
            }
        }

        return suggestions;
    }

    private IEnumerable<RefactoringSuggestion> AnalyzeIsPatterns(SyntaxNode root, string filePath)
    {
        var suggestions = new List<RefactoringSuggestion>();

        // Find patterns like: if (obj is Type t) { use t; }
        var ifStatements = root.DescendantNodes().OfType<IfStatementSyntax>();

        foreach (var ifStatement in ifStatements)
        {
            if (CanConvertToIsPattern(ifStatement, out var patternCode))
            {
                suggestions.Add(new RefactoringSuggestion
                {
                    RuleName = RuleName,
                    Description = "Use is pattern instead of type checking",
                    Category = Category,
                    Severity = RefactoringSeverity.Suggestion,
                    FilePath = filePath,
                    LineNumber = GetLineNumber(root, ifStatement),
                    StartPosition = ifStatement.Span.Start,
                    EndPosition = ifStatement.Span.End,
                    OriginalCode = ifStatement.ToString(),
                    SuggestedCode = patternCode,
                    Rationale = "Is patterns combine type checking and casting in a single operation."
                });
            }
        }

        return suggestions;
    }

    private IEnumerable<RefactoringSuggestion> AnalyzePropertyPatterns(SyntaxNode root, string filePath)
    {
        var suggestions = new List<RefactoringSuggestion>();

        // Find complex if conditions that check multiple properties
        var ifStatements = root.DescendantNodes().OfType<IfStatementSyntax>();

        foreach (var ifStatement in ifStatements)
        {
            if (CanConvertToPropertyPattern(ifStatement, out var patternCode))
            {
                suggestions.Add(new RefactoringSuggestion
                {
                    RuleName = RuleName,
                    Description = "Use property pattern for complex conditions",
                    Category = Category,
                    Severity = RefactoringSeverity.Suggestion,
                    FilePath = filePath,
                    LineNumber = GetLineNumber(root, ifStatement),
                    StartPosition = ifStatement.Span.Start,
                    EndPosition = ifStatement.Span.End,
                    OriginalCode = ifStatement.ToString(),
                    SuggestedCode = patternCode,
                    Rationale = "Property patterns make complex conditional logic more readable."
                });
            }
        }

        return suggestions;
    }

    private bool CanConvertToSwitchExpression(IfStatementSyntax ifStatement, out string switchExpression)
    {
        switchExpression = string.Empty;

        // Simple check: if-else chain with constant comparisons
        // This is a simplified implementation - real implementation would be more complex
        var conditions = new List<string>();
        var bodies = new List<string>();
        var currentIf = ifStatement;

        while (currentIf != null)
        {
            if (currentIf.Condition is BinaryExpressionSyntax binary &&
                binary.IsKind(SyntaxKind.EqualsExpression))
            {
                var left = binary.Left.ToString();
                var right = binary.Right.ToString().Trim('"');

                if (IsConstantExpression(binary.Right))
                {
                    conditions.Add(right);
                    bodies.Add(GetStatementBody(currentIf.Statement));
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            currentIf = currentIf.Else?.Statement as IfStatementSyntax;
        }

        if (conditions.Count >= 2)
        {
            var variable = ifStatement.Condition is BinaryExpressionSyntax bin ? bin.Left.ToString() : "value";
            switchExpression = $"{variable} switch {{ {string.Join(", ", conditions.Zip(bodies, (c, b) => $"{c} => {b}"))}, _ => default }};";
            return true;
        }

        return false;
    }

    private bool CanConvertSwitchStatementToExpression(SwitchStatementSyntax switchStatement, out string switchExpression)
    {
        switchExpression = string.Empty;

        // Check if all case bodies are simple return/assignment statements
        var cases = switchStatement.Sections;
        if (cases.All(section => section.Statements.Count == 1 &&
                                (section.Statements[0] is ReturnStatementSyntax ||
                                 section.Statements[0] is ExpressionStatementSyntax expr &&
                                 expr.Expression is AssignmentExpressionSyntax)))
        {
            var variable = switchStatement.Expression.ToString();
            var caseExpressions = new List<string>();

            foreach (var section in cases)
            {
                var label = section.Labels.First().ToString().Replace("case ", "").Replace(":", "");
                var body = section.Statements[0] is ReturnStatementSyntax ret
                    ? ret.Expression?.ToString() ?? "default"
                    : section.Statements[0].ToString();

                caseExpressions.Add($"{label} => {body}");
            }

            switchExpression = $"{variable} switch {{ {string.Join(", ", caseExpressions)}, _ => default }};";
            return true;
        }

        return false;
    }

    private bool CanConvertToIsPattern(IfStatementSyntax ifStatement, out string patternCode)
    {
        patternCode = string.Empty;

        // Look for: if (obj is Type) { var t = (Type)obj; ... }
        if (ifStatement.Condition is BinaryExpressionSyntax binary &&
            binary.IsKind(SyntaxKind.IsExpression))
        {
            var variable = binary.Left.ToString();
            var type = binary.Right.ToString();

            // Check if the body has a cast
            var body = ifStatement.Statement;
            if (body is BlockSyntax block && block.Statements.Count >= 1)
            {
                var firstStatement = block.Statements[0];
                if (firstStatement is LocalDeclarationStatementSyntax localDecl &&
                    localDecl.Declaration.Variables.Count == 1)
                {
                    var variableDecl = localDecl.Declaration.Variables[0];
                    if (variableDecl.Initializer?.Value is CastExpressionSyntax cast &&
                        cast.Type.ToString() == type &&
                        cast.Expression.ToString() == variable)
                    {
                        var newVariable = variableDecl.Identifier.Text;
                        var remainingStatements = block.Statements.Skip(1);
                        var bodyCode = string.Join("\n", remainingStatements.Select(s => s.ToString()));

                        patternCode = $"if ({variable} is {type} {newVariable})\n{{\n{bodyCode}\n}}";
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private bool CanConvertToPropertyPattern(IfStatementSyntax ifStatement, out string patternCode)
    {
        patternCode = string.Empty;

        // Look for complex conditions checking multiple properties
        // This is simplified - real implementation would analyze the condition tree
        if (ifStatement.Condition is BinaryExpressionSyntax binary &&
            (binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression)))
        {
            // For now, just suggest property patterns for simple cases
            // Real implementation would be more sophisticated
            patternCode = $"// Consider using property pattern: {ifStatement.Condition}";
            return true;
        }

        return false;
    }

    private bool IsConstantExpression(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax;
    }

    private string GetStatementBody(StatementSyntax statement)
    {
        if (statement is BlockSyntax block && block.Statements.Count == 1)
        {
            return block.Statements[0] is ReturnStatementSyntax ret
                ? ret.Expression?.ToString() ?? "default"
                : block.Statements[0].ToString();
        }
        else if (statement is ReturnStatementSyntax ret)
        {
            return ret.Expression?.ToString() ?? "default";
        }

        return statement.ToString();
    }

    private int GetLineNumber(SyntaxNode root, SyntaxNode node)
    {
        var lineSpan = root.SyntaxTree.GetLineSpan(node.Span);
        return lineSpan.StartLinePosition.Line + 1;
    }

    public async Task<SyntaxNode> ApplyRefactoringAsync(SyntaxNode root, RefactoringSuggestion suggestion)
    {
        // Parse the suggested code and replace the original node
        var originalNode = root.FindNode(new TextSpan(suggestion.StartPosition, suggestion.EndPosition - suggestion.StartPosition));

        if (originalNode is IfStatementSyntax || originalNode is SwitchStatementSyntax)
        {
            var newNode = SyntaxFactory.ParseStatement(suggestion.SuggestedCode)
                .WithLeadingTrivia(originalNode.GetLeadingTrivia())
                .WithTrailingTrivia(originalNode.GetTrailingTrivia());

            root = root.ReplaceNode(originalNode, newNode);
        }

        return await Task.FromResult(root);
    }
}
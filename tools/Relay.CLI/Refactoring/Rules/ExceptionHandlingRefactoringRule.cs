using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Relay.CLI.Refactoring;

/// <summary>
/// Modernizes exception handling patterns in C# code
/// </summary>
public class ExceptionHandlingRefactoringRule : IRefactoringRule
{
    public string RuleName => "ExceptionHandlingRefactoring";
    public string Description => "Modernize exception handling with exception filters and best practices";
    public RefactoringCategory Category => RefactoringCategory.BestPractices;

    public async Task<IEnumerable<RefactoringSuggestion>> AnalyzeAsync(string filePath, SyntaxNode root, RefactoringOptions options)
    {
        var suggestions = new List<RefactoringSuggestion>();

        // Analyze for exception filter opportunities
        suggestions.AddRange(AnalyzeExceptionFilters(root, filePath));

        // Analyze for try-catch-finally improvements
        suggestions.AddRange(AnalyzeTryCatchFinally(root, filePath));

        return await Task.FromResult(suggestions);
    }

    private IEnumerable<RefactoringSuggestion> AnalyzeExceptionFilters(SyntaxNode root, string filePath)
    {
        var suggestions = new List<RefactoringSuggestion>();

        // Find catch clauses that can be converted to exception filters
        var catchClauses = root.DescendantNodes().OfType<CatchClauseSyntax>();

        foreach (var catchClause in catchClauses)
        {
            if (CanConvertToExceptionFilter(catchClause, out var filterCondition))
            {
                suggestions.Add(new RefactoringSuggestion
                {
                    RuleName = RuleName,
                    Description = "Use exception filter instead of catch and re-throw",
                    Category = Category,
                    Severity = RefactoringSeverity.Suggestion,
                    FilePath = filePath,
                    LineNumber = GetLineNumber(root, catchClause),
                    StartPosition = catchClause.Span.Start,
                    EndPosition = catchClause.Span.End,
                    OriginalCode = catchClause.ToString(),
                    SuggestedCode = $"catch ({catchClause.Declaration?.ToString() ?? "Exception"} ex) when ({filterCondition})\n{GetBlockContent(catchClause.Block)}",
                    Rationale = "Exception filters are more efficient and provide better performance than catching and re-throwing."
                });
            }
        }

        return suggestions;
    }

    private IEnumerable<RefactoringSuggestion> AnalyzeTryCatchFinally(SyntaxNode root, string filePath)
    {
        var suggestions = new List<RefactoringSuggestion>();

        // Find try-catch-finally blocks that can be improved
        var tryStatements = root.DescendantNodes().OfType<TryStatementSyntax>();

        foreach (var tryStatement in tryStatements)
        {
            // Check for empty finally blocks
            if (tryStatement.Finally != null && IsEmptyBlock(tryStatement.Finally.Block))
            {
                suggestions.Add(new RefactoringSuggestion
                {
                    RuleName = RuleName,
                    Description = "Remove empty finally block",
                    Category = Category,
                    Severity = RefactoringSeverity.Info,
                    FilePath = filePath,
                    LineNumber = GetLineNumber(root, tryStatement.Finally),
                    StartPosition = tryStatement.Finally.Span.Start,
                    EndPosition = tryStatement.Finally.Span.End,
                    OriginalCode = tryStatement.Finally.ToString(),
                    SuggestedCode = "",
                    Rationale = "Empty finally blocks provide no value and can be removed."
                });
            }

            // Check for catch-all without specific handling
            var catchClauses = tryStatement.Catches;
            foreach (var catchClause in catchClauses)
            {
                if (catchClause.Block != null && IsCatchAllWithoutHandling(catchClause))
                {
                    suggestions.Add(new RefactoringSuggestion
                    {
                        RuleName = RuleName,
                        Description = "Avoid catch-all without proper handling",
                        Category = Category,
                        Severity = RefactoringSeverity.Warning,
                        FilePath = filePath,
                        LineNumber = GetLineNumber(root, catchClause),
                        StartPosition = catchClause.Span.Start,
                        EndPosition = catchClause.Span.End,
                        OriginalCode = catchClause.ToString(),
                        SuggestedCode = "// Consider catching specific exceptions or removing if not needed",
                        Rationale = "Catch-all blocks can hide bugs and make debugging difficult."
                    });
                }
            }
        }

        return suggestions;
    }

    private bool CanConvertToExceptionFilter(CatchClauseSyntax catchClause, out string filterCondition)
    {
        filterCondition = string.Empty;

        // Look for patterns like:
        // catch (Exception ex) { if (condition) { ... } else { throw; } }
        if (catchClause.Block?.Statements.Count == 1 &&
            catchClause.Block.Statements[0] is IfStatementSyntax ifStatement)
        {
            // Check if the else clause just re-throws
            if (ifStatement.Else?.Statement != null &&
                IsRethrowStatement(ifStatement.Else.Statement))
            {
                // Extract the condition from the if statement
                filterCondition = ifStatement.Condition.ToString();
                return true;
            }
        }

        // Look for patterns like:
        // catch (Exception ex) { if (!condition) throw; ... }
        if (catchClause.Block?.Statements.Count >= 1)
        {
            var firstStatement = catchClause.Block.Statements[0];
            if (firstStatement is IfStatementSyntax conditionalStatement &&
                IsRethrowStatement(conditionalStatement.Statement) &&
                conditionalStatement.Else == null)
            {
                // Negate the condition for the filter
                var condition = conditionalStatement.Condition;
                if (condition is PrefixUnaryExpressionSyntax prefix &&
                    prefix.IsKind(SyntaxKind.LogicalNotExpression))
                {
                    filterCondition = prefix.Operand.ToString();
                    return true;
                }
                else
                {
                    filterCondition = $"!({condition})";
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsRethrowStatement(StatementSyntax statement)
    {
        if (statement is BlockSyntax block && block.Statements.Count == 1)
        {
            statement = block.Statements[0];
        }

        return statement is ThrowStatementSyntax throwStatement &&
               throwStatement.Expression == null; // Simple throw without expression
    }

    private bool IsEmptyBlock(BlockSyntax block)
    {
        return block.Statements.Count == 0;
    }

    private bool IsCatchAllWithoutHandling(CatchClauseSyntax catchClause)
    {
        // Check if it's a catch-all (no exception type specified)
        if (catchClause.Declaration == null)
        {
            // Check if the block is empty or just logs/re-throws
            if (catchClause.Block?.Statements.Count == 0)
                return true;

            // Check if it only contains logging or re-throwing
            foreach (var statement in catchClause.Block.Statements)
            {
                if (!IsLoggingStatement(statement) && !IsRethrowStatement(statement))
                {
                    return false; // Has actual handling
                }
            }
            return true; // Only logging or re-throwing
        }

        return false;
    }

    private bool IsLoggingStatement(StatementSyntax statement)
    {
        // Simple check for common logging patterns
        if (statement is ExpressionStatementSyntax exprStatement &&
            exprStatement.Expression is InvocationExpressionSyntax invocation)
        {
            var methodName = invocation.Expression.ToString();
            return methodName.Contains("Log") ||
                   methodName.Contains("WriteLine") ||
                   methodName.Contains("Debug") ||
                   methodName.Contains("Trace");
        }

        return false;
    }

    private string GetBlockContent(BlockSyntax block)
    {
        if (block == null) return "{}";

        var statements = string.Join("\n", block.Statements.Select(s => s.ToString()));
        return $"{{{statements}}}";
    }

    private int GetLineNumber(SyntaxNode root, SyntaxNode node)
    {
        var lineSpan = root.SyntaxTree.GetLineSpan(node.Span);
        return lineSpan.StartLinePosition.Line + 1;
    }

    public async Task<SyntaxNode> ApplyRefactoringAsync(SyntaxNode root, RefactoringSuggestion suggestion)
    {
        var originalNode = root.FindNode(new TextSpan(suggestion.StartPosition, suggestion.EndPosition - suggestion.StartPosition));

        if (originalNode is CatchClauseSyntax catchClause)
        {
            // For exception filters, we need to modify the catch clause
            // This is a simplified implementation - in practice, you'd need more sophisticated parsing
            var newCatchClause = catchClause.WithBlock(SyntaxFactory.Block(
                SyntaxFactory.ParseStatement("// Exception filter applied - manual review needed")
            ));

            root = root.ReplaceNode(catchClause, newCatchClause);
        }
        else if (originalNode is FinallyClauseSyntax finallyClause)
        {
            // Remove empty finally block
            var tryStatement = finallyClause.Parent as TryStatementSyntax;
            if (tryStatement != null)
            {
                var newTryStatement = tryStatement.WithFinally(null)
                    .WithTrailingTrivia(tryStatement.GetTrailingTrivia());

                root = root.ReplaceNode(tryStatement, newTryStatement);
            }
        }

        return await Task.FromResult(root);
    }
}
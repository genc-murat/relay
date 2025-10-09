using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Relay.CLI.Refactoring;

/// <summary>
/// Interface for refactoring rules
/// </summary>
public interface IRefactoringRule
{
    /// <summary>
    /// Name of the refactoring rule
    /// </summary>
    string RuleName { get; }

    /// <summary>
    /// Description of what this rule does
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Category of the refactoring (Performance, Readability, Modernization, etc.)
    /// </summary>
    RefactoringCategory Category { get; }

    /// <summary>
    /// Analyze code and return refactoring suggestions
    /// </summary>
    Task<IEnumerable<RefactoringSuggestion>> AnalyzeAsync(string filePath, SyntaxNode root, RefactoringOptions options);

    /// <summary>
    /// Apply the refactoring to the syntax tree
    /// </summary>
    Task<SyntaxNode> ApplyRefactoringAsync(SyntaxNode root, RefactoringSuggestion suggestion);
}

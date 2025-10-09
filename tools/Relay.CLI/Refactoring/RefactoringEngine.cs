using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Relay.CLI.Refactoring;

/// <summary>
/// Core refactoring engine for automated code improvements
/// </summary>
public class RefactoringEngine
{
    private readonly List<IRefactoringRule> _rules = new();

    public RefactoringEngine()
    {
        // Register built-in refactoring rules
        RegisterBuiltInRules();
    }

    public void RegisterRule(IRefactoringRule rule)
    {
        _rules.Add(rule);
    }

    private void RegisterBuiltInRules()
    {
        _rules.Add(new AsyncAwaitRefactoringRule());
        _rules.Add(new NullCheckRefactoringRule());
        _rules.Add(new LinqSimplificationRule());
        _rules.Add(new StringInterpolationRule());
        _rules.Add(new DisposablePatternRule());
    }

    public async Task<RefactoringResult> AnalyzeAsync(RefactoringOptions options)
    {
        var result = new RefactoringResult
        {
            StartTime = DateTime.UtcNow
        };

        var files = Directory.GetFiles(options.ProjectPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\Migrations\\"))
            .ToList();

        result.FilesAnalyzed = files.Count;

        foreach (var file in files)
        {
            var content = await File.ReadAllTextAsync(file);
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = await tree.GetRootAsync();

            var fileRefactorings = new List<RefactoringSuggestion>();

            foreach (var rule in _rules)
            {
                if (options.SpecificRules.Count > 0 && !options.SpecificRules.Contains(rule.RuleName))
                    continue;

                var suggestions = await rule.AnalyzeAsync(file, root, options);
                fileRefactorings.AddRange(suggestions);
            }

            if (fileRefactorings.Count > 0)
            {
                result.FileResults.Add(new FileRefactoringResult
                {
                    FilePath = file,
                    Suggestions = fileRefactorings
                });

                result.SuggestionsCount += fileRefactorings.Count;
            }
        }

        result.EndTime = DateTime.UtcNow;
        result.Duration = result.EndTime - result.StartTime;

        return result;
    }

    public async Task<ApplyResult> ApplyRefactoringsAsync(RefactoringOptions options, RefactoringResult analysis)
    {
        var result = new ApplyResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            foreach (var fileResult in analysis.FileResults)
            {
                if (fileResult.Suggestions.Count == 0)
                    continue;

                var content = await File.ReadAllTextAsync(fileResult.FilePath);
                var tree = CSharpSyntaxTree.ParseText(content);
                var root = await tree.GetRootAsync();

                // Apply refactorings in reverse order by position to maintain correctness
                var orderedSuggestions = fileResult.Suggestions
                    .OrderByDescending(s => s.StartPosition)
                    .ToList();

                var modified = false;

                foreach (var suggestion in orderedSuggestions)
                {
                    if (options.Interactive && !await PromptForApproval(suggestion))
                        continue;

                    var rule = _rules.FirstOrDefault(r => r.RuleName == suggestion.RuleName);
                    if (rule != null)
                    {
                        root = await rule.ApplyRefactoringAsync(root, suggestion);
                        modified = true;
                        result.RefactoringsApplied++;
                    }
                }

                if (modified)
                {
                    if (!options.DryRun)
                    {
                        var newContent = root.ToFullString();
                        await File.WriteAllTextAsync(fileResult.FilePath, newContent);
                        result.FilesModified++;
                    }
                }
            }

            result.Status = RefactoringStatus.Success;
        }
        catch (Exception ex)
        {
            result.Status = RefactoringStatus.Failed;
            result.Error = ex.Message;
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
        }

        return result;
    }

    private async Task<bool> PromptForApproval(RefactoringSuggestion suggestion)
    {
        Console.WriteLine($"\n{suggestion.RuleName}: {suggestion.Description}");
        Console.Write("Apply this refactoring? (y/n): ");
        var input = Console.ReadLine();
        return input?.ToLower() == "y";
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
        _rules.Add(new PatternMatchingRefactoringRule());
        _rules.Add(new ExceptionHandlingRefactoringRule());
    }

    public async Task<RefactoringResult> AnalyzeAsync(RefactoringOptions options)
    {
        var result = new RefactoringResult
        {
            StartTime = DateTime.UtcNow
        };

        List<string> files;
        try
        {
            var allCsFiles = Directory.GetFiles(options.ProjectPath, "*.cs", SearchOption.TopDirectoryOnly);
            files = allCsFiles
                .Where(f => ShouldIncludeFile(f, options.ProjectPath, options.ExcludePatterns))
                .ToList();

            // Debug: Log found files
            if (files.Count == 0 && allCsFiles.Length > 0)
            {
                Console.WriteLine($"Found {allCsFiles.Length} .cs files but all filtered out: {string.Join(", ", allCsFiles)}");
            }
            else if (allCsFiles.Length == 0)
            {
                var allFiles = Directory.GetFiles(options.ProjectPath, "*", SearchOption.AllDirectories);
                Console.WriteLine($"No .cs files found in {options.ProjectPath}. All files: {string.Join(", ", allFiles)}");
            }
        }
        catch (DirectoryNotFoundException)
        {
            // Directory doesn't exist, return empty result
            result.FilesAnalyzed = 0;
            result.EndTime = DateTime.UtcNow;
            result.Duration = result.EndTime - result.StartTime;
            return result;
        }

        result.FilesAnalyzed = files.Count;
        result.FilesSkipped = 0;

        var semaphore = new SemaphoreSlim(Environment.ProcessorCount);

        var analysisTasks = files.Select(async file =>
        {
            await semaphore.WaitAsync();
            try
            {
                var content = await File.ReadAllTextAsync(file);

                SyntaxNode root;
                try
                {
                    var parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None);
                    var tree = CSharpSyntaxTree.ParseText(content, parseOptions);
                    root = await tree.GetRootAsync();

                    // Check for syntax errors
                    var diagnostics = tree.GetDiagnostics();
                    if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                    {
                        Console.WriteLine($"Skipping file with syntax errors: {file}");
                        return new { FilePath = file, Suggestions = new List<RefactoringSuggestion>(), Skipped = true };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse file {file}: {ex.Message}");
                    return new { FilePath = file, Suggestions = new List<RefactoringSuggestion>(), Skipped = true };
                }

                var fileRefactorings = new List<RefactoringSuggestion>();

                foreach (var rule in _rules)
                {
                    if (options.SpecificRules.Count > 0 && !options.SpecificRules.Contains(rule.RuleName))
                        continue;

                    try
                    {
                        var suggestions = await rule.AnalyzeAsync(file, root, options);
                        fileRefactorings.AddRange(suggestions);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error analyzing file {file} with rule {rule.RuleName}: {ex.Message}");
                        // Continue with other rules
                    }
                }

                return new { FilePath = file, Suggestions = fileRefactorings, Skipped = false };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var analysisResults = await Task.WhenAll(analysisTasks);

        if (files.Count > 10)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        foreach (var analysisResult in analysisResults)
        {
            if (analysisResult.Suggestions.Count > 0)
            {
                result.FileResults.Add(new FileRefactoringResult
                {
                    FilePath = analysisResult.FilePath,
                    Suggestions = analysisResult.Suggestions
                });

                result.SuggestionsCount += analysisResult.Suggestions.Count;
            }

            // Count skipped files (those with no suggestions due to errors)
            if (analysisResult.Suggestions.Count == 0 && !string.IsNullOrEmpty(analysisResult.FilePath))
            {
                result.FilesSkipped++;
            }
        }

        // Update FilesAnalyzed to exclude skipped files
        result.FilesAnalyzed -= result.FilesSkipped;

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

                var parseOptions = CSharpParseOptions.Default.WithDocumentationMode(DocumentationMode.None);
                var tree = CSharpSyntaxTree.ParseText(content, parseOptions);
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
                        if (!options.DryRun)
                        {
                            result.RefactoringsApplied++;
                        }
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

    private bool ShouldIncludeFile(string filePath, string projectPath, List<string> excludePatterns)
    {
        // Default exclusions for generated code directories
        var defaultExclusions = new[] { "bin", "obj", "Migrations" };

        // Get the relative path from project root
        var relativePath = Path.GetRelativePath(projectPath, filePath);
        var relativeDirectories = Path.GetDirectoryName(relativePath)?.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            ?? Array.Empty<string>();

        foreach (var exclusion in defaultExclusions.Concat(excludePatterns))
        {
            // Check if any directory in the relative path matches the exclusion
            if (relativeDirectories.Any(dir => dir.Equals(exclusion, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> PromptForApproval(RefactoringSuggestion suggestion)
    {
        Console.WriteLine($"\n{suggestion.RuleName}: {suggestion.Description}");
        Console.Write("Apply this refactoring? (y/n): ");
        var input = Console.ReadLine();
        return input?.ToLower() == "y";
    }
}

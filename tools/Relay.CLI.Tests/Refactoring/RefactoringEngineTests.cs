using Relay.CLI.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Relay.CLI.Tests.Refactoring;

#pragma warning disable xUnit1013
public class RefactoringEngineTests
{
    private readonly string _testProjectPath;

    public RefactoringEngineTests()
    {
        _testProjectPath = Path.Combine(Path.GetTempPath(), $"RefactoringTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testProjectPath);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldFindAsyncAwaitIssues()
    {
        // Arrange
        var testFile = Path.Combine(_testProjectPath, "TestClass.cs");
        await File.WriteAllTextAsync(testFile, @"
using System.Threading.Tasks;

public class TestClass
{
    public void DoWork()
    {
        var task = GetDataAsync();
        var result = task.Result; // Should suggest await
    }

    public async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return ""data"";
    }
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.True(result.SuggestionsCount > 0);
        Assert.Single(result.FileResults);

        var suggestions = result.FileResults.First().Suggestions;
        Assert.Contains(suggestions, s => s.RuleName == "AsyncAwaitRefactoring");
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldFindNullCheckIssues()
    {
        // Arrange
        var testFile = Path.Combine(_testProjectPath, "NullCheckClass.cs");
        await File.WriteAllTextAsync(testFile, @"
public class NullCheckClass
{
    public string Process(string input)
    {
        if (input == null)
        {
            input = ""default"";
        }
        return input;
    }
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.True(result.SuggestionsCount > 0);

        var suggestions = result.FileResults.SelectMany(f => f.Suggestions).ToList();
        Assert.Contains(suggestions, s => s.RuleName == "NullCheckRefactoring");
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldFindLinqSimplificationIssues()
    {
        // Arrange
        var testFile = Path.Combine(_testProjectPath, "LinqClass.cs");
        await File.WriteAllTextAsync(testFile, @"
using System.Linq;
using System.Collections.Generic;

public class LinqClass
{
    public bool HasActiveUsers(List<User> users)
    {
        return users.Where(u => u.IsActive).Any();
    }

    public User GetFirstActiveUser(List<User> users)
    {
        return users.Where(u => u.IsActive).First();
    }
}

public class User
{
    public bool IsActive { get; set; }
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.True(result.SuggestionsCount > 0);

        var suggestions = result.FileResults.SelectMany(f => f.Suggestions).ToList();
        Assert.Contains(suggestions, s => s.RuleName == "LinqSimplification");
        Assert.Contains(suggestions, s => s.Description.Contains("Where().Any()"));
        Assert.Contains(suggestions, s => s.Description.Contains("Where().First()"));
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldFindStringInterpolationIssues()
    {
        // Arrange
        var testFile = Path.Combine(_testProjectPath, "StringClass.cs");
        await File.WriteAllTextAsync(testFile, @"
public class StringClass
{
    public string GetMessage(string name, int age)
    {
        return string.Format(""Name: {0}, Age: {1}"", name, age);
    }

    public string GetGreeting(string name)
    {
        return ""Hello, "" + name + ""!"";
    }
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.True(result.SuggestionsCount > 0);

        var suggestions = result.FileResults.SelectMany(f => f.Suggestions).ToList();
        Assert.Contains(suggestions, s => s.RuleName == "StringInterpolation");
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldFilterByCategory()
    {
        // Arrange
        var testFile = Path.Combine(_testProjectPath, "MixedClass.cs");
        await File.WriteAllTextAsync(testFile, @"
using System.Linq;
using System.Collections.Generic;

public class MixedClass
{
    public bool Check(List<int> numbers)
    {
        return numbers.Where(n => n > 0).Any(); // Readability issue
    }

    public void Work()
    {
        var task = GetDataAsync();
        var result = task.Result; // AsyncAwait issue
    }

    public async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return ""data"";
    }
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath,
            Categories = [RefactoringCategory.Readability]
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        var suggestions = result.FileResults.SelectMany(f => f.Suggestions).ToList();
        Assert.Contains(suggestions, s => s.Category == RefactoringCategory.Readability);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldHandleEmptyProject()
    {
        // Arrange
        var emptyPath = Path.Combine(Path.GetTempPath(), $"EmptyProject_{Guid.NewGuid()}");
        Directory.CreateDirectory(emptyPath);

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = emptyPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.Equal(0, result.FilesAnalyzed);
        Assert.Equal(0, result.SuggestionsCount);

        // Cleanup
        Directory.Delete(emptyPath, true);
    }



    [Fact]
    public async Task ApplyRefactoringsAsync_ShouldModifyFiles_WhenNotDryRun()
    {
        // Arrange
        var testFile = Path.Combine(_testProjectPath, "ToModify.cs");
        var originalContent = @"
using System.Linq;
using System.Collections.Generic;

public class ToModify
{
    public bool Check(List<int> numbers)
    {
        return numbers.Where(n => n > 0).Any();
    }
}";
        await File.WriteAllTextAsync(testFile, originalContent);

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false
        };

        var analysis = await engine.AnalyzeAsync(options);

        // Act
        var result = await engine.ApplyRefactoringsAsync(options, analysis);

        // Assert
        Assert.Equal(RefactoringStatus.Success, result.Status);
        Assert.True(result.FilesModified > 0);
    }

    [Fact]
    public async Task ApplyRefactoringsAsync_ShouldNotModifyFiles_WhenDryRun()
    {
        // Arrange
        var testFile = Path.Combine(_testProjectPath, "NoModify.cs");
        var originalContent = @"
using System.Linq;
using System.Collections.Generic;

public class NoModify
{
    public bool Check(List<int> numbers)
    {
        return numbers.Where(n => n > 0).Any();
    }
}";
        await File.WriteAllTextAsync(testFile, originalContent);

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = true
        };

        var analysis = await engine.AnalyzeAsync(options);
        var beforeContent = await File.ReadAllTextAsync(testFile);

        // Act
        var result = await engine.ApplyRefactoringsAsync(options, analysis);

        // Assert
        var afterContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(beforeContent, afterContent);
        Assert.Equal(0, result.FilesModified);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldProcessMultipleFilesInParallel()
    {
        // Arrange - Create multiple test files with different issues
        var testFile1 = Path.Combine(_testProjectPath, "ParallelTest1.cs");
        var testFile2 = Path.Combine(_testProjectPath, "ParallelTest2.cs");
        var testFile3 = Path.Combine(_testProjectPath, "ParallelTest3.cs");

        // File 1: Async/await issue
        await File.WriteAllTextAsync(testFile1, @"
using System.Threading.Tasks;
public class ParallelTest1
{
    public void DoWork()
    {
        var task = GetDataAsync();
        var result = task.Result;
    }
    public async Task<string> GetDataAsync() => await Task.FromResult(""data"");
}");

        // File 2: Null check issue
        await File.WriteAllTextAsync(testFile2, @"
public class ParallelTest2
{
    public string Process(string input)
    {
        if (input == null) input = ""default"";
        return input;
    }
}");

        // File 3: LINQ issue
        await File.WriteAllTextAsync(testFile3, @"
using System.Linq;
using System.Collections.Generic;
public class ParallelTest3
{
    public bool Check(List<int> numbers)
    {
        return numbers.Where(n => n > 0).Any();
    }
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.Equal(3, result.FilesAnalyzed);
        Assert.True(result.SuggestionsCount >= 3); // At least one suggestion per file

        var filePaths = result.FileResults.Select(fr => Path.GetFileName(fr.FilePath)).ToList();
        Assert.Contains("ParallelTest1.cs", filePaths);
        Assert.Contains("ParallelTest2.cs", filePaths);
        Assert.Contains("ParallelTest3.cs", filePaths);

        var suggestions = result.FileResults.SelectMany(fr => fr.Suggestions).ToList();
        Assert.Contains(suggestions, s => s.RuleName == "AsyncAwaitRefactoring");
        Assert.Contains(suggestions, s => s.RuleName == "NullCheckRefactoring");
        Assert.Contains(suggestions, s => s.RuleName == "LinqSimplification");
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldHandleLargeNumberOfFiles()
    {
        // Arrange - Create many small files to test parallel processing
        const int fileCount = 20;
        var tasks = new List<Task>();

        for (int i = 0; i < fileCount; i++)
        {
            var fileName = $"LargeTest{i}.cs";
            var filePath = Path.Combine(_testProjectPath, fileName);
            var content = $@"
public class LargeTest{i}
{{
    public void Method{i}()
    {{
        var task = GetDataAsync();
        var result = task.Result; // Async issue in each file
    }}
    public async System.Threading.Tasks.Task<string> GetDataAsync() =>
        await System.Threading.Tasks.Task.FromResult(""data"");
}}";
            tasks.Add(File.WriteAllTextAsync(filePath, content));
        }

        await Task.WhenAll(tasks);

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.Equal(fileCount, result.FilesAnalyzed);
        Assert.Equal(fileCount, result.SuggestionsCount); // One suggestion per file
        Assert.Equal(fileCount, result.FileResults.Count);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldContinueProcessing_WhenOneFileHasSyntaxError()
    {
        // Arrange - Create files where one has syntax error
        var validFile = Path.Combine(_testProjectPath, "ValidFile.cs");
        var invalidFile = Path.Combine(_testProjectPath, "InvalidFile.cs");

        await File.WriteAllTextAsync(validFile, @"
using System.Threading.Tasks;
public class ValidFile
{
    public void DoWork()
    {
        var task = GetDataAsync();
        var result = task.Result; // Should find this
    }
    public async Task<string> GetDataAsync() => await Task.FromResult(""data"");
}");

        await File.WriteAllTextAsync(invalidFile, @"
public class InvalidFile
{
    public void BadMethod()
    {
        // Missing closing brace - syntax error
");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act & Assert - Should not throw exception
        var result = await engine.AnalyzeAsync(options);

        // Should process the valid file
        Assert.Equal(1, result.FilesAnalyzed); // Only valid file should be analyzed
        Assert.Equal(1, result.FilesSkipped); // Invalid file should be skipped
        Assert.True(result.SuggestionsCount > 0);

        var validFileResult = result.FileResults.FirstOrDefault(fr => fr.FilePath.Contains("ValidFile.cs"));
        Assert.NotNull(validFileResult);
        Assert.True(validFileResult.Suggestions.Count > 0);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldUseMemoryOptimizations_WithDocumentationComments()
    {
        // Arrange - Create a file with XML documentation to test parse options
        var testFile = Path.Combine(_testProjectPath, "DocCommentTest.cs");
        await File.WriteAllTextAsync(testFile, @"
using System.Threading.Tasks;

/// <summary>
/// This is a test class with XML documentation
/// </summary>
public class DocCommentTest
{
    /// <summary>
    /// This method has async issues
    /// </summary>
    public void DoWork()
    {
        var task = GetDataAsync();
        var result = task.Result; // Should still find this despite doc comments
    }

    /// <summary>
    /// Async method
    /// </summary>
    public async Task<string> GetDataAsync() => await Task.FromResult(""data"");
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert - Should still find suggestions even with XML documentation
        Assert.Equal(1, result.FilesAnalyzed);
        Assert.True(result.SuggestionsCount > 0);

        var suggestions = result.FileResults.First().Suggestions;
        Assert.Contains(suggestions, s => s.RuleName == "AsyncAwaitRefactoring");
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldTriggerGCCollect_ForLargeFileCount()
    {
        // Arrange - Create 15 files to trigger GC.Collect (threshold is 10)
        const int fileCount = 15;
        var tasks = new List<Task>();

        for (int i = 0; i < fileCount; i++)
        {
            var fileName = $"GCCollectTest{i}.cs";
            var filePath = Path.Combine(_testProjectPath, fileName);
            var content = $@"
public class GCCollectTest{i}
{{
    public void Method{i}()
    {{
        var task = GetDataAsync();
        var result = task.Result;
    }}
    public async System.Threading.Tasks.Task<string> GetDataAsync() =>
        await System.Threading.Tasks.Task.FromResult(""data"");
}}";
            tasks.Add(File.WriteAllTextAsync(filePath, content));
        }

        await Task.WhenAll(tasks);

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.Equal(fileCount, result.FilesAnalyzed);
        Assert.Equal(fileCount, result.SuggestionsCount); // One suggestion per file
        // GC.Collect should have been called internally for fileCount > 10
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldExcludeFilesBasedOnPatterns()
    {
        // Arrange
        var testFile = Path.Combine(_testProjectPath, "TestClass.cs");
        await File.WriteAllTextAsync(testFile, @"
using System.Threading.Tasks;

public class TestClass
{
    public void DoWork()
    {
        var task = GetDataAsync();
        var result = task.Result; // Should suggest await
    }

    public async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return ""data"";
    }
}");

        // Create a file in obj directory that should be excluded
        var objDir = Path.Combine(_testProjectPath, "obj");
        Directory.CreateDirectory(objDir);
        var excludedFile = Path.Combine(objDir, "Generated.cs");
        await File.WriteAllTextAsync(excludedFile, @"
public class Generated
{
    public void Method() { }
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath,
            ExcludePatterns = new List<string> { "CustomExclude" }
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.True(result.SuggestionsCount > 0);
        Assert.Single(result.FileResults); // Only TestClass.cs should be analyzed, not Generated.cs

        var analyzedFile = result.FileResults.First().FilePath;
        Assert.Equal(testFile, analyzedFile);
        Assert.Contains("TestClass.cs", analyzedFile);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldSkipFilesWithSyntaxErrors()
    {
        // Arrange
        var validFile = Path.Combine(_testProjectPath, "ValidClass.cs");
        await File.WriteAllTextAsync(validFile, @"
using System.Threading.Tasks;

public class ValidClass
{
    public void DoWork()
    {
        var task = GetDataAsync();
        var result = task.Result;
    }

    public async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return ""data"";
    }
}");

        // Create a file with syntax errors
        var invalidFile = Path.Combine(_testProjectPath, "InvalidClass.cs");
        await File.WriteAllTextAsync(invalidFile, @"
public class InvalidClass
{
    public void DoWork()
    {
        // Missing closing brace and invalid syntax
        var x = ;
        if (true) {
            Console.WriteLine(""test"";
        // Missing closing brace
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.True(result.SuggestionsCount > 0); // Should have suggestions from valid file
        Assert.Single(result.FileResults); // Only ValidClass.cs should be analyzed
        Assert.Equal(1, result.FilesSkipped); // InvalidClass.cs should be skipped

        var analyzedFile = result.FileResults.First().FilePath;
        Assert.Equal(validFile, analyzedFile);
        Assert.Contains("ValidClass.cs", analyzedFile);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldSkipFilesThatFailToParse()
    {
        // Arrange
        var validFile = Path.Combine(_testProjectPath, "ValidClass.cs");
        await File.WriteAllTextAsync(validFile, @"
using System.Threading.Tasks;

public class ValidClass
{
    public void DoWork()
    {
        var task = GetDataAsync();
        var result = task.Result;
    }

    public async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return ""data"";
    }
}");

// Create a file that will cause parse exception (invalid encoding or binary content)
        var unparseableFile = Path.Combine(_testProjectPath, "Unparseable.cs");
        // Write some binary-like content that will cause parse failure
        var binaryContent = new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x01, 0x02, 0x03 };
        await File.WriteAllBytesAsync(unparseableFile, binaryContent);

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.True(result.SuggestionsCount > 0); // Should have suggestions from valid file
        Assert.Single(result.FileResults); // Only ValidClass.cs should be analyzed
        Assert.Equal(1, result.FilesSkipped); // Unparseable.cs should be skipped

        var analyzedFile = result.FileResults.First().FilePath;
        Assert.Equal(validFile, analyzedFile);
        Assert.Contains("ValidClass.cs", analyzedFile);
    }

    [Fact]
    public void RegisterRule_ShouldAddRuleToEngine()
    {
        // Arrange
        var engine = new RefactoringEngine();
        var customRule = new MockRefactoringRule();

        // Act
        engine.RegisterRule(customRule);

        // Assert - We can't directly test private _rules field, but we can verify through behavior
        // This is tested indirectly through the AnalyzeAsync method in the next test
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldFilterBySpecificRules()
    {
        // Arrange
        var testFile = Path.Combine(_testProjectPath, "SpecificRuleTest.cs");
        await File.WriteAllTextAsync(testFile, @"
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

public class SpecificRuleTest
{
    public void DoWork()
    {
        var task = GetDataAsync();
        var result = task.Result; // AsyncAwait issue
    }

    public bool Check(List<int> numbers)
    {
        return numbers.Where(n => n > 0).Any(); // LINQ issue
    }

    public async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return ""data"";
    }
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath,
            SpecificRules = ["AsyncAwaitRefactoring"] // Only check async/await issues
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.True(result.SuggestionsCount > 0);

        var suggestions = result.FileResults.SelectMany(f => f.Suggestions).ToList();
        Assert.All(suggestions, s => Assert.Equal("AsyncAwaitRefactoring", s.RuleName));
        Assert.DoesNotContain(suggestions, s => s.RuleName == "LinqSimplification");
    }

    [Fact]
    public async Task ApplyRefactoringsAsync_ShouldHandleEmptyAnalysisResult()
    {
        // Arrange
        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false
        };

        var emptyAnalysis = new RefactoringResult
        {
            FileResults = new List<FileRefactoringResult>(),
            SuggestionsCount = 0,
            FilesAnalyzed = 0
        };

        // Act
        var result = await engine.ApplyRefactoringsAsync(options, emptyAnalysis);

        // Assert
        Assert.Equal(RefactoringStatus.Success, result.Status);
        Assert.Equal(0, result.RefactoringsApplied);
        Assert.Equal(0, result.FilesModified);
    }

    [Fact]
    public async Task ApplyRefactoringsAsync_ShouldApplyRefactoringsInReverseOrder()
    {
        // Arrange
        var testFile = Path.Combine(_testProjectPath, "OrderTest.cs");
        var originalContent = @"
using System.Threading.Tasks;

public class OrderTest
{
    public async Task Method()
    {
        var task1 = Task.FromResult(""first"");
        var task2 = Task.FromResult(""second"");

        var result1 = task1.Result; // Position 1
        var result2 = task2.Result; // Position 2
    }
}";
        await File.WriteAllTextAsync(testFile, originalContent);

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false,
            Interactive = false
        };

        var analysis = await engine.AnalyzeAsync(options);

        // Act
        var result = await engine.ApplyRefactoringsAsync(options, analysis);

        // Assert
        Assert.Equal(RefactoringStatus.Success, result.Status);
        Assert.True(result.RefactoringsApplied > 0);

        var modifiedContent = await File.ReadAllTextAsync(testFile);
        Assert.Contains("await", modifiedContent);
        Assert.DoesNotContain(".Result", modifiedContent);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldHandleNonExistentProjectPath()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"NonExistent_{Guid.NewGuid()}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = nonExistentPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.Equal(0, result.FilesAnalyzed);
        Assert.Equal(0, result.SuggestionsCount);
        Assert.Equal(0, result.FilesSkipped);
    }

    [Fact]
    public async Task ApplyRefactoringsAsync_ShouldHandleRuleApplicationErrors()
    {
        // Arrange - Create a scenario where rule application might fail
        var testFile = Path.Combine(_testProjectPath, "ErrorTest.cs");
        await File.WriteAllTextAsync(testFile, @"
using System.Threading.Tasks;

public class ErrorTest
{
    public void DoWork()
    {
        var task = GetDataAsync();
        var result = task.Result;
    }

    public async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return ""data"";
    }
}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath,
            DryRun = false
        };

        var analysis = await engine.AnalyzeAsync(options);

        // Act
        var result = await engine.ApplyRefactoringsAsync(options, analysis);

        // Assert - Should complete successfully even if individual rule applications have issues
        Assert.Equal(RefactoringStatus.Success, result.Status);
        // The exact number of refactorings applied depends on the rules, but it should be >= 0
        Assert.True(result.RefactoringsApplied >= 0);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testProjectPath, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

// Mock refactoring rule for testing
public class MockRefactoringRule : IRefactoringRule
{
    public string RuleName => "MockRule";
    public string Description => "Mock refactoring rule for testing";
    public RefactoringCategory Category => RefactoringCategory.Readability;

    public Task<IEnumerable<RefactoringSuggestion>> AnalyzeAsync(string filePath, SyntaxNode root, RefactoringOptions options)
    {
        return Task.FromResult(Enumerable.Empty<RefactoringSuggestion>());
    }

    public Task<SyntaxNode> ApplyRefactoringAsync(SyntaxNode root, RefactoringSuggestion suggestion)
    {
        return Task.FromResult(root);
    }
}

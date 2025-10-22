using Relay.CLI.Refactoring;
using Xunit;

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
            Categories = new List<RefactoringCategory> { RefactoringCategory.Readability }
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
    public async Task AnalyzeAsync_ShouldExcludeBinAndObjFolders()
    {
        // Arrange
        var binPath = Path.Combine(_testProjectPath, "bin");
        var objPath = Path.Combine(_testProjectPath, "obj");
        Directory.CreateDirectory(binPath);
        Directory.CreateDirectory(objPath);

        await File.WriteAllTextAsync(Path.Combine(binPath, "Test.cs"), "public class Test {}");
        await File.WriteAllTextAsync(Path.Combine(objPath, "Test.cs"), "public class Test {}");
        await File.WriteAllTextAsync(Path.Combine(_testProjectPath, "Valid.cs"), "public class Valid {}");

        var engine = new RefactoringEngine();
        var options = new RefactoringOptions
        {
            ProjectPath = _testProjectPath
        };

        // Act
        var result = await engine.AnalyzeAsync(options);

        // Assert
        Assert.Equal(1, result.FilesAnalyzed); // Only Valid.cs
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

    public void Dispose()
    {
        if (Directory.Exists(_testProjectPath))
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
}

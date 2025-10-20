using Microsoft.CodeAnalysis.CSharp;
using Relay.CLI.Refactoring;
using System;
using Xunit;

namespace Relay.CLI.Tests.Refactoring;

public class RefactoringRulesTests
{
    [Fact]
    public async Task AsyncAwaitRule_ShouldDetectResultUsage()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        var task = GetDataAsync();
        var result = task.Result;
    }

    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains(suggestions, s => s.Description.Contains("Result"));
    }

    [Fact]
    public async Task NullCheckRule_ShouldDetectNullCoalescingOpportunity()
    {
        // Arrange
        var code = @"
public class Test
{
    public string GetValue(string input)
    {
        if (input == null)
        {
            input = ""default"";
        }
        return input;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new NullCheckRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains("??=", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task LinqRule_ShouldDetectWhereAnyPattern()
    {
        // Arrange
        var code = @"
using System.Linq;
using System.Collections.Generic;

public class Test
{
    public bool HasItems(List<int> items)
    {
        return items.Where(x => x > 0).Any();
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new LinqSimplificationRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains("Where().Any()", suggestions.First().Description);
        Assert.Contains("Any(", suggestions.First().SuggestedCode);
        Assert.DoesNotContain("Where", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task LinqRule_ShouldDetectWhereFirstPattern()
    {
        // Arrange
        var code = @"
using System.Linq;
using System.Collections.Generic;

public class Test
{
    public int GetFirst(List<int> items)
    {
        return items.Where(x => x > 0).First();
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new LinqSimplificationRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains("Where().First()", suggestions.First().Description);
    }

    [Fact]
    public async Task StringInterpolationRule_ShouldDetectStringFormat()
    {
        // Arrange
        var code = @"
public class Test
{
    public string GetMessage(string name, int age)
    {
        return String.Format(""Name: {0}, Age: {1}"", name, age);
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new StringInterpolationRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains("String.Format", suggestions.First().Description);
        Assert.StartsWith("$\"", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task StringInterpolationRule_ShouldDetectConcatenation()
    {
        // Arrange
        var code = @"
public class Test
{
    public string GetGreeting(string name)
    {
        return ""Hello, "" + name + ""!"";
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new StringInterpolationRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains("concatenation", suggestions.First().Description);
        Assert.StartsWith("$\"", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task DisposableRule_ShouldDetectMissingUsing()
    {
        // Arrange
        var code = @"
using System.IO;

public class Test
{
    public void ReadFile(string path)
    {
        var stream = new FileStream(path, FileMode.Open);
        // Missing using or dispose
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new DisposablePatternRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains("using", suggestions.First().Description);
        Assert.Equal(RefactoringSeverity.Warning, suggestions.First().Severity);
    }

    [Fact]
    public async Task DisposableRule_ShouldNotSuggest_WhenUsingAlreadyPresent()
    {
        // Arrange
        var code = @"
using System.IO;

public class Test
{
    public void ReadFile(string path)
    {
        using var stream = new FileStream(path, FileMode.Open);
        // Already using 'using' keyword
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new DisposablePatternRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task AsyncAwaitRule_ShouldApplyRefactoring()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        var task = GetDataAsync();
        var result = task.Result;
    }

    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First();

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("await", newCode);
        Assert.DoesNotContain(".Result", newCode);
    }

    [Fact]
    public async Task AsyncAwaitRule_ShouldDetectWaitUsage()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        var task = GetDataAsync();
        task.Wait();
    }

    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains(suggestions, s => s.Description.Contains("Wait"));
    }

    [Fact]
    public async Task AsyncAwaitRule_ShouldDetectGetAwaiterUsage()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        var task = GetDataAsync();
        var awaiter = task.GetAwaiter();
    }

    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains(suggestions, s => s.Description.Contains("GetAwaiter"));
    }

    [Fact]
    public async Task AsyncAwaitRule_ShouldApplyWaitRefactoring()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        var task = GetDataAsync();
        task.Wait();
    }

    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First(s => s.Description.Contains("Wait"));

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("await", newCode);
        Assert.DoesNotContain(".Wait()", newCode);
    }

    [Fact]
    public async Task AsyncAwaitRule_ShouldApplyGetAwaiterRefactoring()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        var task = GetDataAsync();
        var awaiter = task.GetAwaiter();
    }

    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First(s => s.Description.Contains("GetAwaiter"));

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("await", newCode);
        Assert.DoesNotContain(".GetAwaiter()", newCode);
    }

    [Fact]
    public async Task AsyncAwaitRule_ShouldMakeMethodAsync_WhenApplyingResultRefactoring()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public string Method()
    {
        var task = GetDataAsync();
        return task.Result;
    }

    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First();

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("async", newCode);
        Assert.Contains("Task<string>", newCode);
        Assert.Contains("await", newCode);
        Assert.DoesNotContain(".Result", newCode);
    }

    [Fact]
    public async Task AsyncAwaitRule_ShouldMakeVoidMethodAsync_WhenApplyingWaitRefactoring()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        var task = GetDataAsync();
        task.Wait();
    }

    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First(s => s.Description.Contains("Wait"));

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("async", newCode);
        Assert.Contains("Task Method", newCode);
        Assert.Contains("await", newCode);
        Assert.DoesNotContain(".Wait()", newCode);
    }

    [Fact]
    public async Task AsyncAwaitRule_ShouldNotMakeMethodAsync_WhenAlreadyAsync()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public async Task Method()
    {
        var task = GetDataAsync();
        var result = task.Result;
    }

    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First();

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("await", newCode);
        Assert.DoesNotContain(".Result", newCode);
        // Should not add another async keyword
        var asyncCount = newCode.Split("async").Length - 1;
        Assert.Equal(2, asyncCount); // One for Method, one for GetDataAsync
    }

    [Fact]
    public async Task AsyncAwaitRule_ShouldDetectResultOnAsyncMethodCall()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        var result = GetDataAsync().Result;
    }

    public async Task<string> GetDataAsync()
    {
        return await Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains(suggestions, s => s.Description.Contains("Result"));
    }

    [Fact]
    public async Task NullCheckRule_ShouldApplyRefactoring()
    {
        // Arrange
        var code = @"
public class Test
{
    public string GetValue(string input)
    {
        if (input == null)
        {
            input = ""default"";
        }
        return input;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new NullCheckRefactoringRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First();

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("??=", newCode);
    }

    [Fact]
    public async Task DisposableRule_ShouldApplyRefactoring()
    {
        // Arrange
        var code = @"
using System.IO;

public class Test
{
    public void ReadFile(string path)
    {
        var stream = new FileStream(path, FileMode.Open);
        // Missing using or dispose
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new DisposablePatternRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First();

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.NotEqual(root.ToFullString(), newCode); // Code should have changed
        Assert.Contains("using", newCode);
        Assert.Contains("var stream", newCode);
    }

    [Fact]
    public async Task DisposableRule_ShouldNotApplyRefactoring_WhenContextIsNotLocalDeclaration()
    {
        // Arrange
        var code = @"
using System.IO;

public class Test
{
    public void ReadFile(string path)
    {
        var stream = new FileStream(path, FileMode.Open);
        // Missing using or dispose
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new DisposablePatternRule();
        var options = new RefactoringOptions();

        // Create a suggestion with non-LocalDeclarationStatementSyntax context
        var suggestion = new RefactoringSuggestion
        {
            RuleName = "DisposablePattern",
            Description = "Test suggestion",
            Category = RefactoringCategory.BestPractices,
            Severity = RefactoringSeverity.Warning,
            FilePath = "test.cs",
            LineNumber = 1,
            StartPosition = 0,
            EndPosition = 1,
            OriginalCode = "test",
            SuggestedCode = "test",
            Rationale = "test",
            Context = null // Not a LocalDeclarationStatementSyntax
        };

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Equal(root.ToFullString(), newCode); // Should be unchanged
    }
}

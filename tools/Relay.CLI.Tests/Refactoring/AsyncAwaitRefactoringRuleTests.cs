using Microsoft.CodeAnalysis.CSharp;
using Relay.CLI.Refactoring;
using System;
using Xunit;

namespace Relay.CLI.Tests.Refactoring;

/// <summary>
/// Comprehensive tests for AsyncAwaitRefactoringRule improvements
/// </summary>
public class AsyncAwaitRefactoringRuleTests
{
    [Fact]
    public async Task ShouldDetectResultOnTaskT()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        Task<string> task = GetDataAsync();
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
        Assert.Single(suggestions);
        Assert.Contains("Result", suggestions[0].Description);
        Assert.Equal("await task", suggestions[0].SuggestedCode);
    }

    [Fact]
    public async Task ShouldDetectResultOnTask()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        Task task = DoWorkAsync();
        task.Wait();
    }

    public async Task DoWorkAsync()
    {
        await Task.Delay(100);
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        Assert.Contains("Wait", suggestions[0].Description);
        Assert.Equal("await task", suggestions[0].SuggestedCode);
    }

    [Fact]
    public async Task ShouldDetectResultOnMethodCall()
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
        Assert.Single(suggestions);
        Assert.Contains("Result", suggestions[0].Description);
        Assert.Equal("await GetDataAsync()", suggestions[0].SuggestedCode);
    }

    [Fact]
    public async Task ShouldNotDetectResultOnNonTaskTypes()
    {
        // Arrange - Test semantic analysis prevents false positives
        var code = @"
using System.Collections.Generic;

public class Test
{
    public void Method()
    {
        var list = new List<string>();
        var count = list.Count; // Not .Result

        var dict = new Dictionary<string, int>();
        var value = dict[""key""]; // Not .Result

        var str = ""hello"";
        var length = str.Length; // Not .Result
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should not suggest refactoring for non-Task properties
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task ShouldNotDetectWaitOnNonTaskTypes()
    {
        // Arrange - Test semantic analysis prevents false positives for Wait()
        var code = @"
using System.Threading;

public class Test
{
    public void Method()
    {
        var manualResetEvent = new ManualResetEvent(false);
        manualResetEvent.WaitOne(); // Not Task.Wait()

        var autoResetEvent = new AutoResetEvent(false);
        autoResetEvent.WaitOne(); // Not Task.Wait()
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should not suggest refactoring for non-Task.Wait()
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task ShouldNotDetectGetAwaiterOnNonTaskTypes()
    {
        // Arrange
        var code = @"
using System.Runtime.CompilerServices;

public class Test
{
    public void Method()
    {
        var customAwaitable = new CustomAwaitable();
        var awaiter = customAwaitable.GetAwaiter(); // Not Task.GetAwaiter()
    }
}

public class CustomAwaitable
{
    public CustomAwaiter GetAwaiter() => new CustomAwaiter();
}

public class CustomAwaiter : INotifyCompletion
{
    public bool IsCompleted => true;
    public void OnCompleted(Action continuation) { }
    public void GetResult() { }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should not suggest refactoring for non-Task.GetAwaiter()
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task ShouldDetectMultipleBlockingCalls()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        Task<string> task1 = GetDataAsync();
        var result1 = task1.Result;

        Task task2 = DoWorkAsync();
        task2.Wait();

        var result2 = GetMoreDataAsync().Result;
    }

    public async Task<string> GetDataAsync() => await Task.FromResult(""test"");
    public async Task DoWorkAsync() => await Task.Delay(100);
    public async Task<string> GetMoreDataAsync() => await Task.FromResult(""more"");
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Equal(3, suggestions.Count);
        Assert.Contains(suggestions, s => s.Description.Contains("Result") && s.SuggestedCode.Contains("task1"));
        Assert.Contains(suggestions, s => s.Description.Contains("Wait") && s.SuggestedCode.Contains("task2"));
        Assert.Contains(suggestions, s => s.Description.Contains("Result") && s.SuggestedCode.Contains("GetMoreDataAsync()"));
    }

    [Fact]
    public async Task ShouldHandleGenericTaskTypes()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        Task<int> intTask = GetIntAsync();
        var intResult = intTask.Result;

        Task<bool> boolTask = GetBoolAsync();
        var boolResult = boolTask.Result;

        Task<List<string>> listTask = GetListAsync();
        var listResult = listTask.Result;
    }

    public async Task<int> GetIntAsync() => await Task.FromResult(42);
    public async Task<bool> GetBoolAsync() => await Task.FromResult(true);
    public async Task<List<string>> GetListAsync() => await Task.FromResult(new List<string>());
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Equal(3, suggestions.Count);
        Assert.All(suggestions, s => Assert.Contains("Result", s.Description));
    }

    [Fact]
    public async Task ShouldApplyResultRefactoring()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        Task<string> task = GetDataAsync();
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
    public async Task ShouldApplyWaitRefactoring()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        Task task = DoWorkAsync();
        task.Wait();
    }

    public async Task DoWorkAsync()
    {
        await Task.Delay(100);
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
    public async Task ShouldApplyGetAwaiterRefactoring()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        Task<string> task = GetDataAsync();
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
    public async Task ShouldHandleVoidMethodReturnType()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        Task task = DoWorkAsync();
        task.Wait();
    }

    public async Task DoWorkAsync()
    {
        await Task.Delay(100);
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
    public async Task ShouldNotModifyAlreadyAsyncMethod()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public async Task Method()
    {
        Task<string> task = GetDataAsync();
        var result = task.Result; // Still blocking even in async method
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
    public async Task ShouldHandleComplexExpressions()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        var result = Task.Run(() => GetDataAsync()).Result.Result;
    }

    public async Task<Task<string>> GetDataAsync()
    {
        return Task.FromResult(""test"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new AsyncAwaitRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should detect .Result calls
        Assert.True(suggestions.Count >= 1);
        Assert.Contains(suggestions, s => s.Description.Contains("Result"));
    }

    [Fact]
    public async Task ShouldHandleValueTask()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;

public class Test
{
    public void Method()
    {
        ValueTask<string> valueTask = GetDataValueAsync();
        var result = valueTask.Result;
    }

    public async ValueTask<string> GetDataValueAsync()
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

        // Assert - ValueTask should also be detected
        Assert.Single(suggestions);
        Assert.Contains("Result", suggestions[0].Description);
    }
}
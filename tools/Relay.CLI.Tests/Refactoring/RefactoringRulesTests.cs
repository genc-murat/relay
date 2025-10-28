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
    public async Task NullCheckRule_ShouldDetectNullConditionalOpportunity()
    {
        // Arrange
        var code = @"
public class Test
{
    public void ProcessPerson(Person person)
    {
        string name;
        if (person != null)
        {
            name = person.Name;
        }
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
        Assert.Contains("?.", suggestions.First().SuggestedCode);
        Assert.Contains("person?.Name", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task NullCheckRule_ShouldDetectTernaryNullCoalescingOpportunity()
    {
        // Arrange
        var code = @"
public class Test
{
    public string GetValue(string input)
    {
        return input == null ? ""default"" : input;
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
        Assert.Contains("??", suggestions.First().SuggestedCode);
        Assert.DoesNotContain(":", suggestions.First().SuggestedCode); // Should not contain ternary operator
    }

    [Fact]
    public async Task NullCheckRule_ShouldHandleReverseNullCheck()
    {
        // Arrange
        var code = @"
public class Test
{
    public string GetValue(string input)
    {
        if (null == input)
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
    public async Task NullCheckRule_ShouldHandleReverseNotNullCheck()
    {
        // Arrange
        var code = @"
public class Test
{
    public void ProcessPerson(Person person)
    {
        string name;
        if (null != person)
        {
            name = person.Name;
        }
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
        Assert.Contains("?.", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task NullCheckRule_ShouldNotRefactorMultipleStatements()
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
            Console.WriteLine(""Assigned default"");
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
        Assert.Empty(suggestions); // Should not suggest refactoring for multiple statements
    }

    [Fact]
    public async Task NullCheckRule_ShouldNotRefactorNonAssignmentStatements()
    {
        // Arrange
        var code = @"
public class Test
{
    public void ProcessValue(string input)
    {
        if (input == null)
        {
            Console.WriteLine(""Input is null"");
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new NullCheckRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Empty(suggestions); // Should not suggest refactoring for non-assignment statements
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
    public async Task LinqRule_ShouldDetectWhereCountPattern()
    {
        // Arrange
        var code = @"
using System.Linq;
using System.Collections.Generic;

public class Test
{
    public int GetCount(List<int> items)
    {
        return items.Where(x => x > 0).Count;
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
        Assert.Contains("Where().Count", suggestions.First().Description);
        Assert.Contains("Count(", suggestions.First().SuggestedCode);
        Assert.DoesNotContain("Where", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task LinqRule_ShouldDetectUnnecessarySelect()
    {
        // Arrange
        var code = @"
using System.Linq;
using System.Collections.Generic;

public class Test
{
    public IEnumerable<int> GetItems(List<int> items)
    {
        return items.Select(x => x);
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
        Assert.Contains("Select(x => x)", suggestions.First().Description);
        Assert.Equal("items", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task LinqRule_ShouldNotDetectSelectWithDifferentParameter()
    {
        // Arrange
        var code = @"
using System.Linq;
using System.Collections.Generic;

public class Test
{
    public IEnumerable<int> GetItems(List<int> items)
    {
        return items.Select(x => x * 2);
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new LinqSimplificationRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task LinqRule_ShouldHandleMultiplePatterns()
    {
        // Arrange
        var code = @"
using System.Linq;
using System.Collections.Generic;

public class Test
{
    public bool HasItems(List<int> items)
    {
        var anyResult = items.Where(x => x > 0).Any();
        var firstResult = items.Where(x => x < 10).First();
        var countResult = items.Where(x => x % 2 == 0).Count;
        var selectResult = items.Select(x => x);
        return anyResult;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new LinqSimplificationRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Equal(4, suggestions.Count);
        Assert.Contains(suggestions, s => s.Description.Contains("Where().Any()"));
        Assert.Contains(suggestions, s => s.Description.Contains("Where().First()"));
        Assert.Contains(suggestions, s => s.Description.Contains("Where().Count"));
        Assert.Contains(suggestions, s => s.Description.Contains("Select(x => x)"));
    }

    [Fact]
    public async Task LinqRule_ApplyRefactoring_ShouldTransformWhereAny()
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

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First(s => s.Description.Contains("Where().Any()"));

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToString();

        // Assert
        Assert.Contains("items.Any(x => x > 0)", newCode);
        Assert.DoesNotContain("items.Where(x => x > 0).Any()", newCode);
    }

    [Fact]
    public async Task LinqRule_ApplyRefactoring_ShouldTransformWhereFirst()
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

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First(s => s.Description.Contains("Where().First()"));

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToString();

        // Assert
        Assert.Contains("items.First(x => x > 0)", newCode);
        Assert.DoesNotContain("items.Where(x => x > 0).First()", newCode);
    }

    [Fact]
    public async Task LinqRule_ApplyRefactoring_ShouldTransformWhereCount()
    {
        // Arrange
        var code = @"
using System.Linq;
using System.Collections.Generic;

public class Test
{
    public int GetCount(List<int> items)
    {
        return items.Where(x => x > 0).Count;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new LinqSimplificationRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First(s => s.Description.Contains("Where().Count"));

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToString();

        // Assert
        Assert.Contains("items.Count(x => x > 0)", newCode);
        Assert.DoesNotContain("items.Where(x => x > 0).Count", newCode);
    }

    [Fact]
    public async Task LinqRule_ApplyRefactoring_ShouldRemoveUnnecessarySelect()
    {
        // Arrange
        var code = @"
using System.Linq;
using System.Collections.Generic;

public class Test
{
    public IEnumerable<int> GetItems(List<int> items)
    {
        return items.Select(x => x);
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new LinqSimplificationRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First(s => s.Description.Contains("Select(x => x)"));

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToString();

        // Assert
        Assert.Contains("return items;", newCode);
        Assert.DoesNotContain("items.Select(x => x)", newCode);
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

    [Fact]
    public async Task DisposableRule_ShouldNotSuggest_WhenDisposeCallExists()
    {
        // Arrange
        var code = @"
using System.IO;

public class Test
{
    public void ReadFile(string path)
    {
        var stream = new FileStream(path, FileMode.Open);
        stream.Dispose(); // Explicit dispose call
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new DisposablePatternRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Empty(suggestions); // Should not suggest since dispose call exists
    }

    [Fact]
    public async Task DisposableRule_ShouldDetectTryFinallyWithDispose()
    {
        // Arrange
        var code = @"
using System.IO;

public class Test
{
    public void ReadFile(string path)
    {
        var stream = new FileStream(path, FileMode.Open);
        try
        {
            // Some code
        }
        finally
        {
            stream.Dispose();
        }
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
        Assert.Contains("try-finally", suggestions.First().Description);
        Assert.Equal(RefactoringSeverity.Suggestion, suggestions.First().Severity);
    }

    [Fact]
    public async Task DisposableRule_ShouldNotSuggest_WhenUsingModifierPresent()
    {
        // Arrange
        var code = @"
using System.IO;

public class Test
{
    public void ReadFile(string path)
    {
        using var stream = new FileStream(path, FileMode.Open);
        // Already has using modifier
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new DisposablePatternRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Empty(suggestions); // Should not suggest since using modifier is present
    }

    [Fact]
    public async Task DisposableRule_ShouldNotSuggest_WhenInsideUsingStatement()
    {
        // Arrange
        var code = @"
using System.IO;

public class Test
{
    public void ReadFile(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open))
        {
            // Already inside using statement
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new DisposablePatternRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Empty(suggestions); // Should not suggest since inside using statement
    }

    [Fact]
    public async Task DisposableRule_ShouldDetectDifferentDisposableTypes()
    {
        // Arrange
        var code = @"
using System.Data.SqlClient;

public class Test
{
    public void QueryDatabase(string connectionString)
    {
        var connection = new SqlConnection(connectionString);
        var command = new SqlCommand(""SELECT * FROM Users"", connection);
        // Missing using for both disposable objects
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new DisposablePatternRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count >= 2); // Should detect both connection and command
        Assert.Contains("connection", suggestions[0].Description.ToLower());
        Assert.Contains("command", suggestions[1].Description.ToLower());
    }

    [Fact]
    public async Task PatternMatchingRule_ShouldDetectIsPatternOpportunity()
    {
        // Arrange
        var code = @"
public class Test
{
    public void ProcessObject(object obj)
    {
        if (obj is string)
        {
            var str = (string)obj;
            Console.WriteLine(str.Length);
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new PatternMatchingRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains("is pattern", suggestions.First().Description.ToLower());
        Assert.Contains("is string str", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task PatternMatchingRule_ShouldDetectSwitchExpressionOpportunity()
    {
        // Arrange
        var code = @"
public class Test
{
    public string GetMessage(int value)
    {
        switch (value)
        {
            case 1: return ""One"";
            case 2: return ""Two"";
            default: return ""Other"";
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new PatternMatchingRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains("switch expression", suggestions.First().Description.ToLower());
        Assert.Contains("switch", suggestions.First().SuggestedCode);
        Assert.Contains("=>", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task PatternMatchingRule_ShouldDetectIfElseChainOpportunity()
    {
        // Arrange
        var code = @"
public class Test
{
    public string GetMessage(string value)
    {
        if (value == ""A"") return ""Alpha"";
        else if (value == ""B"") return ""Beta"";
        else return ""Other"";
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new PatternMatchingRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains("if-else chain", suggestions.First().Description.ToLower());
        Assert.Contains("switch", suggestions.First().SuggestedCode);
    }

    [Fact]
    public async Task PatternMatchingRule_ShouldApplyIsPatternRefactoring()
    {
        // Arrange
        var code = @"
public class Test
{
    public void ProcessObject(object obj)
    {
        if (obj is string)
        {
            var str = (string)obj;
            Console.WriteLine(str.Length);
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new PatternMatchingRefactoringRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First();

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("is string str", newCode);
        Assert.DoesNotContain("(string)obj", newCode);
    }

    [Fact]
    public async Task PatternMatchingRule_ShouldApplySwitchExpressionRefactoring()
    {
        // Arrange
        var code = @"
public class Test
{
    public string GetMessage(int value)
    {
        switch (value)
        {
            case 1: return ""One"";
            case 2: return ""Two"";
            default: return ""Other"";
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new PatternMatchingRefactoringRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First();

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("value switch", newCode);
        Assert.Contains("=>", newCode);
        Assert.DoesNotContain("case 1:", newCode);
        Assert.DoesNotContain("return", newCode);
    }

    [Fact]
    public async Task PatternMatchingRule_ShouldNotDetectSwitchExpression_ForComplexSwitch()
    {
        // Arrange - Switch with complex statements should not be converted
        var code = @"
public class Test
{
    public void ProcessValue(int value)
    {
        switch (value)
        {
            case 1:
                Console.WriteLine(""One"");
                break;
            case 2:
                Console.WriteLine(""Two"");
                break;
            default:
                Console.WriteLine(""Other"");
                break;
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new PatternMatchingRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should not suggest switch expression for complex statements
        var switchSuggestions = suggestions.Where(s => s.Description.Contains("switch expression")).ToList();
        Assert.Empty(switchSuggestions);
    }

    [Fact]
    public async Task PatternMatchingRule_ShouldDetectPropertyPatternOpportunity()
    {
        // Arrange
        var code = @"
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class Test
{
    public void ProcessPerson(Person person)
    {
        if (person != null && person.Age > 18)
        {
            Console.WriteLine(person.Name);
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new PatternMatchingRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.True(suggestions.Count > 0);
        Assert.Contains(suggestions, s => s.Description.Contains("property pattern"));
    }

    [Fact]
    public async Task PatternMatchingRule_ShouldHandleReverseIsPattern()
    {
        // Arrange
        var code = @"
public class Test
{
    public void ProcessObject(object obj)
    {
        if (""test"" is string str)
        {
            Console.WriteLine(str.Length);
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new PatternMatchingRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should not suggest refactoring for already modern pattern
        var isPatternSuggestions = suggestions.Where(s => s.Description.Contains("is pattern")).ToList();
        Assert.Empty(isPatternSuggestions);
    }

    [Fact]
    public async Task PatternMatchingRule_ShouldApplyIfElseToSwitchExpression()
    {
        // Arrange
        var code = @"
public class Test
{
    public string GetMessage(string value)
    {
        if (value == ""A"") return ""Alpha"";
        else if (value == ""B"") return ""Beta"";
        else return ""Other"";
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new PatternMatchingRefactoringRule();
        var options = new RefactoringOptions();

        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var suggestion = suggestions.First(s => s.Description.Contains("if-else chain"));

        // Act
        var newRoot = await rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("value switch", newCode);
        Assert.Contains("=>", newCode);
        Assert.DoesNotContain("if (", newCode);
    }

    [Fact]
    public async Task PatternMatchingRule_ShouldNotDetect_ForSingleIf()
    {
        // Arrange
        var code = @"
public class Test
{
    public void ProcessValue(int value)
    {
        if (value > 0)
        {
            Console.WriteLine(""Positive"");
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var rule = new PatternMatchingRefactoringRule();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should not suggest switch expression for single if
        var switchSuggestions = suggestions.Where(s => s.Description.Contains("switch")).ToList();
        Assert.Empty(switchSuggestions);
    }
}

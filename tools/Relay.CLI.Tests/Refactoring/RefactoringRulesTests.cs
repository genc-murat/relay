using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Relay.CLI.Refactoring;
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
        suggestions.Should().HaveCountGreaterThan(0);
        suggestions.Should().Contain(s => s.Description.Contains("Result"));
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
        suggestions.Should().HaveCountGreaterThan(0);
        suggestions.First().SuggestedCode.Should().Contain("??=");
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
        suggestions.Should().HaveCountGreaterThan(0);
        suggestions.First().Description.Should().Contain("Where().Any()");
        suggestions.First().SuggestedCode.Should().Contain("Any(");
        suggestions.First().SuggestedCode.Should().NotContain("Where");
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
        suggestions.Should().HaveCountGreaterThan(0);
        suggestions.First().Description.Should().Contain("Where().First()");
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
        suggestions.Should().HaveCountGreaterThan(0);
        suggestions.First().Description.Should().Contain("String.Format");
        suggestions.First().SuggestedCode.Should().StartWith("$\"");
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
        suggestions.Should().HaveCountGreaterThan(0);
        suggestions.First().Description.Should().Contain("concatenation");
        suggestions.First().SuggestedCode.Should().StartWith("$\"");
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
        suggestions.Should().HaveCountGreaterThan(0);
        suggestions.First().Description.Should().Contain("using");
        suggestions.First().Severity.Should().Be(RefactoringSeverity.Warning);
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
        suggestions.Should().BeEmpty();
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
        newCode.Should().Contain("await");
        newCode.Should().NotContain(".Result");
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
        newCode.Should().Contain("??=");
    }
}

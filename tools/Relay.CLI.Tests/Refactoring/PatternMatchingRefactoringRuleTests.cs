using Microsoft.CodeAnalysis.CSharp;
using Relay.CLI.Refactoring;
using System.Linq;
using Xunit;

namespace Relay.CLI.Tests.Refactoring;

/// <summary>
/// Comprehensive tests for PatternMatchingRefactoringRule covering all refactoring scenarios
/// </summary>
public class PatternMatchingRefactoringRuleTests
{
    private readonly PatternMatchingRefactoringRule _rule = new();

    [Fact]
    public async Task AnalyzeAsync_DetectsSwitchExpressionOpportunities()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method(string value)
    {
        if (value == ""a"")
        {
            Console.WriteLine(""A"");
        }
        else if (value == ""b"")
        {
            Console.WriteLine(""B"");
        }
        else
        {
            Console.WriteLine(""Default"");
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();

        // Assert
        Assert.Single(suggestions);
        Assert.Contains("switch expression", suggestions[0].Description);
        Assert.Equal("PatternMatchingRefactoring", suggestions[0].RuleName);
        Assert.Equal(RefactoringCategory.Modernization, suggestions[0].Category);
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsMultipleSwitchExpressionOpportunities()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method1(string value)
    {
        if (value == ""a"")
            Console.WriteLine(""A"");
        else if (value == ""b"")
            Console.WriteLine(""B"");
    }

    public void Method2(int num)
    {
        if (num == 1)
            return ""One"";
        else if (num == 2)
            return ""Two"";
        else if (num == 3)
            return ""Three"";
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();

        // Assert
        Assert.Equal(3, suggestions.Count); // Method1 has 1 chain, Method2 has 2 overlapping chains
        Assert.All(suggestions, s => Assert.Contains("switch expression", s.Description));
    }

    [Fact]
    public async Task AnalyzeAsync_IgnoresInvalidSwitchExpressionCases()
    {
        // Arrange - Single if, non-constant comparisons, non-equality operators
        var code = @"
public class Test
{
    public void SingleIf(string value)
    {
        if (value == ""a"")
            Console.WriteLine(""A"");
    }

    public void NonConstantComparison(string value, string other)
    {
        if (value == other)
            Console.WriteLine(""Equal"");
        else if (value == ""constant"")
            Console.WriteLine(""Constant"");
    }

    public void NonEqualityOperators(int x)
    {
        if (x > 5)
            Console.WriteLine(""Greater"");
        else if (x < 0)
            Console.WriteLine(""Less"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsSwitchStatementToExpressionOpportunities()
    {
        // Arrange
        var code = @"
public class Test
{
    public string Method(string value)
    {
        switch (value)
        {
            case ""a"":
                return ""A"";
            case ""b"":
                return ""B"";
            default:
                return ""Default"";
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();

        // Assert
        Assert.Single(suggestions);
        Assert.Contains("switch statement to switch expression", suggestions[0].Description);
    }

    [Fact]
    public async Task AnalyzeAsync_IgnoresInvalidSwitchStatementConversions()
    {
        // Arrange - Multiple statements per case, non-return assignments
        var code = @"
public class Test
{
    public void Method1(string value)
    {
        switch (value)
        {
            case ""a"":
                Console.WriteLine(""A"");
                return;
            case ""b"":
                return;
        }
    }

    public void Method2(string value)
    {
        string result;
        switch (value)
        {
            case ""a"":
                result = ""A"";
                break;
            case ""b"":
                result = ""B"";
                break;
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsIsPatternOpportunities()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method(object obj)
    {
        if (obj is string)
        {
            var s = (string)obj;
            Console.WriteLine(s.Length);
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();

        // Assert
        Assert.Single(suggestions);
        Assert.Contains("is pattern", suggestions[0].Description);
    }

    [Fact]
    public async Task AnalyzeAsync_IgnoresInvalidIsPatternCases()
    {
        // Arrange - No cast after is check, wrong cast type
        var code = @"
public class Test
{
    public void NoCast(object obj)
    {
        if (obj is string)
        {
            Console.WriteLine(""It's a string"");
        }
    }

    public void WrongCastType(object obj)
    {
        if (obj is string)
        {
            var s = (int)obj;
            Console.WriteLine(s);
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsPropertyPatternOpportunities()
    {
        // Arrange
        var code = @"
public class Test
{
    public void CheckPerson(Person p)
    {
        if (p != null && p.Age > 18)
        {
            Console.WriteLine(""Adult"");
        }
    }

    public void CheckPersonOr(Person p)
    {
        if (p == null || p.Age < 18)
        {
            Console.WriteLine(""Not adult"");
        }
    }
}

public class Person
{
    public int Age { get; set; }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();

        // Assert
        Assert.Equal(2, suggestions.Count);
        Assert.All(suggestions, s => Assert.Contains("property pattern", s.Description));
    }

    [Fact]
    public async Task AnalyzeAsync_IgnoresSimpleConditionsForPropertyPatterns()
    {
        // Arrange
        var code = @"
public class Test
{
    public void SimpleCondition(int x)
    {
        if (x > 5)
        {
            Console.WriteLine(""Greater"");
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task ApplyRefactoringAsync_AppliesSwitchExpressionRefactoring()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method(string value)
    {
        if (value == ""a"")
        {
            Console.WriteLine(""A"");
        }
        else if (value == ""b"")
        {
            Console.WriteLine(""B"");
        }
        else
        {
            Console.WriteLine(""Default"");
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();
        var suggestion = suggestions.First();

        // Act
        var newRoot = await _rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("switch", newCode);
        Assert.Contains("=>", newCode);
        Assert.DoesNotContain("if (", newCode);
        Assert.DoesNotContain("else if", newCode);
    }

    [Fact]
    public async Task ApplyRefactoringAsync_AppliesSwitchStatementRefactoring()
    {
        // Arrange
        var code = @"
public class Test
{
    public string Method(string value)
    {
        switch (value)
        {
            case ""a"":
                return ""A"";
            case ""b"":
                return ""B"";
            default:
                return ""Default"";
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();
        var suggestion = suggestions.First();

        // Act
        var newRoot = await _rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("switch", newCode);
        Assert.Contains("=>", newCode);
        Assert.DoesNotContain("case", newCode);
        Assert.DoesNotContain("break", newCode);
    }

    [Fact]
    public async Task ApplyRefactoringAsync_AppliesIsPatternRefactoring()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method(object obj)
    {
        if (obj is string)
        {
            var s = (string)obj;
            Console.WriteLine(s);
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions())).ToList();
        var suggestion = suggestions.First();

        // Act
        var newRoot = await _rule.ApplyRefactoringAsync(root, suggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("is string", newCode);
        Assert.Contains("s", newCode); // The variable name
        Assert.DoesNotContain("(string)obj", newCode);
    }

    [Fact]
    public async Task RuleProperties_ReturnCorrectValues()
    {
        // Assert
        Assert.Equal("PatternMatchingRefactoring", _rule.RuleName);
        Assert.Contains("pattern matching", _rule.Description);
        Assert.Equal(RefactoringCategory.Modernization, _rule.Category);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsEmptyList_WhenNoPatternsFound()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method()
    {
        Console.WriteLine(""Hello"");
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Act
        var suggestions = await _rule.AnalyzeAsync("test.cs", root, new RefactoringOptions());

        // Assert
        Assert.Empty(suggestions);
    }
}
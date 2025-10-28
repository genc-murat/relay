using Microsoft.CodeAnalysis.CSharp;
using Relay.CLI.Refactoring;
using Xunit;

namespace Relay.CLI.Tests.Refactoring;

/// <summary>
/// Comprehensive tests for NullCheckRefactoringRule
/// </summary>
public class NullCheckRefactoringRuleTests
{
    private readonly NullCheckRefactoringRule _rule = new();

    [Fact]
    public async Task ShouldDetectNullCoalescingOpportunity()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method(string value)
    {
        if (value == null)
        {
            value = ""default"";
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("??=", suggestion.SuggestedCode);
        Assert.Equal("Replace if-null check with ??= operator", suggestion.Description);
    }

    [Fact]
    public async Task ShouldDetectNullConditionalOpportunity()
    {
        // Arrange
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetCity(Person person)
    {
        if (person != null)
        {
            return person.Address.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("person?.Address.City", suggestion.SuggestedCode);
        Assert.Equal("Replace null check with null-conditional operator (?)", suggestion.Description);
    }

    [Fact]
    public async Task ShouldDetectTernaryNullCoalescingOpportunity()
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
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("input ?? \"default\"", suggestion.SuggestedCode);
        Assert.Equal("Replace ternary null check with ?? operator", suggestion.Description);
    }

    [Fact]
    public async Task ShouldHandleReverseNullCheck()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method(string value)
    {
        if (null == value)
        {
            value = ""default"";
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("??=", suggestion.SuggestedCode);
    }

    [Fact]
    public async Task ShouldHandleReverseNotNullCheck()
    {
        // Arrange
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetCity(Person person)
    {
        if (null != person)
        {
            return person.Address.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("person?.Address.City", suggestion.SuggestedCode);
    }

    [Fact]
    public async Task ShouldNotRefactorMultipleStatements()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method(string value)
    {
        if (value == null)
        {
            value = ""default"";
            Console.WriteLine(value);
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task ShouldNotRefactorNonAssignmentStatements()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method(string value)
    {
        if (value == null)
        {
            Console.WriteLine(""Value is null"");
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task ShouldDetectNestedPropertyAccess()
    {
        // Arrange
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetCity(Person person)
    {
        if (person != null && person.Address != null)
        {
            return person.Address.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("person?.Address?.City", suggestion.SuggestedCode);
        Assert.Contains("nested null checks", suggestion.Description);
    }

    [Fact]
    public async Task ShouldDetectDeepNestedPropertyAccess()
    {
        // Arrange
        var code = @"
public class Company
{
    public Department Department { get; set; }
}

public class Department
{
    public Manager Manager { get; set; }
}

public class Manager
{
    public string Name { get; set; }
}

public class Test
{
    public string GetManagerName(Company company)
    {
        if (company != null && company.Department != null && company.Department.Manager != null)
        {
            return company.Department.Manager.Name;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("company?.Department?.Manager?.Name", suggestion.SuggestedCode);
        Assert.Contains("nested null checks", suggestion.Description);
    }

    [Fact]
    public async Task ShouldDetectNullConditionalChainExtension()
    {
        // Arrange
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetCity(Person person)
    {
        if (person?.Address != null)
        {
            return person?.Address?.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("return person?.Address?.City;", suggestion.SuggestedCode);
        Assert.Contains("redundant null check", suggestion.Description);
    }

    [Fact]
    public async Task ShouldNotDetectNestedAccessWithoutAllConditions()
    {
        // Arrange - Only checks person != null but accesses person.Address.City
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetCity(Person person)
    {
        if (person != null)
        {
            return person.Address.City; // Missing null check for Address
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should not suggest nested access since not all conditions are checked
        var nestedSuggestions = suggestions.Where(s => s.Description.Contains("nested null checks")).ToList();
        Assert.Empty(nestedSuggestions);
    }

    [Fact]
    public async Task ShouldHandleComplexNestedAccess()
    {
        // Arrange
        var code = @"
public class Order
{
    public Customer Customer { get; set; }
}

public class Customer
{
    public Address BillingAddress { get; set; }
    public Address ShippingAddress { get; set; }
}

public class Address
{
    public string Street { get; set; }
    public string City { get; set; }
}

public class Test
{
    public string GetBillingStreet(Order order)
    {
        if (order != null && order.Customer != null && order.Customer.BillingAddress != null)
        {
            return order.Customer.BillingAddress.Street;
        }
        return null;
    }

    public string GetShippingCity(Order order)
    {
        if (order != null && order.Customer != null && order.Customer.ShippingAddress != null)
        {
            return order.Customer.ShippingAddress.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Equal(2, suggestions.Count);
        var nestedSuggestions = suggestions.Where(s => s.Description.Contains("nested null checks")).ToList();
        Assert.Equal(2, nestedSuggestions.Count);

        Assert.Contains(nestedSuggestions, s => s.SuggestedCode.Contains("order?.Customer?.BillingAddress?.Street"));
        Assert.Contains(nestedSuggestions, s => s.SuggestedCode.Contains("order?.Customer?.ShippingAddress?.City"));
    }

    [Fact]
    public async Task ShouldApplyNestedPropertyAccessRefactoring()
    {
        // Arrange
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetCity(Person person)
    {
        if (person != null && person.Address != null)
        {
            return person.Address.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var nestedSuggestion = suggestions.First(s => s.Description.Contains("nested null checks"));

        // Act
        var newRoot = await _rule.ApplyRefactoringAsync(root, nestedSuggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("return person?.Address?.City;", newCode);
        Assert.DoesNotContain("if (person != null && person.Address != null)", newCode);
    }

    [Fact]
    public async Task ShouldApplyNullConditionalChainExtension()
    {
        // Arrange
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetCity(Person person)
    {
        if (person?.Address != null)
        {
            return person?.Address?.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();
        var chainSuggestion = suggestions.First(s => s.Description.Contains("redundant null check"));

        // Act
        var newRoot = await _rule.ApplyRefactoringAsync(root, chainSuggestion);
        var newCode = newRoot.ToFullString();

        // Assert
        Assert.Contains("return person?.Address?.City;", newCode);
        Assert.DoesNotContain("if (person?.Address != null)", newCode);
    }

    [Fact]
    public async Task ShouldHandleNestedPropertyAccessWithAssignment()
    {
        // Arrange - Currently assignments to nested properties are not supported
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public void UpdateCity(Person person, string newCity)
    {
        if (person != null && person.Address != null)
        {
            person.Address.City = newCity;
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Currently not supported, so no suggestions
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task ShouldHandleSingleNullCheck()
    {
        // Arrange
        var code = @"
public class Person
{
    public string Name { get; set; }
}

public class Test
{
    public string GetName(Person person)
    {
        if (person != null)
        {
            return person.Name;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("person?.Name", suggestion.SuggestedCode);
        Assert.Equal("Replace null check with null-conditional operator (?)", suggestion.Description);
    }

    [Fact]
    public async Task ShouldNotRefactorWhenNoNullChecks()
    {
        // Arrange
        var code = @"
public class Test
{
    public void Method()
    {
        var x = 1;
        if (x > 0)
        {
            x = 2;
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public async Task ShouldHandleComplexLogicalExpressions()
    {
        // Arrange
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetCity(Person person)
    {
        if ((person != null) && (person.Address != null))
        {
            return person.Address.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("person?.Address?.City", suggestion.SuggestedCode);
    }

    [Fact]
    public async Task ShouldHandleMixedNullAndNonNullChecks()
    {
        // Arrange - This should not be refactored as it has mixed conditions
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetCity(Person person, bool useDefault)
    {
        if (person != null && person.Address != null && useDefault)
        {
            return person.Address.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should not suggest nested access since there are non-null conditions
        var nestedSuggestions = suggestions.Where(s => s.Description.Contains("nested null checks")).ToList();
        Assert.Empty(nestedSuggestions);
    }
}
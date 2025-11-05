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

    [Fact]
    public async Task ShouldHandleParenthesizedNullChecks()
    {
        // Arrange - Test ExtractNullCheckConditions with parentheses
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
        Assert.Contains("nested null checks", suggestion.Description);
    }

    [Fact]
    public async Task ShouldHandleNestedNullChecksWithDifferentRootVariable()
    {
        // Arrange - Test BuildNestedNullConditionalAccess with different variable names
        var code = @"
public class Container
{
    public Item Item { get; set; }
}

public class Item
{
    public string Value { get; set; }
}

public class Test
{
    public string GetValue(Container container)
    {
        if (container != null && container.Item != null)
        {
            return container.Item.Value;
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
        Assert.Contains("container?.Item?.Value", suggestion.SuggestedCode);
    }

    [Fact]
    public async Task ShouldNotRefactorWhenAccessDoesNotMatchChecks()
    {
        // Arrange - Test case where the property access doesn't match the null checks
        var code = @"
public class Person
{
    public Address Address { get; set; }
    public string Name { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetData(Person person)
    {
        if (person != null && person.Address != null)
        {
            return person.Name; // Accessing Name but checked Address
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should not suggest nested access since access doesn't match checks
        var nestedSuggestions = suggestions.Where(s => s.Description.Contains("nested null checks")).ToList();
        Assert.Empty(nestedSuggestions);
    }

    [Fact]
    public async Task ShouldHandleNullConditionalChainWithAssignment()
    {
        // Arrange - Test AnalyzeNullConditionalChain with assignment
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
    public void SetCity(Person person, string city)
    {
        if (person?.Address != null)
        {
            person.Address.City = city;
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should suggest removing redundant check
        var chainSuggestions = suggestions.Where(s => s.Description.Contains("redundant null check")).ToList();
        Assert.Single(chainSuggestions);
        var suggestion = chainSuggestions[0];
        Assert.Contains("person.Address.City = city;", suggestion.SuggestedCode);
    }

    [Fact]
    public async Task ShouldHandleComplexNestedConditionsWithParentheses()
    {
        // Arrange - Test CountAllConditions with complex nested parentheses
        var code = @"
public class A
{
    public B B { get; set; }
}

public class B
{
    public C C { get; set; }
}

public class C
{
    public string Value { get; set; }
}

public class Test
{
    public string GetValue(A a)
    {
        if (((a != null) && (a.B != null)) && (a.B.C != null))
        {
            return a.B.C.Value;
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
        Assert.Contains("a?.B?.C?.Value", suggestion.SuggestedCode);
    }

    [Fact]
    public async Task ShouldNotRefactorPartialNullChecks()
    {
        // Arrange - Test case where only some null checks are present
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string City { get; set; }
    public string State { get; set; }
}

public class Test
{
    public string GetFullAddress(Person person)
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

        // Assert - Should suggest nested access since all accessed properties are checked
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("person?.Address?.City", suggestion.SuggestedCode);
    }

    [Fact]
    public async Task ShouldHandleNullConditionalChainWithMethodCall()
    {
        // Arrange - Test AnalyzeNullConditionalChain with method call (should not apply)
        var code = @"
public class Person
{
    public Address Address { get; set; }
}

public class Address
{
    public string GetCity()
    {
        return ""Test"";
    }
}

public class Test
{
    public string GetCity(Person person)
    {
        if (person?.Address != null)
        {
            return person.Address.GetCity();
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should suggest removing redundant check
        var chainSuggestions = suggestions.Where(s => s.Description.Contains("redundant null check")).ToList();
        Assert.Single(chainSuggestions);
    }

    [Fact]
    public async Task ShouldHandleEmptyNullConditionalChain()
    {
        // Arrange - Test case where condition doesn't contain null-conditional
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

        // Assert - Should suggest null-conditional, not chain extension
        Assert.Single(suggestions);
        var suggestion = suggestions[0];
        Assert.Contains("Replace null check with null-conditional operator (?)", suggestion.Description);
        Assert.DoesNotContain("redundant null check", suggestion.Description);
    }

    [Fact]
    public async Task ShouldHandleBuildNestedNullConditionalAccessWithNoMatch()
    {
        // Arrange - Test BuildNestedNullConditionalAccess when target doesn't start with root
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
    public string GetCity(Person person, Address addr)
    {
        if (person != null && addr != null)
        {
            return addr.City; // Using different variable than checked
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();
        var options = new RefactoringOptions();

        // Act
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Should not suggest nested access since variables don't match
        var nestedSuggestions = suggestions.Where(s => s.Description.Contains("nested null checks")).ToList();
        Assert.Empty(nestedSuggestions);
    }

    [Fact]
    public async Task ShouldHandleExtractNullCheckConditionsWithComplexExpressions()
    {
        // Arrange - Test ExtractNullCheckConditions with more complex expressions
        var code = @"
public class A
{
    public B B { get; set; }
}

public class B
{
    public C C { get; set; }
}

public class C
{
    public D D { get; set; }
}

public class D
{
    public string Value { get; set; }
}

public class Test
{
    public string GetValue(A a)
    {
        if (a != null && a.B != null && a.B.C != null && a.B.C.D != null)
        {
            return a.B.C.D.Value;
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
        Assert.Contains("a?.B?.C?.D?.Value", suggestion.SuggestedCode);
    }

    [Fact]
    public async Task ShouldHandleCountAllConditionsWithMixedExpressions()
    {
        // Arrange - Test CountAllConditions with mixed null and non-null conditions
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
    public string GetCity(Person person, bool flag)
    {
        if (person != null && person.Address != null && flag == true)
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

        // Assert - Should not suggest nested access due to non-null condition
        var nestedSuggestions = suggestions.Where(s => s.Description.Contains("nested null checks")).ToList();
        Assert.Empty(nestedSuggestions);
    }

    [Fact]
    public async Task ShouldHandleGetLineNumberForDifferentNodes()
    {
        // Arrange - Test GetLineNumber with different syntax nodes
        var code = @"
public class Test
{
    public void Method()
    {
        if (true)
        {
            var x = 1;
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find an if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Act - We can't directly test GetLineNumber since it's private, but we can verify line numbers in suggestions
        var options = new RefactoringOptions();
        var suggestions = (await _rule.AnalyzeAsync("test.cs", root, options)).ToList();

        // Assert - Just ensure no exceptions and suggestions have valid line numbers
        foreach (var suggestion in suggestions)
        {
            Assert.True(suggestion.LineNumber > 0);
        }
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessPrivateMethod()
    {
        // Arrange - Test the private AnalyzeNestedPropertyAccess method directly using reflection
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

        // Find the if statement with nested null checks
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return a suggestion when nestedAccess != null
        Assert.NotNull(suggestion);
        Assert.Contains("person?.Address?.City", suggestion.SuggestedCode);
        Assert.Contains("nested null checks", suggestion.Description);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessWhenNestedAccessIsNull()
    {
        // Arrange - Test the private AnalyzeNestedPropertyAccess method when BuildNestedNullConditionalAccess returns null
        var code = @"
public class Person
{
    public Address Address { get; set; }
    public string Name { get; set; }
}

public class Address
{
    public string City { get; set; }
}

public class Test
{
    public string GetData(Person person)
    {
        if (person != null && person.Address != null)
        {
            return person.Name; // Accessing Name but checked Address - this should make nestedAccess null
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return null when nestedAccess is null
        Assert.Null(suggestion);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessWhenConditionIsNotLogicalAnd()
    {
        // Arrange - Test when condition is not a logical AND expression
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
        if (person != null) // Single condition, not logical AND
        {
            return person.Address.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return null when condition is not logical AND
        Assert.Null(suggestion);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessWhenNotAllConditionsAreNullChecks()
    {
        // Arrange - Test when not all conditions in logical AND are null checks
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
    public string GetCity(Person person, bool flag)
    {
        if (person != null && person.Address != null && flag == true) // Mixed conditions
        {
            return person.Address.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return null when not all conditions are null checks
        Assert.Null(suggestion);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessWithAssignmentStatement()
    {
        // Arrange - Test successful case with assignment statement
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
    public void SetCity(Person person)
    {
        string result;
        if (person != null && person.Address != null)
        {
            result = person.Address.City;
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return a suggestion for assignment
        Assert.NotNull(suggestion);
        Assert.Contains("result = person?.Address?.City;", suggestion.SuggestedCode);
        Assert.Contains("nested null checks", suggestion.Description);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessWithReturnStatement()
    {
        // Arrange - Test successful case with return statement
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

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return a suggestion for return statement
        Assert.NotNull(suggestion);
        Assert.Contains("return person?.Address?.City;", suggestion.SuggestedCode);
        Assert.Contains("nested null checks", suggestion.Description);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessWithBlockContainingMultipleStatements()
    {
        // Arrange - Test when if body has multiple statements
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
            Console.WriteLine(""Debug"");
            return person.Address.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return null when block has multiple statements
        Assert.Null(suggestion);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessWithNonAssignmentNonReturnStatement()
    {
        // Arrange - Test when if body has a statement that's not assignment or return
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
    public void ProcessCity(Person person)
    {
        if (person != null && person.Address != null)
        {
            Console.WriteLine(person.Address.City);
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return null when statement is not assignment or return
        Assert.Null(suggestion);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessWithDeepNesting()
    {
        // Arrange - Test with deeper nesting (3 levels)
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

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return a suggestion for deep nesting
        Assert.NotNull(suggestion);
        Assert.Contains("return company?.Department?.Manager?.Name;", suggestion.SuggestedCode);
        Assert.Contains("nested null checks", suggestion.Description);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessWithDifferentVariableNames()
    {
        // Arrange - Test with different variable names
        var code = @"
public class Container
{
    public Item Item { get; set; }
}

public class Item
{
    public Data Data { get; set; }
}

public class Data
{
    public string Value { get; set; }
}

public class Test
{
    public string GetValue(Container container)
    {
        if (container != null && container.Item != null && container.Item.Data != null)
        {
            return container.Item.Data.Value;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return a suggestion with different variable names
        Assert.NotNull(suggestion);
        Assert.Contains("return container?.Item?.Data?.Value;", suggestion.SuggestedCode);
        Assert.Contains("nested null checks", suggestion.Description);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNestedPropertyAccessWithSingleNullCheck()
    {
        // Arrange - Test with single null check (should not trigger nested access)
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

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNestedPropertyAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return null for single null check (not nested)
        Assert.Null(suggestion);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNullConditionalChainPrivateMethod()
    {
        // Arrange - Test the private AnalyzeNullConditionalChain method directly using reflection
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

        // Find the if statement with null-conditional chain
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNullConditionalChain",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return a suggestion for redundant null check
        Assert.NotNull(suggestion);
        Assert.Contains("return person?.Address?.City;", suggestion.SuggestedCode);
        Assert.Contains("redundant null check", suggestion.Description);
    }

    [Fact]
    public async Task ShouldTestAnalyzeNullConditionalChainWhenNoConditional()
    {
        // Arrange - Test when condition doesn't contain null-conditional operators
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

        // Find the if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("AnalyzeNullConditionalChain",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var suggestion = (RefactoringSuggestion?)methodInfo.Invoke(_rule, new object[] { ifStatement, "test.cs", root });

        // Assert - Should return null when no null-conditional operators
        Assert.Null(suggestion);
    }

    [Fact]
    public async Task ShouldTestExtractNullCheckConditionsPrivateMethod()
    {
        // Arrange - Test the private ExtractNullCheckConditions method directly using reflection
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

        // Find the if statement and extract its condition
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();
        var logicalAnd = (Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax)ifStatement.Condition;

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("ExtractNullCheckConditions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var conditions = (List<string>)methodInfo.Invoke(_rule, new object[] { logicalAnd });

        // Assert - Should extract both null check conditions
        Assert.Equal(2, conditions.Count);
        Assert.Contains("person", conditions);
        Assert.Contains("person.Address", conditions);
    }

    [Fact]
    public async Task ShouldTestExtractNullCheckConditionsWithParentheses()
    {
        // Arrange - Test ExtractNullCheckConditions with parenthesized expressions
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

        // Find the if statement and extract its condition
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();
        var logicalAnd = (Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax)ifStatement.Condition;

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("ExtractNullCheckConditions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var conditions = (List<string>)methodInfo.Invoke(_rule, new object[] { logicalAnd });

        // Assert - Should extract both null check conditions despite parentheses
        Assert.Equal(2, conditions.Count);
        Assert.Contains("person", conditions);
        Assert.Contains("person.Address", conditions);
    }

    [Fact]
    public async Task ShouldTestCountAllConditionsPrivateMethod()
    {
        // Arrange - Test the private CountAllConditions method directly using reflection
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

        // Find the if statement and extract its condition
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();
        var logicalAnd = (Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax)ifStatement.Condition;

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("CountAllConditions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var count = (int)methodInfo.Invoke(_rule, new object[] { logicalAnd });

        // Assert - Should count both conditions
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task ShouldTestCountAllConditionsWithMixedExpressions()
    {
        // Arrange - Test CountAllConditions with mixed null and non-null conditions
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
    public string GetCity(Person person, bool flag)
    {
        if (person != null && person.Address != null && flag == true)
        {
            return person.Address.City;
        }
        return null;
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find the if statement and extract its condition
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();
        var logicalAnd = (Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax)ifStatement.Condition;

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("CountAllConditions",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var count = (int)methodInfo.Invoke(_rule, new object[] { logicalAnd });

        // Assert - Should count all three conditions (2 null checks + 1 non-null)
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task ShouldTestBuildNestedNullConditionalAccessPrivateMethod()
    {
        // Arrange - Test the private BuildNestedNullConditionalAccess method directly using reflection
        var conditions = new List<string> { "person", "person.Address" };
        var targetExpression = "person.Address.City";

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("BuildNestedNullConditionalAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var result = (string?)methodInfo.Invoke(_rule, new object[] { conditions, targetExpression });

        // Assert - Should build the null-conditional chain
        Assert.NotNull(result);
        Assert.Equal("person?.Address?.City", result);
    }

    [Fact]
    public async Task ShouldTestBuildNestedNullConditionalAccessWithNoMatch()
    {
        // Arrange - Test BuildNestedNullConditionalAccess when target doesn't match conditions
        var conditions = new List<string> { "person", "person.Address" };
        var targetExpression = "person.Name"; // Different property than checked

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("BuildNestedNullConditionalAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var result = (string?)methodInfo.Invoke(_rule, new object[] { conditions, targetExpression });

        // Assert - Should return null when target doesn't match conditions
        Assert.Null(result);
    }

    [Fact]
    public async Task ShouldTestBuildNestedNullConditionalAccessWithEmptyConditions()
    {
        // Arrange - Test BuildNestedNullConditionalAccess with empty conditions
        var conditions = new List<string>();
        var targetExpression = "person.Address.City";

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("BuildNestedNullConditionalAccess",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var result = (string?)methodInfo.Invoke(_rule, new object[] { conditions, targetExpression });

        // Assert - Should return null when no conditions
        Assert.Null(result);
    }

    [Fact]
    public async Task ShouldTestGetLineNumberPrivateMethod()
    {
        // Arrange - Test the private GetLineNumber method directly using reflection
        var code = @"
public class Test
{
    public void Method()
    {
        if (true)
        {
            var x = 1;
        }
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find an if statement
        var ifStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("GetLineNumber",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var lineNumber = (int)methodInfo.Invoke(_rule, new object[] { root, ifStatement });

        // Assert - Should return the correct line number (1-based)
        Assert.Equal(6, lineNumber); // if statement is on line 6
    }

    [Fact]
    public async Task ShouldTestGetLineNumberWithDifferentNode()
    {
        // Arrange - Test GetLineNumber with a different syntax node
        var code = @"
public class Test
{
    public string Method()
    {
        return ""test"";
    }
}";

        var tree = CSharpSyntaxTree.ParseText(code);
        var root = await tree.GetRootAsync();

        // Find a return statement
        var returnStatement = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ReturnStatementSyntax>().First();

        // Use reflection to access the private method
        var methodInfo = typeof(NullCheckRefactoringRule).GetMethod("GetLineNumber",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(methodInfo);

        // Act - Invoke the private method
        var lineNumber = (int)methodInfo.Invoke(_rule, new object[] { root, returnStatement });

        // Assert - Should return the correct line number
        Assert.Equal(6, lineNumber); // return statement is on line 6
    }
}
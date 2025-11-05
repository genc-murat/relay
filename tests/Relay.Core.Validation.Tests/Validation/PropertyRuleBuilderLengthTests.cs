using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder length validation methods (MinLength, MaxLength)
/// </summary>
public class PropertyRuleBuilderLengthTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
    }

    #region Length Validation Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_MinLength_WithValidLength_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).MinLength(3);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Valid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Empty(allErrors);
    }

    [Fact]
    public async Task PropertyRuleBuilder_MinLength_WithInvalidLength_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).MinLength(5);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Hi" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("at least 5 characters"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_MinLength_WithNullValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).MinLength(3);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = null };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Empty(allErrors); // Null values pass string-only validations
    }

    [Fact]
    public async Task PropertyRuleBuilder_MaxLength_WithValidLength_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).MaxLength(10);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Valid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Empty(allErrors);
    }

    [Fact]
    public async Task PropertyRuleBuilder_MaxLength_WithInvalidLength_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).MaxLength(3);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Too Long" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("must not exceed 3 characters"));
    }

    #endregion
}
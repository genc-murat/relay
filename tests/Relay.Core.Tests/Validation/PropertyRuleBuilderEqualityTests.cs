using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder equality and collection validation methods
/// </summary>
public class PropertyRuleBuilderEqualityTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Status { get; set; }
    }

    #region Equality and Collection Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_EqualTo_WithMatchingValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).EqualTo("active");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "active" };

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
    public async Task PropertyRuleBuilder_EqualTo_WithNonMatchingValue_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).EqualTo("active");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "inactive" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("must equal active"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_NotEqualTo_WithDifferentValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).NotEqualTo("inactive");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "active" };

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
    public async Task PropertyRuleBuilder_In_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).In(new[] { "active", "pending", "suspended" });

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "active" };

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
    public async Task PropertyRuleBuilder_In_WithInvalidValue_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).In(new[] { "active", "pending", "suspended" });

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "deleted" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("valid values"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_NotIn_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).NotIn(new[] { "deleted", "banned" });

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "active" };

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

    #endregion
}
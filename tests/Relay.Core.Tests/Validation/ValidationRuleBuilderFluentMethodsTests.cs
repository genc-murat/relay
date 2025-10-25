using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ValidationRuleBuilder new fluent methods
/// </summary>
public class ValidationRuleBuilderFluentMethodsTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Status { get; set; }
        public int Age { get; set; }
    }

    #region New Fluent Methods Tests

    [Fact]
    public async Task ValidationRuleBuilder_EqualTo_ShouldValidateEquality()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).EqualTo("active");

        var rules = builder.Build().ToList();

        // Test valid case
        var request1 = new TestRequest { Status = "active" };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.Empty(errors1);

        // Test invalid case
        var request2 = new TestRequest { Status = "inactive" };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors2);
    }

    [Fact]
    public async Task ValidationRuleBuilder_NotEqualTo_ShouldValidateInequality()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).NotEqualTo("inactive");

        var rules = builder.Build().ToList();

        // Test valid case
        var request1 = new TestRequest { Status = "active" };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.Empty(errors1);

        // Test invalid case
        var request2 = new TestRequest { Status = "inactive" };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors2);
    }

    [Fact]
    public async Task ValidationRuleBuilder_In_ShouldValidateInclusion()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).In(new[] { "active", "pending", "suspended" });

        var rules = builder.Build().ToList();

        // Test valid case
        var request1 = new TestRequest { Status = "active" };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.Empty(errors1);

        // Test invalid case
        var request2 = new TestRequest { Status = "deleted" };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors2);
    }

    [Fact]
    public async Task ValidationRuleBuilder_NotIn_ShouldValidateExclusion()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).NotIn(new[] { "deleted", "banned" });

        var rules = builder.Build().ToList();

        // Test valid case
        var request1 = new TestRequest { Status = "active" };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.Empty(errors1);

        // Test invalid case
        var request2 = new TestRequest { Status = "deleted" };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors2);
    }

    [Fact]
    public async Task ValidationRuleBuilder_Between_ShouldValidateRange()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).Between(18, 65);

        var rules = builder.Build().ToList();

        // Test valid case
        var request1 = new TestRequest { Age = 25 };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.Empty(errors1);

        // Test invalid case (too low)
        var request2 = new TestRequest { Age = 16 };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors2);

        // Test invalid case (too high)
        var request3 = new TestRequest { Age = 70 };
        var errors3 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request3);
            errors3.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors3);
    }

    [Fact]
    public async Task ValidationRuleBuilder_GreaterThanOrEqualTo_ShouldValidateMinimum()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).GreaterThanOrEqualTo(18);

        var rules = builder.Build().ToList();

        // Test valid case
        var request1 = new TestRequest { Age = 25 };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.Empty(errors1);

        // Test boundary case
        var request2 = new TestRequest { Age = 18 };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.Empty(errors2);

        // Test invalid case
        var request3 = new TestRequest { Age = 16 };
        var errors3 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request3);
            errors3.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors3);
    }

    [Fact]
    public async Task ValidationRuleBuilder_LessThanOrEqualTo_ShouldValidateMaximum()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).LessThanOrEqualTo(65);

        var rules = builder.Build().ToList();

        // Test valid case
        var request1 = new TestRequest { Age = 60 };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.Empty(errors1);

        // Test boundary case
        var request2 = new TestRequest { Age = 65 };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.Empty(errors2);

        // Test invalid case
        var request3 = new TestRequest { Age = 70 };
        var errors3 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request3);
            errors3.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors3);
    }

    [Fact]
    public void ValidationRuleBuilder_GetPropertyName_ShouldThrowArgumentException_ForInvalidExpression()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();

        // Act & Assert - Invalid expression (method call instead of property access)
        Assert.Throws<ArgumentException>(() =>
            builder.RuleFor(x => x.Status!.ToString()));
    }

    [Fact]
    public void ValidationRuleBuilder_GetPropertyName_ShouldHandleValueTypeProperties()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();

        // Act - This should work for value types (Age is int)
        var ruleBuilder = builder.RuleFor(x => x.Age);

        // Assert - Should not throw and should have correct property name
        Assert.NotNull(ruleBuilder);
        // The property name is extracted internally, so we can't directly test it,
        // but if it works without throwing, the UnaryExpression case is handled
    }

    #endregion
}
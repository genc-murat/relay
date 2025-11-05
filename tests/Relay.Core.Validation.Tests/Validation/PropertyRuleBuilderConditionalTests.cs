using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder conditional validation methods (When, Unless, WhenProperty)
/// </summary>
public class PropertyRuleBuilderConditionalTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public string? Country { get; set; }
        public string? TurkishId { get; set; }
    }

    #region Conditional Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_When_WithTrueCondition_ShouldApplyValidation()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Email)
            .When(request => request.IsActive)
            .NotNull();

        var rules = builder.Build().ToList();

        // Test when condition is true
        var request1 = new TestRequest { IsActive = true, Email = null };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors1);

        // Test when condition is false
        var request2 = new TestRequest { IsActive = false, Email = null };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.Empty(errors2);
    }

    [Fact]
    public async Task PropertyRuleBuilder_Unless_WithFalseCondition_ShouldApplyValidation()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Email)
            .Unless(request => !request.IsActive)
            .NotNull();

        var rules = builder.Build().ToList();

        // Test when unless condition is false (should validate)
        var request1 = new TestRequest { IsActive = true, Email = null };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors1);
    }

    [Fact]
    public async Task PropertyRuleBuilder_WhenProperty_WithMatchingCondition_ShouldApplyValidation()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishId)
            .WhenProperty(x => x.Country, country => country == "Turkey")
            .NotNull();

        var rules = builder.Build().ToList();

        // Test when dependent property matches condition
        var request1 = new TestRequest { Country = "Turkey", TurkishId = null };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors1);

        // Test when dependent property doesn't match condition
        var request2 = new TestRequest { Country = "USA", TurkishId = null };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.Empty(errors2);
    }

    #endregion
}
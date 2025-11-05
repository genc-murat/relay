using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ConditionalPropertyRuleBuilder Must method
/// </summary>
public class ConditionalPropertyRuleBuilderMustTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Description { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string? Status { get; set; }
    }

    #region Must Method Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_Must_WhenConditionTrue_ShouldValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Age)
            .When(x => x.IsActive)
            .Must(age => age >= 18, "Age must be at least 18");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Age = 16 };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Age"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_Must_WhenConditionFalse_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Age)
            .When(x => x.IsActive)
            .Must(age => age >= 18, "Age must be at least 18");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Age = 16 };

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
    public async Task ConditionalPropertyRuleBuilder_Must_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Age)
            .When(x => x.IsActive)
            .Must(age => age >= 18, "Age must be at least 18");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Age = 25 };

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
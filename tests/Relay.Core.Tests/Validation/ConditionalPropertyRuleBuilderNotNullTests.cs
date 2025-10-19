using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ConditionalPropertyRuleBuilder NotNull method
/// </summary>
public class ConditionalPropertyRuleBuilderNotNullTests
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

    #region Constructor and Basic Setup Tests

    [Fact]
    public void ConditionalPropertyRuleBuilder_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var rules = new List<IValidationRuleConfiguration<TestRequest>>();
        var propertyFunc = new Func<TestRequest, string?>(x => x.Name);
        var condition = new Func<TestRequest, bool>(x => x.IsActive);

        // Act
        var builder = new ConditionalPropertyRuleBuilder<TestRequest, string?>(
            "Name", propertyFunc, rules, condition, true);

        // Assert
        Assert.NotNull(builder);
        // Constructor is internal, so we can't directly test it, but we can test through public API
    }

    #endregion

    #region NotNull Method Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NotNull_WhenConditionTrue_ShouldValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotNull();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = null };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Name") && e.Contains("null"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NotNull_WhenConditionFalse_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotNull();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Name = null };

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
    public async Task ConditionalPropertyRuleBuilder_NotNull_UnlessConditionTrue_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .Unless(x => !x.IsActive)
            .NotNull();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Name = null };

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
    public async Task ConditionalPropertyRuleBuilder_NotNull_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotNull("Custom error message");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = null };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Custom error message"));
    }

    #endregion
}
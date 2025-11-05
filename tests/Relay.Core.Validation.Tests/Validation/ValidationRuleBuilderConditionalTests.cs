using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ValidationRuleBuilder conditional validation methods
/// </summary>
public class ValidationRuleBuilderConditionalTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
    }

    #region Conditional Validation Tests

    [Fact]
    public async Task ValidationRuleBuilder_When_ShouldApplyValidationConditionally()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Email)
            .When(request => request.IsActive)
            .NotNull()
            .EmailAddress();

        var rules = builder.Build().ToList();

        // Test case 1: When condition is true
        var request1 = new TestRequest { IsActive = true, Email = null };

        // Act
        var allErrors1 = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request1);
            allErrors1.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors1);
        Assert.Contains(allErrors1, e => e.Contains("Email"));

        // Test case 2: When condition is false
        var request2 = new TestRequest { IsActive = false, Email = null };

        // Act
        var allErrors2 = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request2);
            allErrors2.AddRange(errors);
        }

        // Assert
        Assert.Empty(allErrors2);
    }

    [Fact]
    public async Task ValidationRuleBuilder_Unless_ShouldApplyValidationConditionally()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Email)
            .Unless(request => !request.IsActive)
            .NotNull()
            .EmailAddress();

        var rules = builder.Build().ToList();

        // Test case 1: Unless condition is false (should validate)
        var request1 = new TestRequest { IsActive = true, Email = null };

        // Act
        var allErrors1 = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request1);
            allErrors1.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors1);

        // Test case 2: Unless condition is true (should not validate)
        var request2 = new TestRequest { IsActive = false, Email = null };

        // Act
        var allErrors2 = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request2);
            allErrors2.AddRange(errors);
        }

        // Assert
        Assert.Empty(allErrors2);
    }

    [Fact]
    public async Task ValidationRuleBuilder_ConditionalValidation_ShouldChainMultipleRules()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Email)
            .When(request => request.IsActive)
            .NotNull()
            .NotEmpty()
            .EmailAddress();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Email = "" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("empty"));
    }

    #endregion
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ConditionalPropertyRuleBuilder EmailAddress method
/// </summary>
public class ConditionalPropertyRuleBuilderEmailTests
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

    #region EmailAddress Method Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_EmailAddress_WhenConditionTrue_ShouldValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Email)
            .When(x => x.IsActive)
            .EmailAddress();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Email = "invalid-email" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Email"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_EmailAddress_WhenConditionFalse_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Email)
            .When(x => x.IsActive)
            .EmailAddress();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Email = "invalid-email" };

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
    public async Task ConditionalPropertyRuleBuilder_EmailAddress_WithValidEmail_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Email)
            .When(x => x.IsActive)
            .EmailAddress();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Email = "test@example.com" };

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
    public async Task ConditionalPropertyRuleBuilder_EmailAddress_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Email)
            .When(x => x.IsActive)
            .EmailAddress("Custom email error");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Email = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Custom email error"));
    }

    #endregion
}
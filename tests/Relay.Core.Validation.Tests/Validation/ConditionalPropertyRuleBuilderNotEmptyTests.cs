using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ConditionalPropertyRuleBuilder NotEmpty method
/// </summary>
public class ConditionalPropertyRuleBuilderNotEmptyTests
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

    #region NotEmpty Method Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NotEmpty_WhenConditionTrue_ShouldValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotEmpty();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Name") && e.Contains("empty"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NotEmpty_WhenConditionFalse_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotEmpty();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Name = "" };

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
    public async Task ConditionalPropertyRuleBuilder_NotEmpty_WithWhitespace_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotEmpty();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "   " };

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

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NotEmpty_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotEmpty("Custom empty message");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Custom empty message"));
    }

    #endregion
}
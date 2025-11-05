using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder general string validation methods (Numeric, Alpha, etc.)
/// </summary>
public class PropertyRuleBuilderStringTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Numeric { get; set; }
        public string? Alpha { get; set; }
        public string? Alphanumeric { get; set; }
        public string? DigitsOnly { get; set; }
        public string? NoWhitespace { get; set; }
    }

    #region General String Validators Tests

    [Fact]
    public async Task PropertyRuleBuilder_Numeric_WithValidNumber_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Numeric).Numeric();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Numeric = "12345" };

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
    public async Task PropertyRuleBuilder_Alpha_WithValidLetters_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Alpha).Alpha();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Alpha = "ValidLetters" };

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
    public async Task PropertyRuleBuilder_Alphanumeric_WithValidChars_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Alphanumeric).Alphanumeric();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Alphanumeric = "Valid123" };

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
    public async Task PropertyRuleBuilder_DigitsOnly_WithValidDigits_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.DigitsOnly).DigitsOnly();

        var rules = builder.Build().ToList();
        var request = new TestRequest { DigitsOnly = "12345" };

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
    public async Task PropertyRuleBuilder_NoWhitespace_WithValidString_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.NoWhitespace).NoWhitespace();

        var rules = builder.Build().ToList();
        var request = new TestRequest { NoWhitespace = "NoWhitespace" };

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
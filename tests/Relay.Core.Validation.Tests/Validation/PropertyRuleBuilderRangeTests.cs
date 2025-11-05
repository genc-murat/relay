using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder range validation methods (Between, GreaterThanOrEqualTo, etc.)
/// </summary>
public class PropertyRuleBuilderRangeTests
{
    // Test request classes
    public class TestRequest
    {
        public int Age { get; set; }
    }

    #region Range Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_Between_WithValueInRange_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).Between(18, 65);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 25 };

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
    public async Task PropertyRuleBuilder_Between_WithValueBelowRange_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).Between(18, 65);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 16 };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("between 18 and 65"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_GreaterThanOrEqualTo_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).GreaterThanOrEqualTo(18);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 25 };

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
    public async Task PropertyRuleBuilder_LessThanOrEqualTo_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).LessThanOrEqualTo(65);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 60 };

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
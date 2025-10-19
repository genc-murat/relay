using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder comparison methods (GreaterThan, LessThan, etc.)
/// </summary>
public class PropertyRuleBuilderComparisonTests
{
    // Test request classes
    public class TestRequest
    {
        public int Age { get; set; }
    }

    #region Comparison Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_GreaterThan_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).GreaterThan(18);

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
    public async Task PropertyRuleBuilder_GreaterThan_WithInvalidValue_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).GreaterThan(18);

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
        Assert.Contains(allErrors, e => e.Contains("must be greater than 18"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_LessThan_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).LessThan(65);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 30 };

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
    public async Task PropertyRuleBuilder_LessThan_WithInvalidValue_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).LessThan(65);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 70 };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("must be less than 65"));
    }

    #endregion
}
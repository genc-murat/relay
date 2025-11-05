using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder constructor and basic setup
/// </summary>
public class PropertyRuleBuilderConstructorTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    #region Constructor and Basic Setup Tests

    [Fact]
    public void PropertyRuleBuilder_RuleFor_ShouldCreateBuilderCorrectly()
    {
        // Arrange
        var ruleBuilder = new ValidationRuleBuilder<TestRequest>();

        // Act
        var propertyBuilder = ruleBuilder.RuleFor(r => r.Name);

        // Assert
        Assert.NotNull(propertyBuilder);
    }

    [Fact]
    public void PropertyRuleBuilder_RuleFor_WithValueType_ShouldCreateBuilderCorrectly()
    {
        // Arrange
        var ruleBuilder = new ValidationRuleBuilder<TestRequest>();

        // Act
        var propertyBuilder = ruleBuilder.RuleFor(r => r.Age);

        // Assert
        Assert.NotNull(propertyBuilder);
    }

    #endregion
}
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
    public void PropertyRuleBuilder_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var rules = new List<IValidationRuleConfiguration<TestRequest>>();
        var propertyFunc = new Func<TestRequest, string?>(r => r.Name);

        // Act
        var builder = new PropertyRuleBuilder<TestRequest, string?>("Name", propertyFunc, rules);

        // Assert
        Assert.NotNull(builder);
        Assert.Empty(rules); // No rules added yet
    }

    [Fact]
    public void PropertyRuleBuilder_Constructor_WithValueType_ShouldInitializeCorrectly()
    {
        // Arrange
        var rules = new List<IValidationRuleConfiguration<TestRequest>>();
        var propertyFunc = new Func<TestRequest, int>(r => r.Age);

        // Act
        var builder = new PropertyRuleBuilder<TestRequest, int>("Age", propertyFunc, rules);

        // Assert
        Assert.NotNull(builder);
        Assert.Empty(rules);
    }

    #endregion
}
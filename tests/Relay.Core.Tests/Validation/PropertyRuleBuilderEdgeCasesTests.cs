using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder edge cases and error handling
/// </summary>
public class PropertyRuleBuilderEdgeCasesTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task PropertyRuleBuilder_NonStringProperty_WithStringMethods_ShouldNotAddRules()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age)
            .NotEmpty() // This should not add any rules for int property
            .MinLength(1); // This should not add any rules for int property

        var rules = builder.Build().ToList();

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(new TestRequest { Age = 25 });
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Empty(allErrors); // No rules should be added for string-only methods on int property
    }

    [Fact]
    public void PropertyRuleBuilder_Must_WithNullPredicate_ShouldThrowException()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.RuleFor(x => x.Name).Must(null!, "Error message"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_WithNullRequest_ShouldHandleGracefully()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).NotNull();

        var rules = builder.Build().ToList();

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(null!);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Request cannot be null"));
    }

    #endregion
}
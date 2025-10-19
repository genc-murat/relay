using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder Must method
/// </summary>
public class PropertyRuleBuilderMustTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
    }

    #region Must Method Tests

    [Fact]
    public async Task PropertyRuleBuilder_Must_WithValidPredicate_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).Must(name => name?.Length > 3, "Name must be longer than 3 characters");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Valid" };

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
    public async Task PropertyRuleBuilder_Must_WithInvalidPredicate_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).Must(name => name?.Length > 10, "Name must be longer than 10 characters");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Short" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains("Name must be longer than 10 characters", allErrors.First());
    }

    [Fact]
    public async Task PropertyRuleBuilder_Must_WithNullValue_ShouldHandleNullCorrectly()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).Must(name => name != null && name.Length > 3, "Name must not be null and longer than 3 characters");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = null };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
    }

    #endregion
}
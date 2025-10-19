using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder basic validation methods (NotNull, NotEmpty)
/// </summary>
public class PropertyRuleBuilderBasicValidationTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
    }

    #region Basic Validation Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_NotNull_WithNullValue_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).NotNull();

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
        Assert.Contains(allErrors, e => e.Contains("must not be null"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_NotNull_WithValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).NotNull();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Valid Name" };

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
    public async Task PropertyRuleBuilder_NotNull_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).NotNull("Custom null error message");

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
        Assert.Contains("Custom null error message", allErrors.First());
    }

    [Fact]
    public async Task PropertyRuleBuilder_NotEmpty_WithEmptyString_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).NotEmpty();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("must not be empty"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_NotEmpty_WithWhitespace_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).NotEmpty();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "   " };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("must not be empty"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_NotEmpty_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).NotEmpty();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Valid Name" };

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
    public async Task PropertyRuleBuilder_NotEmpty_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).NotEmpty("Custom empty error message");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains("Custom empty error message", allErrors.First());
    }

    #endregion
}
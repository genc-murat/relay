using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder regex validation methods
/// </summary>
public class PropertyRuleBuilderRegexTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
    }

    #region Regex Matches Tests

    [Fact]
    public async Task PropertyRuleBuilder_Matches_WithValidPattern_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).Matches(@"^[A-Za-z]+$");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "ValidName" };

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
    public async Task PropertyRuleBuilder_Matches_WithInvalidPattern_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).Matches(@"^[A-Za-z]+$");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Invalid123" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("format is invalid"));
    }

    #endregion
}
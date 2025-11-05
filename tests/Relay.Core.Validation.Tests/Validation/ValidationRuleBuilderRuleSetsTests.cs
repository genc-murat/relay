using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ValidationRuleBuilder rule sets functionality
/// </summary>
public class ValidationRuleBuilderRuleSetsTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
    }

    #region Validation Sets Tests

    [Fact]
    public async Task ValidationRuleBuilder_RuleSet_ShouldCreateNamedRuleSets()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleSet("BasicValidation", ruleSetBuilder =>
        {
            ruleSetBuilder.RuleFor(x => x.Name).NotNull().NotEmpty();
        });

        builder.RuleSet("EmailValidation", ruleSetBuilder =>
        {
            ruleSetBuilder.RuleFor(x => x.Email).NotNull().EmailAddress();
        });

        // Test basic validation rule set
        var basicRules = builder.BuildRuleSet("BasicValidation").ToList();
        var request1 = new TestRequest { Name = null };

        // Act
        var basicErrors = new List<string>();
        foreach (var rule in basicRules)
        {
            var errors = await rule.ValidateAsync(request1);
            basicErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(basicErrors);
        Assert.Contains(basicErrors, e => e.Contains("Name"));

        // Test email validation rule set
        var emailRules = builder.BuildRuleSet("EmailValidation").ToList();
        var request2 = new TestRequest { Email = "invalid-email" };

        // Act
        var emailErrors = new List<string>();
        foreach (var rule in emailRules)
        {
            var errors = await rule.ValidateAsync(request2);
            emailErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(emailErrors);
        Assert.Contains(emailErrors, e => e.Contains("Email"));
    }

    [Fact]
    public async Task ValidationRuleBuilder_IncludeRuleSet_ShouldIncludeRulesFromRuleSet()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleSet("EmailRules", ruleSetBuilder =>
        {
            ruleSetBuilder.RuleFor(x => x.Email).NotNull().EmailAddress();
        });

        builder.IncludeRuleSet("EmailRules");
        builder.RuleFor(x => x.Name).NotNull();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = null, Email = "invalid-email" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Equal(2, allErrors.Count);
        Assert.Contains(allErrors, e => e.Contains("Name"));
        Assert.Contains(allErrors, e => e.Contains("Email"));
    }

    [Fact]
    public void ValidationRuleBuilder_BuildRuleSet_WithNonExistentRuleSet_ShouldReturnEmpty()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();

        // Act
        var rules = builder.BuildRuleSet("NonExistentRuleSet");

        // Assert
        Assert.Empty(rules);
    }

    #endregion
}
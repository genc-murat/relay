using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder method chaining
/// </summary>
public class PropertyRuleBuilderChainingTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
    }

    #region Method Chaining Tests

    [Fact]
    public async Task PropertyRuleBuilder_MethodChaining_ShouldWorkCorrectly()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .MinLength(2)
            .MaxLength(50);

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
    public async Task PropertyRuleBuilder_MethodChaining_WithMultipleFailures_ShouldReturnAllErrors()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .MinLength(5);

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
        Assert.Equal(2, allErrors.Count); // NotEmpty and MinLength should both fail
    }

    #endregion
}
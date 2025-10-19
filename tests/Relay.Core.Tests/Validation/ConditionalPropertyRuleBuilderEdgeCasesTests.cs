using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ConditionalPropertyRuleBuilder edge cases and error handling
/// </summary>
public class ConditionalPropertyRuleBuilderEdgeCasesTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Description { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public string? Status { get; set; }
    }

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NonStringProperty_WithStringMethods_ShouldNotAddRules()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Age) // Age is int, not string
            .When(x => x.IsActive)
            .NotEmpty() // This should not add any rules since Age is not a string
            .EmailAddress() // This should not add any rules since Age is not a string
            .MinLength(5) // This should not add any rules since Age is not a string
            .MaxLength(10); // This should not add any rules since Age is not a string

        var rules = builder.Build().ToList();

        // Act - Should not have any rules since Age is not a string
        var request = new TestRequest { IsActive = true, Age = 25 };
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Empty(allErrors); // No rules should be added for non-string properties
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_Must_WithNullPredicate_ShouldThrowException()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            builder.RuleFor(x => x.Name)
                .When(x => x.IsActive)
                .Must(null!, "Test error message");
        });
    }

    #endregion
}
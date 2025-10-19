using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ConditionalPropertyRuleBuilder method chaining
/// </summary>
public class ConditionalPropertyRuleBuilderChainingTests
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

    #region Method Chaining Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_MethodChaining_ShouldWorkCorrectly()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotNull()
            .NotEmpty()
            .MinLength(3)
            .MaxLength(10);

        var rules = builder.Build().ToList();

        // Test valid case
        var validRequest = new TestRequest { IsActive = true, Name = "John" };
        var validErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(validRequest);
            validErrors.AddRange(errors);
        }
        Assert.Empty(validErrors);

        // Test invalid case - null
        var nullRequest = new TestRequest { IsActive = true, Name = null };
        var nullErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(nullRequest);
            nullErrors.AddRange(errors);
        }
        Assert.NotEmpty(nullErrors);

        // Test invalid case - empty
        var emptyRequest = new TestRequest { IsActive = true, Name = "" };
        var emptyErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(emptyRequest);
            emptyErrors.AddRange(errors);
        }
        Assert.NotEmpty(emptyErrors);

        // Test invalid case - too short
        var shortRequest = new TestRequest { IsActive = true, Name = "A" };
        var shortErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(shortRequest);
            shortErrors.AddRange(errors);
        }
        Assert.NotEmpty(shortErrors);

        // Test invalid case - too long
        var longRequest = new TestRequest { IsActive = true, Name = "ThisNameIsWayTooLong" };
        var longErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(longRequest);
            longErrors.AddRange(errors);
        }
        Assert.NotEmpty(longErrors);
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_MethodChaining_WithUnless_ShouldWorkCorrectly()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .Unless(x => !x.IsActive)
            .NotNull()
            .MinLength(2);

        var rules = builder.Build().ToList();

        // Test case 1: Condition met (IsActive = true), should validate
        var activeRequest = new TestRequest { IsActive = true, Name = "A" };
        var activeErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(activeRequest);
            activeErrors.AddRange(errors);
        }
        Assert.NotEmpty(activeErrors); // Should fail min length

        // Test case 2: Condition not met (IsActive = false), should not validate
        var inactiveRequest = new TestRequest { IsActive = false, Name = "A" };
        var inactiveErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(inactiveRequest);
            inactiveErrors.AddRange(errors);
        }
        Assert.Empty(inactiveErrors); // Should not validate
    }

    #endregion
}
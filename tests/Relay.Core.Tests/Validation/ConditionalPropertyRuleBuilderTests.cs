using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ConditionalPropertyRuleBuilder class to increase code coverage
/// </summary>
public class ConditionalPropertyRuleBuilderTests
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

    #region Constructor and Basic Setup Tests

    [Fact]
    public void ConditionalPropertyRuleBuilder_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var rules = new List<IValidationRuleConfiguration<TestRequest>>();
        var propertyFunc = new Func<TestRequest, string?>(x => x.Name);
        var condition = new Func<TestRequest, bool>(x => x.IsActive);

        // Act
        var builder = new ConditionalPropertyRuleBuilder<TestRequest, string?>(
            "Name", propertyFunc, rules, condition, true);

        // Assert
        Assert.NotNull(builder);
        // Constructor is internal, so we can't directly test it, but we can test through public API
    }

    #endregion

    #region NotNull Method Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NotNull_WhenConditionTrue_ShouldValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotNull();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = null };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Name") && e.Contains("null"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NotNull_WhenConditionFalse_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotNull();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Name = null };

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
    public async Task ConditionalPropertyRuleBuilder_NotNull_UnlessConditionTrue_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .Unless(x => !x.IsActive)
            .NotNull();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Name = null };

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
    public async Task ConditionalPropertyRuleBuilder_NotNull_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotNull("Custom error message");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = null };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Custom error message"));
    }

    #endregion

    #region NotEmpty Method Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NotEmpty_WhenConditionTrue_ShouldValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotEmpty();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Name") && e.Contains("empty"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NotEmpty_WhenConditionFalse_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotEmpty();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Name = "" };

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
    public async Task ConditionalPropertyRuleBuilder_NotEmpty_WithWhitespace_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotEmpty();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "   " };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("empty"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_NotEmpty_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .NotEmpty("Custom empty message");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Custom empty message"));
    }

    #endregion

    #region Must Method Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_Must_WhenConditionTrue_ShouldValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Age)
            .When(x => x.IsActive)
            .Must(age => age >= 18, "Age must be at least 18");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Age = 16 };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Age"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_Must_WhenConditionFalse_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Age)
            .When(x => x.IsActive)
            .Must(age => age >= 18, "Age must be at least 18");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Age = 16 };

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
    public async Task ConditionalPropertyRuleBuilder_Must_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Age)
            .When(x => x.IsActive)
            .Must(age => age >= 18, "Age must be at least 18");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Age = 25 };

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

    #endregion

    #region EmailAddress Method Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_EmailAddress_WhenConditionTrue_ShouldValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Email)
            .When(x => x.IsActive)
            .EmailAddress();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Email = "invalid-email" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Email"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_EmailAddress_WhenConditionFalse_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Email)
            .When(x => x.IsActive)
            .EmailAddress();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Email = "invalid-email" };

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
    public async Task ConditionalPropertyRuleBuilder_EmailAddress_WithValidEmail_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Email)
            .When(x => x.IsActive)
            .EmailAddress();

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Email = "test@example.com" };

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
    public async Task ConditionalPropertyRuleBuilder_EmailAddress_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Email)
            .When(x => x.IsActive)
            .EmailAddress("Custom email error");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Email = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Custom email error"));
    }

    #endregion

    #region MinLength Method Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_MinLength_WhenConditionTrue_ShouldValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .MinLength(5);

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "abc" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Name") && e.Contains("5"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_MinLength_WhenConditionFalse_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .MinLength(5);

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Name = "abc" };

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
    public async Task ConditionalPropertyRuleBuilder_MinLength_WithValidLength_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .MinLength(5);

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "abcdef" };

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
    public async Task ConditionalPropertyRuleBuilder_MinLength_WithNullValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .MinLength(5);

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = null };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Empty(allErrors); // null values are allowed for MinLength
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_MinLength_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .MinLength(5, "Custom min length error");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "abc" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Custom min length error"));
    }

    #endregion

    #region MaxLength Method Tests

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_MaxLength_WhenConditionTrue_ShouldValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .MaxLength(5);

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "abcdefg" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Name") && e.Contains("5"));
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_MaxLength_WhenConditionFalse_ShouldNotValidate()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .MaxLength(5);

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = false, Name = "abcdefg" };

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
    public async Task ConditionalPropertyRuleBuilder_MaxLength_WithValidLength_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .MaxLength(10);

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "abcdef" };

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
    public async Task ConditionalPropertyRuleBuilder_MaxLength_WithNullValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .MaxLength(5);

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = null };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Empty(allErrors); // null values are allowed for MaxLength
    }

    [Fact]
    public async Task ConditionalPropertyRuleBuilder_MaxLength_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        var conditionalBuilder = builder.RuleFor(x => x.Name)
            .When(x => x.IsActive)
            .MaxLength(5, "Custom max length error");

        var rules = builder.Build().ToList();
        var request = new TestRequest { IsActive = true, Name = "abcdefg" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Custom max length error"));
    }

    #endregion

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
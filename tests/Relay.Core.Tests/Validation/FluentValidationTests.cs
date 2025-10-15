using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Validation;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Simple test logger for tests
/// </summary>
internal class TestLogger<T> : ILogger<T>
{
    public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}

public class FluentValidationTests
{
    [Fact]
    public async Task AbstractValidator_ShouldValidateNotNull()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest { Name = null };

        // Act
        var errors = (await validator.ValidateAsync(request)).ToList();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Name"));
    }

    [Fact]
    public async Task AbstractValidator_ShouldValidateNotEmpty()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest { Name = "", Email = "test@example.com" };

        // Act
        var errors = (await validator.ValidateAsync(request)).ToList();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Name") && e.Contains("empty"));
    }

    [Fact]
    public async Task AbstractValidator_ShouldValidateMinLength()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest { Name = "ab", Email = "test@example.com" };

        // Act
        var errors = (await validator.ValidateAsync(request)).ToList();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Name") && e.Contains("3 characters"));
    }

    [Fact]
    public async Task AbstractValidator_ShouldValidateMaxLength()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest { Name = "VeryLongNameThatExceedsMaximum", Email = "test@example.com" };

        // Act
        var errors = (await validator.ValidateAsync(request)).ToList();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Name") && e.Contains("20 characters"));
    }

    [Fact]
    public async Task AbstractValidator_ShouldValidateEmail()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest { Name = "Test", Email = "invalid-email" };

        // Act
        var errors = (await validator.ValidateAsync(request)).ToList();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Email"));
    }

    [Fact]
    public async Task AbstractValidator_ShouldValidateGreaterThan()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest { Name = "Test", Email = "test@example.com", Age = 5 };

        // Act
        var errors = (await validator.ValidateAsync(request)).ToList();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Age") && e.Contains("greater than"));
    }

    [Fact]
    public async Task AbstractValidator_ShouldValidateLessThan()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest { Name = "Test", Email = "test@example.com", Age = 150 };

        // Act
        var errors = (await validator.ValidateAsync(request)).ToList();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Age") && e.Contains("less than"));
    }

    [Fact]
    public async Task AbstractValidator_ShouldValidateCustomMust()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest { Name = "Test", Email = "test@example.com", Age = 25, Status = "invalid" };

        // Act
        var errors = (await validator.ValidateAsync(request)).ToList();

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Status"));
    }

    [Fact]
    public async Task AbstractValidator_ShouldPassValidRequest()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest
        {
            Name = "Murat Doe",
            Email = "murat@example.com",
            Age = 30,
            Status = "active"
        };

        // Act
        var errors = (await validator.ValidateAsync(request)).ToList();

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task AbstractValidator_IsValidAsync_ShouldReturnTrue_WhenValid()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest
        {
            Name = "Murat",
            Email = "murat@example.com",
            Age = 25,
            Status = "active"
        };

        // Act
        var isValid = await validator.IsValidAsync(request);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public async Task AbstractValidator_IsValidAsync_ShouldReturnFalse_WhenInvalid()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var request = new TestRequest { Name = null };

        // Act
        var isValid = await validator.IsValidAsync(request);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public async Task ValidationRuleBuilder_ShouldSupportMultipleRules()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name)
            .NotNull()
            .NotEmpty()
            .MinLength(3)
            .MaxLength(20);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "ab" };

        // Act
        var allErrors = new System.Collections.Generic.List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("3 characters"));
    }

    [Fact]
    public async Task ValidationRuleBuilder_ShouldSupportCustomRules()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.Custom((request, ct) =>
        {
            var errors = new System.Collections.Generic.List<string>();
            if (request.Name?.Length < 5)
            {
                errors.Add("Name must be at least 5 characters long");
            }
            if (request.Age < 21)
            {
                errors.Add("Must be at least 21 years old");
            }
            return new ValueTask<System.Collections.Generic.IEnumerable<string>>(errors);
        });

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Bob", Age = 19 };

        // Act
        var allErrors = new System.Collections.Generic.List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Equal(2, allErrors.Count);
        Assert.Contains("Name must be at least 5 characters long", allErrors);
        Assert.Contains("Must be at least 21 years old", allErrors);
    }

    [Fact]
    public async Task ValidationRuleBuilder_ShouldSupportRegisteredCustomRules()
    {
        // Arrange
        var registry = new CustomValidationRuleRegistry();
        registry.RegisterRule("ageCheck", (request, ct) =>
        {
            var errors = new System.Collections.Generic.List<string>();
            if (request is TestRequest testRequest && testRequest.Age < 18)
            {
                errors.Add("Must be at least 18 years old");
            }
            return new ValueTask<System.Collections.Generic.IEnumerable<string>>(errors);
        });

        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.Custom("ageCheck", registry);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Alice", Age = 16 };

        // Act
        var allErrors = new System.Collections.Generic.List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Single(allErrors);
        Assert.Equal("Must be at least 18 years old", allErrors.First());
    }

    [Fact]
    public void ValidationRuleBuilder_Custom_With_Invalid_RuleName_Should_Throw_ArgumentException()
    {
        // Arrange
        var registry = new CustomValidationRuleRegistry();
        var builder = new ValidationRuleBuilder<TestRequest>();

        // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                builder.Custom("nonexistentRule", registry));
    }

    [Fact]
    public async Task ValidationRuleBuilder_ShouldSupportCustomRuleInstances()
    {
        // Arrange
        var customRule = new TestCustomValidationRule();
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.Custom(customRule);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "", Age = 16 };

        // Act
        var allErrors = new System.Collections.Generic.List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Equal(2, allErrors.Count);
        Assert.Contains("Name is required", allErrors);
        Assert.Contains("Must be at least 18 years old", allErrors);
    }

    // Test classes
    public class TestRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Age { get; set; }
        public string? Status { get; set; }
    }

    public class TestRequestValidator : AbstractValidator<TestRequest>
    {
        protected override void ConfigureRules()
        {
            var builder = RuleBuilder();

            builder.RuleFor(x => x.Name)
                .NotNull()
                .NotEmpty()
                .MinLength(3)
                .MaxLength(20);

            builder.RuleFor(x => x.Email)
                .NotNull()
                .NotEmpty()
                .EmailAddress();

            builder.RuleFor(x => x.Age)
                .GreaterThan(18)
                .LessThan(120);

            builder.RuleFor(x => x.Status)
                .Must(status => status == "active" || status == "inactive",
                    "Status must be either 'active' or 'inactive'.");

            AddRules(builder);
        }
    }

    private class TestCustomValidationRule : CustomValidationRuleBase<TestRequest>
    {
        protected override ValueTask ValidateCoreAsync(TestRequest request, System.Collections.Generic.List<string> errors, System.Threading.CancellationToken cancellationToken)
        {
            AddErrorIf(string.IsNullOrEmpty(request.Name), "Name is required", errors);
            AddErrorIf(request.Age < 18, "Must be at least 18 years old", errors);
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ValidationRuleBuilder_ShouldSupportBusinessValidation()
    {
        // Arrange
        var businessRulesEngine = new DefaultBusinessRulesEngine(new TestLogger<DefaultBusinessRulesEngine>());
        var builder = new ValidationRuleBuilder<BusinessValidationRequest>();
        builder.Business(businessRulesEngine);

        var validator = new DefaultValidator<BusinessValidationRequest>(builder.Build());
        var request = new BusinessValidationRequest
        {
            Amount = 1500m,
            PaymentMethod = "credit_card",
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "retail",
            UserTransactionCount = 5
        };

        // Act
        var errors = await validator.ValidateAsync(request);

        // Assert - Should pass business validation
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidationRuleBuilder_BusinessValidation_ShouldFail_WhenBusinessRulesFail()
    {
        // Arrange
        var businessRulesEngine = new DefaultBusinessRulesEngine(new TestLogger<DefaultBusinessRulesEngine>());
        var builder = new ValidationRuleBuilder<BusinessValidationRequest>();
        builder.Business(businessRulesEngine);

        var validator = new DefaultValidator<BusinessValidationRequest>(builder.Build());
        var request = new BusinessValidationRequest
        {
            Amount = 150000m, // Too high
            PaymentMethod = "credit_card",
            StartDate = DateTime.UtcNow.AddDays(-35), // Too old
            EndDate = DateTime.UtcNow.AddDays(30),
            IsRecurring = false,
            UserType = UserType.Regular,
            CountryCode = "US",
            BusinessCategory = "retail",
            UserTransactionCount = 5
        };

        // Act
        var errors = await validator.ValidateAsync(request);

        // Assert - Should have business validation errors
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Transaction amount exceeds maximum limit"));
        Assert.Contains(errors, e => e.Contains("start date cannot be more than"));
    }
}

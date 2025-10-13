using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation;
using Relay.Core.Validation.Builder;
using Xunit;

namespace Relay.Core.Tests.Validation;

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
}

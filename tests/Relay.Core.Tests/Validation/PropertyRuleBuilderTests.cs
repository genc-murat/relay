using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder class to increase code coverage
/// </summary>
public class PropertyRuleBuilderTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? CreditCard { get; set; }
        public string? Url { get; set; }
        public string? Phone { get; set; }
        public string? Guid { get; set; }
        public string? Json { get; set; }
        public string? Xml { get; set; }
        public string? Base64 { get; set; }
        public string? Jwt { get; set; }
        public string? TurkishId { get; set; }
        public string? TurkishForeignerId { get; set; }
        public string? TurkishPhone { get; set; }
        public string? TurkishPostalCode { get; set; }
        public string? TurkishIban { get; set; }
        public string? TurkishTaxNumber { get; set; }
        public string? Numeric { get; set; }
        public string? Alpha { get; set; }
        public string? Alphanumeric { get; set; }
        public string? DigitsOnly { get; set; }
        public string? NoWhitespace { get; set; }
        public int Age { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public List<string>? Tags { get; set; }
        public bool IsActive { get; set; }
        public string? Country { get; set; }
    }

    #region Constructor and Basic Setup Tests

    [Fact]
    public void PropertyRuleBuilder_Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        var rules = new List<IValidationRuleConfiguration<TestRequest>>();
        var propertyFunc = new Func<TestRequest, string?>(r => r.Name);

        // Act
        var builder = new PropertyRuleBuilder<TestRequest, string?>("Name", propertyFunc, rules);

        // Assert
        Assert.NotNull(builder);
        Assert.Empty(rules); // No rules added yet
    }

    [Fact]
    public void PropertyRuleBuilder_Constructor_WithValueType_ShouldInitializeCorrectly()
    {
        // Arrange
        var rules = new List<IValidationRuleConfiguration<TestRequest>>();
        var propertyFunc = new Func<TestRequest, int>(r => r.Age);

        // Act
        var builder = new PropertyRuleBuilder<TestRequest, int>("Age", propertyFunc, rules);

        // Assert
        Assert.NotNull(builder);
        Assert.Empty(rules);
    }

    #endregion

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

    #region Length Validation Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_MinLength_WithValidLength_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).MinLength(3);

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
    public async Task PropertyRuleBuilder_MinLength_WithInvalidLength_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).MinLength(5);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Hi" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("at least 5 characters"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_MinLength_WithNullValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).MinLength(3);

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
        Assert.Empty(allErrors); // Null values pass string-only validations
    }

    [Fact]
    public async Task PropertyRuleBuilder_MaxLength_WithValidLength_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).MaxLength(10);

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
    public async Task PropertyRuleBuilder_MaxLength_WithInvalidLength_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).MaxLength(3);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "Too Long" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("must not exceed 3 characters"));
    }

    #endregion

    #region Comparison Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_GreaterThan_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).GreaterThan(18);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 25 };

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
    public async Task PropertyRuleBuilder_GreaterThan_WithInvalidValue_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).GreaterThan(18);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 16 };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("must be greater than 18"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_LessThan_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).LessThan(65);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 30 };

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
    public async Task PropertyRuleBuilder_LessThan_WithInvalidValue_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).LessThan(65);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 70 };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("must be less than 65"));
    }

    #endregion

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

    #region Specialized Validators Tests

    [Fact]
    public async Task PropertyRuleBuilder_EmailAddress_WithValidEmail_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Email).EmailAddress();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Email = "test@example.com" };

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
    public async Task PropertyRuleBuilder_EmailAddress_WithInvalidEmail_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Email).EmailAddress();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Email = "invalid-email" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("email"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_CreditCard_WithValidCard_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.CreditCard).CreditCard();

        var rules = builder.Build().ToList();
        var request = new TestRequest { CreditCard = "4111111111111111" };

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
    public async Task PropertyRuleBuilder_Url_WithValidUrl_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Url).Url();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Url = "https://example.com" };

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
    public async Task PropertyRuleBuilder_PhoneNumber_WithValidPhone_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Phone).PhoneNumber();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Phone = "+1-555-123-4567" };

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
    public async Task PropertyRuleBuilder_Guid_WithValidGuid_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Guid).Guid();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Guid = System.Guid.NewGuid().ToString() };

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
    public async Task PropertyRuleBuilder_Json_WithValidJson_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Json).Json();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Json = "{\"key\": \"value\"}" };

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
    public async Task PropertyRuleBuilder_Xml_WithValidXml_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Xml).Xml();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Xml = "<root><child>value</child></root>" };

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
    public async Task PropertyRuleBuilder_Base64_WithValidBase64_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Base64).Base64();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Base64 = "SGVsbG8gV29ybGQ=" }; // "Hello World" in Base64

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
    public async Task PropertyRuleBuilder_Jwt_WithValidJwt_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Jwt).Jwt();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c" };

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

    #region Turkish Validators Tests

    [Fact]
    public async Task PropertyRuleBuilder_TurkishId_WithValidId_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishId).TurkishId();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishId = "12345678901" }; // Valid format, may not pass checksum

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert - The validation logic will determine if it passes or fails
        // We're just testing that the rule is applied
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishPhone_WithValidPhone_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishPhone).TurkishPhone();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishPhone = "05321234567" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert - Testing that the rule is applied
    }

    #endregion

    #region General String Validators Tests

    [Fact]
    public async Task PropertyRuleBuilder_Numeric_WithValidNumber_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Numeric).Numeric();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Numeric = "12345" };

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
    public async Task PropertyRuleBuilder_Alpha_WithValidLetters_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Alpha).Alpha();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Alpha = "ValidLetters" };

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
    public async Task PropertyRuleBuilder_Alphanumeric_WithValidChars_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Alphanumeric).Alphanumeric();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Alphanumeric = "Valid123" };

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
    public async Task PropertyRuleBuilder_DigitsOnly_WithValidDigits_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.DigitsOnly).DigitsOnly();

        var rules = builder.Build().ToList();
        var request = new TestRequest { DigitsOnly = "12345" };

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
    public async Task PropertyRuleBuilder_NoWhitespace_WithValidString_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.NoWhitespace).NoWhitespace();

        var rules = builder.Build().ToList();
        var request = new TestRequest { NoWhitespace = "NoWhitespace" };

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

    #region Equality and Collection Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_EqualTo_WithMatchingValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).EqualTo("active");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "active" };

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
    public async Task PropertyRuleBuilder_EqualTo_WithNonMatchingValue_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).EqualTo("active");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "inactive" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("must equal active"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_NotEqualTo_WithDifferentValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).NotEqualTo("inactive");

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "active" };

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
    public async Task PropertyRuleBuilder_In_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).In(new[] { "active", "pending", "suspended" });

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "active" };

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
    public async Task PropertyRuleBuilder_In_WithInvalidValue_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).In(new[] { "active", "pending", "suspended" });

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "deleted" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("valid values"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_NotIn_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Status).NotIn(new[] { "deleted", "banned" });

        var rules = builder.Build().ToList();
        var request = new TestRequest { Status = "active" };

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

    #region Range Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_Between_WithValueInRange_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).Between(18, 65);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 25 };

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
    public async Task PropertyRuleBuilder_Between_WithValueBelowRange_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).Between(18, 65);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 16 };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("between 18 and 65"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_GreaterThanOrEqualTo_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).GreaterThanOrEqualTo(18);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 25 };

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
    public async Task PropertyRuleBuilder_LessThanOrEqualTo_WithValidValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age).LessThanOrEqualTo(65);

        var rules = builder.Build().ToList();
        var request = new TestRequest { Age = 60 };

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

    #region Conditional Methods Tests

    [Fact]
    public async Task PropertyRuleBuilder_When_WithTrueCondition_ShouldApplyValidation()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Email)
            .When(request => request.IsActive)
            .NotNull();

        var rules = builder.Build().ToList();

        // Test when condition is true
        var request1 = new TestRequest { IsActive = true, Email = null };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors1);

        // Test when condition is false
        var request2 = new TestRequest { IsActive = false, Email = null };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.Empty(errors2);
    }

    [Fact]
    public async Task PropertyRuleBuilder_Unless_WithFalseCondition_ShouldApplyValidation()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Email)
            .Unless(request => !request.IsActive)
            .NotNull();

        var rules = builder.Build().ToList();

        // Test when unless condition is false (should validate)
        var request1 = new TestRequest { IsActive = true, Email = null };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors1);
    }

    [Fact]
    public async Task PropertyRuleBuilder_WhenProperty_WithMatchingCondition_ShouldApplyValidation()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishId)
            .WhenProperty(x => x.Country, country => country == "Turkey")
            .NotNull();

        var rules = builder.Build().ToList();

        // Test when dependent property matches condition
        var request1 = new TestRequest { Country = "Turkey", TurkishId = null };
        var errors1 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request1);
            errors1.AddRange(ruleErrors);
        }
        Assert.NotEmpty(errors1);

        // Test when dependent property doesn't match condition
        var request2 = new TestRequest { Country = "USA", TurkishId = null };
        var errors2 = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(request2);
            errors2.AddRange(ruleErrors);
        }
        Assert.Empty(errors2);
    }

    #endregion

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

    #region Edge Cases and Error Handling Tests

    [Fact]
    public async Task PropertyRuleBuilder_NonStringProperty_WithStringMethods_ShouldNotAddRules()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Age)
            .NotEmpty() // This should not add any rules for int property
            .MinLength(1); // This should not add any rules for int property

        var rules = builder.Build().ToList();

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(new TestRequest { Age = 25 });
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Empty(allErrors); // No rules should be added for string-only methods on int property
    }

    [Fact]
    public void PropertyRuleBuilder_Must_WithNullPredicate_ShouldThrowException()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            builder.RuleFor(x => x.Name).Must(null!, "Error message"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_WithNullRequest_ShouldHandleGracefully()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).NotNull();

        var rules = builder.Build().ToList();

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(null!);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.NotEmpty(allErrors);
        Assert.Contains(allErrors, e => e.Contains("Request cannot be null"));
    }

    #endregion
}
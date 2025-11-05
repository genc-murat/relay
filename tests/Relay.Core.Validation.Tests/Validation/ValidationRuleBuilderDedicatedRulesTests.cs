using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for ValidationRuleBuilder dedicated validation rules
/// </summary>
public class ValidationRuleBuilderDedicatedRulesTests
{
    // Test request classes
    public class TestRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? CreditCard { get; set; }
        public string? Url { get; set; }
        public string? Phone { get; set; }
    }

    #region Dedicated Validation Rules Tests

    [Fact]
    public async Task ValidationRuleBuilder_ShouldUseDedicatedEmailValidationRule()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Email).EmailAddress();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Email = "invalid..email@test.com" }; // consecutive dots should fail

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
    public async Task ValidationRuleBuilder_ShouldUseDedicatedCreditCardValidationRule()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.CreditCard).CreditCard();

        var rules = builder.Build().ToList();
        var request = new TestRequest { CreditCard = "4111111111111111" }; // Valid credit card

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
    public async Task ValidationRuleBuilder_ShouldUseDedicatedUrlValidationRule()
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
    public async Task ValidationRuleBuilder_ShouldUseDedicatedPhoneNumberValidationRule()
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
    public async Task ValidationRuleBuilder_ShouldUseDedicatedGuidValidationRule()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).Guid(); // Using Name field for GUID test

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = Guid.NewGuid().ToString() };

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
    public async Task ValidationRuleBuilder_ShouldUseDedicatedJsonValidationRule()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).Json();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "{\"key\": \"value\"}" };

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
    public async Task ValidationRuleBuilder_ShouldUseDedicatedXmlValidationRule()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).Xml();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "<root><child>value</child></root>" };

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
    public async Task ValidationRuleBuilder_ShouldUseDedicatedBase64ValidationRule()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).Base64();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "SGVsbG8gV29ybGQ=" }; // "Hello World" in Base64

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
    public async Task ValidationRuleBuilder_ShouldUseDedicatedJwtValidationRule()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.Name).Jwt();

        var rules = builder.Build().ToList();
        var request = new TestRequest { Name = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c" };

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
}
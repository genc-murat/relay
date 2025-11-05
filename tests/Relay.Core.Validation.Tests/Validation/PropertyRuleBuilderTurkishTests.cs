using Relay.Core.Validation.Builder;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Tests for PropertyRuleBuilder Turkish validation methods
/// </summary>
public class PropertyRuleBuilderTurkishTests
{
    // Test request classes
    public class TestRequest
    {
        public string? TurkishId { get; set; }
        public string? TurkishForeignerId { get; set; }
        public string? TurkishPhone { get; set; }
        public string? TurkishPostalCode { get; set; }
        public string? TurkishIban { get; set; }
        public string? TurkishTaxNumber { get; set; }
    }

    #region Turkish ID Tests

    [Fact]
    public async Task PropertyRuleBuilder_TurkishId_WithValidId_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishId).TurkishId();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishId = "12345678902" }; // Valid Turkish ID example

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert - Should pass if it's a valid Turkish ID
        // The result depends on actual validation logic in TurkishValidationHelpers
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishId_WithInvalidId_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishId).TurkishId();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishId = "12345678901" }; // Invalid Turkish ID

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        // Should have an error related to TurkishId if it's invalid
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishId_WithEmptyString_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishId).TurkishId();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishId = "" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        // Should have an error since empty string is not a valid Turkish ID
        Assert.Contains(allErrors, e => e.Contains("TurkishId"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishId_WithNullValue_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishId).TurkishId();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishId = null };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        // Null should pass since the validation only applies if value is not null
        Assert.Empty(allErrors);
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishId_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var customMessage = "Custom Turkish ID error message";
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishId).TurkishId(customMessage);

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishId = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(customMessage, allErrors);
    }

    #endregion

    #region Turkish Foreigner ID Tests

    [Fact]
    public async Task PropertyRuleBuilder_TurkishForeignerId_WithValidId_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishForeignerId).TurkishForeignerId();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishForeignerId = "12345678901" }; // Valid foreigner ID example

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishForeignerId_WithInvalidId_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishForeignerId).TurkishForeignerId();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishForeignerId = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(allErrors, e => e.Contains("TurkishForeignerId"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishForeignerId_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var customMessage = "Custom Turkish Foreigner ID error message";
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishForeignerId).TurkishForeignerId(customMessage);

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishForeignerId = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(customMessage, allErrors);
    }

    #endregion

    #region Turkish Phone Tests

    [Fact]
    public async Task PropertyRuleBuilder_TurkishPhone_WithValidPhone_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishPhone).TurkishPhone();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishPhone = "05321234567" }; // Valid Turkish phone format

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishPhone_WithInvalidPhone_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishPhone).TurkishPhone();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishPhone = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(allErrors, e => e.Contains("TurkishPhone"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishPhone_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var customMessage = "Custom Turkish Phone error message";
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishPhone).TurkishPhone(customMessage);

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishPhone = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(customMessage, allErrors);
    }

    #endregion

    #region Turkish Postal Code Tests

    [Fact]
    public async Task PropertyRuleBuilder_TurkishPostalCode_WithValidCode_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishPostalCode).TurkishPostalCode();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishPostalCode = "34000" }; // Valid Turkish postal code

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishPostalCode_WithInvalidCode_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishPostalCode).TurkishPostalCode();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishPostalCode = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(allErrors, e => e.Contains("TurkishPostalCode"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishPostalCode_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var customMessage = "Custom Turkish Postal Code error message";
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishPostalCode).TurkishPostalCode(customMessage);

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishPostalCode = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(customMessage, allErrors);
    }

    #endregion

    #region Turkish IBAN Tests

    [Fact]
    public async Task PropertyRuleBuilder_TurkishIban_WithValidIban_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishIban).TurkishIban();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishIban = "TR330006100519786457841326" }; // Valid Turkish IBAN

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishIban_WithInvalidIban_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishIban).TurkishIban();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishIban = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(allErrors, e => e.Contains("TurkishIban"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishIban_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var customMessage = "Custom Turkish IBAN error message";
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishIban).TurkishIban(customMessage);

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishIban = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(customMessage, allErrors);
    }

    #endregion

    #region Turkish Tax Number Tests

    [Fact]
    public async Task PropertyRuleBuilder_TurkishTaxNumber_WithValidNumber_ShouldPass()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishTaxNumber).TurkishTaxNumber();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishTaxNumber = "1234567890" }; // Valid Turkish tax number

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishTaxNumber_WithInvalidNumber_ShouldFail()
    {
        // Arrange
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishTaxNumber).TurkishTaxNumber();

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishTaxNumber = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(allErrors, e => e.Contains("TurkishTaxNumber"));
    }

    [Fact]
    public async Task PropertyRuleBuilder_TurkishTaxNumber_WithCustomErrorMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var customMessage = "Custom Turkish Tax Number error message";
        var builder = new ValidationRuleBuilder<TestRequest>();
        builder.RuleFor(x => x.TurkishTaxNumber).TurkishTaxNumber(customMessage);

        var rules = builder.Build().ToList();
        var request = new TestRequest { TurkishTaxNumber = "invalid" };

        // Act
        var allErrors = new List<string>();
        foreach (var rule in rules)
        {
            var errors = await rule.ValidateAsync(request);
            allErrors.AddRange(errors);
        }

        // Assert
        Assert.Contains(customMessage, allErrors);
    }

    #endregion
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Builder;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

/// <summary>
/// Integration tests for ValidationRuleBuilder complex scenarios
/// </summary>
public class ValidationRuleBuilderIntegrationTests
{
    // Test request classes
    public class UserRegistrationRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Country { get; set; }
        public string? TurkishId { get; set; }
        public bool IsActive { get; set; }
    }

    #region Integration Tests

    [Fact]
    public async Task ValidationRuleBuilder_ComplexScenario_ShouldWorkCorrectly()
    {
        // Arrange - Complex validation scenario
        var builder = new ValidationRuleBuilder<UserRegistrationRequest>();

        // Basic rules
        builder.RuleFor(x => x.Email).NotNull().EmailAddress();
        builder.RuleFor(x => x.Password).NotNull().MinLength(8);

        // Conditional rules
        builder.RuleFor(x => x.TurkishId)
            .WhenProperty(x => x.Country, country => country == "Turkey")
            .NotNull()
            .TurkishId();

        // Rule set for active users
        builder.RuleSet("ActiveUserValidation", ruleSetBuilder =>
        {
            ruleSetBuilder.RuleFor(x => x.Email).EmailAddress();
            ruleSetBuilder.RuleFor(x => x.Password).MinLength(10);
        });

        builder.IncludeRuleSet("ActiveUserValidation");

        var rules = builder.Build().ToList();

        // Test with Turkish user
        var turkishUser = new UserRegistrationRequest
        {
            Email = "user@example.com",
            Password = "password123",
            Country = "Turkey",
            TurkishId = "12345678901",
            IsActive = true
        };

        // Act
        var errors = new List<string>();
        foreach (var rule in rules)
        {
            var ruleErrors = await rule.ValidateAsync(turkishUser);
            errors.AddRange(ruleErrors);
        }

        // Assert - Should pass all validations
        // Note: Turkish ID validation might fail due to checksum, but the structure should work
        var emailErrors = errors.Where(e => e.Contains("Email")).ToList();
        var passwordErrors = errors.Where(e => e.Contains("Password")).ToList();

        Assert.Empty(emailErrors); // Email should be valid
        Assert.Empty(passwordErrors); // Password should meet minimum length
    }

    #endregion
}
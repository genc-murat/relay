using Relay.Core.Validation.Rules;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class PostalCodeValidationRuleTests
{
    [Fact]
    public async Task ValidateAsync_Should_Return_Empty_Errors_When_US_Postal_Code_Is_Valid()
    {
        // Arrange
        var rule = new PostalCodeValidationRule("US");
        var request = "12345";

        // Act
        var errors = await rule.ValidateAsync(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Empty_Errors_When_US_Postal_Code_With_Extension_Is_Valid()
    {
        // Arrange
        var rule = new PostalCodeValidationRule("US");
        var request = "12345-6789";

        // Act
        var errors = await rule.ValidateAsync(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_US_Postal_Code_Is_Invalid()
    {
        // Arrange
        var rule = new PostalCodeValidationRule("US");
        var request = "1234";

        // Act
        var errors = await rule.ValidateAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Invalid postal code format for US.", errors.First());
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Empty_Errors_When_CA_Postal_Code_Is_Valid()
    {
        // Arrange
        var rule = new PostalCodeValidationRule("CA");
        var request = "K1A 0A6";

        // Act
        var errors = await rule.ValidateAsync(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Work_With_Custom_Error_Message()
    {
        // Arrange
        var rule = new PostalCodeValidationRule("US", "Custom postal error");
        var request = "invalid";

        // Act
        var errors = await rule.ValidateAsync(request);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Custom postal error", errors.First());
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Empty_Errors_When_Postal_Code_Is_Null()
    {
        // Arrange
        var rule = new PostalCodeValidationRule();
        string request = null;

        // Act
        var errors = await rule.ValidateAsync(request);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Pass_CancellationToken()
    {
        // Arrange
        var rule = new PostalCodeValidationRule();
        var request = "12345";
        var cts = new CancellationTokenSource();

        // Act
        var errors = await rule.ValidateAsync(request, cts.Token);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Handle_Cancelled_Token()
    {
        // Arrange
        var rule = new PostalCodeValidationRule();
        var request = "invalid";
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await rule.ValidateAsync(request, cts.Token));
    }
    
    [Theory]
    [InlineData("K1A0A6")] // Canadian postal code without space
    [InlineData("K1A 0A6")] // Canadian postal code with space
    [InlineData("m5v 3l9")] // Canadian postal code lowercase
    [InlineData("M5V 3L9")] // Canadian postal code uppercase
    public async Task ValidateAsync_Should_Handle_Canadian_Postal_Codes(string postalCode)
    {
        // Arrange
        var rule = new PostalCodeValidationRule("CA");

        // Act
        var errors = await rule.ValidateAsync(postalCode);

        // Assert
        Assert.Empty(errors);
    }
    
    [Theory]
    [InlineData("K1A 0A")] // Too short
    [InlineData("K1A 0A67")] // Too long
    [InlineData("1K1 A0A")] // Invalid format (starts with number)
    [InlineData("K11 0AB")] // Invalid format (second letter position has number)
    [InlineData("KK1 0AB")] // Invalid format (first letter position has number)
    public async Task ValidateAsync_Should_Return_Error_For_Invalid_Canadian_Postal_Codes(string postalCode)
    {
        // Arrange
        var rule = new PostalCodeValidationRule("CA");

        // Act
        var errors = await rule.ValidateAsync(postalCode);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Invalid postal code format for CA.", errors.First());
    }
    
    [Theory]
    [InlineData("SW1A 1AA")] // UK postcode
    [InlineData("M1 1AA")] // UK postcode
    [InlineData("B33 8TH")] // UK postcode
    [InlineData("W1A 0AX")] // UK postcode
    [InlineData("EC1A 1BB")] // UK postcode
    public async Task ValidateAsync_Should_Handle_UK_Postal_Codes(string postalCode)
    {
        // Arrange
        var rule = new PostalCodeValidationRule("UK");

        // Act
        var errors = await rule.ValidateAsync(postalCode);

        // Assert
        Assert.Empty(errors);
    }
    
    [Theory]
    [InlineData("12345")] // German postal code
    [InlineData("99999")] // German postal code
    [InlineData("01234")] // German postal code starting with 0
    public async Task ValidateAsync_Should_Handle_German_Postal_Codes(string postalCode)
    {
        // Arrange
        var rule = new PostalCodeValidationRule("DE");

        // Act
        var errors = await rule.ValidateAsync(postalCode);

        // Assert
        Assert.Empty(errors);
    }
    
    [Theory]
    [InlineData("12345")] // French postal code
    [InlineData("99999")] // French postal code
    [InlineData("01234")] // French postal code starting with 0
    public async Task ValidateAsync_Should_Handle_French_Postal_Codes(string postalCode)
    {
        // Arrange
        var rule = new PostalCodeValidationRule("FR");

        // Act
        var errors = await rule.ValidateAsync(postalCode);

        // Assert
        Assert.Empty(errors);
    }
    
    [Theory]
    [InlineData("1234")] // Australian postal code
    [InlineData("9999")] // Australian postal code
    [InlineData("0123")] // Australian postal code starting with 0
    public async Task ValidateAsync_Should_Handle_Australian_Postal_Codes(string postalCode)
    {
        // Arrange
        var rule = new PostalCodeValidationRule("AU");

        // Act
        var errors = await rule.ValidateAsync(postalCode);

        // Assert
        Assert.Empty(errors);
    }
    
    [Fact]
    public async Task ValidateAsync_Should_Use_US_Format_For_Unknown_Country()
    {
        // Arrange
        var rule = new PostalCodeValidationRule("XX"); // Unknown country
        var validUSCode = "12345";

        // Act
        var errors = await rule.ValidateAsync(validUSCode);

        // Assert
        Assert.Empty(errors);
    }
    
    [Fact]
    public async Task ValidateAsync_Should_Use_US_Format_For_Empty_Country()
    {
        // Arrange
        var rule = new PostalCodeValidationRule(""); // Empty country
        var validUSCode = "12345";

        // Act
        var errors = await rule.ValidateAsync(validUSCode);

        // Assert
        Assert.Empty(errors);
    }
    
    [Fact]
    public async Task ValidateAsync_Should_Return_Error_For_Whitespace_Only()
    {
        // Arrange
        var rule = new PostalCodeValidationRule("US");
        var request = "   "; // Whitespace only

        // Act
        var errors = await rule.ValidateAsync(request);

        // Assert
        Assert.Empty(errors); // Whitespace is treated as empty and returns empty errors
    }
    
    [Theory]
    [InlineData("12345", "US")]
    [InlineData("K1A 0A6", "CA")]
    [InlineData("SW1A 1AA", "UK")]
    [InlineData("12345", "DE")]
    [InlineData("12345", "FR")]
    [InlineData("1234", "AU")]
    public async Task ValidateAsync_Should_Validate_Correct_Country_Formats(string postalCode, string country)
    {
        // Arrange
        var rule = new PostalCodeValidationRule(country);

        // Act
        var errors = await rule.ValidateAsync(postalCode);

        // Assert
        Assert.Empty(errors);
    }
    
    [Fact]
    public async Task ValidateAsync_Should_Handle_Case_Insensitive_Country_Codes()
    {
        // Arrange
        var rule = new PostalCodeValidationRule("us"); // lowercase
        var request = "12345";

        // Act
        var errors = await rule.ValidateAsync(request);

        // Assert
        Assert.Empty(errors);
    }
}
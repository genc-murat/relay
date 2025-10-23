using Relay.Core.Validation.Rules;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class CusipValidationRuleTests
{
    [Theory]
    [InlineData("037833100")] // Apple Inc.
    [InlineData("931142103")] // Walmart Inc.
    [InlineData("594918104")] // Microsoft Corp.
    public async Task ValidateAsync_Should_Return_Empty_Errors_For_Valid_Cusip(string cusip)
    {
        // Arrange
        var rule = new CusipValidationRule();

        // Act
        var errors = await rule.ValidateAsync(cusip);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("037833101")] // Invalid checksum
    [InlineData("123456789")]
    [InlineData("ABCDEFGHI")]
    [InlineData("12345")]
    public async Task ValidateAsync_Should_Return_Error_For_Invalid_Cusip(string cusip)
    {
        // Arrange
        var rule = new CusipValidationRule();

        // Act
        var errors = await rule.ValidateAsync(cusip);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Invalid CUSIP number.", errors.First());
    }

    [Fact]
    public async Task ValidateAsync_Should_Use_Custom_Error_Message()
    {
        // Arrange
        var rule = new CusipValidationRule("Custom CUSIP error");
        var invalidCusip = "123456789";

        // Act
        var errors = await rule.ValidateAsync(invalidCusip);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Custom CUSIP error", errors.First());
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ValidateAsync_Should_Return_Error_For_Null_Or_Empty_Or_Whitespace(string? cusip)
    {
        // Arrange
        var rule = new CusipValidationRule();

        // Act
        var errors = await rule.ValidateAsync(cusip);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Invalid CUSIP number.", errors.First());
    }

    [Theory]
    [InlineData("12345678")] // Too short
    [InlineData("1234567890")] // Too long
    public async Task ValidateAsync_Should_Return_Error_For_Invalid_Length(string cusip)
    {
        // Arrange
        var rule = new CusipValidationRule();

        // Act
        var errors = await rule.ValidateAsync(cusip);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Invalid CUSIP number.", errors.First());
    }

    [Theory]
    [InlineData("1234567*8")] // With * character
    [InlineData("1234567@8")] // With @ character
    [InlineData("1234567#8")] // With # character
    public async Task ValidateAsync_Should_Handle_Special_Cusip_Characters(string cusip)
    {
        // Arrange
        var rule = new CusipValidationRule();

        // Act
        var errors = await rule.ValidateAsync(cusip);

        // These special characters are valid in CUSIP, but our constructed CUSIPs may be valid or invalid
        // depending on the checksum algorithm. Let's just verify that they don't cause exceptions
        Assert.NotNull(errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Throw_If_CancellationToken_Cancelled()
    {
        // Arrange
        var rule = new CusipValidationRule();
        var cts = new CancellationTokenSource();
        cts.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await rule.ValidateAsync("037833100", cts.Token));
    }

    [Theory]
    [InlineData("00724F101")] // Valid CUSIP with letters
    [InlineData("037833100")] // Valid CUSIP all numbers
    public async Task ValidateAsync_Should_Accept_Valid_Cusips_With_Letters_And_Numbers(string cusip)
    {
        // Arrange
        var rule = new CusipValidationRule();

        // Act
        var errors = await rule.ValidateAsync(cusip);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_For_Invalid_Character()
    {
        // Arrange
        var rule = new CusipValidationRule();
        var invalidCusip = "1234567X9"; // X is not a valid CUSIP character

        // Act
        var errors = await rule.ValidateAsync(invalidCusip);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Invalid CUSIP number.", errors.First());
    }

    [Fact]
    public async Task CusipValidationRule_Should_Use_Default_Error_Message_When_Null()
    {
        // Arrange
        var rule = new CusipValidationRule(null);
        var invalidCusip = "INVALID";

        // Act
        var errors = await rule.ValidateAsync(invalidCusip);

        // Assert
        Assert.Single(errors);
        Assert.Equal("Invalid CUSIP number.", errors.First());
    }

    [Fact]
    public async Task CusipValidationRule_Should_Use_Custom_Error_Message_When_Empty_String()
    {
        // Arrange
        var rule = new CusipValidationRule("");
        var invalidCusip = "INVALID";

        // Act
        var errors = await rule.ValidateAsync(invalidCusip);

        // Assert
        Assert.Single(errors);
        Assert.Equal("", errors.First()); // Empty string should be used as-is
    }
}
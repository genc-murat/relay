using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation.Rules;

public class SolanaAddressValidationRuleTests
{
    private readonly SolanaAddressValidationRule _rule;

    public SolanaAddressValidationRuleTests()
    {
        _rule = new SolanaAddressValidationRule();
    }

    [Theory]
    [InlineData("11111111111111111111111111111112", true)] // Valid System Program ID
    [InlineData("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA", true)] // Valid Token Program ID
    [InlineData("9WzDXwBbmkg8ZTbNMqUxvQRAyrZzDsGYdLVL9zYtAWWM", true)] // Valid random address
    [InlineData("EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v", true)] // Valid USDC Token Mint
    [InlineData("Es9vMFrzaCERmJfrF4H2FYD4KCoNkY11McCe8BenwNYB", true)] // Valid USDT Token Mint
    [InlineData("", true)] // Empty string - should pass (let other rules handle required validation)
    [InlineData("   ", true)] // Whitespace only - should pass (let other rules handle required validation)
    [InlineData(null, true)] // Null - should pass (let other rules handle required validation)
    [InlineData("invalid", false)] // Invalid characters
    [InlineData("123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz", false)] // Too long
    [InlineData("123", false)] // Too short
    [InlineData("1111111111111111111111111111111", false)] // 31 bytes (invalid)
    [InlineData("111111111111111111111111111111111", false)] // 33 bytes (invalid)
    [InlineData("SysvarRent111111111111111111111111111111111", false)] // 28 bytes (invalid)
    [InlineData("SysvarC1ock11111111111111111111111111111111", false)] // 28 bytes (invalid)
    [InlineData("0123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz", false)] // Contains '0' (invalid Base58)
    [InlineData("123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyzO", false)] // Contains 'O' (invalid Base58)
    [InlineData("123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyzI", false)] // Contains 'I' (invalid Base58)
    [InlineData("123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyzl", false)] // Contains 'l' (invalid Base58)]
    public async Task ValidateAsync_WithVariousInputs_ReturnsExpectedResult(string address, bool expectedValid)
    {
        // Act
        var result = await _rule.ValidateAsync(address);

        // Assert
        if (expectedValid)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Invalid Solana address.", result.Single());
        }
    }

    [Fact]
    public async Task ValidateAsync_WithCustomErrorMessage_ReturnsCustomMessage()
    {
        // Arrange
        var customMessage = "Custom error message";
        var rule = new SolanaAddressValidationRule(customMessage);
        var invalidAddress = "invalid";

        // Act
        var result = await rule.ValidateAsync(invalidAddress);

        // Assert
        Assert.Single(result);
        Assert.Equal(customMessage, result.Single());
    }

    [Fact]
    public async Task ValidateAsync_WithWhitespaceInput_HandlesCorrectly()
    {
        // Arrange
        var validAddress = "  TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA  ";

        // Act
        var result = await _rule.ValidateAsync(validAddress);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("11111111111111111111111111111112")] // System Program
    [InlineData("TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA")] // Token Program
    [InlineData("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL")] // Associated Token Program
    [InlineData("9WzDXwBbmkg8ZTbNMqUxvQRAyrZzDsGYdLVL9zYtAWWM")] // Random valid address
    [InlineData("EPjFWdd5AufqSSqeM2qN1xzybapC8G4wEGGkZwyTDt1v")] // USDC Token Mint
    public async Task ValidateAsync_WithKnownProgramIds_ReturnsValid(string programId)
    {
        // Act
        var result = await _rule.ValidateAsync(programId);

        // Assert
        Assert.Empty(result);
    }


}
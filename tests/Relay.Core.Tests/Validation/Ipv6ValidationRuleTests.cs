using System.Linq;
using System.Threading.Tasks;
 using Relay.Core.Validation.Rules;
 using Xunit;

namespace Relay.Core.Tests.Validation;

public class Ipv6ValidationRuleTests
{
    private readonly Ipv6ValidationRule _rule = new();

    [Theory]
    [InlineData("2001:0db8:85a3:0000:0000:8a2e:0370:7334")] // Full address
    [InlineData("2001:db8:85a3:0:0:8a2e:370:7334")] // Compressed zeros
    [InlineData("2001:db8:85a3::8a2e:370:7334")] // Compressed notation
    [InlineData("::1")] // Loopback
    [InlineData("::")] // Unspecified
    [InlineData("::ffff:192.0.2.1")] // IPv4 mapped
    [InlineData("2001:db8::1")] // Compressed with one group
    [InlineData("::ffff:c000:0201")] // IPv4 mapped alternative
    [InlineData("2001:0db8:0000:0000:0000:0000:0000:0001")] // Another full address
    public async Task ValidateAsync_ValidIpv6Addresses_ReturnsEmptyErrors(string ipv6)
    {
        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Empty(result);
    }



    [Theory]
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    [InlineData("   ")] // Whitespace
    public async Task ValidateAsync_EmptyOrWhitespace_ReturnsEmptyErrors(string ipv6)
    {
        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("192.168.1.1", "Address is not a valid IPv6 address.")] // IPv4 address
    [InlineData("not-an-ip", "Invalid IPv6 address format.")] // Invalid format
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370", "Invalid IPv6 address format.")] // Too few groups
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370:7334:1234", "Invalid IPv6 address format.")] // Too many groups
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370:gggg", "Invalid IPv6 address format.")] // Invalid hex
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370:12345", "Invalid IPv6 address format.")] // 5 digits
    public async Task ValidateAsync_InvalidIpv6Formats_ReturnsError(string ipv6, string expectedError)
    {
        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Single(result);
        Assert.Equal(expectedError, result.First());
    }

    [Theory]
    [InlineData(":::")] // Triple colon
    [InlineData("::::")] // Quadruple colon
    [InlineData("2001:::db8")] // Multiple compression
    public async Task ValidateAsync_InvalidCompression_ReturnsError(string ipv6)
    {
        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid IPv6 address format.", result.First());
    }

    [Theory]
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370:7334:1234")] // 9 groups
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370:7334:1234:5678")] // 10 groups
    public async Task ValidateAsync_TooManyColons_ReturnsError(string ipv6)
    {
        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid IPv6 address format.", result.First());
    }

    [Theory]
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370:7334")] // 8 colons (9 groups - 1)
    [InlineData("::1")] // 2 colons minimum for compressed
    public async Task ValidateAsync_ValidColonCounts_ReturnsEmptyErrors(string ipv6)
    {
        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("::ffff:192.168.1.1")] // Valid IPv4 mapped
    [InlineData("::ffff:c0a8:0101")] // Valid IPv4 mapped hex
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370:192.168.1.1")] // Invalid - IPv4 at end without mapping
    public async Task ValidateAsync_Ipv4MappedIpv6_ReturnsAppropriateResults(string ipv6)
    {
        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        if (ipv6.StartsWith("::ffff:") && (ipv6.Contains(".") || ipv6.EndsWith(":192.168.1.1")))
        {
            // Valid IPv4 mapped should pass
            Assert.Empty(result);
        }
        else if (ipv6.Contains("192.168.1.1") && !ipv6.StartsWith("::ffff:"))
        {
            // Invalid mapping should fail
            Assert.Single(result);
            Assert.Contains("Invalid IPv6 address format", result.First());
        }
        else
        {
            Assert.Empty(result);
        }
    }

    [Theory]
    [InlineData("2001:DB8::1")] // Uppercase
    [InlineData("2001:db8::1")] // Lowercase
    [InlineData("2001:Db8::1")] // Mixed case
    public async Task ValidateAsync_CaseInsensitiveHex_ReturnsEmptyErrors(string ipv6)
    {
        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("::1")] // Compressed loopback
    [InlineData("::ffff:0:0")] // Compressed IPv4 mapping
    [InlineData("2001:db8::")] // Compressed at end
    [InlineData("::2001:db8")] // Compressed at start
    public async Task ValidateAsync_VariousCompressionPatterns_ReturnsEmptyErrors(string ipv6)
    {
        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370:733g")] // Invalid character 'g'
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370:733-")] // Invalid character '-'
    [InlineData("2001:db8:85a3:8421:1010:8a2e:370:733_")] // Invalid character '_'
    public async Task ValidateAsync_InvalidCharacters_ReturnsError(string ipv6)
    {
        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid IPv6 address format.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_LocalhostIpv6_ReturnsEmptyErrors()
    {
        // Arrange
        var ipv6 = "::1";

        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_UnspecifiedIpv6_ReturnsEmptyErrors()
    {
        // Arrange
        var ipv6 = "::";

        // Act
        var result = await _rule.ValidateAsync(ipv6);

        // Assert
        Assert.Empty(result);
    }
}
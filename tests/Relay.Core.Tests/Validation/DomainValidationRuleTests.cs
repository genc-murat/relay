using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class DomainValidationRuleTests
{
    private readonly DomainValidationRule _rule = new();

    [Theory]
    [InlineData("example.com")]
    [InlineData("sub.example.com")]
    [InlineData("test-domain.com")]
    [InlineData("a.b.c.example.com")]
    [InlineData("EXAMPLE.COM")]
    [InlineData("Example.Com")]
    [InlineData("xn--fsq.xn--0zwm56d")] // IDN example
    [InlineData("123.com")]
    [InlineData("a.co")]
    [InlineData("very-long-domain-name-with-many-characters.co")]
    public async Task ValidateAsync_ValidDomains_ReturnsEmptyErrors(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("example..com")] // Consecutive dots
    [InlineData("example.com.")] // Trailing dot
    [InlineData(".example.com")] // Leading dot
    [InlineData("exam ple.com")] // Space
    [InlineData("example!.com")] // Invalid character
    [InlineData("-example.com")] // Starts with hyphen
    [InlineData("example-.com")] // Ends with hyphen
    [InlineData("example.-com")] // Hyphen before dot
    [InlineData("exam-ple.-com")] // Multiple issues
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidDomainFormats_ReturnsError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        if (string.IsNullOrWhiteSpace(domain))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle().Which.Should().Contain("Invalid domain name format");
        }
    }

    [Theory]
    [InlineData("example.invalid")] // Reserved TLD
    [InlineData("test.example")] // Reserved TLD
    [InlineData("sub.localhost")] // Reserved TLD
    public async Task ValidateAsync_ReservedTlds_ReturnsError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle("Domain uses reserved TLD.");
    }

    [Fact]
    public async Task ValidateAsync_DomainTooLong_ReturnsError()
    {
        // Arrange
        var longDomain = new string('a', 250) + ".com"; // 254+ characters

        // Act
        var result = await _rule.ValidateAsync(longDomain);

        // Assert
        result.Should().ContainSingle("Domain name too long (maximum 253 characters).");
    }

    [Fact]
    public async Task ValidateAsync_LabelTooLong_ReturnsError()
    {
        // Arrange
        var longLabel = new string('a', 64); // 64 characters
        var domain = $"{longLabel}.com";

        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle("Domain label too long (maximum 63 characters).");
    }

    [Theory]
    [InlineData("example")] // Single label
    [InlineData("com")] // TLD only
    public async Task ValidateAsync_NoSubdomain_ReturnsError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle("Domain name must have at least one subdomain.");
    }

    [Theory]
    [InlineData("example..com")] // Consecutive dots
    [InlineData("example...com")] // Multiple consecutive dots
    public async Task ValidateAsync_ConsecutiveDots_ReturnsError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle("Domain name contains consecutive dots.");
    }

    [Theory]
    [InlineData("exam-ple.com")] // Valid hyphen usage
    [InlineData("sub.exam-ple.com")] // Valid in subdomain
    public async Task ValidateAsync_ValidHyphenUsage_ReturnsEmptyErrors(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().BeEmpty();
    }
}
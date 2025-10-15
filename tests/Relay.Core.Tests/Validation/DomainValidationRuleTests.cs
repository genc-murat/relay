using System;
using System.Threading;
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

    [Theory]
    [InlineData("example.com")] // Standard domain
    [InlineData("EXAMPLE.COM")] // Uppercase
    [InlineData("Example.Com")] // Mixed case
    [InlineData("  example.com  ")] // Leading/trailing spaces
    [InlineData("\texample.com\t")] // Tabs
    [InlineData("\nexample.com\n")] // Newlines
    public async Task ValidateAsync_CaseAndWhitespaceHandling_ReturnsEmptyErrors(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("a.com")] // Single char domain
    [InlineData("1.com")] // Number domain
    [InlineData("a-b.com")] // Hyphen in domain
    [InlineData("sub.a-b.com")] // Hyphen in subdomain
    [InlineData("a1b.com")] // Alphanumeric
    [InlineData("test.123")] // Numbers in subdomain
    public async Task ValidateAsync_ValidSimpleDomains_ReturnsEmptyErrors(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("example.test")] // Reserved TLD
    [InlineData("sub.example")] // Reserved TLD
    [InlineData("domain.localhost")] // Reserved TLD
    [InlineData("site.invalid")] // Reserved TLD
    public async Task ValidateAsync_AllReservedTlds_ReturnsError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle("Domain uses reserved TLD.");
    }

    [Fact]
    public async Task ValidateAsync_ExactMaxLengthDomain_ReturnsEmptyErrors()
    {
        // Arrange - exactly 253 characters
        var domain = new string('a', 245) + ".com"; // 245 + 4 = 249 chars, wait let me calculate properly
        // Domain format: [63 chars].[63 chars].[63 chars].[63 chars] but max total is 253
        // Let's create a domain that's exactly 253 chars
        var part1 = new string('a', 63);
        var part2 = new string('b', 63);
        var part3 = new string('c', 63);
        var part4 = new string('d', 61); // 63+63+63+61 + 3 dots = 253
        var exactDomain = $"{part1}.{part2}.{part3}.{part4}";

        // Act
        var result = await _rule.ValidateAsync(exactDomain);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_OverMaxLengthDomain_ReturnsError()
    {
        // Arrange - 254 characters
        var domain = new string('a', 246) + ".com"; // Should be over 253

        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle("Domain name too long (maximum 253 characters).");
    }

    [Theory]
    [InlineData("a")] // Single char, no TLD
    [InlineData("1")] // Number only
    [InlineData("com")] // TLD only
    [InlineData("org")] // Another TLD only
    [InlineData("net")] // Another TLD only
    public async Task ValidateAsync_SingleLabelDomains_ReturnsError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle("Domain name must have at least one subdomain.");
    }

    [Theory]
    [InlineData("example..com")] // Double dots
    [InlineData("example...com")] // Triple dots
    [InlineData("example....com")] // Quadruple dots
    [InlineData("..example.com")] // Leading double dots
    [InlineData("example.com..")] // Trailing double dots
    public async Task ValidateAsync_MultipleConsecutiveDots_ReturnsError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle("Domain name contains consecutive dots.");
    }

    [Theory]
    [InlineData("exam!ple.com")] // Exclamation
    [InlineData("exam@ple.com")] // At symbol
    [InlineData("exam#ple.com")] // Hash
    [InlineData("exam$ple.com")] // Dollar
    [InlineData("exam%ple.com")] // Percent
    [InlineData("exam^ple.com")] // Caret
    [InlineData("exam&ple.com")] // Ampersand
    [InlineData("exam*ple.com")] // Asterisk
    [InlineData("exam(ple.com")] // Parentheses
    [InlineData("exam)ple.com")] // Parentheses
    [InlineData("exam+ple.com")] // Plus
    [InlineData("exam=ple.com")] // Equals
    [InlineData("exam{ple.com")] // Braces
    [InlineData("exam}ple.com")] // Braces
    [InlineData("exam[ple.com")] // Brackets
    [InlineData("exam]ple.com")] // Brackets
    [InlineData("exam|ple.com")] // Pipe
    [InlineData("exam\\ple.com")] // Backslash
    [InlineData("exam/ple.com")] // Forward slash
    [InlineData("exam?ple.com")] // Question mark
    [InlineData("exam<ple.com")] // Less than
    [InlineData("exam>ple.com")] // Greater than
    [InlineData("exam\"ple.com")] // Quote
    [InlineData("exam'ple.com")] // Apostrophe
    [InlineData("exam:ple.com")] // Colon
    [InlineData("exam;ple.com")] // Semicolon
    [InlineData("exam,ple.com")] // Comma
    public async Task ValidateAsync_InvalidCharacters_ReturnsError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle().Which.Should().Contain("Invalid domain name format");
    }

    [Theory]
    [InlineData("-example.com")] // Starts with hyphen
    [InlineData("example-.com")] // Ends with hyphen
    [InlineData("-sub.example.com")] // Subdomain starts with hyphen
    [InlineData("sub-.example.com")] // Subdomain ends with hyphen
    [InlineData("example.-com")] // TLD starts with hyphen
    [InlineData("example.com-")] // TLD ends with hyphen
    public async Task ValidateAsync_HyphenBoundaries_ReturnsError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle("Domain labels cannot start or end with hyphens.");
    }

    [Fact]
    public async Task ValidateAsync_ExactMaxLabelLength_ReturnsEmptyErrors()
    {
        // Arrange - exactly 63 chars per label
        var label = new string('a', 63);
        var domain = $"{label}.com";

        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_OverMaxLabelLength_ReturnsError()
    {
        // Arrange - 64 chars in label
        var label = new string('a', 64);
        var domain = $"{label}.com";

        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().ContainSingle("Domain label too long (maximum 63 characters).");
    }

    [Theory]
    [InlineData("sub1.sub2.sub3.sub4.sub5.example.com")] // Deep subdomain
    [InlineData("a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.u.v.w.x.y.z.com")] // Many subdomains
    public async Task ValidateAsync_ComplexDomainStructures_ReturnsEmptyErrors(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("xn--fsq.xn--0zwm56d")] // IDN example
    [InlineData("xn--nxasmq6b.com")] // Another IDN with TLD
    [InlineData("xn--fiqs8s.cn")] // Chinese IDN with TLD
    public async Task ValidateAsync_IDNDomains_ReturnsEmptyErrors(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("   ")] // Only whitespace
    [InlineData("\t\t")] // Only tabs
    [InlineData("\n\n")] // Only newlines
    public async Task ValidateAsync_OnlyWhitespace_ReturnsEmptyErrors(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_CancellationToken_ThrowsWhenCancelled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _rule.ValidateAsync("example.com", cts.Token));
    }

    [Theory]
    [InlineData("example.com.")] // Trailing dot
    [InlineData(".example.com")] // Leading dot
    [InlineData("example..com")] // Consecutive dots
    [InlineData("example.com..")] // Trailing consecutive dots
    [InlineData("..example.com")] // Leading consecutive dots
    public async Task ValidateAsync_DotBoundaryIssues_ReturnsError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        if (domain.Contains(".."))
        {
            result.Should().ContainSingle("Domain name contains consecutive dots.");
        }
        else
        {
            result.Should().ContainSingle().Which.Should().Contain("Invalid domain name format");
        }
    }

    [Theory]
    [InlineData("a.b.com")] // Minimal valid
    [InlineData("1.2.com")] // Numbers
    [InlineData("a-b.c-d.com")] // Hyphens
    [InlineData("sub.domain.co.uk")] // Multiple levels
    public async Task ValidateAsync_BoundaryValidDomains_ReturnsEmptyErrors(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        result.Should().BeEmpty();
    }
}
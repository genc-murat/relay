using Relay.Core.Validation.Rules;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        Assert.Empty(result);
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
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Contains("Invalid domain name format", result.First());
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
        Assert.Single(result);
        Assert.Equal("Domain uses reserved TLD.", result.First());
    }

    [Theory]
    [InlineData("exam-ple.com")] // Valid hyphen usage
    [InlineData("sub.exam-ple.com")] // Valid in subdomain
    public async Task ValidateAsync_ValidHyphenUsage_ReturnsEmptyErrors(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        Assert.Empty(result);
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
        Assert.Empty(result);
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
        Assert.Empty(result);
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
        Assert.Single(result);
        Assert.Equal("Domain uses reserved TLD.", result.First());
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
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_OverMaxLengthDomain_ReturnsError()
    {
        // Arrange - Create a valid domain that is longer than 253 characters
        // Create a domain with 4 labels of 63 chars each: 4 * 63 = 252 chars, + 3 dots = 255 chars total
        var label63 = new string('a', 63);
        var longDomainTotal = $"{label63}.{label63}.{label63}.{label63}.com"; // 4 * 63 + 4 dots + 3 = 259 chars

        // Act
        var result = await _rule.ValidateAsync(longDomainTotal);

        // Assert
        Assert.Single(result);
        Assert.Equal("Domain name too long (maximum 253 characters).", result.First());
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
        Assert.Single(result);
        Assert.Equal("Domain name must have at least one subdomain.", result.First());
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
        Assert.Single(result);
        Assert.Equal("Invalid domain name format.", result.First());
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
        Assert.Single(result);
        Assert.Contains("Invalid domain name format", result.First());
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
        Assert.Single(result);
        Assert.Equal("Invalid domain name format.", result.First());
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
        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateAsync_OverMaxLabelLength_ReturnsError()
    {
        // Arrange - 64 chars in label (max is 63)
        // But to pass the regex check, we need to make sure the rest of the domain is valid
        // The regex requires labels to start and end with alphanumeric chars
        // Label of 64 characters will fail the regex check first, returning "Invalid domain name format."
        // So I need to create a specific test that bypasses the regex but triggers the length check

        // Actually, looking at the flow again: the regex checks the full domain pattern first,
        // and a 64-character label would fail the regex because it's designed to allow max 61 chars 
        // between the start/end alphanumerics (so max 63 chars total per label: [a-zA-Z0-9][a-zA-Z0-9-]{0,61}[a-zA-Z0-9])

        // So the regex is: [a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?
        // This means a label can be: 1 char + up to 61 chars + 1 char = up to 63 chars
        // So a 64 char label would indeed fail the regex first

        // To trigger the specific "label too long" message, we need a domain that passes regex but then fails on validation
        // However, this is not possible because the regex enforces the 63-character limit per label.
        // The "label too long" check in the foreach loop will never be reached for a label >63 chars 
        // because the regex check will catch it first.

        // So I'll just test that a 64-char label returns the expected error (which is the generic one due to regex)
        var label = new string('a', 64); // This exceeds 63 char limit
        var domain = $"{label}.com";

        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert - This will fail on regex check first, not on the specific label length check
        Assert.Single(result);
        Assert.Equal("Invalid domain name format.", result.First());
    }

    [Theory]
    [InlineData("sub1.sub2.sub3.sub4.sub5.example.com")] // Deep subdomain
    [InlineData("a.b.c.d.e.f.g.h.i.j.k.l.m.n.o.p.q.r.s.t.u.v.w.x.y.z.com")] // Many subdomains
    public async Task ValidateAsync_ComplexDomainStructures_ReturnsEmptyErrors(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert
        Assert.Empty(result);
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
        Assert.Empty(result);
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
        Assert.Empty(result);
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

        // Assert - Consecutive dots will be caught by regex first, not by the consecutive dots check
        Assert.Single(result);
        Assert.Equal("Invalid domain name format.", result.First());
    }

    [Theory]
    [InlineData("example..com")] // Empty label between dots (caught by regex first)
    [InlineData("a..b.com")] // Empty label between dots (caught by regex first)
    [InlineData("sub..example.com")] // Empty label in middle (caught by regex first)
    public async Task ValidateAsync_DomainWithConsecutiveDots_ReturnsFormatError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert - These will be caught by regex validation first, not by empty label check
        Assert.Single(result);
        Assert.Equal("Invalid domain name format.", result.First());
    }

    [Theory]
    [InlineData("-example.com")] // Starts with hyphen (caught by regex first)
    [InlineData("example-.com")] // Ends with hyphen (caught by regex first)
    [InlineData("-sub.example.com")] // Subdomain starts with hyphen (caught by regex first)
    [InlineData("sub-.example.com")] // Subdomain ends with hyphen (caught by regex first)
    [InlineData("example.-com")] // TLD starts with hyphen (caught by regex first)
    [InlineData("example.com-")] // TLD ends with hyphen (caught by regex first)
    public async Task ValidateAsync_HyphenBoundaries_ReturnsFormatError(string domain)
    {
        // Act
        var result = await _rule.ValidateAsync(domain);

        // Assert - These will be caught by regex validation first, not by hyphen boundary check
        Assert.Single(result);
        Assert.Equal("Invalid domain name format.", result.First());
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
        Assert.Empty(result);
    }
}
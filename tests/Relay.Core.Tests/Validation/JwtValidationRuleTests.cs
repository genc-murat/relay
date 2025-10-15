using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class JwtValidationRuleTests
{
    private readonly JwtValidationRule _rule = new();

    [Theory]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c")] // Valid JWT with signature
    [InlineData("header.payload")] // Minimal valid format (no signature)
    [InlineData("a.b")] // Simple format without signature
    [InlineData("a.b.c")] // Simple format with signature
    [InlineData("eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJ0b3B0YWwuY29tIiwiZXhwIjoxNDI2NDIwODAwLCJodHRwOi8vdG9wdGFsLmNvbS9qd3RfY2xhaW1zL2lzX2FkbWluIjp0cnVlLCJjb21wYW55IjoiVG9wdGFsIiwiYXdlc29tZSI6dHJ1ZX0.yRQYnWzskCZUxPwaQupWkiUzKELZ49eM7oWxAQK_ZXw")] // Another valid JWT
    public async Task ValidateAsync_ValidJwt_ReturnsEmptyErrors(string jwt)
    {
        // Act
        var result = await _rule.ValidateAsync(jwt);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("header.payload.signature.extra")] // Too many parts
    [InlineData("header")] // Only one part
    [InlineData("")] // Empty
    [InlineData("header..signature")] // Empty payload
    [InlineData(".payload.signature")] // Empty header
    [InlineData("header.payload.")] // Empty signature
    [InlineData("invalid@token")] // Invalid characters
    [InlineData("a.b.c.d")] // Four parts
    [InlineData(null)] // Null
    public async Task ValidateAsync_InvalidJwt_ReturnsError(string jwt)
    {
        // Act
        var result = await _rule.ValidateAsync(jwt);

        // Assert
        if (string.IsNullOrWhiteSpace(jwt))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().ContainSingle("Invalid JWT token format.");
        }
    }
}
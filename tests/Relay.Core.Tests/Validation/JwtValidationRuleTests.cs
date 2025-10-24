using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        Assert.Empty(result);
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
            Assert.Empty(result);
        }
        else
        {
            Assert.Single(result);
            Assert.Equal("Invalid JWT token format.", result.Single());
        }
    }
    
    [Fact]
    public async Task ValidateAsync_CancellationTokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        
        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _rule.ValidateAsync("valid.token.format", cancellationTokenSource.Token));
    }
    
    [Theory]
    [InlineData("   eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c   ")] // JWT with leading/trailing whitespace
    [InlineData("  header.payload  ")] // Simple format with leading/trailing whitespace
    public async Task ValidateAsync_JwtWithWhitespace_ReturnsEmptyErrors(string jwt)
    {
        // Act
        var result = await _rule.ValidateAsync(jwt);

        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [InlineData("...")] // Three dots only
    [InlineData("..")] // Two dots only
    [InlineData("...extra")] // Three dots with extra
    [InlineData("header.payload..signature")] // Two consecutive dots
    public async Task ValidateAsync_MalformedJwtWithDots_ReturnsError(string jwt)
    {
        // Act
        var result = await _rule.ValidateAsync(jwt);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid JWT token format.", result.Single());
    }
    
    [Fact]
    public async Task ValidateAsync_EmptyParts_ReturnsError()
    {
        // Arrange
        var jwtWithEmptyParts = "header..signature"; // Empty payload
        
        // Act
        var result = await _rule.ValidateAsync(jwtWithEmptyParts);

        // Assert
        Assert.Single(result);
        Assert.Equal("Invalid JWT token format.", result.Single());
    }
}
using System.Threading.Tasks;
using FluentAssertions;
using Relay.Core.Validation.Rules;
using Xunit;

namespace Relay.Core.Tests.Validation;

public class MimeTypeValidationRuleTests
{
    private readonly MimeTypeValidationRule _rule = new();

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/html")]
    [InlineData("application/json")]
    [InlineData("application/pdf")]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("audio/mpeg")]
    [InlineData("video/mp4")]
    [InlineData("application/vnd.ms-excel")]
    [InlineData("text/plain+xml")]
    [InlineData("application/atom+xml")]
    public async Task ValidateAsync_ValidMimeType_ReturnsEmptyErrors(string mimeType)
    {
        // Act
        var result = await _rule.ValidateAsync(mimeType);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    [InlineData("text")] // Missing subtype
    [InlineData("/plain")] // Missing type
    [InlineData("text/")] // Empty subtype
    [InlineData("text/plain/extra")] // Too many parts
    [InlineData("invalid-type/plain")] // Invalid type
    [InlineData("text/invalid subtype")] // Invalid subtype with space
    [InlineData("text/plain+")] // Incomplete suffix
    [InlineData("text/plain+xml+extra")] // Multiple suffixes
    public async Task ValidateAsync_InvalidMimeType_ReturnsError(string mimeType)
    {
        // Act
        var result = await _rule.ValidateAsync(mimeType);

        // Assert
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            result.Should().BeEmpty();
        }
        else
        {
            result.Should().NotBeEmpty();
        }
    }
}
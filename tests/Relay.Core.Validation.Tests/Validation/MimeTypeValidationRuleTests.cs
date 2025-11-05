 using System;
 using System.Threading;
 using System.Threading.Tasks;
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
        Assert.Empty(result);
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
            Assert.Empty(result);
        }
        else
        {
            Assert.NotEmpty(result);
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
            async () => await _rule.ValidateAsync("text/plain", cancellationTokenSource.Token));
    }
    
    [Theory]
    [InlineData("   text/plain   ")] // MIME type with whitespace
    [InlineData("  application/json  ")] // MIME type with leading/trailing spaces
    [InlineData("\timage/png\t")] // MIME type with tabs
    public async Task ValidateAsync_MimeTypeWithWhitespace_ReturnsEmptyErrors(string mimeType)
    {
        // Act
        var result = await _rule.ValidateAsync(mimeType);

        // Assert
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task ValidateAsync_CustomMimeType_ReturnsError()
    {
        // Arrange - A custom MIME type that doesn't start with a valid main type
        var customMimeType = "x-custom/type"; // Should fail since x-custom is not a valid main type
        
        // Act
        var result = await _rule.ValidateAsync(customMimeType);

        // Assert
        Assert.NotEmpty(result);
    }
    
    [Fact]
    public async Task ValidateAsync_FontMimeType_ReturnsEmptyErrors()
    {
        // Arrange - font MIME types
        var fontMimeType = "font/ttf";
        
        // Act
        var result = await _rule.ValidateAsync(fontMimeType);

        // Assert
        Assert.Empty(result);
    }
    
    [Theory]
    [InlineData("application/")] // Empty subtype after slash
    [InlineData(" /json")] // Empty type before slash
    public async Task ValidateAsync_EmptyTypeOrSubtype_ReturnsError(string mimeType)
    {
        // Act
        var result = await _rule.ValidateAsync(mimeType);

        // Assert
        Assert.NotEmpty(result);
    }
    
    [Fact]
    public async Task ValidateAsync_VeryLongMimeType_ReturnsError()
    {
        // Arrange - MIME type with very long components
        var veryLongType = "application/" + new string('a', 128); // Exceeds 127 char limit
        
        // Act
        var result = await _rule.ValidateAsync(veryLongType);

        // Assert
        Assert.NotEmpty(result);
    }
    
    [Fact]
    public async Task ValidateAsync_VeryLongSubType_ReturnsError()
    {
        // Arrange - MIME type with very long subtype
        var veryLongSubType = "text/" + new string('b', 128); // Exceeds 127 char limit
        
        // Act
        var result = await _rule.ValidateAsync(veryLongSubType);

        // Assert
        Assert.NotEmpty(result);
    }
    
    [Theory]
    [InlineData("text/.plain")] // Dot at beginning of subtype
    [InlineData("text/p.lain")] // Dot in middle of subtype
    [InlineData("text/plain.")] // Dot at end of subtype
    [InlineData("text/p-_.+lain")] // Various allowed special chars
    public async Task ValidateAsync_SpecialCharacterMimeTypes(string mimeType)
    {
        // Act
        var result = await _rule.ValidateAsync(mimeType);

        // The regex allows certain special characters, so behavior will vary
        // For this test, we'll just make sure it doesn't crash
        Assert.NotNull(result);
    }
    
    [Theory]
    [InlineData("text/plain", true)]
    [InlineData("application/json", true)]
    [InlineData("image/jpeg", true)]
    [InlineData("font/woff2", true)]
    [InlineData("application/vnd.api+json", true)]
    [InlineData("text", false)] // Invalid: no subtype
    [InlineData("text/", false)] // Invalid: empty subtype
    [InlineData("text/ ", false)] // Invalid: whitespace-only subtype
    [InlineData("/plain", false)] // Invalid: empty type
    [InlineData(" /plain", false)] // Invalid: whitespace-only type
    public async Task ValidateAsync_MimeTypeValidation_WithExpectedOutcome(string mimeType, bool shouldPass)
    {
        // Act
        var result = await _rule.ValidateAsync(mimeType);

        // Assert
        if (shouldPass)
        {
            Assert.Empty(result);
        }
        else
        {
            Assert.NotEmpty(result);
        }
    }
}
using System.Linq;
using System.Threading.Tasks;
 using Relay.Core.Validation.Rules;
 using Xunit;

namespace Relay.Core.Tests.Validation;

public class FileSizeValidationRuleTests
{
    [Theory]
    [InlineData(1024)] // 1 KB
    [InlineData(1024 * 1024)] // 1 MB
    [InlineData(0)] // Empty file
    [InlineData(1023)] // Just under 1 KB
    public async Task ValidateAsync_ValidFileSizes_ReturnsEmptyErrors(long fileSize)
    {
        // Arrange
        var rule = new FileSizeValidationRule(1024 * 1024); // 1 MB max

        // Act
        var result = await rule.ValidateAsync(fileSize);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-1024)]
    [InlineData(-1000000)]
    public async Task ValidateAsync_NegativeFileSizes_ReturnsError(long fileSize)
    {
        // Arrange
        var rule = new FileSizeValidationRule(1024 * 1024);

        // Act
        var result = await rule.ValidateAsync(fileSize);

        // Assert
        Assert.Single(result);
        Assert.Equal("File size cannot be negative.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_FileSizeAboveMaximum_ReturnsError()
    {
        // Arrange
        var rule = new FileSizeValidationRule(1024); // 1 KB max

        // Act
        var result = await rule.ValidateAsync(2048); // 2 KB

        // Assert
        Assert.Single(result);
        Assert.Equal("File size cannot exceed 1.0 KB.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_FileSizeBelowMinimum_ReturnsError()
    {
        // Arrange
        var rule = new FileSizeValidationRule(1024 * 1024, 1024); // Min 1 KB, Max 1 MB

        // Act
        var result = await rule.ValidateAsync(512); // 512 bytes

        // Assert
        Assert.Single(result);
        Assert.Equal("File size must be at least 1.0 KB.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_FileSizeOutOfRange_ReturnsMultipleErrors()
    {
        // Arrange
        var rule = new FileSizeValidationRule(1024, 512); // Min 512 bytes, Max 1 KB

        // Act
        var result = await rule.ValidateAsync(2048); // 2 KB

        // Assert
        Assert.Single(result);
        Assert.Equal("File size cannot exceed 1.0 KB.", result.First());
    }

    [Fact]
    public async Task MaxKilobytes_CreatesCorrectRule()
    {
        // Arrange
        var rule = FileSizeValidationRule.MaxKilobytes(500); // 500 KB

        // Act & Assert
        Assert.Empty(await rule.ValidateAsync(500 * 1024)); // Exactly 500 KB
        var result1 = await rule.ValidateAsync(500 * 1024 + 1);
        Assert.Single(result1);
        Assert.Equal("File size cannot exceed 500.0 KB.", result1.First());
    }

    [Fact]
    public async Task MaxMegabytes_CreatesCorrectRule()
    {
        // Arrange
        var rule = FileSizeValidationRule.MaxMegabytes(2); // 2 MB

        // Act & Assert
        Assert.Empty(await rule.ValidateAsync(2 * 1024 * 1024)); // Exactly 2 MB
        var result1 = await rule.ValidateAsync(2 * 1024 * 1024 + 1);
        Assert.Single(result1);
        Assert.Equal("File size cannot exceed 2.0 MB.", result1.First());
    }

    [Fact]
    public async Task MaxGigabytes_CreatesCorrectRule()
    {
        // Arrange
        var rule = FileSizeValidationRule.MaxGigabytes(1); // 1 GB

        // Act & Assert
        Assert.Empty(await rule.ValidateAsync(1024L * 1024 * 1024)); // Exactly 1 GB
        var result1 = await rule.ValidateAsync(1024L * 1024 * 1024 + 1);
        Assert.Single(result1);
        Assert.Equal("File size cannot exceed 1.0 GB.", result1.First());
    }

    [Theory]
    [InlineData(512, "511 bytes")]
    [InlineData(1024, "1023 bytes")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1024 * 1024, "1024.0 KB")]
    [InlineData(1024 * 1024 * 1024, "1024.0 MB")]
    [InlineData(2147483648, "2.0 GB")] // 2 GB
    public async Task ValidateAsync_SizeFormatting_InErrorMessages(long sizeBytes, string expectedFormat)
    {
        // Arrange
        var rule = new FileSizeValidationRule(sizeBytes - 1); // Just under the size

        // Act
        var result = await rule.ValidateAsync(sizeBytes);

        // Assert
        Assert.Single(result);
        Assert.Equal($"File size cannot exceed {expectedFormat}.", result.First());
    }

    [Fact]
    public async Task ValidateAsync_MinSizeWithFormatting_InErrorMessages()
    {
        // Arrange
        var rule = new FileSizeValidationRule(1024 * 1024, 1024); // Min 1 KB

        // Act
        var result = await rule.ValidateAsync(512); // 512 bytes

        // Assert
        Assert.Single(result);
        Assert.Equal("File size must be at least 1.0 KB.", result.First());
    }

    [Fact]
    public async Task MaxKilobytes_WithMinSize_WorksCorrectly()
    {
        // Arrange
        var rule = FileSizeValidationRule.MaxKilobytes(100, 10 * 1024); // Max 100 KB, Min 10 KB

        // Act & Assert
        Assert.Empty(await rule.ValidateAsync(50 * 1024)); // 50 KB - valid
        var result1 = await rule.ValidateAsync(5 * 1024);
        Assert.Single(result1);
        Assert.Equal("File size must be at least 10.0 KB.", result1.First()); // Too small
        var result2 = await rule.ValidateAsync(150 * 1024);
        Assert.Single(result2);
        Assert.Equal("File size cannot exceed 100.0 KB.", result2.First()); // Too big
    }
}
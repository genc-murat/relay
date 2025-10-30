using Relay.MessageBroker.Deduplication;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class DeduplicationOptionsTests
{
    [Fact]
    public void Validate_WithValidOptions_ShouldNotThrow()
    {
        // Arrange
        var options = new DeduplicationOptions
        {
            Enabled = true,
            Window = TimeSpan.FromMinutes(30),
            MaxCacheSize = 50000,
            Strategy = DeduplicationStrategy.ContentHash,
            CustomHashFunction = null
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithWindowTooShort_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new DeduplicationOptions { Window = TimeSpan.FromSeconds(30) };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithWindowTooLong_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new DeduplicationOptions { Window = TimeSpan.FromHours(25) };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithMaxCacheSizeTooSmall_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new DeduplicationOptions { MaxCacheSize = 0 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithMaxCacheSizeTooLarge_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new DeduplicationOptions { MaxCacheSize = 1_000_001 };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithCustomStrategyAndNullCustomHashFunction_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new DeduplicationOptions
        {
            Strategy = DeduplicationStrategy.Custom,
            CustomHashFunction = null
        };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => options.Validate());
    }

    [Fact]
    public void Validate_WithCustomStrategyAndValidCustomHashFunction_ShouldNotThrow()
    {
        // Arrange
        var options = new DeduplicationOptions
        {
            Strategy = DeduplicationStrategy.Custom,
            CustomHashFunction = data => "hash"
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void DefaultValues_ShouldBeValid()
    {
        // Arrange
        var options = new DeduplicationOptions();

        // Act & Assert
        options.Validate(); // Should not throw with defaults
        Assert.False(options.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Window);
        Assert.Equal(100_000, options.MaxCacheSize);
        Assert.Equal(DeduplicationStrategy.ContentHash, options.Strategy);
        Assert.Null(options.CustomHashFunction);
    }
}
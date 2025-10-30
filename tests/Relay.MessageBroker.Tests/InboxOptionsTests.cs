using Relay.MessageBroker.Inbox;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class InboxOptionsTests
{
    [Fact]
    public void Validate_WithDefaultValues_ShouldNotThrow()
    {
        // Arrange
        var options = new InboxOptions();

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithValidValues_ShouldNotThrow()
    {
        // Arrange
        var options = new InboxOptions
        {
            Enabled = true,
            RetentionPeriod = TimeSpan.FromDays(30),
            CleanupInterval = TimeSpan.FromHours(2),
            ConsumerName = "TestConsumer"
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithRetentionPeriodTooShort_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new InboxOptions
        {
            RetentionPeriod = TimeSpan.FromHours(12)
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("RetentionPeriod must be at least 24 hours", exception.Message);
        Assert.Equal("RetentionPeriod", exception.ParamName);
    }

    [Fact]
    public void Validate_WithCleanupIntervalTooShort_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new InboxOptions
        {
            CleanupInterval = TimeSpan.FromMinutes(30)
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("CleanupInterval must be at least 1 hour", exception.Message);
        Assert.Equal("CleanupInterval", exception.ParamName);
    }

    [Fact]
    public void Validate_WithMinimumValidValues_ShouldNotThrow()
    {
        // Arrange
        var options = new InboxOptions
        {
            RetentionPeriod = TimeSpan.FromHours(24),
            CleanupInterval = TimeSpan.FromHours(1)
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new InboxOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(TimeSpan.FromDays(7), options.RetentionPeriod);
        Assert.Equal(TimeSpan.FromHours(1), options.CleanupInterval);
        Assert.Null(options.ConsumerName);
    }
}
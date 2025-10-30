using Relay.MessageBroker.Batch;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BatchOptionsTests
{
    [Fact]
    public void BatchOptions_DefaultValues_ShouldBeCorrect()
    {
        // Act
        var options = new BatchOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(100, options.MaxBatchSize);
        Assert.Equal(TimeSpan.FromMilliseconds(100), options.FlushInterval);
        Assert.True(options.EnableCompression);
        Assert.True(options.PartialRetry);
    }

    [Fact]
    public void Validate_WithValidOptions_ShouldNotThrow()
    {
        // Arrange
        var options = new BatchOptions
        {
            Enabled = true,
            MaxBatchSize = 500,
            FlushInterval = TimeSpan.FromSeconds(5),
            EnableCompression = false,
            PartialRetry = false
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithMaxBatchSizeTooSmall_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new BatchOptions { MaxBatchSize = 0 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        Assert.Equal("MaxBatchSize", exception.ParamName);
    }

    [Fact]
    public void Validate_WithMaxBatchSizeTooLarge_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new BatchOptions { MaxBatchSize = 10001 };

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        Assert.Equal("MaxBatchSize", exception.ParamName);
    }

    [Fact]
    public void Validate_WithFlushIntervalTooSmall_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new BatchOptions { FlushInterval = TimeSpan.FromMilliseconds(0.5) };

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        Assert.Equal("FlushInterval", exception.ParamName);
    }

    [Fact]
    public void Validate_WithFlushIntervalZero_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new BatchOptions { FlushInterval = TimeSpan.Zero };

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        Assert.Equal("FlushInterval", exception.ParamName);
    }

    [Fact]
    public void Validate_WithNegativeFlushInterval_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new BatchOptions { FlushInterval = TimeSpan.FromMilliseconds(-100) };

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        Assert.Equal("FlushInterval", exception.ParamName);
    }
}
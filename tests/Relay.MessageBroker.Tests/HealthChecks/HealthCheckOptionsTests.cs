using Relay.MessageBroker.HealthChecks;
using Xunit;

namespace Relay.MessageBroker.Tests.HealthChecks;

public class HealthCheckOptionsTests
{
    [Fact]
    public void Validate_WithDefaultValues_ShouldNotThrow()
    {
        // Arrange
        var options = new HealthCheckOptions();

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithValidValues_ShouldNotThrow()
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            Interval = TimeSpan.FromSeconds(60),
            ConnectivityTimeout = TimeSpan.FromSeconds(5),
            IncludeCircuitBreakerState = true,
            IncludeConnectionPoolMetrics = false,
            Name = "CustomHealthCheck",
            Tags = new[] { "custom", "broker" }
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void Validate_WithIntervalTooShort_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            Interval = TimeSpan.FromSeconds(2)
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("Health check interval must be at least 5 seconds", exception.Message);
        Assert.Equal("Interval", exception.ParamName);
    }

    [Fact]
    public void Validate_WithZeroConnectivityTimeout_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            ConnectivityTimeout = TimeSpan.Zero
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("Connectivity timeout must be greater than zero", exception.Message);
        Assert.Equal("ConnectivityTimeout", exception.ParamName);
    }

    [Fact]
    public void Validate_WithNegativeConnectivityTimeout_ShouldThrowArgumentException()
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            ConnectivityTimeout = TimeSpan.FromSeconds(-1)
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Contains("Connectivity timeout must be greater than zero", exception.Message);
        Assert.Equal("ConnectivityTimeout", exception.ParamName);
    }

    [Fact]
    public void Validate_WithMinimumValidValues_ShouldNotThrow()
    {
        // Arrange
        var options = new HealthCheckOptions
        {
            Interval = TimeSpan.FromSeconds(5),
            ConnectivityTimeout = TimeSpan.FromMilliseconds(1)
        };

        // Act & Assert
        options.Validate(); // Should not throw
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new HealthCheckOptions();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), options.Interval);
        Assert.Equal(TimeSpan.FromSeconds(2), options.ConnectivityTimeout);
        Assert.True(options.IncludeCircuitBreakerState);
        Assert.True(options.IncludeConnectionPoolMetrics);
        Assert.Equal("MessageBroker", options.Name);
        Assert.Equal(new[] { "messagebroker", "ready" }, options.Tags);
    }

    [Fact]
    public void Name_ShouldBeSettable()
    {
        // Arrange
        var options = new HealthCheckOptions();

        // Act
        options.Name = "CustomName";

        // Assert
        Assert.Equal("CustomName", options.Name);
    }

    [Fact]
    public void Tags_ShouldBeSettable()
    {
        // Arrange
        var options = new HealthCheckOptions();
        var newTags = new[] { "tag1", "tag2" };

        // Act
        options.Tags = newTags;

        // Assert
        Assert.Equal(newTags, options.Tags);
    }
}
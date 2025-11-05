using System;
using Xunit;
using Relay.Core.Testing;

namespace Relay.Core.Testing.Tests;

public class LoadTestConfigurationTests
{
    [Fact]
    public void DefaultConstructor_SetsDefaultValues()
    {
        // Arrange & Act
        var config = new LoadTestConfiguration();

        // Assert
        Assert.Equal(100, config.TotalRequests);
        Assert.Equal(10, config.MaxConcurrency);
        Assert.Equal(0, config.RampUpDelayMs);
        Assert.Equal(TimeSpan.FromMinutes(1), config.Duration);
        Assert.Equal(1, config.ConcurrentUsers);
        Assert.Equal(TimeSpan.Zero, config.RampUpTime);
        Assert.Equal(TimeSpan.FromSeconds(1), config.RequestInterval);
        Assert.False(config.MonitorMemoryUsage);
        Assert.True(config.CollectDetailedTiming);
        Assert.Equal(TimeSpan.FromSeconds(10), config.WarmUpDuration);
    }

    [Fact]
    public void ParameterizedConstructor_WithValidParameters_SetsValues()
    {
        // Arrange & Act
        var config = new LoadTestConfiguration(200, 20, 100);

        // Assert
        Assert.Equal(200, config.TotalRequests);
        Assert.Equal(20, config.MaxConcurrency);
        Assert.Equal(100, config.RampUpDelayMs);
        // Other properties should still have defaults
        Assert.Equal(TimeSpan.FromMinutes(1), config.Duration);
        Assert.Equal(1, config.ConcurrentUsers);
    }

    [Fact]
    public void ParameterizedConstructor_WithRampUpDelayDefault_SetsZeroRampUpDelay()
    {
        // Arrange & Act
        var config = new LoadTestConfiguration(150, 15);

        // Assert
        Assert.Equal(150, config.TotalRequests);
        Assert.Equal(15, config.MaxConcurrency);
        Assert.Equal(0, config.RampUpDelayMs);
    }

    [Fact]
    public void TotalRequests_SetValidValue_Succeeds()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act
        config.TotalRequests = 500;

        // Assert
        Assert.Equal(500, config.TotalRequests);
    }

    [Fact]
    public void TotalRequests_SetZero_ThrowsArgumentException()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.TotalRequests = 0);
        Assert.Contains("TotalRequests must be greater than 0", exception.Message);
    }

    [Fact]
    public void TotalRequests_SetNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.TotalRequests = -1);
        Assert.Contains("TotalRequests must be greater than 0", exception.Message);
    }

    [Fact]
    public void MaxConcurrency_SetValidValue_Succeeds()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act
        config.MaxConcurrency = 50;

        // Assert
        Assert.Equal(50, config.MaxConcurrency);
    }

    [Fact]
    public void MaxConcurrency_SetZero_ThrowsArgumentException()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.MaxConcurrency = 0);
        Assert.Contains("MaxConcurrency must be greater than 0", exception.Message);
    }

    [Fact]
    public void MaxConcurrency_SetNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.MaxConcurrency = -5);
        Assert.Contains("MaxConcurrency must be greater than 0", exception.Message);
    }

    [Fact]
    public void RampUpDelayMs_SetValidValue_Succeeds()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act
        config.RampUpDelayMs = 200;

        // Assert
        Assert.Equal(200, config.RampUpDelayMs);
    }

    [Fact]
    public void RampUpDelayMs_SetZero_Succeeds()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act
        config.RampUpDelayMs = 0;

        // Assert
        Assert.Equal(0, config.RampUpDelayMs);
    }

    [Fact]
    public void RampUpDelayMs_SetNegativeValue_ThrowsArgumentException()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => config.RampUpDelayMs = -10);
        Assert.Contains("RampUpDelayMs cannot be negative", exception.Message);
    }

    [Fact]
    public void Duration_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var config = new LoadTestConfiguration();
        var duration = TimeSpan.FromMinutes(5);

        // Act
        config.Duration = duration;

        // Assert
        Assert.Equal(duration, config.Duration);
    }

    [Fact]
    public void ConcurrentUsers_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act
        config.ConcurrentUsers = 10;

        // Assert
        Assert.Equal(10, config.ConcurrentUsers);
    }

    [Fact]
    public void RampUpTime_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var config = new LoadTestConfiguration();
        var rampUpTime = TimeSpan.FromSeconds(30);

        // Act
        config.RampUpTime = rampUpTime;

        // Assert
        Assert.Equal(rampUpTime, config.RampUpTime);
    }

    [Fact]
    public void RequestInterval_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var config = new LoadTestConfiguration();
        var interval = TimeSpan.FromMilliseconds(500);

        // Act
        config.RequestInterval = interval;

        // Assert
        Assert.Equal(interval, config.RequestInterval);
    }

    [Fact]
    public void MonitorMemoryUsage_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act
        config.MonitorMemoryUsage = true;

        // Assert
        Assert.True(config.MonitorMemoryUsage);
    }

    [Fact]
    public void CollectDetailedTiming_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act
        config.CollectDetailedTiming = false;

        // Assert
        Assert.False(config.CollectDetailedTiming);
    }

    [Fact]
    public void WarmUpDuration_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var config = new LoadTestConfiguration();
        var warmUp = TimeSpan.FromSeconds(30);

        // Act
        config.WarmUpDuration = warmUp;

        // Assert
        Assert.Equal(warmUp, config.WarmUpDuration);
    }

    [Fact]
    public void Constructor_WithInvalidTotalRequests_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LoadTestConfiguration(0, 10));
        Assert.Contains("TotalRequests must be greater than 0", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidMaxConcurrency_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LoadTestConfiguration(100, 0));
        Assert.Contains("MaxConcurrency must be greater than 0", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidRampUpDelay_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new LoadTestConfiguration(100, 10, -5));
        Assert.Contains("RampUpDelayMs cannot be negative", exception.Message);
    }

    [Fact]
    public void Constructor_WithValidRampUpDelay_Succeeds()
    {
        // Act
        var config = new LoadTestConfiguration(100, 10, 100);

        // Assert
        Assert.Equal(100, config.RampUpDelayMs);
    }

    [Fact]
    public void MultiplePropertyChanges_WorkCorrectly()
    {
        // Arrange
        var config = new LoadTestConfiguration();

        // Act
        config.TotalRequests = 1000;
        config.MaxConcurrency = 100;
        config.RampUpDelayMs = 50;
        config.Duration = TimeSpan.FromMinutes(10);
        config.ConcurrentUsers = 50;
        config.RampUpTime = TimeSpan.FromSeconds(60);
        config.RequestInterval = TimeSpan.FromMilliseconds(200);
        config.MonitorMemoryUsage = true;
        config.CollectDetailedTiming = false;
        config.WarmUpDuration = TimeSpan.FromSeconds(20);

        // Assert
        Assert.Equal(1000, config.TotalRequests);
        Assert.Equal(100, config.MaxConcurrency);
        Assert.Equal(50, config.RampUpDelayMs);
        Assert.Equal(TimeSpan.FromMinutes(10), config.Duration);
        Assert.Equal(50, config.ConcurrentUsers);
        Assert.Equal(TimeSpan.FromSeconds(60), config.RampUpTime);
        Assert.Equal(TimeSpan.FromMilliseconds(200), config.RequestInterval);
        Assert.True(config.MonitorMemoryUsage);
        Assert.False(config.CollectDetailedTiming);
        Assert.Equal(TimeSpan.FromSeconds(20), config.WarmUpDuration);
    }
}
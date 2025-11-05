using Relay.Core.Performance.Profiling;
using Xunit;

namespace Relay.Core.Tests.Performance.Profiling;

/// <summary>
/// Tests for PerformanceProfilingOptions class
/// </summary>
public class PerformanceProfilingOptionsTests
{
    [Fact]
    public void PerformanceProfilingOptions_DefaultConstructor_InitializesProperties()
    {
        // Act
        var options = new PerformanceProfilingOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.False(options.LogAllRequests);
        Assert.Equal(1000, options.SlowRequestThresholdMs);
        Assert.Equal(1000, options.MaxRecentMetrics);
    }

    [Fact]
    public void PerformanceProfilingOptions_CanSetEnabled()
    {
        // Arrange
        var options = new PerformanceProfilingOptions();

        // Act
        options.Enabled = true;

        // Assert
        Assert.True(options.Enabled);
    }

    [Fact]
    public void PerformanceProfilingOptions_CanSetLogAllRequests()
    {
        // Arrange
        var options = new PerformanceProfilingOptions();

        // Act
        options.LogAllRequests = true;

        // Assert
        Assert.True(options.LogAllRequests);
    }

    [Fact]
    public void PerformanceProfilingOptions_CanSetSlowRequestThresholdMs()
    {
        // Arrange
        var options = new PerformanceProfilingOptions();

        // Act
        options.SlowRequestThresholdMs = 500;

        // Assert
        Assert.Equal(500, options.SlowRequestThresholdMs);
    }

    [Fact]
    public void PerformanceProfilingOptions_CanSetMaxRecentMetrics()
    {
        // Arrange
        var options = new PerformanceProfilingOptions();

        // Act
        options.MaxRecentMetrics = 5000;

        // Assert
        Assert.Equal(5000, options.MaxRecentMetrics);
    }

    [Fact]
    public void PerformanceProfilingOptions_ObjectInitialization_Works()
    {
        // Act
        var options = new PerformanceProfilingOptions
        {
            Enabled = true,
            LogAllRequests = true,
            SlowRequestThresholdMs = 2000,
            MaxRecentMetrics = 5000
        };

        // Assert
        Assert.True(options.Enabled);
        Assert.True(options.LogAllRequests);
        Assert.Equal(2000, options.SlowRequestThresholdMs);
        Assert.Equal(5000, options.MaxRecentMetrics);
    }

    [Fact]
    public void PerformanceProfilingOptions_WithZeroThreshold_HandlesCorrectly()
    {
        // Act
        var options = new PerformanceProfilingOptions
        {
            Enabled = true,
            SlowRequestThresholdMs = 0,
            MaxRecentMetrics = 1
        };

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(0, options.SlowRequestThresholdMs);
        Assert.Equal(1, options.MaxRecentMetrics);
    }

    [Fact]
    public void PerformanceProfilingOptions_WithNegativeValues_HandlesCorrectly()
    {
        // Act
        var options = new PerformanceProfilingOptions
        {
            SlowRequestThresholdMs = -100,
            MaxRecentMetrics = -1
        };

        // Assert
        Assert.Equal(-100, options.SlowRequestThresholdMs);
        Assert.Equal(-1, options.MaxRecentMetrics);
    }

    [Fact]
    public void PerformanceProfilingOptions_WithLargeValues_HandlesCorrectly()
    {
        // Act
        var options = new PerformanceProfilingOptions
        {
            SlowRequestThresholdMs = int.MaxValue,
            MaxRecentMetrics = int.MaxValue
        };

        // Assert
        Assert.Equal(int.MaxValue, options.SlowRequestThresholdMs);
        Assert.Equal(int.MaxValue, options.MaxRecentMetrics);
    }

    [Fact]
    public void PerformanceProfilingOptions_ReferenceEquality_Works()
    {
        // Arrange
        var options1 = new PerformanceProfilingOptions
        {
            Enabled = true,
            LogAllRequests = false,
            SlowRequestThresholdMs = 1000,
            MaxRecentMetrics = 1000
        };

        var options2 = new PerformanceProfilingOptions
        {
            Enabled = true,
            LogAllRequests = false,
            SlowRequestThresholdMs = 1000,
            MaxRecentMetrics = 1000
        };

        // Assert - Reference equality should work for class instances
        Assert.NotSame(options1, options2);
        Assert.Equal(options1.Enabled, options2.Enabled);
        Assert.Equal(options1.LogAllRequests, options2.LogAllRequests);
        Assert.Equal(options1.SlowRequestThresholdMs, options2.SlowRequestThresholdMs);
        Assert.Equal(options1.MaxRecentMetrics, options2.MaxRecentMetrics);
    }

    [Fact]
    public void PerformanceProfilingOptions_CanBeModifiedAfterCreation()
    {
        // Arrange
        var options = new PerformanceProfilingOptions();

        // Act - Modify all properties
        options.Enabled = true;
        options.LogAllRequests = true;
        options.SlowRequestThresholdMs = 500;
        options.MaxRecentMetrics = 2000;

        // Assert
        Assert.True(options.Enabled);
        Assert.True(options.LogAllRequests);
        Assert.Equal(500, options.SlowRequestThresholdMs);
        Assert.Equal(2000, options.MaxRecentMetrics);
    }
}

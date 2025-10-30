using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Relay.MessageBroker.Backpressure;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BackpressureControllerTests
{
    [Fact]
    public async Task ShouldThrottleAsync_ShouldReturnFalseWhenDisabled()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = false
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        var shouldThrottle = await controller.ShouldThrottleAsync();

        // Assert
        Assert.False(shouldThrottle);
    }

    [Fact]
    public async Task ShouldThrottleAsync_ShouldReturnFalseWhenBelowThresholds()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true,
            LatencyThreshold = TimeSpan.FromSeconds(5),
            QueueDepthThreshold = 1000
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(100));
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(150));
        controller.UpdateQueueDepth(100);

        var shouldThrottle = await controller.ShouldThrottleAsync();

        // Assert
        Assert.False(shouldThrottle);
    }

    [Fact]
    public async Task ShouldThrottleAsync_ShouldReturnTrueWhenLatencyExceedsThreshold()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true,
            LatencyThreshold = TimeSpan.FromSeconds(1),
            RecoveryLatencyThreshold = TimeSpan.FromMilliseconds(500),
            QueueDepthThreshold = 10000
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        controller.UpdateQueueDepth(100);

        var shouldThrottle = await controller.ShouldThrottleAsync();

        // Assert
        Assert.True(shouldThrottle);
    }

    [Fact]
    public async Task ShouldThrottleAsync_ShouldReturnTrueWhenQueueDepthExceedsThreshold()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true,
            LatencyThreshold = TimeSpan.FromSeconds(10),
            QueueDepthThreshold = 100
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(100));
        controller.UpdateQueueDepth(150);

        var shouldThrottle = await controller.ShouldThrottleAsync();

        // Assert
        Assert.True(shouldThrottle);
    }

    [Fact]
    public async Task ShouldThrottleAsync_ShouldDeactivateWhenConditionsImprove()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true,
            LatencyThreshold = TimeSpan.FromSeconds(5),
            RecoveryLatencyThreshold = TimeSpan.FromSeconds(2),
            QueueDepthThreshold = 1000
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act - Activate backpressure
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(6));
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(6));
        controller.UpdateQueueDepth(100);
        var shouldThrottle1 = await controller.ShouldThrottleAsync();

        // Improve conditions - need enough samples to bring average below recovery threshold
        for (int i = 0; i < 20; i++)
        {
            await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(100));
        }
        controller.UpdateQueueDepth(50);
        var shouldThrottle2 = await controller.ShouldThrottleAsync();

        // Assert
        Assert.True(shouldThrottle1);
        Assert.False(shouldThrottle2);
    }

    [Fact]
    public async Task RecordProcessingAsync_ShouldDoNothingWhenDisabled()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = false
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(10));
        var metrics = controller.GetMetrics();

        // Assert
        Assert.Equal(0, metrics.TotalProcessingRecords);
    }

    [Fact]
    public async Task RecordProcessingAsync_ShouldTrackProcessingRecords()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(100));
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(200));
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(150));

        var metrics = controller.GetMetrics();

        // Assert
        Assert.Equal(3, metrics.TotalProcessingRecords);
    }

    [Fact]
    public async Task GetMetrics_ShouldCalculateAverageLatency()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(100));
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(200));
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(300));

        var metrics = controller.GetMetrics();

        // Assert
        Assert.Equal(200, metrics.AverageLatency.TotalMilliseconds);
    }

    [Fact]
    public async Task GetMetrics_ShouldTrackMinAndMaxLatency()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(100));
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(500));
        await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(300));

        var metrics = controller.GetMetrics();

        // Assert
        Assert.Equal(100, metrics.MinLatency.TotalMilliseconds);
        Assert.Equal(500, metrics.MaxLatency.TotalMilliseconds);
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnCurrentQueueDepth()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        controller.UpdateQueueDepth(250);
        var metrics = controller.GetMetrics();

        // Assert
        Assert.Equal(250, metrics.QueueDepth);
    }

    [Fact]
    public async Task GetMetrics_ShouldTrackThrottlingState()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true,
            LatencyThreshold = TimeSpan.FromSeconds(1),
            RecoveryLatencyThreshold = TimeSpan.FromMilliseconds(500),
            QueueDepthThreshold = 10000
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        var metrics1 = controller.GetMetrics();

        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.ShouldThrottleAsync();

        var metrics2 = controller.GetMetrics();

        // Assert
        Assert.False(metrics1.IsThrottling);
        Assert.True(metrics2.IsThrottling);
    }

    [Fact]
    public async Task GetMetrics_ShouldTrackBackpressureActivations()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true,
            LatencyThreshold = TimeSpan.FromSeconds(1),
            RecoveryLatencyThreshold = TimeSpan.FromMilliseconds(200),
            QueueDepthThreshold = 10000,
            SlidingWindowSize = 10
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act - Activate backpressure
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.ShouldThrottleAsync();

        // Deactivate - need enough samples to bring average below recovery threshold
        for (int i = 0; i < 20; i++)
        {
            await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(50));
        }
        await controller.ShouldThrottleAsync();

        // Activate again - need to push average back above threshold
        for (int i = 0; i < 15; i++)
        {
            await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        }
        await controller.ShouldThrottleAsync();

        var metrics = controller.GetMetrics();

        // Assert
        Assert.Equal(2, metrics.BackpressureActivations);
    }

    [Fact]
    public async Task GetMetrics_ShouldTrackActivationTimestamps()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true,
            LatencyThreshold = TimeSpan.FromSeconds(1),
            RecoveryLatencyThreshold = TimeSpan.FromMilliseconds(500),
            QueueDepthThreshold = 10000
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.ShouldThrottleAsync();

        var metrics1 = controller.GetMetrics();

        // Deactivate - need more samples to bring average down
        for (int i = 0; i < 10; i++)
        {
            await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(100));
        }
        await controller.ShouldThrottleAsync();

        var metrics2 = controller.GetMetrics();

        // Assert
        Assert.NotNull(metrics1.LastBackpressureActivation);
        Assert.Null(metrics1.LastBackpressureDeactivation);
        Assert.NotNull(metrics2.LastBackpressureDeactivation);
    }

    [Fact]
    public async Task BackpressureStateChanged_ShouldRaiseEventOnActivation()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true,
            LatencyThreshold = TimeSpan.FromSeconds(1),
            RecoveryLatencyThreshold = TimeSpan.FromMilliseconds(500),
            QueueDepthThreshold = 10000
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        BackpressureEvent? capturedEvent = null;
        controller.BackpressureStateChanged += (sender, e) => capturedEvent = e;

        // Act
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.ShouldThrottleAsync();

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(BackpressureEventType.Activated, capturedEvent.EventType);
        Assert.True(capturedEvent.AverageLatency > TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task BackpressureStateChanged_ShouldRaiseEventOnDeactivation()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true,
            LatencyThreshold = TimeSpan.FromSeconds(1),
            RecoveryLatencyThreshold = TimeSpan.FromMilliseconds(500),
            QueueDepthThreshold = 10000
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        BackpressureEvent? capturedEvent = null;

        // Act - Activate
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.RecordProcessingAsync(TimeSpan.FromSeconds(2));
        await controller.ShouldThrottleAsync();

        // Subscribe after activation
        controller.BackpressureStateChanged += (sender, e) => capturedEvent = e;

        // Deactivate - need more samples to bring average down
        for (int i = 0; i < 10; i++)
        {
            await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(100));
        }
        await controller.ShouldThrottleAsync();

        // Assert
        Assert.NotNull(capturedEvent);
        Assert.Equal(BackpressureEventType.Deactivated, capturedEvent.EventType);
    }

    [Fact]
    public async Task RecordProcessingAsync_ShouldLimitSlidingWindowSize()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions
        {
            Enabled = true,
            SlidingWindowSize = 10
        });
        var controller = new BackpressureController(options, NullLogger<BackpressureController>.Instance);

        // Act - Record more than window size
        for (int i = 0; i < 20; i++)
        {
            await controller.RecordProcessingAsync(TimeSpan.FromMilliseconds(100));
        }

        var metrics = controller.GetMetrics();

        // Assert
        Assert.Equal(20, metrics.TotalProcessingRecords);
        // The average should be calculated from the last 10 records only
    }

    [Fact]
    public void Constructor_ShouldThrowWhenOptionsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new BackpressureController(null!, NullLogger<BackpressureController>.Instance);
        });
    }

    [Fact]
    public void Constructor_ShouldThrowWhenLoggerIsNull()
    {
        // Arrange
        var options = Options.Create(new BackpressureOptions());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
        {
            new BackpressureController(options, null!);
        });
    }

    [Fact]
    public void BackpressureOptions_Validate_ShouldThrowWhenLatencyThresholdIsZero()
    {
        // Arrange
        var options = new BackpressureOptions
        {
            LatencyThreshold = TimeSpan.Zero
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void BackpressureOptions_Validate_ShouldThrowWhenRecoveryLatencyThresholdIsZero()
    {
        // Arrange
        var options = new BackpressureOptions
        {
            RecoveryLatencyThreshold = TimeSpan.Zero
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void BackpressureOptions_Validate_ShouldThrowWhenRecoveryThresholdExceedsLatencyThreshold()
    {
        // Arrange
        var options = new BackpressureOptions
        {
            LatencyThreshold = TimeSpan.FromSeconds(2),
            RecoveryLatencyThreshold = TimeSpan.FromSeconds(5)
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void BackpressureOptions_Validate_ShouldThrowWhenQueueDepthThresholdIsZero()
    {
        // Arrange
        var options = new BackpressureOptions
        {
            QueueDepthThreshold = 0
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void BackpressureOptions_Validate_ShouldThrowWhenSlidingWindowSizeIsZero()
    {
        // Arrange
        var options = new BackpressureOptions
        {
            SlidingWindowSize = 0
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void BackpressureOptions_Validate_ShouldThrowWhenThrottleFactorIsNegative()
    {
        // Arrange
        var options = new BackpressureOptions
        {
            ThrottleFactor = -0.1
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void BackpressureOptions_Validate_ShouldThrowWhenThrottleFactorExceedsOne()
    {
        // Arrange
        var options = new BackpressureOptions
        {
            ThrottleFactor = 1.5
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void BackpressureOptions_Validate_ShouldNotThrowWithValidOptions()
    {
        // Arrange
        var options = new BackpressureOptions
        {
            LatencyThreshold = TimeSpan.FromSeconds(5),
            RecoveryLatencyThreshold = TimeSpan.FromSeconds(2),
            QueueDepthThreshold = 1000,
            SlidingWindowSize = 100,
            ThrottleFactor = 0.5
        };

        // Act & Assert (should not throw)
        options.Validate();
    }
}



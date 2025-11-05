using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class DefaultMetricsProviderRetrievalTests
{
    private readonly Mock<ILogger<DefaultMetricsProvider>> _loggerMock;
    private readonly TestableDefaultMetricsProvider _metricsProvider;

    public DefaultMetricsProviderRetrievalTests()
    {
        _loggerMock = new Mock<ILogger<DefaultMetricsProvider>>();
        _metricsProvider = new TestableDefaultMetricsProvider(_loggerMock.Object);
    }

    [Fact]
    public void GetTimingBreakdown_WithExistingBreakdown_ShouldReturnBreakdown()
    {
        // Arrange
        var breakdown = new TimingBreakdown
        {
            OperationId = "test-operation",
            TotalDuration = TimeSpan.FromMilliseconds(500),
            PhaseTimings = new Dictionary<string, TimeSpan>
            {
                ["Handler"] = TimeSpan.FromMilliseconds(300),
                ["Validation"] = TimeSpan.FromMilliseconds(100),
                ["Pipeline"] = TimeSpan.FromMilliseconds(100)
            }
        };
        _metricsProvider.RecordTimingBreakdown(breakdown);

        // Act
        var retrieved = _metricsProvider.GetTimingBreakdown("test-operation");

        // Assert
        Assert.Equal("test-operation", retrieved.OperationId);
        Assert.Equal(TimeSpan.FromMilliseconds(500), retrieved.TotalDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(300), retrieved.PhaseTimings["Handler"]);
        Assert.Equal(TimeSpan.FromMilliseconds(100), retrieved.PhaseTimings["Validation"]);
        Assert.Equal(TimeSpan.FromMilliseconds(100), retrieved.PhaseTimings["Pipeline"]);
    }

    [Fact]
    public void GetTimingBreakdown_WithNonExistingBreakdown_ShouldReturnEmptyBreakdown()
    {
        // Act
        var retrieved = _metricsProvider.GetTimingBreakdown("non-existing-operation");

        // Assert
        Assert.Equal("non-existing-operation", retrieved.OperationId);
        Assert.Equal(TimeSpan.Zero, retrieved.TotalDuration);
        Assert.Empty(retrieved.PhaseTimings);
    }
}

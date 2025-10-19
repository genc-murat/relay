using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class DefaultMetricsProviderTimingBreakdownTests
{
    private readonly Mock<ILogger<DefaultMetricsProvider>> _loggerMock;
    private readonly TestableDefaultMetricsProvider _metricsProvider;

    public DefaultMetricsProviderTimingBreakdownTests()
    {
        _loggerMock = new Mock<ILogger<DefaultMetricsProvider>>();
        _metricsProvider = new TestableDefaultMetricsProvider(_loggerMock.Object);
    }

    [Fact]
    public void RecordTimingBreakdown_ShouldCleanupOldBreakdowns_WhenLimitExceeded()
    {
        // Arrange - Record more than the limit (10000) to test cleanup
        const int recordCount = 11000;
        const int expectedCleanupCount = 1000; // recordCount - MaxTimingBreakdowns

        for (int i = 0; i < recordCount; i++)
        {
            var breakdown = new TimingBreakdown
            {
                OperationId = $"cleanup-test-{i}",
                TotalDuration = TimeSpan.FromMilliseconds(100)
            };
            _metricsProvider.RecordTimingBreakdown(breakdown);
        }

        // Act - Try to retrieve some breakdowns
        var firstBreakdown = _metricsProvider.GetTimingBreakdown("cleanup-test-0");
        var middleBreakdown = _metricsProvider.GetTimingBreakdown("cleanup-test-500");
        var lastBreakdown = _metricsProvider.GetTimingBreakdown($"cleanup-test-{recordCount - 1}");

        // Assert - Some old entries should be cleaned up, newer ones should remain
        // The first entry should either be cleaned up (empty breakdown) or still exist
        Assert.Equal("cleanup-test-0", firstBreakdown.OperationId);
        Assert.Equal("cleanup-test-500", middleBreakdown.OperationId);
        Assert.Equal($"cleanup-test-{recordCount - 1}", lastBreakdown.OperationId);

        // The last entry should definitely exist with the correct duration
        Assert.Equal(TimeSpan.FromMilliseconds(100), lastBreakdown.TotalDuration);

        // At least some cleanup should have happened (we can't predict exactly which entries are removed
        // since the cleanup removes the "oldest" keys, but since we add them in order, the first ones should be removed)
        var hasCleanupOccurred = firstBreakdown.TotalDuration == TimeSpan.Zero ||
                                middleBreakdown.TotalDuration == TimeSpan.Zero;
        Assert.True(hasCleanupOccurred, "Some timing breakdowns should have been cleaned up when limit exceeded");
    }
}
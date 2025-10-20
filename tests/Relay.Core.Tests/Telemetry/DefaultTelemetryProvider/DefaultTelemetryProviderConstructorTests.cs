using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

/// <summary>
/// Tests for DefaultTelemetryProvider constructor functionality
/// </summary>
public class DefaultTelemetryProviderConstructorTests
{
    private readonly Mock<ILogger<DefaultTelemetryProvider>> _loggerMock;
    private readonly Mock<IMetricsProvider> _metricsProviderMock;

    public DefaultTelemetryProviderConstructorTests()
    {
        _loggerMock = new Mock<ILogger<DefaultTelemetryProvider>>();
        _metricsProviderMock = new Mock<IMetricsProvider>();
    }

    [Fact]
    public void Constructor_WithLoggerAndMetricsProvider_ShouldInitializeCorrectly()
    {
        // Act
        var provider = new DefaultTelemetryProvider(_loggerMock.Object, _metricsProviderMock.Object);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal(_metricsProviderMock.Object, provider.MetricsProvider);
    }

    [Fact]
    public void Constructor_WithLoggerOnly_ShouldCreateDefaultMetricsProvider()
    {
        // Act
        var provider = new DefaultTelemetryProvider(_loggerMock.Object);

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<DefaultMetricsProvider>(provider.MetricsProvider);
    }

    [Fact]
    public void Constructor_WithMetricsProviderOnly_ShouldInitializeCorrectly()
    {
        // Act
        var provider = new DefaultTelemetryProvider(null, _metricsProviderMock.Object);

        // Assert
        Assert.NotNull(provider);
        Assert.Equal(_metricsProviderMock.Object, provider.MetricsProvider);
    }

    [Fact]
    public void Constructor_WithNoParameters_ShouldCreateDefaultMetricsProvider()
    {
        // Act
        var provider = new DefaultTelemetryProvider();

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<DefaultMetricsProvider>(provider.MetricsProvider);
    }
}
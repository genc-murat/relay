using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI.Metrics.Implementations;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.Metrics;

public class DefaultAIMetricsExporterConstructorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DefaultAIMetricsExporter(null!));
    }

    [Fact]
    public void Constructor_WithValidLogger_InitializesSuccessfully()
    {
        // Arrange & Act
        var loggerMock = new Mock<ILogger<DefaultAIMetricsExporter>>();
        using var exporter = new DefaultAIMetricsExporter(loggerMock.Object);

        // Assert - Should not throw
        Assert.NotNull(exporter);
    }

    #endregion
}
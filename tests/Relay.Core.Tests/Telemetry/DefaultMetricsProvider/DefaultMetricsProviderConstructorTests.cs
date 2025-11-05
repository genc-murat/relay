using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Testing;
using Xunit;
using DefaultMetricsProvider = Relay.Core.Telemetry.DefaultMetricsProvider;

namespace Relay.Core.Tests.Telemetry;

public class DefaultMetricsProviderConstructorTests
{
    [Fact]
    public void Constructor_WithNullLogger_ShouldInitializeCorrectly()
    {
        // Act
        var provider = new DefaultMetricsProvider(null);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public void Constructor_WithLogger_ShouldInitializeCorrectly()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<DefaultMetricsProvider>>();

        // Act
        var provider = new DefaultMetricsProvider(loggerMock.Object);

        // Assert
        Assert.NotNull(provider);
    }
}

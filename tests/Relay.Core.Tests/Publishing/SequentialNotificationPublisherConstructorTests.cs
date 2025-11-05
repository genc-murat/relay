using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Publishing.Strategies;
using System;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class SequentialNotificationPublisherConstructorTests
    {
        [Fact]
        public void Constructor_WithDefaults_CreatesInstance()
        {
            // Act
            var publisher = new SequentialNotificationPublisher();

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithNullLogger_CreatesInstance()
        {
            // Act
            var publisher = new SequentialNotificationPublisher(null);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithLogger_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SequentialNotificationPublisher>>();

            // Act
            var publisher = new SequentialNotificationPublisher(mockLogger.Object);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithMockLogger_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SequentialNotificationPublisher>>();
            mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            // Act
            var publisher = new SequentialNotificationPublisher(mockLogger.Object);

            // Assert
            Assert.NotNull(publisher);
        }
    }
}

using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Publishing.Strategies;
using System;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class ParallelNotificationPublisherConstructorTests
    {
        [Fact]
        public void Constructor_WithDefaults_CreatesInstance()
        {
            // Act
            var publisher = new ParallelNotificationPublisher();

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithNullLogger_CreatesInstance()
        {
            // Act
            var publisher = new ParallelNotificationPublisher(null);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithLogger_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ParallelNotificationPublisher>>();

            // Act
            var publisher = new ParallelNotificationPublisher(mockLogger.Object);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithMockLogger_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ParallelNotificationPublisher>>();
            mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            // Act
            var publisher = new ParallelNotificationPublisher(mockLogger.Object);

            // Assert
            Assert.NotNull(publisher);
        }
    }
}
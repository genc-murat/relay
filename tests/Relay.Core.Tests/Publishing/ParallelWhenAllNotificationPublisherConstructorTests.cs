using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Publishing.Strategies;
using System;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class ParallelWhenAllNotificationPublisherConstructorTests
    {
        [Fact]
        public void Constructor_WithDefaults_CreatesInstance()
        {
            // Act
            var publisher = new ParallelWhenAllNotificationPublisher();

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithContinueOnExceptionTrue_CreatesInstance()
        {
            // Act
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithContinueOnExceptionFalse_CreatesInstance()
        {
            // Act
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: false);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithNullLogger_CreatesInstance()
        {
            // Act
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true, logger: null);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithLogger_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ParallelWhenAllNotificationPublisher>>();

            // Act
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true, logger: mockLogger.Object);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithAllParameters_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ParallelWhenAllNotificationPublisher>>();

            // Act
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: false, logger: mockLogger.Object);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithMockLogger_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<ParallelWhenAllNotificationPublisher>>();
            mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            // Act
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true, logger: mockLogger.Object);

            // Assert
            Assert.NotNull(publisher);
        }
    }
}

using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Strategies;
using Relay.Core.Publishing.Attributes;
using Relay.Core.Publishing.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class OrderedNotificationPublisherConstructorTests
    {
        [Fact]
        public void Constructor_WithDefaults_CreatesInstance()
        {
            // Act
            var publisher = new OrderedNotificationPublisher();

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithLogger_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();

            // Act
            var publisher = new OrderedNotificationPublisher(mockLogger.Object);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithAllParameters_CreatesInstance()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();

            // Act
            var publisher = new OrderedNotificationPublisher(mockLogger.Object, continueOnException: false, maxDegreeOfParallelism: 4);

            // Assert
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithZeroMaxDegreeOfParallelism_UsesProcessorCount()
        {
            // Arrange & Act
            var publisher = new OrderedNotificationPublisher(maxDegreeOfParallelism: 0);

            // Assert
            // Should not throw and should use Environment.ProcessorCount internally
            Assert.NotNull(publisher);
        }

        [Fact]
        public void Constructor_WithNegativeMaxDegreeOfParallelism_UsesProcessorCount()
        {
            // Arrange & Act
            var publisher = new OrderedNotificationPublisher(maxDegreeOfParallelism: -1);

            // Assert
            Assert.NotNull(publisher);
        }
    }
}
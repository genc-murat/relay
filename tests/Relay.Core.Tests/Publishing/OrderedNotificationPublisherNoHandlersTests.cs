using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Strategies;
using Relay.Core.Publishing.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class OrderedNotificationPublisherNoHandlersTests
    {
        public record TestNotification(string Message) : INotification;

        [Fact]
        public async Task PublishAsync_WithNoHandlers_CompletesSuccessfully()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<OrderedNotificationPublisher>>();
            var publisher = new OrderedNotificationPublisher(mockLogger.Object);
            var handlers = new List<INotificationHandler<TestNotification>>();
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert - verify debug log was called
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No handlers registered")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}

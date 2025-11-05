using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Strategies;
using Relay.Core.Publishing.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class ParallelWhenAllNotificationPublisherNullArgumentTests
    {
        public record TestNotification(string Message) : INotification;

        [Fact]
        public async Task PublishAsync_WithNullNotification_ThrowsArgumentNullException()
        {
            // Arrange
            var publisher = new ParallelWhenAllNotificationPublisher();
            var handlers = new List<INotificationHandler<TestNotification>>();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                publisher.PublishAsync<TestNotification>(null!, handlers, CancellationToken.None).AsTask());
        }

        [Fact]
        public async Task PublishAsync_WithNullHandlers_ThrowsArgumentNullException()
        {
            // Arrange
            var publisher = new ParallelWhenAllNotificationPublisher();
            var notification = new TestNotification("test");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                publisher.PublishAsync(notification, null!, CancellationToken.None).AsTask());
        }
    }
}

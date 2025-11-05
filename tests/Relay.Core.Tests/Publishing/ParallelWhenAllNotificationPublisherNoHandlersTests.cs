using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Publishing.Strategies;
using Relay.Core.Publishing.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    public class ParallelWhenAllNotificationPublisherNoHandlersTests
    {
        public record TestNotification(string Message) : INotification;

        [Fact]
        public async Task PublishAsync_WithNoHandlers_CompletesSuccessfully()
        {
            // Arrange
            var testLogger = new TestLogger<ParallelWhenAllNotificationPublisher>();
            var publisher = new ParallelWhenAllNotificationPublisher(continueOnException: true, testLogger);
            var handlers = new List<INotificationHandler<TestNotification>>();
            var notification = new TestNotification("test");

            // Act
            await publisher.PublishAsync(notification, handlers, CancellationToken.None);

            // Assert - verify debug log was called
            Assert.Contains(testLogger.LoggedMessages, msg =>
                msg.LogLevel == LogLevel.Debug &&
                msg.Message.Contains("No handlers registered"));
        }
    }
}

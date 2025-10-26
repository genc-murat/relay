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
    public class ParallelWhenAllNotificationPublisherCancellationTests
    {
        public record TestNotification(string Message) : INotification;

        [Fact]
        public async Task PublishAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var publisher = new ParallelWhenAllNotificationPublisher();
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestCancellationHandler(ct => { /* Handler should not execute */ })
            };
            var notification = new TestNotification("test");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                publisher.PublishAsync(notification, handlers, cts.Token).AsTask());
        }

        private class TestCancellationHandler : INotificationHandler<TestNotification>
        {
            private readonly Action<CancellationToken> _action;

            public TestCancellationHandler(Action<CancellationToken> action)
            {
                _action = action;
            }

            public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
            {
                _action(cancellationToken);
                return ValueTask.CompletedTask;
            }
        }
    }
}
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
    public class ParallelNotificationPublisherCancellationTests
    {
        public record TestNotification(string Message) : INotification;

        [Fact]
        public async Task PublishAsync_WithCancellationToken_PropagatesToken()
        {
            // Arrange
            var publisher = new ParallelNotificationPublisher();
            var tokenPassed = false;
            var handlers = new INotificationHandler<TestNotification>[]
            {
                new TestCancellationHandler(ct =>
                {
                    tokenPassed = ct.IsCancellationRequested;
                })
            };
            var notification = new TestNotification("test");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            await publisher.PublishAsync(notification, handlers, cts.Token);

            // Assert
            Assert.True(tokenPassed);
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

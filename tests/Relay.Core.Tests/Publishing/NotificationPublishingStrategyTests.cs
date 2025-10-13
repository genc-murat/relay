using System;
using Relay.Core.Publishing.Interfaces;
using Relay.Core.Publishing.Options;
using Relay.Core.Publishing.Strategies;
using Xunit;

namespace Relay.Core.Tests.Publishing
{
    /// <summary>
    /// Tests for NotificationPublishingStrategy static class.
    /// </summary>
    public class NotificationPublishingStrategyTests
    {
        [Fact]
        public void Sequential_Should_Return_SequentialNotificationPublisher_Type()
        {
            // Act
            var result = NotificationPublishingStrategy.Sequential;

            // Assert
            Assert.Equal(typeof(SequentialNotificationPublisher), result);
        }

        [Fact]
        public void Parallel_Should_Return_ParallelNotificationPublisher_Type()
        {
            // Act
            var result = NotificationPublishingStrategy.Parallel;

            // Assert
            Assert.Equal(typeof(ParallelNotificationPublisher), result);
        }

        [Fact]
        public void ParallelWhenAll_Should_Return_ParallelWhenAllNotificationPublisher_Type()
        {
            // Act
            var result = NotificationPublishingStrategy.ParallelWhenAll;

            // Assert
            Assert.Equal(typeof(ParallelWhenAllNotificationPublisher), result);
        }

        [Fact]
        public void All_Strategies_Should_Return_Valid_Types()
        {
            // Act
            var sequential = NotificationPublishingStrategy.Sequential;
            var parallel = NotificationPublishingStrategy.Parallel;
            var parallelWhenAll = NotificationPublishingStrategy.ParallelWhenAll;

            // Assert
            Assert.NotNull(sequential);
            Assert.NotNull(parallel);
            Assert.NotNull(parallelWhenAll);

            Assert.True(typeof(INotificationPublisher).IsAssignableFrom(sequential));
            Assert.True(typeof(INotificationPublisher).IsAssignableFrom(parallel));
            Assert.True(typeof(INotificationPublisher).IsAssignableFrom(parallelWhenAll));
        }
    }
}
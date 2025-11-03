using System;
using Relay.Core.Transactions;
using Xunit;

namespace Relay.Core.Tests.Transactions
{
    public class RetryStrategyTests
    {
        [Fact]
        public void LinearRetryStrategy_Should_Use_Fixed_Delay()
        {
            var strategy = new LinearRetryStrategy();
            var baseDelay = TimeSpan.FromMilliseconds(100);

            var delay1 = strategy.CalculateDelay(1, baseDelay);
            var delay2 = strategy.CalculateDelay(2, baseDelay);
            var delay3 = strategy.CalculateDelay(3, baseDelay);

            Assert.Equal(baseDelay, delay1);
            Assert.Equal(baseDelay, delay2);
            Assert.Equal(baseDelay, delay3);
        }

        [Fact]
        public void LinearRetryStrategy_Should_Throw_On_Invalid_RetryAttempt()
        {
            var strategy = new LinearRetryStrategy();

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                strategy.CalculateDelay(0, TimeSpan.FromMilliseconds(100)));
        }

        [Fact]
        public void ExponentialBackoffRetryStrategy_Should_Increase_Delay()
        {
            var strategy = new ExponentialBackoffRetryStrategy();
            var baseDelay = TimeSpan.FromMilliseconds(100);

            var delay1 = strategy.CalculateDelay(1, baseDelay);
            var delay2 = strategy.CalculateDelay(2, baseDelay);
            var delay3 = strategy.CalculateDelay(3, baseDelay);

            Assert.Equal(TimeSpan.FromMilliseconds(100), delay1);
            Assert.Equal(TimeSpan.FromMilliseconds(200), delay2);
            Assert.Equal(TimeSpan.FromMilliseconds(400), delay3);
        }

        [Fact]
        public void ExponentialBackoffRetryStrategy_Should_Cap_At_MaxDelay()
        {
            var maxDelay = TimeSpan.FromSeconds(1);
            var strategy = new ExponentialBackoffRetryStrategy(maxDelay);
            var baseDelay = TimeSpan.FromMilliseconds(100);

            var delay10 = strategy.CalculateDelay(10, baseDelay);

            Assert.True(delay10 <= maxDelay);
        }

        [Fact]
        public void ExponentialBackoffRetryStrategy_Should_Throw_On_Invalid_RetryAttempt()
        {
            var strategy = new ExponentialBackoffRetryStrategy();

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                strategy.CalculateDelay(0, TimeSpan.FromMilliseconds(100)));
        }

        [Fact]
        public void ExponentialBackoffRetryStrategy_Should_Throw_On_Negative_MaxDelay()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                new ExponentialBackoffRetryStrategy(TimeSpan.FromSeconds(-1)));
        }

        [Fact]
        public void LinearRetryStrategy_Should_Throw_On_Negative_BaseDelay()
        {
            var strategy = new LinearRetryStrategy();

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                strategy.CalculateDelay(1, TimeSpan.FromMilliseconds(-100)));
        }

        [Fact]
        public void ExponentialBackoffRetryStrategy_Should_Throw_On_Negative_BaseDelay()
        {
            var strategy = new ExponentialBackoffRetryStrategy();

            Assert.Throws<ArgumentOutOfRangeException>(() => 
                strategy.CalculateDelay(1, TimeSpan.FromMilliseconds(-100)));
        }

        [Fact]
        public void ExponentialBackoffRetryStrategy_Should_Use_Default_MaxDelay()
        {
            var strategy = new ExponentialBackoffRetryStrategy();
            var baseDelay = TimeSpan.FromMilliseconds(100);

            // With default max delay of 30 seconds, attempt 20 should be capped
            var delay20 = strategy.CalculateDelay(20, baseDelay);

            Assert.True(delay20 <= TimeSpan.FromSeconds(30));
        }

        [Fact]
        public void ExponentialBackoffRetryStrategy_Should_Double_Each_Attempt()
        {
            var strategy = new ExponentialBackoffRetryStrategy(TimeSpan.FromHours(1)); // High max to not cap
            var baseDelay = TimeSpan.FromMilliseconds(100);

            var delay1 = strategy.CalculateDelay(1, baseDelay);
            var delay2 = strategy.CalculateDelay(2, baseDelay);
            var delay3 = strategy.CalculateDelay(3, baseDelay);
            var delay4 = strategy.CalculateDelay(4, baseDelay);

            Assert.Equal(TimeSpan.FromMilliseconds(100), delay1);
            Assert.Equal(TimeSpan.FromMilliseconds(200), delay2);
            Assert.Equal(TimeSpan.FromMilliseconds(400), delay3);
            Assert.Equal(TimeSpan.FromMilliseconds(800), delay4);
        }

        [Fact]
        public void LinearRetryStrategy_Should_Always_Return_Same_Delay()
        {
            var strategy = new LinearRetryStrategy();
            var baseDelay = TimeSpan.FromMilliseconds(250);

            for (int i = 1; i <= 10; i++)
            {
                var delay = strategy.CalculateDelay(i, baseDelay);
                Assert.Equal(baseDelay, delay);
            }
        }
    }
}

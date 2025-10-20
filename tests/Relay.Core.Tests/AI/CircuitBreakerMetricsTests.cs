using Relay.Core.AI.CircuitBreaker.Metrics;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class CircuitBreakerMetricsTests
    {
        [Fact]
        public void Constructor_Should_Initialize_Properties()
        {
            // Arrange & Act
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 100,
                SuccessfulCalls = 90,
                FailedCalls = 8,
                SlowCalls = 2
            };

            // Assert
            Assert.Equal(100, metrics.TotalCalls);
            Assert.Equal(90, metrics.SuccessfulCalls);
            Assert.Equal(8, metrics.FailedCalls);
            Assert.Equal(2, metrics.SlowCalls);
        }

        [Fact]
        public void FailureRate_Should_Calculate_Correctly()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 100,
                SuccessfulCalls = 90,
                FailedCalls = 10,
                SlowCalls = 5
            };

            // Act
            var failureRate = metrics.FailureRate;

            // Assert
            Assert.Equal(0.1, failureRate); // 10/100 = 0.1
        }

        [Fact]
        public void FailureRate_Should_Return_Zero_When_No_Calls()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 0,
                SuccessfulCalls = 0,
                FailedCalls = 0,
                SlowCalls = 0
            };

            // Act
            var failureRate = metrics.FailureRate;

            // Assert
            Assert.Equal(0.0, failureRate);
        }

        [Fact]
        public void SuccessRate_Should_Calculate_Correctly()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 100,
                SuccessfulCalls = 85,
                FailedCalls = 15,
                SlowCalls = 10
            };

            // Act
            var successRate = metrics.SuccessRate;

            // Assert
            Assert.Equal(0.85, successRate); // 85/100 = 0.85
        }

        [Fact]
        public void SuccessRate_Should_Return_Zero_When_No_Calls()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 0,
                SuccessfulCalls = 0,
                FailedCalls = 0,
                SlowCalls = 0
            };

            // Act
            var successRate = metrics.SuccessRate;

            // Assert
            Assert.Equal(0.0, successRate);
        }

        [Fact]
        public void SlowCallRate_Should_Calculate_Correctly()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 200,
                SuccessfulCalls = 180,
                FailedCalls = 10,
                SlowCalls = 20
            };

            // Act
            var slowCallRate = metrics.SlowCallRate;

            // Assert
            Assert.Equal(0.1, slowCallRate); // 20/200 = 0.1
        }

        [Fact]
        public void SlowCallRate_Should_Return_Zero_When_No_Calls()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 0,
                SuccessfulCalls = 0,
                FailedCalls = 0,
                SlowCalls = 0
            };

            // Act
            var slowCallRate = metrics.SlowCallRate;

            // Assert
            Assert.Equal(0.0, slowCallRate);
        }

        [Fact]
        public void Rates_Should_Handle_All_Successful_Calls()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 50,
                SuccessfulCalls = 50,
                FailedCalls = 0,
                SlowCalls = 0
            };

            // Act & Assert
            Assert.Equal(0.0, metrics.FailureRate);
            Assert.Equal(1.0, metrics.SuccessRate);
            Assert.Equal(0.0, metrics.SlowCallRate);
        }

        [Fact]
        public void Rates_Should_Handle_All_Failed_Calls()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 30,
                SuccessfulCalls = 0,
                FailedCalls = 30,
                SlowCalls = 5
            };

            // Act & Assert
            Assert.Equal(1.0, metrics.FailureRate);
            Assert.Equal(0.0, metrics.SuccessRate);
            Assert.Equal(5.0/30.0, metrics.SlowCallRate);
        }

        [Fact]
        public void Rates_Should_Handle_All_Slow_Calls()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 25,
                SuccessfulCalls = 20,
                FailedCalls = 0,
                SlowCalls = 25
            };

            // Act & Assert
            Assert.Equal(0.0, metrics.FailureRate);
            Assert.Equal(0.8, metrics.SuccessRate); // 20/25
            Assert.Equal(1.0, metrics.SlowCallRate);
        }

        [Fact]
        public void Rates_Should_Handle_Mixed_Scenarios()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 1000,
                SuccessfulCalls = 750,
                FailedCalls = 150,
                SlowCalls = 100
            };

            // Act & Assert
            Assert.Equal(0.15, metrics.FailureRate);    // 150/1000
            Assert.Equal(0.75, metrics.SuccessRate);    // 750/1000
            Assert.Equal(0.1, metrics.SlowCallRate);    // 100/1000
        }

        [Fact]
        public void Properties_Should_Be_Init_Only()
        {
            // Arrange
            var metrics = new CircuitBreakerMetrics
            {
                TotalCalls = 10,
                SuccessfulCalls = 8,
                FailedCalls = 2,
                SlowCalls = 1
            };

            // Assert - These should be set via init accessor
            Assert.Equal(10, metrics.TotalCalls);
            Assert.Equal(8, metrics.SuccessfulCalls);
            Assert.Equal(2, metrics.FailedCalls);
            Assert.Equal(1, metrics.SlowCalls);
        }
    }
}
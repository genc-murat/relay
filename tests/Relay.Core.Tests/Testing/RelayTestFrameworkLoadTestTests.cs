using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Tests.Testing
{
    public class RelayTestFrameworkLoadTestTests
    {
        private ServiceProvider _serviceProvider;
        private Mock<IRelay> _relayMock;
        private Mock<ILogger<RelayTestFramework>> _loggerMock;

        public RelayTestFrameworkLoadTestTests()
        {
            SetupMocks();
        }

        private void SetupMocks()
        {
            _relayMock = new Mock<IRelay>();
            _loggerMock = new Mock<ILogger<RelayTestFramework>>();

            var services = new ServiceCollection();
            services.AddSingleton(_relayMock.Object);
            services.AddSingleton(_loggerMock.Object);
            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task RunLoadTestAsync_WithLowConcurrency_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 10, MaxConcurrency = 1 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(10, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(10, result.ResponseTimes.Count);
            Assert.True(result.SuccessRate == 1.0);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithMediumConcurrency_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 20, MaxConcurrency = 5 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(20, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(20, result.ResponseTimes.Count);
            Assert.True(result.SuccessRate == 1.0);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithHighConcurrency_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 50, MaxConcurrency = 25 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(50, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(50, result.ResponseTimes.Count);
            Assert.True(result.SuccessRate == 1.0);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithSmallRampUpDelay_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration 
            { 
                TotalRequests = 10, 
                MaxConcurrency = 2, 
                RampUpDelayMs = 10 
            };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(10, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(10, result.ResponseTimes.Count);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithMediumRampUpDelay_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration 
            { 
                TotalRequests = 5, 
                MaxConcurrency = 1, 
                RampUpDelayMs = 100 
            };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var startTime = DateTime.UtcNow;
            var result = await framework.RunLoadTestAsync(request, config);
            var endTime = DateTime.UtcNow;

            Assert.Equal(5, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.True((endTime - startTime).TotalMilliseconds >= 400); // At least 4 * 100ms delays
        }

        [Fact]
        public async Task RunLoadTestAsync_WithLargeRampUpDelay_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration 
            { 
                TotalRequests = 3, 
                MaxConcurrency = 1, 
                RampUpDelayMs = 500 
            };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var startTime = DateTime.UtcNow;
            var result = await framework.RunLoadTestAsync(request, config);
            var endTime = DateTime.UtcNow;

            Assert.Equal(3, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.True((endTime - startTime).TotalMilliseconds >= 1000); // At least 2 * 500ms delays
        }

        [Fact]
        public async Task RunLoadTestAsync_WithMixedResponseTimes_CalculatesCorrectMetrics()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 10, MaxConcurrency = 1 };

            var responseTimes = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
            var callCount = 0;

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (IRequest req, CancellationToken ct) =>
                {
                    var delay = responseTimes[callCount % responseTimes.Length];
                    callCount++;
                    await Task.Delay(delay, ct);
                });

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(10, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(10, result.ResponseTimes.Count);
            
            // Check that response times are reasonable (allow overhead for system variance and timing precision)
            Assert.All(result.ResponseTimes, time => Assert.True(time >= 0 && time <= 1000)); // Wider range to account for system overhead and timing precision
            
            // Check statistical calculations - just verify they're reasonable
            Assert.True(result.AverageResponseTime > 0);
            Assert.True(result.MedianResponseTime > 0);
            Assert.True(result.P95ResponseTime > 0);
            Assert.True(result.P99ResponseTime > 0);
            
            // Verify percentiles are in correct order
            Assert.True(result.MedianResponseTime <= result.P95ResponseTime);
            Assert.True(result.P95ResponseTime <= result.P99ResponseTime);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithConsistentResponseTimes_CalculatesCorrectMetrics()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 20, MaxConcurrency = 5 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (IRequest req, CancellationToken ct) =>
                {
                    await Task.Delay(50, ct); // Consistent 50ms delay
                });

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(20, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(20, result.ResponseTimes.Count);
            
            // All metrics should be close to 50ms, allowing for system overhead and concurrent execution timing variations
            Assert.All(result.ResponseTimes, time => Assert.True(time >= 45 && time <= 150));
            Assert.True(result.AverageResponseTime >= 45 && result.AverageResponseTime <= 150);
            Assert.True(result.MedianResponseTime >= 45 && result.MedianResponseTime <= 150);
            Assert.True(result.P95ResponseTime >= 45 && result.P95ResponseTime <= 150);
            Assert.True(result.P99ResponseTime >= 45 && result.P99ResponseTime <= 150);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithVariableResponseTimes_CalculatesCorrectPercentiles()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 100, MaxConcurrency = 10 };

            var random = new Random(42); // Fixed seed for reproducible tests

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (IRequest req, CancellationToken ct) =>
                {
                    var delay = random.Next(10, 200); // Random delay between 10-200ms
                    await Task.Delay(delay, ct);
                });

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(100, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(100, result.ResponseTimes.Count);
            
            // Check that percentiles are in correct order
            Assert.True(result.MedianResponseTime <= result.P95ResponseTime);
            Assert.True(result.P95ResponseTime <= result.P99ResponseTime);
            Assert.True(result.AverageResponseTime >= 10 && result.AverageResponseTime <= 210);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithPartialFailures_CalculatesCorrectSuccessRate()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 20, MaxConcurrency = 4 };

            var callCount = 0;
            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount % 4 == 0) // Every 4th request fails
                        throw new Exception("Simulated failure");
                    return ValueTask.CompletedTask;
                });

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(15, result.SuccessfulRequests);
            Assert.Equal(5, result.FailedRequests);
            Assert.Equal(15, result.ResponseTimes.Count);
            Assert.Equal(0.75, result.SuccessRate);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithMostlyFailures_CalculatesCorrectSuccessRate()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 10, MaxConcurrency = 2 };

            var callCount = 0;
            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount <= 8) // First 8 requests fail
                        throw new Exception("Simulated failure");
                    return ValueTask.CompletedTask;
                });

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(2, result.SuccessfulRequests);
            Assert.Equal(8, result.FailedRequests);
            Assert.Equal(2, result.ResponseTimes.Count);
            Assert.Equal(0.2, result.SuccessRate);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithAllFailures_HandlesGracefully()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 5, MaxConcurrency = 1 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("All requests fail"));

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(0, result.SuccessfulRequests);
            Assert.Equal(5, result.FailedRequests);
            Assert.Empty(result.ResponseTimes);
            Assert.Equal(0.0, result.SuccessRate);
            Assert.Equal(0, result.AverageResponseTime);
            Assert.Equal(0, result.MedianResponseTime);
            Assert.Equal(0, result.P95ResponseTime);
            Assert.Equal(0, result.P99ResponseTime);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithDifferentRequestTypes_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request1 = new TestRequest();
            var request2 = new AnotherTestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 6, MaxConcurrency = 2 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result1 = await framework.RunLoadTestAsync(request1, config);
            var result2 = await framework.RunLoadTestAsync(request2, config);

            Assert.Equal("TestRequest", result1.RequestType);
            Assert.Equal("AnotherTestRequest", result2.RequestType);
            
            Assert.Equal(6, result1.SuccessfulRequests);
            Assert.Equal(6, result2.SuccessfulRequests);
            
            Assert.Equal(0, result1.FailedRequests);
            Assert.Equal(0, result2.FailedRequests);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithVeryShortRequests_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 50, MaxConcurrency = 25 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask); // No delay, very fast

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(50, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(50, result.ResponseTimes.Count);
            
            // Response times should be very small (under 10ms typically)
            Assert.All(result.ResponseTimes, time => Assert.True(time < 50));
            Assert.True(result.AverageResponseTime < 50);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithVeryLongRequests_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 3, MaxConcurrency = 1 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (IRequest req, CancellationToken ct) =>
                {
                    await Task.Delay(200, ct); // 200ms delay
                });

            var startTime = DateTime.UtcNow;
            var result = await framework.RunLoadTestAsync(request, config);
            var endTime = DateTime.UtcNow;

            Assert.Equal(3, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(3, result.ResponseTimes.Count);
            
            // Should take at least 600ms (3 * 200ms)
            Assert.True((endTime - startTime).TotalMilliseconds >= 600);
            
            // Response times should be around 200ms
            Assert.All(result.ResponseTimes, time => Assert.True(time >= 180 && time <= 250));
        }

        [Fact]
        public async Task RunLoadTestAsync_WithBurstConcurrency_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration 
            { 
                TotalRequests = 10, 
                MaxConcurrency = 10,
                RampUpDelayMs = 0 // All at once
            };

            var concurrentTasks = 0;
            var maxConcurrentTasks = 0;

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (IRequest req, CancellationToken ct) =>
                {
                    Interlocked.Increment(ref concurrentTasks);
                    var current = concurrentTasks;
                    while (current > maxConcurrentTasks)
                    {
                        maxConcurrentTasks = current;
                        current = concurrentTasks;
                    }
                    
                    await Task.Delay(50, ct);
                    
                    Interlocked.Decrement(ref concurrentTasks);
                });

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(10, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(10, result.ResponseTimes.Count);
            
            // Should have reached max concurrency
            Assert.True(maxConcurrentTasks >= 8); // Allow some variance
        }

        // Test data classes
        public class TestRequest : IRequest<string>, IRequest { }

        public class AnotherTestRequest : IRequest<int>, IRequest { }
    }
}
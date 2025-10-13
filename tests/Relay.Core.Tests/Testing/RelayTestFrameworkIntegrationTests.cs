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
    public class RelayTestFrameworkIntegrationTests
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IRelay> _relayMock;
        private readonly Mock<ILogger<RelayTestFramework>> _loggerMock;

        public RelayTestFrameworkIntegrationTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _relayMock = new Mock<IRelay>();
            _loggerMock = new Mock<ILogger<RelayTestFramework>>();

            _serviceProviderMock
                .Setup(x => x.GetRequiredService(typeof(IRelay)))
                .Returns(_relayMock.Object);

            _serviceProviderMock
                .Setup(x => x.GetService(typeof(ILogger<RelayTestFramework>)))
                .Returns(_loggerMock.Object);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithSuccessfulScenario_ReturnsSuccessResult()
        {
            var framework = new RelayTestFramework(_serviceProviderMock.Object);
            var request = new TestRequest();
            var notification = new TestNotification();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult("response"));

            _relayMock
                .Setup(x => x.PublishAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            framework.Scenario("Integration Test")
                .SendRequest(request, "Send Request")
                .PublishNotification(notification, "Publish Notification")
                .Verify(() => Task.FromResult(true), "Verify Success")
                .Wait(TimeSpan.FromMilliseconds(100), "Wait");

            var result = await framework.RunAllScenariosAsync();

            Assert.NotNull(result);
            Assert.Single(result.ScenarioResults);
            Assert.True(result.Success);
            Assert.True(result.ScenarioResults[0].Success);
            Assert.Equal(4, result.ScenarioResults[0].StepResults.Count);
            Assert.All(result.ScenarioResults[0].StepResults, step => Assert.True(step.Success));
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithFailingVerification_ReturnsFailureResult()
        {
            var framework = new RelayTestFramework(_serviceProviderMock.Object);
            var request = new TestRequest();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("response");

            framework.Scenario("Failing Test")
                .SendRequest(request, "Send Request")
                .Verify(() => Task.FromResult(false), "Verify Failure");

            var result = await framework.RunAllScenariosAsync();

            Assert.NotNull(result);
            Assert.Single(result.ScenarioResults);
            Assert.False(result.Success);
            Assert.False(result.ScenarioResults[0].Success);
            Assert.Equal(2, result.ScenarioResults[0].StepResults.Count);
            Assert.True(result.ScenarioResults[0].StepResults[0].Success); // SendRequest succeeds
            Assert.False(result.ScenarioResults[0].StepResults[1].Success); // Verify fails
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithExceptionInStep_RecordsError()
        {
            var framework = new RelayTestFramework(_serviceProviderMock.Object);
            var request = new TestRequest();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test error"));

            framework.Scenario("Exception Test")
                .SendRequest(request, "Send Request")
                .Verify(() => Task.FromResult(true), "Verify");

            var result = await framework.RunAllScenariosAsync();

            Assert.NotNull(result);
            Assert.Single(result.ScenarioResults);
            Assert.False(result.Success);
            Assert.False(result.ScenarioResults[0].Success);
            Assert.Single(result.ScenarioResults[0].StepResults); // Only first step executed
            Assert.False(result.ScenarioResults[0].StepResults[0].Success);
            Assert.Contains("Test error", result.ScenarioResults[0].StepResults[0].Error);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithMixedSuccessAndFailure_CalculatesCorrectMetrics()
        {
            var framework = new RelayTestFramework(_serviceProviderMock.Object);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 10, MaxConcurrency = 3 };

            // Setup alternating success/failure
            var callCount = 0;
            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    if (callCount % 2 == 0)
                        return ValueTask.FromResult("response");
                    else
                        throw new Exception("Simulated failure");
                });

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(10, result.SuccessfulRequests + result.FailedRequests);
            Assert.True(result.SuccessRate >= 0.4 && result.SuccessRate <= 0.6); // Roughly 50%
            Assert.Equal(result.ResponseTimes.Count, result.SuccessfulRequests);
            Assert.True(result.AverageResponseTime >= 0);
            Assert.True(result.MedianResponseTime >= 0);
            Assert.True(result.P95ResponseTime >= 0);
            Assert.True(result.P99ResponseTime >= 0);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithRampUpDelay_ExecutesWithDelays()
        {
            var framework = new RelayTestFramework(_serviceProviderMock.Object);
            var request = new TestRequest();
            var config = new LoadTestConfiguration
            {
                TotalRequests = 3,
                MaxConcurrency = 1,
                RampUpDelayMs = 50
            };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult("response"));

            var startTime = DateTime.UtcNow;
            var result = await framework.RunLoadTestAsync(request, config);
            var endTime = DateTime.UtcNow;

            // Should take at least (3-1) * 50ms = 100ms due to ramp-up delays
            var actualDuration = endTime - startTime;
            Assert.True(actualDuration.TotalMilliseconds >= 100);

            Assert.Equal(3, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
        }

        [Fact]
        public async Task ComplexScenario_WithMultipleSteps_ExecutesInOrder()
        {
            var framework = new RelayTestFramework(_serviceProviderMock.Object);
            var request1 = new TestRequest();
            var request2 = new TestRequest();
            var notification = new TestNotification();

            var executionOrder = new List<string>();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult("response"))
                .Callback(() => executionOrder.Add("SendAsync"));

            _relayMock
                .Setup(x => x.PublishAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask)
                .Callback(() => executionOrder.Add("PublishAsync"));

            framework.Scenario("Complex Scenario")
                .SendRequest(request1, "First Request")
                .SendRequest(request2, "Second Request")
                .PublishNotification(notification, "Notification")
                .Verify(() =>
                {
                    executionOrder.Add("Verify");
                    return Task.FromResult(true);
                }, "Verification")
                .Wait(TimeSpan.FromMilliseconds(10), "Wait");

            var result = await framework.RunAllScenariosAsync();

            Assert.True(result.Success);
            Assert.Equal(new[] { "SendAsync", "SendAsync", "PublishAsync", "Verify" }, executionOrder);
        }

        [Fact]
        public async Task LoadTestResult_CalculatesPercentilesCorrectly()
        {
            var framework = new RelayTestFramework(_serviceProviderMock.Object);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 1, MaxConcurrency = 1 };

            // Setup a request that takes some time
            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                {
                    await Task.Delay(10); // Simulate some processing time
                    return "response";
                });

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Single(result.ResponseTimes);
            Assert.True(result.ResponseTimes[0] >= 10); // Should be at least 10ms
            Assert.Equal(result.ResponseTimes[0], result.MedianResponseTime);
            Assert.Equal(result.ResponseTimes[0], result.P95ResponseTime);
            Assert.Equal(result.ResponseTimes[0], result.P99ResponseTime);
        }

        [Fact]
        public async Task Scenario_WithCancellation_HandlesCancellationGracefully()
        {
            var framework = new RelayTestFramework(_serviceProviderMock.Object);
            var request = new TestRequest();
            var cts = new CancellationTokenSource();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .Returns(async (IRequest<string> r, CancellationToken ct) =>
                {
                    await Task.Delay(100, ct); // Long running operation
                    return "response";
                });

            framework.Scenario("Cancellation Test")
                .SendRequest(request, "Send Request");

            // Cancel after a short delay
            cts.CancelAfter(50);

            var result = await framework.RunAllScenariosAsync(cts.Token);

            Assert.False(result.Success);
            Assert.Contains(result.ScenarioResults[0].StepResults,
                step => step.Error?.Contains("cancelled") == true ||
                       step.Error?.Contains("Cancellation") == true);
        }

        // Test data classes
        public class TestRequest : IRequest<string>, IRequest { }

        public class TestNotification : INotification { }
    }
}
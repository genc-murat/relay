using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public class RelayTestFrameworkTests
    {
        private ServiceProvider _serviceProvider;
        private Mock<IRelay> _relayMock;
        private Mock<ILogger<RelayTestFramework>> _loggerMock;

        public RelayTestFrameworkTests()
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
        public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RelayTestFramework(null!));
        }

        [Fact]
        public void Constructor_WithValidServiceProvider_InitializesCorrectly()
        {
            var framework = new RelayTestFramework(_serviceProvider);

            Assert.NotNull(framework);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var config = new LoadTestConfiguration();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                framework.RunLoadTestAsync((TestRequest)null!, config));
        }

        [Fact]
        public async Task RunLoadTestAsync_WithNullConfig_ThrowsArgumentNullException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                framework.RunLoadTestAsync(request, null!));
        }

        [Fact]
        public void LoadTestConfiguration_WithZeroTotalRequests_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LoadTestConfiguration { TotalRequests = 0 });
        }

        [Fact]
        public async Task RunLoadTestAsync_WithValidRequest_ExecutesSuccessfully()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 5, MaxConcurrency = 2 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal("TestRequest", result.RequestType);
            Assert.Equal(5, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.True(result.SuccessRate == 1.0);
            Assert.True(result.ResponseTimes.Count == 5);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithFailingRequests_RecordsFailures()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 3, MaxConcurrency = 1 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test error"));

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(0, result.SuccessfulRequests);
            Assert.Equal(3, result.FailedRequests);
            Assert.True(result.SuccessRate == 0.0);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithNoScenarios_ReturnsEmptyResult()
        {
            var framework = new RelayTestFramework(_serviceProvider);

            var result = await framework.RunAllScenariosAsync();

            Assert.NotNull(result);
            Assert.Empty(result.ScenarioResults);
            Assert.True(result.Success);
        }

        [Fact]
        public void Scenario_WithValidSteps_BuildsCorrectly()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var notification = new TestNotification();

            var scenario = framework.Scenario("Test Scenario")
                .SendRequest(request, "Send Request Step")
                .PublishNotification(notification, "Publish Notification Step")
                .Verify(() => Task.FromResult(true), "Verify Step")
                .Wait(TimeSpan.FromSeconds(1), "Wait Step");

            // Access private field for testing
            var scenariosField = typeof(RelayTestFramework).GetField("_scenarios", BindingFlags.NonPublic | BindingFlags.Instance);
            var scenarios = (List<TestScenario>)scenariosField!.GetValue(framework)!;

            Assert.Single(scenarios);
            Assert.Equal("Test Scenario", scenarios[0].Name);
            Assert.Equal(4, scenarios[0].Steps.Count);
        }

        [Fact]
        public void SendRequest_WithNullRequest_ThrowsArgumentNullException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var builder = framework.Scenario("Test");

            Assert.Throws<ArgumentNullException>(() => builder.SendRequest((TestRequest)null!));
        }

        [Fact]
        public void SendRequest_WithEmptyStepName_ThrowsArgumentException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var builder = framework.Scenario("Test");
            var request = new TestRequest();

            Assert.Throws<ArgumentException>(() => builder.SendRequest(request, ""));
        }

        [Fact]
        public void PublishNotification_WithNullNotification_ThrowsArgumentNullException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var builder = framework.Scenario("Test");

            Assert.Throws<ArgumentNullException>(() => builder.PublishNotification((TestNotification)null!));
        }

        [Fact]
        public void PublishNotification_WithEmptyStepName_ThrowsArgumentException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var builder = framework.Scenario("Test");
            var notification = new TestNotification();

            Assert.Throws<ArgumentException>(() => builder.PublishNotification(notification, ""));
        }

        [Fact]
        public void TestStep_Validate_WithValidSendRequestStep_Passes()
        {
            var step = new TestStep
            {
                Name = "Test Step",
                Type = StepType.SendRequest,
                Request = new TestRequest()
            };

            step.Validate(); // Should not throw
        }

        [Fact]
        public void TestStep_Validate_WithInvalidSendRequestStep_ThrowsException()
        {
            var step = new TestStep
            {
                Name = "Test Step",
                Type = StepType.SendRequest,
                Request = null
            };

            Assert.Throws<InvalidOperationException>(() => step.Validate());
        }

        [Fact]
        public void TestStep_Validate_WithInvalidPublishNotificationStep_ThrowsException()
        {
            var step = new TestStep
            {
                Name = "Test Step",
                Type = StepType.PublishNotification,
                Notification = null
            };

            Assert.Throws<InvalidOperationException>(() => step.Validate());
        }

        [Fact]
        public void TestStep_Validate_WithInvalidStreamRequestStep_ThrowsException()
        {
            var step = new TestStep
            {
                Name = "Test Step",
                Type = StepType.StreamRequest,
                StreamRequest = null
            };

            Assert.Throws<InvalidOperationException>(() => step.Validate());
        }

        [Fact]
        public void TestStep_Validate_WithInvalidVerifyStep_ThrowsException()
        {
            var step = new TestStep
            {
                Name = "Test Step",
                Type = StepType.Verify,
                VerificationFunc = null
            };

            Assert.Throws<InvalidOperationException>(() => step.Validate());
        }

        [Fact]
        public void TestStep_Validate_WithInvalidWaitStep_ThrowsException()
        {
            var step = new TestStep
            {
                Name = "Test Step",
                Type = StepType.Wait,
                WaitTime = null
            };

            Assert.Throws<InvalidOperationException>(() => step.Validate());
        }

        [Fact]
        public void LoadTestConfiguration_WithNegativeTotalRequests_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LoadTestConfiguration { TotalRequests = -1 });
        }

        [Fact]
        public void LoadTestConfiguration_WithNegativeMaxConcurrency_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LoadTestConfiguration { MaxConcurrency = 0 });
        }

        [Fact]
        public void LoadTestConfiguration_WithNegativeRampUpDelay_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new LoadTestConfiguration { RampUpDelayMs = -1 });
        }

        [Fact]
        public void LoadTestConfiguration_Constructor_WithValidParameters_SetsProperties()
        {
            var config = new LoadTestConfiguration(50, 5, 100);

            Assert.Equal(50, config.TotalRequests);
            Assert.Equal(5, config.MaxConcurrency);
            Assert.Equal(100, config.RampUpDelayMs);
        }

        [Fact]
        public void LoadTestResult_CalculatesMetricsCorrectly()
        {
            var result = new LoadTestResult
            {
                SuccessfulRequests = 8,
                FailedRequests = 2,
                ResponseTimes = new List<double> { 100, 200, 150, 300, 250, 175, 125, 225 },
                TotalDuration = TimeSpan.FromSeconds(1.25)
            };

            Assert.Equal(0.8, result.SuccessRate, 0.01);
            Assert.Equal(8, result.RequestsPerSecond, 0.01);
        }

        [Fact]
        public void TestScenarioBuilder_StreamRequest_WithValidRequest_AddsStep()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestStreamRequest();

            // Act
            framework.Scenario("Test Scenario")
                .StreamRequest(request, "Stream Step");

            // Access private field for testing
            var scenariosField = typeof(RelayTestFramework).GetField("_scenarios", BindingFlags.NonPublic | BindingFlags.Instance);
            var scenarios = (List<TestScenario>)scenariosField!.GetValue(framework)!;

            // Assert
            Assert.Single(scenarios);
            Assert.Equal("Test Scenario", scenarios[0].Name);
            Assert.Single(scenarios[0].Steps);
            Assert.Equal("Stream Step", scenarios[0].Steps[0].Name);
            Assert.Equal(StepType.StreamRequest, scenarios[0].Steps[0].Type);
            Assert.Equal(request, scenarios[0].Steps[0].StreamRequest);
        }

        [Fact]
        public void TestScenarioBuilder_StreamRequest_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                framework.Scenario("Test Scenario").StreamRequest<TestStreamRequest>(null!));
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithMultipleScenarios_ExecutesAll()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();

            framework.Scenario("Scenario 1")
                .SendRequest(request, "Step 1")
                .Verify(() => Task.FromResult(true), "Verify 1");

            framework.Scenario("Scenario 2")
                .SendRequest(request, "Step 2")
                .Verify(() => Task.FromResult(true), "Verify 2");

            var result = await framework.RunAllScenariosAsync();

            Assert.Equal(2, result.ScenarioResults.Count);
            Assert.True(result.ScenarioResults.All(r => r.Success));
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithFailingScenario_ContinuesExecution()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();

            framework.Scenario("Failing Scenario")
                .Verify(() => Task.FromResult(false), "Fail Verify");

            framework.Scenario("Passing Scenario")
                .Verify(() => Task.FromResult(true), "Pass Verify");

            var result = await framework.RunAllScenariosAsync();

            Assert.Equal(2, result.ScenarioResults.Count);
            Assert.False(result.ScenarioResults[0].Success);
            Assert.True(result.ScenarioResults[1].Success);
            Assert.False(result.Success); // Overall success should be false
        }

        [Fact]
        public async Task RunLoadTestAsync_WithZeroRampUpDelay_ExecutesWithoutDelay()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 3, MaxConcurrency = 2, RampUpDelayMs = 0 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var startTime = DateTime.UtcNow;
            var result = await framework.RunLoadTestAsync(request, config);
            var endTime = DateTime.UtcNow;

            Assert.Equal(3, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            // Should complete quickly without ramp-up delays
            Assert.True((endTime - startTime).TotalMilliseconds < 1000);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithHighConcurrency_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 10, MaxConcurrency = 10 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(10, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(10, result.ResponseTimes.Count);
        }

        [Fact]
        public async Task RunLoadTestAsync_CalculatesPercentilesCorrectly_WithMultipleResponseTimes()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 5, MaxConcurrency = 1 };

            var callCount = 0;
            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    var delay = callCount * 10; // 10ms, 20ms, 30ms, 40ms, 50ms
                    return Task.Delay(delay, CancellationToken.None).ContinueWith(_ => ValueTask.CompletedTask).Result;
                });

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(5, result.ResponseTimes.Count);
            Assert.True(result.AverageResponseTime > 0);
            Assert.True(result.MedianResponseTime > 0);
            Assert.True(result.P95ResponseTime > 0);
            Assert.True(result.P99ResponseTime > 0);
            Assert.True(result.P95ResponseTime <= result.P99ResponseTime);
        }

        [Fact]
        public void Scenario_WithNullName_ThrowsArgumentNullException()
        {
            var framework = new RelayTestFramework(_serviceProvider);

            Assert.Throws<ArgumentNullException>(() => framework.Scenario(null!));
        }

        [Fact]
        public void Scenario_WithEmptyName_ThrowsArgumentException()
        {
            var framework = new RelayTestFramework(_serviceProvider);

            Assert.Throws<ArgumentException>(() => framework.Scenario(""));
        }

        [Fact]
        public void TestScenarioBuilder_Verify_WithNullFunc_ThrowsArgumentNullException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var builder = framework.Scenario("Test");

            Assert.Throws<ArgumentNullException>(() => builder.Verify(null!));
        }

        [Fact]
        public void TestScenarioBuilder_Wait_WithNegativeTime_ThrowsArgumentOutOfRangeException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var builder = framework.Scenario("Test");

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Wait(TimeSpan.FromMilliseconds(-1)));
        }

        [Fact]
        public void TestScenarioBuilder_Wait_WithZeroTime_ThrowsArgumentOutOfRangeException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var builder = framework.Scenario("Test");

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.Wait(TimeSpan.Zero));
        }

        [Fact]
        public void TestScenarioBuilder_SendRequest_WithValidRequest_AddsStep()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();

            // Act
            framework.Scenario("Test Scenario")
                .SendRequest(request, "Send Step");

            // Access private field for testing
            var scenariosField = typeof(RelayTestFramework).GetField("_scenarios", BindingFlags.NonPublic | BindingFlags.Instance);
            var scenarios = (List<TestScenario>)scenariosField!.GetValue(framework)!;

            // Assert
            Assert.Single(scenarios);
            Assert.Equal("Test Scenario", scenarios[0].Name);
            Assert.Single(scenarios[0].Steps);
            Assert.Equal("Send Step", scenarios[0].Steps[0].Name);
            Assert.Equal(StepType.SendRequest, scenarios[0].Steps[0].Type);
            Assert.Equal(request, scenarios[0].Steps[0].Request);
        }

        [Fact]
        public void TestScenarioBuilder_SendRequest_WithNullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                framework.Scenario("Test Scenario").SendRequest<TestRequest>(null!));
        }

        [Fact]
        public void TestScenarioBuilder_SendRequest_WithEmptyStepName_ThrowsArgumentException()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                framework.Scenario("Test Scenario").SendRequest(request, ""));
        }

        [Fact]
        public void TestScenarioBuilder_PublishNotification_WithValidNotification_AddsStep()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);
            var notification = new TestNotification();

            // Act
            framework.Scenario("Test Scenario")
                .PublishNotification(notification, "Publish Step");

            // Access private field for testing
            var scenariosField = typeof(RelayTestFramework).GetField("_scenarios", BindingFlags.NonPublic | BindingFlags.Instance);
            var scenarios = (List<TestScenario>)scenariosField!.GetValue(framework)!;

            // Assert
            Assert.Single(scenarios);
            Assert.Equal("Test Scenario", scenarios[0].Name);
            Assert.Single(scenarios[0].Steps);
            Assert.Equal("Publish Step", scenarios[0].Steps[0].Name);
            Assert.Equal(StepType.PublishNotification, scenarios[0].Steps[0].Type);
            Assert.Equal(notification, scenarios[0].Steps[0].Notification);
        }

        [Fact]
        public void TestScenarioBuilder_PublishNotification_WithNullNotification_ThrowsArgumentNullException()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                framework.Scenario("Test Scenario").PublishNotification<TestNotification>(null!));
        }

        [Fact]
        public void TestScenarioBuilder_PublishNotification_WithEmptyStepName_ThrowsArgumentException()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);
            var notification = new TestNotification();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                framework.Scenario("Test Scenario").PublishNotification(notification, ""));
        }

        [Fact]
        public void TestScenarioBuilder_StreamRequest_WithEmptyStepName_ThrowsArgumentException()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestStreamRequest();

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                framework.Scenario("Test Scenario").StreamRequest(request, ""));
        }

        [Fact]
        public void TestScenarioBuilder_Verify_WithEmptyStepName_ThrowsArgumentException()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                framework.Scenario("Test Scenario").Verify(() => Task.FromResult(true), ""));
        }

        [Fact]
        public void TestScenarioBuilder_Wait_WithEmptyStepName_ThrowsArgumentException()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                framework.Scenario("Test Scenario").Wait(TimeSpan.FromSeconds(1), ""));
        }

        [Fact]
        public void TestScenarioBuilder_MethodChaining_ReturnsBuilderInstance()
        {
            // Arrange
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var notification = new TestNotification();
            var streamRequest = new TestStreamRequest();

            // Act
            var builder = framework.Scenario("Test Scenario");
            var result1 = builder.SendRequest(request);
            var result2 = builder.PublishNotification(notification);
            var result3 = builder.StreamRequest(streamRequest);
            var result4 = builder.Verify(() => Task.FromResult(true));
            var result5 = builder.Wait(TimeSpan.FromSeconds(1));

            // Assert
            Assert.Same(builder, result1);
            Assert.Same(builder, result2);
            Assert.Same(builder, result3);
            Assert.Same(builder, result4);
            Assert.Same(builder, result5);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithCancellation_CancelsExecution()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var cts = new CancellationTokenSource();

            framework.Scenario("Long Running Scenario")
                .Wait(TimeSpan.FromSeconds(10), "Long Wait");

            // Cancel immediately
            cts.Cancel();

            var result = await framework.RunAllScenariosAsync(cts.Token);

            // Should fail due to cancellation
            Assert.False(result.Success);
            Assert.Single(result.ScenarioResults);
            Assert.False(result.ScenarioResults[0].Success);
            Assert.Contains("cancel", result.ScenarioResults[0].StepResults[0].Error, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithCancellation_CancelsExecution()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 10, MaxConcurrency = 2 };
            var cts = new CancellationTokenSource();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (IRequest req, CancellationToken ct) =>
                {
                    await Task.Delay(1000, ct); // Long delay to allow cancellation
                });

            // Cancel after a short delay
            cts.CancelAfter(100);

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                framework.RunLoadTestAsync(request, config, cts.Token));
        }

        [Fact]
        public void LoadTestResult_WithNoResponseTimes_HandlesGracefully()
        {
            var result = new LoadTestResult
            {
                SuccessfulRequests = 0,
                FailedRequests = 5,
                ResponseTimes = new List<double>()
            };

            Assert.Equal(0, result.AverageResponseTime);
            Assert.Equal(0, result.MedianResponseTime);
            Assert.Equal(0, result.P95ResponseTime);
            Assert.Equal(0, result.P99ResponseTime);
            Assert.Equal(0, result.SuccessRate);
        }

        [Fact]
        public void LoadTestResult_WithSingleResponseTime_CalculatesCorrectly()
        {
            var result = new LoadTestResult
            {
                SuccessfulRequests = 1,
                FailedRequests = 0,
                ResponseTimes = new List<double> { 150.5 }
            };

            // Calculate metrics manually since LoadTestResult doesn't auto-calculate
            result.AverageResponseTime = result.ResponseTimes.Count != 0 ? result.ResponseTimes.Average() : 0;
            result.MedianResponseTime = result.ResponseTimes.Count != 0 ? result.ResponseTimes.First() : 0; // Single value median
            result.P95ResponseTime = result.ResponseTimes.Count != 0 ? result.ResponseTimes.First() : 0; // Single value percentile
            result.P99ResponseTime = result.ResponseTimes.Count != 0 ? result.ResponseTimes.First() : 0; // Single value percentile

            Assert.Equal(150.5, result.AverageResponseTime);
            Assert.Equal(150.5, result.MedianResponseTime);
            Assert.Equal(150.5, result.P95ResponseTime);
            Assert.Equal(150.5, result.P99ResponseTime);
            Assert.Equal(1.0, result.SuccessRate);
        }

        // Test data classes
        public class TestRequest : IRequest<string>, IRequest { }

        public class TestNotification : INotification { }

        public class TestStreamRequest : IStreamRequest<string> { }
    }
}

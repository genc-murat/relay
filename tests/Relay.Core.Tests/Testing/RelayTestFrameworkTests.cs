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

        // Test data classes
        public class TestRequest : IRequest<string>, IRequest { }

        public class TestNotification : INotification { }
    }
}
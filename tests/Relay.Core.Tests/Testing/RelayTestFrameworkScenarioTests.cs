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
    public class RelayTestFrameworkScenarioTests
    {
        private ServiceProvider _serviceProvider;
        private Mock<IRelay> _relayMock;
        private Mock<ILogger<RelayTestFramework>> _loggerMock;

        public RelayTestFrameworkScenarioTests()
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
        public async Task RunAllScenariosAsync_WithSendRequestStep_ExecutesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            framework.Scenario("Send Request Scenario")
                .SendRequest(request, "Send Test Request");

            var result = await framework.RunAllScenariosAsync();

            Assert.True(result.Success);
            Assert.Single(result.ScenarioResults);
            Assert.True(result.ScenarioResults[0].Success);
            Assert.Single(result.ScenarioResults[0].StepResults);
            Assert.True(result.ScenarioResults[0].StepResults[0].Success);
            
            _relayMock.Verify(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithPublishNotificationStep_ExecutesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var notification = new TestNotification();

            _relayMock
                .Setup(x => x.PublishAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            framework.Scenario("Publish Notification Scenario")
                .PublishNotification(notification, "Publish Test Notification");

            var result = await framework.RunAllScenariosAsync();

            Assert.True(result.Success);
            Assert.Single(result.ScenarioResults);
            Assert.True(result.ScenarioResults[0].Success);
            Assert.Single(result.ScenarioResults[0].StepResults);
            Assert.True(result.ScenarioResults[0].StepResults[0].Success);
            
            _relayMock.Verify(x => x.PublishAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithStreamRequestStep_ExecutesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var streamRequest = new TestStreamRequest();

            var mockAsyncEnumerable = new Mock<IAsyncEnumerable<string>>();
            var mockEnumerator = new Mock<IAsyncEnumerator<string>>();
            
            mockEnumerator.Setup(x => x.MoveNextAsync()).ReturnsAsync(false); // Empty stream
            // mockEnumerator.Setup(x => x.Current).Returns("test"); // Not used since MoveNextAsync returns false
            mockAsyncEnumerable.Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>())).Returns(mockEnumerator.Object);

            _relayMock
                .Setup(x => x.StreamAsync<string>(It.IsAny<IStreamRequest<string>>(), It.IsAny<CancellationToken>()))
                .Returns(mockAsyncEnumerable.Object);

            framework.Scenario("Stream Request Scenario")
                .StreamRequest(streamRequest, "Stream Test Request");

            var result = await framework.RunAllScenariosAsync();

            Assert.True(result.Success);
            Assert.Single(result.ScenarioResults);
            Assert.True(result.ScenarioResults[0].Success);
            Assert.Single(result.ScenarioResults[0].StepResults);
            Assert.True(result.ScenarioResults[0].StepResults[0].Success);
            Assert.Single(result.ScenarioResults);
            Assert.True(result.ScenarioResults[0].Success);
            Assert.Single(result.ScenarioResults[0].StepResults);
            Assert.True(result.ScenarioResults[0].StepResults[0].Success);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithVerifyStep_ExecutesCorrectly()
        {
            var framework = new RelayTestFramework(_serviceProvider);

            framework.Scenario("Verify Scenario")
                .Verify(() => Task.FromResult(true), "Verify Condition");

            var result = await framework.RunAllScenariosAsync();

            Assert.True(result.Success);
            Assert.Single(result.ScenarioResults);
            Assert.True(result.ScenarioResults[0].Success);
            Assert.Single(result.ScenarioResults[0].StepResults);
            Assert.True(result.ScenarioResults[0].StepResults[0].Success);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithWaitStep_ExecutesCorrectly()
        {
            var framework = new RelayTestFramework(_serviceProvider);

            framework.Scenario("Wait Scenario")
                .Wait(TimeSpan.FromMilliseconds(50), "Wait Step");

            var startTime = DateTime.UtcNow;
            var result = await framework.RunAllScenariosAsync();
            var endTime = DateTime.UtcNow;

            Assert.True(result.Success);
            Assert.Single(result.ScenarioResults);
            Assert.True(result.ScenarioResults[0].Success);
            Assert.Single(result.ScenarioResults[0].StepResults);
            Assert.True(result.ScenarioResults[0].StepResults[0].Success);
            
            // Should have waited at least 50ms
            Assert.True((endTime - startTime).TotalMilliseconds >= 45); // Allow some variance
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithMultipleSteps_ExecutesInOrder()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var notification = new TestNotification();

            var executionOrder = new List<string>();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Callback(() => executionOrder.Add("SendRequest"))
                .Returns(ValueTask.CompletedTask);

            _relayMock
                .Setup(x => x.PublishAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .Callback(() => executionOrder.Add("PublishNotification"))
                .Returns(ValueTask.CompletedTask);

            framework.Scenario("Multi-Step Scenario")
                .SendRequest(request, "Step 1: Send Request")
                .PublishNotification(notification, "Step 2: Publish Notification")
                .Verify(() => 
                {
                    executionOrder.Add("Verify");
                    return Task.FromResult(true);
                }, "Step 3: Verify")
                .Wait(TimeSpan.FromMilliseconds(10), "Step 4: Wait");

            var result = await framework.RunAllScenariosAsync();

            Assert.True(result.Success);
            Assert.Single(result.ScenarioResults);
            Assert.True(result.ScenarioResults[0].Success);
            Assert.Equal(4, result.ScenarioResults[0].StepResults.Count);
            Assert.All(result.ScenarioResults[0].StepResults, step => Assert.True(step.Success));
            
            Assert.Equal(new[] { "SendRequest", "PublishNotification", "Verify" }, executionOrder);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithFailingSendRequestStep_RecordsFailure()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult("test"));

            _relayMock
                .Setup(x => x.PublishAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Publish failed"));

            var notification = new TestNotification();
            
            framework.Scenario("Mixed Steps Scenario")
                .SendRequest(request, "Success Step")
                .PublishNotification(notification, "Failure Step")
                .Verify(() => Task.FromResult(true), "Recovery Step");

            var result = await framework.RunAllScenariosAsync();

            Assert.False(result.Success);
            Assert.Single(result.ScenarioResults);
            Assert.False(result.ScenarioResults[0].Success);
            Assert.Equal(3, result.ScenarioResults[0].StepResults.Count);
            
            Assert.True(result.ScenarioResults[0].StepResults[0].Success); // SendRequest succeeds
            Assert.False(result.ScenarioResults[0].StepResults[1].Success); // PublishNotification fails
            Assert.True(result.ScenarioResults[0].StepResults[2].Success); // Verify succeeds
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithComplexScenario_ExecutesAllStepTypes()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var notification = new TestNotification();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult("test"));

            _relayMock
                .Setup(x => x.PublishAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            framework.Scenario("Complex Scenario")
                .SendRequest(request, "Send Request")
                .PublishNotification(notification, "Publish Notification")
                .Verify(() => Task.FromResult(true), "Verify")
                .Wait(TimeSpan.FromMilliseconds(10), "Wait");

            var result = await framework.RunAllScenariosAsync();

            // Check that all steps were executed successfully
            Assert.Single(result.ScenarioResults);
            Assert.True(result.ScenarioResults[0].Success);
            Assert.Equal(4, result.ScenarioResults[0].StepResults.Count);
            Assert.All(result.ScenarioResults[0].StepResults, step => Assert.True(step.Success));
            
            // Verify that basic steps work
            _relayMock.Verify(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()), Times.Once);
            _relayMock.Verify(x => x.PublishAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithMultipleScenarios_ExecutesAll()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var notification = new TestNotification();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromResult("test"));

            _relayMock
                .Setup(x => x.PublishAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            framework.Scenario("Scenario 1")
                .SendRequest(request, "Request 1")
                .Verify(() => Task.FromResult(true), "Verify 1");

            framework.Scenario("Scenario 2")
                .PublishNotification(notification, "Notification 2")
                .Verify(() => Task.FromResult(true), "Verify 2");

            framework.Scenario("Scenario 3")
                .Wait(TimeSpan.FromMilliseconds(10), "Wait 3");

            var result = await framework.RunAllScenariosAsync();

            Assert.True(result.Success);
            Assert.Equal(3, result.ScenarioResults.Count);
            Assert.All(result.ScenarioResults, scenario => Assert.True(scenario.Success));
            
            Assert.Equal(2, result.ScenarioResults[0].StepResults.Count);
            Assert.Equal(2, result.ScenarioResults[1].StepResults.Count);
            Assert.Single(result.ScenarioResults[2].StepResults);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithScenarioException_ContinuesToNextScenario()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Scenario exception"));

            framework.Scenario("Failing Scenario")
                .SendRequest(request, "Failing Step");

            framework.Scenario("Passing Scenario")
                .Verify(() => Task.FromResult(true), "Passing Step");

            var result = await framework.RunAllScenariosAsync();

            Assert.Equal(2, result.ScenarioResults.Count);
            Assert.False(result.ScenarioResults[0].Success);
            Assert.True(result.ScenarioResults[1].Success);
            Assert.False(result.Success); // Overall success should be false
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
        public async Task RunAllScenariosAsync_WithStepCancellation_CancelsStep()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var cts = new CancellationTokenSource();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (IRequest req, CancellationToken ct) =>
                {
                    await Task.Delay(1000, ct); // Long delay to allow cancellation
                });

            framework.Scenario("Cancellation Scenario")
                .SendRequest(request, "Cancellable Step");

            // Cancel after a short delay
            cts.CancelAfter(100);

            var result = await framework.RunAllScenariosAsync(cts.Token);

            Assert.False(result.Success);
            Assert.Contains("cancel", result.ScenarioResults[0].StepResults[0].Error, StringComparison.OrdinalIgnoreCase);
        }

        // Test data classes
        public class TestRequest : IRequest<string>, IRequest { }

        public class TestNotification : INotification { }

        public class TestStreamRequest : IStreamRequest<string> { }
    }
}
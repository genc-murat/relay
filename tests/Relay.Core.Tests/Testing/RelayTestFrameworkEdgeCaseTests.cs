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
    public class RelayTestFrameworkEdgeCaseTests
    {
        private ServiceProvider _serviceProvider;
        private Mock<IRelay> _relayMock;
        private Mock<ILogger<RelayTestFramework>> _loggerMock;

        public RelayTestFrameworkEdgeCaseTests()
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
        public async Task RunLoadTestAsync_WithVeryLargeNumberOfRequests_HandlesMemoryEfficiently()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 1000, MaxConcurrency = 50 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(1000, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(1000, result.ResponseTimes.Count);
            Assert.True(result.SuccessRate == 1.0);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithMaxConcurrencyGreaterThanRequests_HandlesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 2, MaxConcurrency = 10 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(2, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(2, result.ResponseTimes.Count);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithVeryLongRampUpDelay_ExecutesSequentially()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration 
            { 
                TotalRequests = 3, 
                MaxConcurrency = 1, 
                RampUpDelayMs = 100 
            };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var startTime = DateTime.UtcNow;
            var result = await framework.RunLoadTestAsync(request, config);
            var endTime = DateTime.UtcNow;

            Assert.Equal(3, result.SuccessfulRequests);
            Assert.True((endTime - startTime).TotalMilliseconds >= 200); // At least 2 * 100ms delays
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithExceptionInScenarioContinuesToNext()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Test exception"));

            framework.Scenario("Failing Scenario")
                .SendRequest(request, "Failing Step");

            framework.Scenario("Passing Scenario")
                .Verify(() => Task.FromResult(true), "Passing Step");

            var result = await framework.RunAllScenariosAsync();

            Assert.Equal(2, result.ScenarioResults.Count);
            Assert.False(result.ScenarioResults[0].Success);
            Assert.True(result.ScenarioResults[1].Success);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithNullLogger_StillWorks()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_relayMock.Object);
            // Don't add logger
            var providerWithoutLogger = services.BuildServiceProvider();

            var framework = new RelayTestFramework(providerWithoutLogger);
            var request = new TestRequest();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            framework.Scenario("Test Scenario")
                .SendRequest(request, "Test Step");

            var result = await framework.RunAllScenariosAsync();

            Assert.True(result.Success);
            Assert.Single(result.ScenarioResults);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithNullLogger_StillWorks()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_relayMock.Object);
            // Don't add logger
            var providerWithoutLogger = services.BuildServiceProvider();

            var framework = new RelayTestFramework(providerWithoutLogger);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 3, MaxConcurrency = 1 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(3, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithOperationCanceledException_PropagatesCancellation()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 5, MaxConcurrency = 2 };
            var cts = new CancellationTokenSource();

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(async (IRequest req, CancellationToken ct) =>
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(100, ct);
                });

            // Cancel immediately to trigger cancellation
            cts.Cancel();

            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                framework.RunLoadTestAsync(request, config, cts.Token));
        }

        [Fact]
        public void LoadTestConfiguration_WithVeryLargeValues_HandlesCorrectly()
        {
            var config = new LoadTestConfiguration 
            { 
                TotalRequests = int.MaxValue,
                MaxConcurrency = 1000,
                RampUpDelayMs = 60000 // 1 minute
            };

            Assert.Equal(int.MaxValue, config.TotalRequests);
            Assert.Equal(1000, config.MaxConcurrency);
            Assert.Equal(60000, config.RampUpDelayMs);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithEmptyScenarioName_HandlesGracefully()
        {
            var framework = new RelayTestFramework(_serviceProvider);

            // This should throw ArgumentException when creating the scenario
            Assert.Throws<ArgumentException>(() => framework.Scenario(""));
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithWhitespaceScenarioName_HandlesGracefully()
        {
            var framework = new RelayTestFramework(_serviceProvider);

            // This should throw ArgumentException when creating the scenario
            Assert.Throws<ArgumentException>(() => framework.Scenario("   "));
        }

        [Fact]
        public async Task RunLoadTestAsync_WithRequestThatThrowsDifferentExceptions_HandlesAll()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 4, MaxConcurrency = 1 };

            var callCount = 0;
            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    return callCount switch
                    {
                        1 => throw new InvalidOperationException("First error"),
                        2 => throw new ArgumentException("Second error"),
                        3 => throw new NullReferenceException("Third error"),
                        4 => ValueTask.CompletedTask,
                        _ => throw new Exception("Unexpected error")
                    };
                });

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(1, result.SuccessfulRequests);
            Assert.Equal(3, result.FailedRequests);
            Assert.Equal(0.25, result.SuccessRate);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithVerySmallResponseTimes_CalculatesCorrectly()
        {
            SetupMocks();
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var config = new LoadTestConfiguration { TotalRequests = 10, MaxConcurrency = 5 };

            _relayMock
                .Setup(x => x.SendAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            var result = await framework.RunLoadTestAsync(request, config);

            Assert.Equal(10, result.SuccessfulRequests);
            Assert.Equal(0, result.FailedRequests);
            Assert.Equal(10, result.ResponseTimes.Count);
            Assert.All(result.ResponseTimes, time => Assert.True(time >= 0));
            Assert.True(result.AverageResponseTime >= 0);
        }

        [Fact]
        public void TestScenarioBuilder_WithVeryLongStepName_HandlesCorrectly()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var longStepName = new string('A', 1000);

            var builder = framework.Scenario("Test Scenario")
                .SendRequest(request, longStepName);

            // Access private field for testing
            var scenariosField = typeof(RelayTestFramework).GetField("_scenarios", BindingFlags.NonPublic | BindingFlags.Instance);
            var scenarios = (List<TestScenario>)scenariosField!.GetValue(framework)!;

            Assert.Single(scenarios);
            Assert.Equal(longStepName, scenarios[0].Steps[0].Name);
        }

        [Fact]
        public void TestScenarioBuilder_WithSpecialCharactersInStepName_HandlesCorrectly()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            var specialName = "Test Step with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?";

            var builder = framework.Scenario("Test Scenario")
                .SendRequest(request, specialName);

            // Access private field for testing
            var scenariosField = typeof(RelayTestFramework).GetField("_scenarios", BindingFlags.NonPublic | BindingFlags.Instance);
            var scenarios = (List<TestScenario>)scenariosField!.GetValue(framework)!;

            Assert.Single(scenarios);
            Assert.Equal(specialName, scenarios[0].Steps[0].Name);
        }

        [Fact]
        public async Task RunAllScenariosAsync_WithScenarioContainingNoSteps_HandlesGracefully()
        {
            var framework = new RelayTestFramework(_serviceProvider);

            framework.Scenario("Empty Scenario");

            var result = await framework.RunAllScenariosAsync();

            Assert.Single(result.ScenarioResults);
            Assert.True(result.ScenarioResults[0].Success);
            Assert.Empty(result.ScenarioResults[0].StepResults);
        }

        [Fact]
        public async Task RunLoadTestAsync_WithZeroRequests_ThrowsArgumentException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            
            Assert.Throws<ArgumentException>(() =>
                new LoadTestConfiguration { TotalRequests = 0, MaxConcurrency = 1 });
        }

        [Fact]
        public void RunLoadTestAsync_WithNegativeRequests_ThrowsArgumentException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            
            Assert.Throws<ArgumentException>(() =>
                new LoadTestConfiguration { TotalRequests = -1, MaxConcurrency = 1 });
        }

        [Fact]
        public void RunLoadTestAsync_WithZeroConcurrency_ThrowsArgumentException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            
            Assert.Throws<ArgumentException>(() =>
                new LoadTestConfiguration { TotalRequests = 5, MaxConcurrency = 0 });
        }

        [Fact]
        public void RunLoadTestAsync_WithNegativeConcurrency_ThrowsArgumentException()
        {
            var framework = new RelayTestFramework(_serviceProvider);
            var request = new TestRequest();
            
            Assert.Throws<ArgumentException>(() =>
                new LoadTestConfiguration { TotalRequests = 5, MaxConcurrency = -1 });
        }

        // Test data classes
        public class TestRequest : IRequest<string>, IRequest { }

        public class TestNotification : INotification { }

        public class TestStreamRequest : IStreamRequest<string> { }
    }
}
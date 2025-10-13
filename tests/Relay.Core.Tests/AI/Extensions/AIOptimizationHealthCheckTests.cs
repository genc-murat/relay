using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Extensions
{
    public class AIOptimizationHealthCheckTests
    {
        private readonly Mock<IAIOptimizationEngine> _engineMock;
        private readonly ILogger<AIOptimizationHealthCheck> _logger;
        private readonly AIHealthCheckOptions _options;

        public AIOptimizationHealthCheckTests()
        {
            _engineMock = new Mock<IAIOptimizationEngine>();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<AIOptimizationHealthCheck>();
            _options = new AIHealthCheckOptions
            {
                MinSystemHealthScore = 0.7
            };
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Engine_Is_Null()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIOptimizationHealthCheck(null!, _logger, optionsMock.Object));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIOptimizationHealthCheck(_engineMock.Object, null!, optionsMock.Object));
        }

        [Fact]
        public void Constructor_Should_Use_Default_Options_When_Options_Is_Null()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns((AIHealthCheckOptions)null!);

            // Act
            var healthCheck = new AIOptimizationHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Assert - should not throw, uses default options
            Assert.NotNull(healthCheck);
        }

        #endregion

        #region CheckHealthAsync Tests

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Healthy_Result_For_Good_Insights()
        {
            // Arrange
            var insights = CreateGoodSystemInsights();
            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(insights);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIOptimizationHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Equal("Operational", result.Status);
            Assert.Equal("AI Optimization Engine", result.ComponentName);
            Assert.Equal(insights.HealthScore.Overall, result.HealthScore);
            Assert.Contains("operational", result.Description);
            Assert.True(result.Duration > TimeSpan.Zero);
            Assert.Equal(insights.PerformanceGrade, result.Data["PerformanceGrade"]);
            Assert.Equal(insights.HealthScore.Overall, result.Data["HealthScore"]);
            Assert.Equal(insights.Bottlenecks.Count, result.Data["BottleneckCount"]);
            Assert.Equal(insights.Opportunities.Count, result.Data["OpportunityCount"]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Add_Warning_For_Low_Health_Score()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(5),
                PerformanceGrade = 'A',
                HealthScore = new SystemHealthScore
                {
                    Overall = 0.5, // Below threshold
                    Performance = 0.85,
                    Reliability = 0.95,
                    Scalability = 0.88,
                    Security = 0.92,
                    Maintainability = 0.87,
                    Status = "Good",
                    CriticalAreas = new List<string>()
                },
                Bottlenecks = new List<PerformanceBottleneck>(),
                Opportunities = new List<OptimizationOpportunity>()
            };
            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(insights);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIOptimizationHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy); // Still healthy, just warning
            Assert.Contains("below threshold", result.Warnings[0]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Add_Warning_For_Critical_Areas()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(5),
                PerformanceGrade = 'A',
                HealthScore = new SystemHealthScore
                {
                    Overall = 0.9,
                    Performance = 0.85,
                    Reliability = 0.95,
                    Scalability = 0.88,
                    Security = 0.92,
                    Maintainability = 0.87,
                    Status = "Good",
                    CriticalAreas = new List<string> { "Memory", "CPU" }
                },
                Bottlenecks = new List<PerformanceBottleneck>(),
                Opportunities = new List<OptimizationOpportunity>()
            };
            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(insights);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIOptimizationHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy); // Still healthy, just warning
            Assert.Contains("Critical areas detected", result.Warnings[0]);
            Assert.Contains("Memory", result.Warnings[0]);
            Assert.Contains("CPU", result.Warnings[0]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Failed_Result_On_Exception()
        {
            // Arrange
            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                      .ThrowsAsync(new InvalidOperationException("Test exception"));

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIOptimizationHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal("Failed", result.Status);
            Assert.Contains("Test exception", result.Description);
            Assert.NotNull(result.Exception);
            Assert.Contains("Test exception", result.Errors[0]);
            Assert.True(result.Duration > TimeSpan.Zero);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Handle_Null_HealthScore()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(5),
                PerformanceGrade = 'B',
                HealthScore = null // Null health score
            };
            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(insights);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIOptimizationHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Equal(0.0, result.HealthScore); // Should default to 0.0
            Assert.Equal(0.0, result.Data["HealthScore"]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Handle_Null_Bottlenecks_And_Opportunities()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(5),
                PerformanceGrade = 'A',
                HealthScore = new SystemHealthScore { Overall = 0.9 },
                Bottlenecks = null, // Null bottlenecks
                Opportunities = null // Null opportunities
            };
            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                      .ReturnsAsync(insights);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIOptimizationHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Equal(0, result.Data["BottleneckCount"]);
            Assert.Equal(0, result.Data["OpportunityCount"]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Pass_CancellationToken_To_Engine()
        {
            // Arrange
            var insights = CreateGoodSystemInsights();
            var cancellationToken = new CancellationToken(true);
            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), cancellationToken))
                      .ReturnsAsync(insights);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIOptimizationHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            await healthCheck.CheckHealthAsync(cancellationToken);

            // Assert
            _engineMock.Verify(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), cancellationToken), Times.Once);
        }

        #endregion

        #region Helper Methods

        private static SystemPerformanceInsights CreateGoodSystemInsights()
        {
            return new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(5),
                PerformanceGrade = 'A',
                HealthScore = new SystemHealthScore
                {
                    Overall = 0.9,
                    Performance = 0.85,
                    Reliability = 0.95,
                    Scalability = 0.88,
                    Security = 0.92,
                    Maintainability = 0.87,
                    Status = "Excellent",
                    CriticalAreas = new List<string>()
                },
                Bottlenecks = new List<PerformanceBottleneck>(),
                Opportunities = new List<OptimizationOpportunity>(),
                KeyMetrics = new Dictionary<string, double>
                {
                    ["Throughput"] = 1000.0,
                    ["Latency"] = 50.0
                }
            };
        }

        #endregion
    }
}
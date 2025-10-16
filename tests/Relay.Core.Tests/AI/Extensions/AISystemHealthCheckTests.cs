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
    public class AISystemHealthCheckTests
    {
        private readonly Mock<IAIOptimizationEngine> _engineMock;
        private readonly Mock<ILogger<AISystemHealthCheck>> _loggerMock;
        private readonly AIHealthCheckOptions _options;

        public AISystemHealthCheckTests()
        {
            _engineMock = new Mock<IAIOptimizationEngine>();
            _loggerMock = new Mock<ILogger<AISystemHealthCheck>>();
            _options = new AIHealthCheckOptions
            {
                MinSystemHealthScore = 0.7
            };
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenEngineIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AISystemHealthCheck(null!, _loggerMock.Object, Options.Create(_options)));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AISystemHealthCheck(_engineMock.Object, null!, Options.Create(_options)));
        }

        [Fact]
        public void Constructor_CreatesInstance_WhenOptionsIsNull()
        {
            // Act
            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, null);

            // Assert
            Assert.NotNull(healthCheck);
        }

        [Fact]
        public async Task CheckHealthAsync_ReturnsHealthyResult_WhenSystemHealthScoreIsAboveThreshold()
        {
            // Arrange
            var healthScore = new SystemHealthScore
            {
                Overall = 0.85,
                Performance = 0.9,
                Reliability = 0.8,
                Scalability = 0.85,
                Security = 0.9,
                Maintainability = 0.8,
                Status = "Good",
                CriticalAreas = new List<string>()
            };

            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(10),
                HealthScore = healthScore,
                PerformanceGrade = 'A',
                Bottlenecks = new List<PerformanceBottleneck>(),
                Opportunities = new List<OptimizationOpportunity>(),
                KeyMetrics = new Dictionary<string, double> { ["Throughput"] = 1000.0 }
            };

            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(insights);

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(_options));

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Equal("AI System", result.ComponentName);
            Assert.Equal(0.85, result.HealthScore);
            Assert.Equal("Good", result.Status);
            Assert.Contains("System health: Good (Score: 85.0%)", result.Description);
            Assert.True(result.Duration > TimeSpan.Zero);

            // Check data contains expected metrics
            Assert.Equal(0.85, result.Data["Overall"]);
            Assert.Equal(0.9, result.Data["Performance"]);
            Assert.Equal(0.8, result.Data["Reliability"]);
            Assert.Equal(0.85, result.Data["Scalability"]);
            Assert.Equal(0.9, result.Data["Security"]);
            Assert.Equal(0.8, result.Data["Maintainability"]);
            Assert.Equal('A', result.Data["PerformanceGrade"]);
            Assert.Equal(0, result.Data["BottleneckCount"]);
            Assert.Equal(0, result.Data["OpportunityCount"]);
        }

        [Fact]
        public async Task CheckHealthAsync_ReturnsUnhealthyResult_WhenSystemHealthScoreIsBelowThreshold()
        {
            // Arrange
            var healthScore = new SystemHealthScore
            {
                Overall = 0.5, // Below threshold of 0.7
                Performance = 0.4,
                Reliability = 0.6,
                Scalability = 0.5,
                Security = 0.7,
                Maintainability = 0.4,
                Status = "Poor",
                CriticalAreas = new List<string> { "Performance", "Maintainability" }
            };

            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(10),
                HealthScore = healthScore,
                PerformanceGrade = 'D',
                Bottlenecks = new List<PerformanceBottleneck>(),
                Opportunities = new List<OptimizationOpportunity>(),
                KeyMetrics = new Dictionary<string, double>()
            };

            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(insights);

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(_options));

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal(0.5, result.HealthScore);
            Assert.Equal("Poor", result.Status);
            Assert.Contains("Critical area: Performance", result.Warnings);
            Assert.Contains("Critical area: Maintainability", result.Warnings);
        }

        [Fact]
        public async Task CheckHealthAsync_ReturnsUnhealthyResult_WhenHealthScoreIsNull()
        {
            // Arrange
            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(10),
                HealthScore = null, // Null health score
                PerformanceGrade = 'F',
                Bottlenecks = new List<PerformanceBottleneck>(),
                Opportunities = new List<OptimizationOpportunity>(),
                KeyMetrics = new Dictionary<string, double>()
            };

            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(insights);

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(_options));

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal(0.0, result.HealthScore);
            Assert.Equal("Unknown", result.Status);
        }

        [Fact]
        public async Task CheckHealthAsync_AddsWarning_WhenMultipleBottlenecksDetected()
        {
            // Arrange
            var healthScore = new SystemHealthScore
            {
                Overall = 0.8,
                Status = "Fair"
            };

            var bottlenecks = new List<PerformanceBottleneck>();
            for (int i = 0; i < 6; i++) // More than 5 bottlenecks
            {
                bottlenecks.Add(new PerformanceBottleneck
                {
                    Component = $"Component{i}",
                    Severity = BottleneckSeverity.High,
                    Description = $"Bottleneck {i}"
                });
            }

            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(10),
                HealthScore = healthScore,
                PerformanceGrade = 'C',
                Bottlenecks = bottlenecks,
                Opportunities = new List<OptimizationOpportunity>(),
                KeyMetrics = new Dictionary<string, double>()
            };

            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(insights);

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(_options));

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.Contains("Multiple performance bottlenecks detected: 6", result.Warnings);
        }

        [Fact]
        public async Task CheckHealthAsync_HandlesException_AndReturnsFailedResult()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Engine failure");
            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(_options));

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal("Failed", result.Status);
            Assert.Contains("System health check failed: Engine failure", result.Description);
            Assert.Equal(expectedException, result.Exception);
            Assert.Contains("Engine failure", result.Errors);
            Assert.True(result.Duration > TimeSpan.Zero);
        }

        [Fact]
        public async Task CheckHealthAsync_UsesCorrectTimeWindow()
        {
            // Arrange
            var healthScore = new SystemHealthScore
            {
                Overall = 0.8,
                Status = "Good"
            };

            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(10),
                HealthScore = healthScore,
                PerformanceGrade = 'B',
                Bottlenecks = new List<PerformanceBottleneck>(),
                Opportunities = new List<OptimizationOpportunity>(),
                KeyMetrics = new Dictionary<string, double>()
            };

            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(insights);

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(_options));

            // Act
            await healthCheck.CheckHealthAsync();

            // Assert
            _engineMock.Verify(e => e.GetSystemInsightsAsync(TimeSpan.FromMinutes(10), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CheckHealthAsync_UsesCustomOptions_WhenProvided()
        {
            // Arrange
            var customOptions = new AIHealthCheckOptions
            {
                MinSystemHealthScore = 0.8 // Higher threshold
            };

            var healthScore = new SystemHealthScore
            {
                Overall = 0.75, // Below custom threshold but above default
                Status = "Fair"
            };

            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(10),
                HealthScore = healthScore,
                PerformanceGrade = 'C',
                Bottlenecks = new List<PerformanceBottleneck>(),
                Opportunities = new List<OptimizationOpportunity>(),
                KeyMetrics = new Dictionary<string, double>()
            };

            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(insights);

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(customOptions));

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy); // Should be unhealthy due to custom threshold
        }

        [Fact]
        public async Task CheckHealthAsync_LogsDebugInformation_WhenSuccessful()
        {
            // Arrange
            var healthScore = new SystemHealthScore
            {
                Overall = 0.9,
                Status = "Excellent"
            };

            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(10),
                HealthScore = healthScore,
                PerformanceGrade = 'A',
                Bottlenecks = new List<PerformanceBottleneck>(),
                Opportunities = new List<OptimizationOpportunity>(),
                KeyMetrics = new Dictionary<string, double>()
            };

            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(insights);

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(_options));

            // Act
            await healthCheck.CheckHealthAsync();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI System health check: Excellent")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CheckHealthAsync_LogsError_WhenExceptionOccurs()
        {
            // Arrange
            var expectedException = new Exception("Test error");
            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(_options));

            // Act
            await healthCheck.CheckHealthAsync();

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("AI System health check failed")),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CheckHealthAsync_IncludesAllHealthScoreMetrics_InResultData()
        {
            // Arrange
            var healthScore = new SystemHealthScore
            {
                Overall = 0.75,
                Performance = 0.8,
                Reliability = 0.7,
                Scalability = 0.8,
                Security = 0.9,
                Maintainability = 0.6,
                Status = "Good",
                CriticalAreas = new List<string>()
            };

            var insights = new SystemPerformanceInsights
            {
                AnalysisTime = DateTime.UtcNow,
                AnalysisPeriod = TimeSpan.FromMinutes(10),
                HealthScore = healthScore,
                PerformanceGrade = 'B',
                Bottlenecks = new List<PerformanceBottleneck> { new PerformanceBottleneck() },
                Opportunities = new List<OptimizationOpportunity> { new OptimizationOpportunity() },
                KeyMetrics = new Dictionary<string, double> { ["Metric1"] = 42.0 }
            };

            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(insights);

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(_options));

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.Equal(0.75, result.Data["Overall"]);
            Assert.Equal(0.8, result.Data["Performance"]);
            Assert.Equal(0.7, result.Data["Reliability"]);
            Assert.Equal(0.8, result.Data["Scalability"]);
            Assert.Equal(0.9, result.Data["Security"]);
            Assert.Equal(0.6, result.Data["Maintainability"]);
            Assert.Equal('B', result.Data["PerformanceGrade"]);
            Assert.Equal(1, result.Data["BottleneckCount"]);
            Assert.Equal(1, result.Data["OpportunityCount"]);
        }

        [Fact]
        public async Task CheckHealthAsync_RespectsCancellationToken()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            _engineMock.Setup(e => e.GetSystemInsightsAsync(It.IsAny<TimeSpan>(), cancellationTokenSource.Token))
                .ThrowsAsync(new OperationCanceledException(cancellationTokenSource.Token));

            var healthCheck = new AISystemHealthCheck(_engineMock.Object, _loggerMock.Object, Options.Create(_options));

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                healthCheck.CheckHealthAsync(cancellationTokenSource.Token));
        }
    }
}
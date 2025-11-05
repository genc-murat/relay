using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Validation;

public class AIValidationFrameworkSystemHealthTests
{
    private readonly Mock<ILogger<AIValidationFramework>> _mockLogger;
    private readonly AIValidationFramework _validationFramework;
    private readonly AIOptimizationOptions _options;

    public AIValidationFrameworkSystemHealthTests()
    {
        _mockLogger = new Mock<ILogger<AIValidationFramework>>();
        _options = new AIOptimizationOptions
        {
            MinConfidenceScore = 0.7,
            MaxAutomaticOptimizationRisk = RiskLevel.Medium,
            EnableAutomaticOptimization = true
        };
        _validationFramework = new AIValidationFramework(_mockLogger.Object, _options);
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithHealthySystem_ReturnsStable()
    {
        // Arrange
        var insights = CreateHealthySystemInsights();

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.True(result.IsStable);
        Assert.True(result.StabilityScore >= 0.7);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithLowOverallHealth_ReturnsUnstable()
    {
        // Arrange
        var insights = new SystemPerformanceInsights
        {
            HealthScore = new SystemHealthScore
            {
                Overall = 0.5, // Low health score
                Performance = 0.8,
                Reliability = 0.9,
                Scalability = 0.7,
                Status = "Poor Health"
            },
            PerformanceGrade = 'C',
            Bottlenecks = new List<PerformanceBottleneck>(),
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.False(result.IsStable);
        Assert.True(result.StabilityScore < 0.7);
        Assert.NotEmpty(result.Issues);
        Assert.Contains(result.Issues, i => i.Component == "Overall System");
        Assert.Equal(ValidationSeverity.Error, result.Issues.First(i => i.Component == "Overall System").Severity);
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithCriticalBottlenecks_ReturnsUnstable()
    {
        // Arrange
        var insights = new SystemPerformanceInsights
        {
        HealthScore = new SystemHealthScore
        {
            Overall = 0.3,
            Performance = 0.2,
            Reliability = 0.4,
            Scalability = 0.3,
            Security = 0.5,
            Maintainability = 0.4,
            Status = "Critical",
            CriticalAreas = new List<string> { "Database", "Memory" }
        },
            PerformanceGrade = 'B',
            Bottlenecks = new List<PerformanceBottleneck>
            {
                new PerformanceBottleneck
                {
                    Component = "Database",
                    Severity = BottleneckSeverity.Critical,
                    Description = "Database connection pool exhausted",
                    Impact = 0.9,
                    RecommendedActions = new List<string> { "Increase connection pool size" }
                }
            },
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.False(result.IsStable);
        Assert.NotEmpty(result.Issues);
        Assert.Contains(result.Issues, i => i.Component == "Database");
        Assert.Equal(ValidationSeverity.Error, result.Issues.First(i => i.Component == "Database").Severity);
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithPoorPerformanceGrade_ReturnsUnstable()
    {
        // Arrange
        var insights = new SystemPerformanceInsights
        {
            HealthScore = new SystemHealthScore
            {
                Overall = 0.8,
                Performance = 0.4,
                Reliability = 0.9,
                Scalability = 0.7
            },
            PerformanceGrade = 'F', // Poor grade
            Bottlenecks = new List<PerformanceBottleneck>(),
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.False(result.IsStable);
        Assert.NotEmpty(result.Issues);
        Assert.Contains(result.Issues, i => i.Component == "Performance");
        Assert.Equal(ValidationSeverity.Error, result.Issues.First(i => i.Component == "Performance").Severity);
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithLowReliability_ReturnsWarning()
    {
        // Arrange
        var insights = new SystemPerformanceInsights
        {
            HealthScore = new SystemHealthScore
            {
                Overall = 0.8,
                Performance = 0.8,
                Reliability = 0.85, // Below 0.9 threshold
                Scalability = 0.7
            },
            PerformanceGrade = 'B',
            Bottlenecks = new List<PerformanceBottleneck>(),
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.True(result.IsStable); // Still stable but with warning
        Assert.NotEmpty(result.Issues);
        Assert.Contains(result.Issues, i => i.Component == "Reliability");
        Assert.Equal(ValidationSeverity.Warning, result.Issues.First(i => i.Component == "Reliability").Severity);
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithLowPredictionConfidence_ReturnsWarning()
    {
        // Arrange
        var insights = new SystemPerformanceInsights
        {
            HealthScore = new SystemHealthScore
            {
                Overall = 0.8,
                Performance = 0.8,
                Reliability = 0.9,
                Scalability = 0.7
            },
            PerformanceGrade = 'B',
            Bottlenecks = new List<PerformanceBottleneck>(),
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.6 // Below 0.7 threshold
            }
        };

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.True(result.IsStable); // Still stable but with warning
        Assert.NotEmpty(result.Issues);
        Assert.Contains(result.Issues, i => i.Component == "Predictive Analytics");
        Assert.Equal(ValidationSeverity.Warning, result.Issues.First(i => i.Component == "Predictive Analytics").Severity);
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithMultipleIssues_CalculatesCorrectStabilityScore()
    {
        // Arrange
        var insights = new SystemPerformanceInsights
        {
            HealthScore = new SystemHealthScore
            {
                Overall = 0.6, // Low overall health
                Performance = 0.5,
                Reliability = 0.85, // Low reliability
                Scalability = 0.7
            },
            PerformanceGrade = 'D', // Poor grade
            Bottlenecks = new List<PerformanceBottleneck>
            {
                new PerformanceBottleneck
                {
                    Component = "Database",
                    Severity = BottleneckSeverity.Critical,
                    Description = "Database connection pool exhausted",
                    Impact = 0.9,
                    RecommendedActions = new List<string> { "Increase connection pool size" }
                }
            },
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.6 // Low confidence
            }
        };

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.False(result.IsStable);
        Assert.True(result.StabilityScore < 0.6); // Should be significantly reduced
        Assert.True(result.Issues.Length >= 4); // Multiple issues detected
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithModerateBottlenecks_ReturnsWarning()
    {
        // Arrange
        var insights = new SystemPerformanceInsights
        {
            HealthScore = new SystemHealthScore
            {
                Overall = 0.8,
                Performance = 0.8,
                Reliability = 0.9,
                Scalability = 0.7
            },
            PerformanceGrade = 'B',
            Bottlenecks = new List<PerformanceBottleneck>
            {
                new PerformanceBottleneck
                {
                    Component = "Memory",
                    Severity = BottleneckSeverity.Medium,
                    Description = "High memory usage detected",
                    Impact = 0.6,
                    RecommendedActions = new List<string> { "Optimize memory usage", "Increase memory allocation" }
                }
            },
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.True(result.IsStable); // Still stable as bottlenecks are not critical
        Assert.Empty(result.Issues); // Moderate bottlenecks don't create issues
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithHighBottlenecks_ReturnsWarning()
    {
        // Arrange
        var insights = new SystemPerformanceInsights
        {
            HealthScore = new SystemHealthScore
            {
                Overall = 0.8,
                Performance = 0.8,
                Reliability = 0.9,
                Scalability = 0.7
            },
            PerformanceGrade = 'B',
            Bottlenecks = new List<PerformanceBottleneck>
            {
                new PerformanceBottleneck
                {
                    Component = "CPU",
                    Severity = BottleneckSeverity.High,
                    Description = "High CPU utilization",
                    Impact = 0.8,
                    RecommendedActions = new List<string> { "Scale up resources", "Optimize CPU usage" }
                }
            },
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.True(result.IsStable); // Still stable as bottlenecks are not critical
        Assert.Empty(result.Issues); // High bottlenecks don't create issues
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithLowBottlenecks_ReturnsStable()
    {
        // Arrange
        var insights = new SystemPerformanceInsights
        {
            HealthScore = new SystemHealthScore
            {
                Overall = 0.8,
                Performance = 0.8,
                Reliability = 0.9,
                Scalability = 0.7
            },
            PerformanceGrade = 'B',
            Bottlenecks = new List<PerformanceBottleneck>
            {
                new PerformanceBottleneck
                {
                    Component = "Network",
                    Severity = BottleneckSeverity.Low,
                    Description = "Minor network latency",
                    Impact = 0.3,
                    RecommendedActions = new List<string> { "Monitor network performance" }
                }
            },
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.True(result.IsStable);
        Assert.Empty(result.Issues); // Low bottlenecks don't create issues
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithMultipleCriticalBottlenecks_ReturnsUnstable()
    {
        // Arrange
        var insights = new SystemPerformanceInsights
        {
            HealthScore = new SystemHealthScore
            {
                Overall = 0.8,
                Performance = 0.8,
                Reliability = 0.9,
                Scalability = 0.7
            },
            PerformanceGrade = 'B',
            Bottlenecks = new List<PerformanceBottleneck>
            {
                new PerformanceBottleneck
                {
                    Component = "Database",
                    Severity = BottleneckSeverity.Critical,
                    Description = "Database connection pool exhausted",
                    Impact = 0.9,
                    RecommendedActions = new List<string> { "Increase connection pool size" }
                },
                new PerformanceBottleneck
                {
                    Component = "Memory",
                    Severity = BottleneckSeverity.Critical,
                    Description = "Out of memory errors",
                    Impact = 0.95,
                    RecommendedActions = new List<string> { "Increase memory allocation" }
                }
            },
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.8
            }
        };

        // Act
        var result = await _validationFramework.ValidateSystemHealthAsync(insights);

        // Assert
        Assert.False(result.IsStable);
        Assert.Equal(2, System.Linq.Enumerable.Count(result.Issues, i => i.Severity == ValidationSeverity.Error));
        Assert.True(result.StabilityScore < 0.4); // Significantly reduced stability
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var insights = CreateHealthySystemInsights();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _validationFramework.ValidateSystemHealthAsync(insights, cts.Token).AsTask());
    }

    [Fact]
    public async Task ValidateSystemHealthAsync_WithNullInsights_HandlesGracefully()
    {
        // Arrange
        SystemPerformanceInsights insights = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _validationFramework.ValidateSystemHealthAsync(insights).AsTask());
    }

    private SystemPerformanceInsights CreateHealthySystemInsights()
    {
        return new SystemPerformanceInsights
        {
            HealthScore = new SystemHealthScore
            {
                Overall = 0.9,
                Performance = 0.9,
                Reliability = 0.95,
                Scalability = 0.85
            },
            PerformanceGrade = 'A',
            Bottlenecks = new List<PerformanceBottleneck>(),
            Predictions = new PredictiveAnalysis
            {
                PredictionConfidence = 0.85
            }
        };
    }


}
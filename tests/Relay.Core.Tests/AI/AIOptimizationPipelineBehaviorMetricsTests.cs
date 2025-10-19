using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.AI;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationPipelineBehaviorMetricsTests
    {
        private readonly ServiceCollection _services;
        private readonly ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>> _logger;
        private readonly ILogger<SystemLoadMetricsProvider> _systemLogger;
        private readonly AIOptimizationOptions _options;

        public AIOptimizationPipelineBehaviorMetricsTests()
        {
            _services = new ServiceCollection();
            _services.AddLogging();
            var provider = _services.BuildServiceProvider();

            _logger = provider.GetRequiredService<ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>>>();
            _systemLogger = provider.GetRequiredService<ILogger<SystemLoadMetricsProvider>>();

            _options = new AIOptimizationOptions
            {
                Enabled = true,
                LearningEnabled = true,
                MinConfidenceScore = 0.7,
                MinExecutionsForAnalysis = 5
            };
        }

        [Fact]
        public async Task SystemLoadMetricsProvider_Should_Return_Valid_Metrics()
        {
            // Arrange
            var provider = new SystemLoadMetricsProvider(_systemLogger);

            // Act
            var metrics = await provider.GetCurrentLoadAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(metrics);
            Assert.InRange(metrics.CpuUtilization, 0.0, 1.0);
            Assert.InRange(metrics.MemoryUtilization, 0.0, 1.0);
            Assert.True(metrics.AvailableMemory >= 0);
            Assert.True(metrics.ActiveRequestCount >= 0);
            Assert.InRange(metrics.ThreadPoolUtilization, 0.0, 1.0);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithDatabaseCallsProperty_ReturnsCorrectCount()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>
                {
                    ["DatabaseCalls"] = 5
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act - We need to access the private method via reflection
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithAvgDatabaseCallsProperty_ReturnsRoundedValue()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>
                {
                    ["AvgDatabaseCalls"] = 7.8
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(8, result); // Rounded from 7.8
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithLongDatabaseCallsProperty_ReturnsIntValue()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>
                {
                    ["DatabaseCalls"] = 15L
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(15, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithDoubleDatabaseCallsProperty_ReturnsIntValue()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>
                {
                    ["DatabaseCalls"] = 12.0
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(12, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithNoPropertiesAndLongExecutionTime_EstimatesBasedOnTime()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>(),
                AverageExecutionTime = TimeSpan.FromMilliseconds(250) // > 100ms
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert - Should estimate 250ms / 50ms = 5 DB calls
            Assert.Equal(5, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithNoPropertiesAndShortExecutionTime_ReturnsZero()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>(),
                AverageExecutionTime = TimeSpan.FromMilliseconds(50) // < 100ms
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_WithInvalidPropertyType_ReturnsZero()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>
                {
                    ["DatabaseCalls"] = "invalid_string" // Not a numeric type
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert - Should fall back to execution time estimation (0)
            Assert.Equal(0, result);
        }

        [Fact]
        public async Task ExtractDatabaseCalls_PrefersDatabaseCallsOverAvgDatabaseCalls()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);
            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var stats = new HandlerExecutionStats
            {
                Properties = new Dictionary<string, object>
                {
                    ["DatabaseCalls"] = 10,
                    ["AvgDatabaseCalls"] = 8.5 // Should be ignored since DatabaseCalls is present
                },
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            };

            // Act
            var method = typeof(AIOptimizationPipelineBehavior<TestRequest, TestResponse>)
                .GetMethod("ExtractDatabaseCalls", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = (int)method!.Invoke(behavior, new object[] { stats })!;

            // Assert - Should use DatabaseCalls (10) not AvgDatabaseCalls (8.5)
            Assert.Equal(10, result);
        }

        // Test Request and Response classes
        public class TestRequest : IRequest<TestResponse>
        {
            public string Value { get; set; } = string.Empty;
        }

        public class TestResponse
        {
            public string Result { get; set; } = string.Empty;
        }

        // Mock AI Optimization Engine
        private class MockAIOptimizationEngine : IAIOptimizationEngine
        {
            public bool AnalyzeCalled { get; private set; }
            public bool LearnCalled { get; private set; }
            public int BatchSizeToReturn { get; set; } = 10;
            public bool ThrowOnAnalyze { get; set; }
            public bool LowConfidence { get; set; }

            public OptimizationRecommendation RecommendationToReturn { get; set; } = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.None,
                ConfidenceScore = 0.5,
                EstimatedImprovement = TimeSpan.Zero,
                Reasoning = "Mock recommendation",
                Priority = OptimizationPriority.Low,
                EstimatedGainPercentage = 0.0,
                Risk = RiskLevel.VeryLow
            };

            public ValueTask<OptimizationRecommendation> AnalyzeRequestAsync<TRequest>(
                TRequest request,
                RequestExecutionMetrics executionMetrics,
                CancellationToken cancellationToken = default)
            {
                AnalyzeCalled = true;
                cancellationToken.ThrowIfCancellationRequested();

                if (ThrowOnAnalyze)
                {
                    throw new InvalidOperationException("AI Engine analyze failed");
                }

                var recommendation = RecommendationToReturn;
                if (LowConfidence)
                {
                    recommendation = new OptimizationRecommendation
                    {
                        Strategy = OptimizationStrategy.None,
                        ConfidenceScore = 0.3, // Below threshold
                        EstimatedImprovement = TimeSpan.Zero,
                        Reasoning = "Low confidence mock recommendation",
                        Priority = OptimizationPriority.Low,
                        EstimatedGainPercentage = 0.0,
                        Risk = RiskLevel.VeryLow
                    };
                }

                return new ValueTask<OptimizationRecommendation>(recommendation);
            }

            public ValueTask<int> PredictOptimalBatchSizeAsync(
                Type requestType,
                SystemLoadMetrics currentLoad,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<int>(BatchSizeToReturn);
            }

            public ValueTask<CachingRecommendation> ShouldCacheAsync(
                Type requestType,
                AccessPattern[] accessPatterns,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<CachingRecommendation>(new CachingRecommendation
                {
                    ShouldCache = false,
                    RecommendedTtl = TimeSpan.FromMinutes(5),
                    Strategy = CacheStrategy.None,
                    ExpectedHitRate = 0.0,
                    CacheKey = string.Empty,
                    Scope = CacheScope.Global,
                    ConfidenceScore = 0.5
                });
            }

            public ValueTask LearnFromExecutionAsync(
                Type requestType,
                OptimizationStrategy[] appliedOptimizations,
                RequestExecutionMetrics actualMetrics,
                CancellationToken cancellationToken = default)
            {
                LearnCalled = true;
                return ValueTask.CompletedTask;
            }

            public ValueTask<SystemPerformanceInsights> GetSystemInsightsAsync(
                TimeSpan timeWindow,
                CancellationToken cancellationToken = default)
            {
                return new ValueTask<SystemPerformanceInsights>(new SystemPerformanceInsights
                {
                    AnalysisTime = DateTime.UtcNow,
                    AnalysisPeriod = timeWindow,
                    PerformanceGrade = 'A'
                });
            }

            public void SetLearningMode(bool enabled)
            {
                // Mock implementation
            }

            public AIModelStatistics GetModelStatistics()
            {
                return new AIModelStatistics
                {
                    ModelTrainingDate = DateTime.UtcNow,
                    TotalPredictions = 0,
                    AccuracyScore = 0.0,
                    PrecisionScore = 0.0,
                    RecallScore = 0.0,
                    F1Score = 0.0,
                    AveragePredictionTime = TimeSpan.Zero,
                    TrainingDataPoints = 0,
                    ModelVersion = "1.0.0",
                    LastRetraining = DateTime.UtcNow,
                    ModelConfidence = 0.0
                };
            }
        }
    }
}
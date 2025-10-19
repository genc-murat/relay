using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.AI;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationPipelineBehaviorDatabaseTests
    {
        private readonly ServiceCollection _services;
        private readonly ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>> _logger;
        private readonly ILogger<SystemLoadMetricsProvider> _systemLogger;
        private readonly AIOptimizationOptions _options;

        public AIOptimizationPipelineBehaviorDatabaseTests()
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
        public async Task HandleAsync_AppliesDatabaseOptimization_WhenRecommended()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.DatabaseOptimization,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableQueryOptimization"] = true,
                    ["MaxRetries"] = 3
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(executed);
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Database_Optimization_With_Transient_Errors()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.DatabaseOptimization,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableQueryOptimization"] = true,
                    ["MaxRetries"] = 3,
                    ["RetryDelayMs"] = 10
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var callCount = 0;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                callCount++;
                if (callCount <= 2) // Fail first 2 attempts with transient error
                {
                    throw new TimeoutException("Database timeout - transient error");
                }
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert - Should succeed after retries
            Assert.Equal(3, callCount); // Should have been called 3 times (2 failures + 1 success)
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Database_Optimization_With_Non_Transient_Errors()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.DatabaseOptimization,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableQueryOptimization"] = true,
                    ["MaxRetries"] = 3,
                    ["RetryDelayMs"] = 10
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var callCount = 0;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                callCount++;
                // Always throw non-transient error (e.g., constraint violation)
                throw new InvalidOperationException("Constraint violation - non-transient error");
            };

            // Act & Assert - Should fail immediately without retries for non-transient errors
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await behavior.HandleAsync(request, next, CancellationToken.None));

            Assert.Equal(1, callCount); // Should only be called once
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Handles_Cancellation_In_Database_Optimization()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            aiEngine.RecommendationToReturn = new OptimizationRecommendation
            {
                Strategy = OptimizationStrategy.DatabaseOptimization,
                ConfidenceScore = 0.9,
                Parameters = new Dictionary<string, object>
                {
                    ["EnableQueryOptimization"] = true,
                    ["MaxRetries"] = 3,
                     ["RetryDelayMs"] = 200 // Longer delay to ensure cancellation happens
                }
            };

            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var executed = false;
            RequestHandlerDelegate<TestResponse> next = () =>
            {
                executed = true;
                throw new TimeoutException("Database timeout - will trigger retry");
            };

            // Create a cancellation token that will cancel during retry delay
            var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Cancel during the 200ms delay

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await behavior.HandleAsync(request, next, cts.Token));

            Assert.True(executed);
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
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
                // Don't throw on cancellation here - let the optimization logic handle it

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
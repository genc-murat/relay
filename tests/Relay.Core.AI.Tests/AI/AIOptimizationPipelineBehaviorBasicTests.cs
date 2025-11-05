using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.AI;
using Relay.Core.AI.Pipeline.Behaviors;
using Relay.Core.AI.Pipeline.Metrics;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationPipelineBehaviorBasicTests
    {
        private readonly ServiceCollection _services;
        private readonly ILogger<AIOptimizationPipelineBehavior<TestRequest, TestResponse>> _logger;
        private readonly ILogger<SystemLoadMetricsProvider> _systemLogger;
        private readonly AIOptimizationOptions _options;

        public AIOptimizationPipelineBehaviorBasicTests()
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
        public async Task HandleAsync_Should_Execute_Without_Optimization_When_Disabled()
        {
            // Arrange
            var disabledOptions = new AIOptimizationOptions { Enabled = false };
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(disabledOptions),
                systemMetrics);

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
            Assert.False(aiEngine.AnalyzeCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Call_AIEngine_When_Enabled()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            var result = await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.Equal("success", result.Result);
            Assert.True(aiEngine.AnalyzeCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Learn_From_Successful_Execution()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Learn_From_Failed_Execution()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(_options),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                throw new InvalidOperationException("Test exception");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await behavior.HandleAsync(request, next, CancellationToken.None));

            Assert.True(aiEngine.AnalyzeCalled);
            Assert.True(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_Should_Not_Learn_When_Learning_Disabled()
        {
            // Arrange
            var disabledLearningOptions = new AIOptimizationOptions
            {
                Enabled = true,
                LearningEnabled = false
            };
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine,
                _logger,
                Options.Create(disabledLearningOptions),
                systemMetrics);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            await behavior.HandleAsync(request, next, CancellationToken.None);

            // Assert
            Assert.True(aiEngine.AnalyzeCalled);
            Assert.False(aiEngine.LearnCalled);
        }

        [Fact]
        public async Task HandleAsync_HandlesCancellationToken()
        {
            // Arrange
            var aiEngine = new MockAIOptimizationEngine();
            var systemMetrics = new SystemLoadMetricsProvider(_systemLogger);

            var behavior = new AIOptimizationPipelineBehavior<TestRequest, TestResponse>(
                aiEngine, _logger, Options.Create(_options), systemMetrics);

            var request = new TestRequest { Value = "test" };
            var cancellationToken = new CancellationToken(true); // Already cancelled

            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                await behavior.HandleAsync(request, next, cancellationToken));
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
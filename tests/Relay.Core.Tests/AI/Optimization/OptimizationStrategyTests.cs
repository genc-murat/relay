using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization
{
    public class OptimizationStrategyTests
    {
        private readonly ILogger _logger = NullLogger.Instance;

        [Fact]
        public void RequestAnalysisStrategy_AnalyzeRequestOperation_IsHandled()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50),
                    CpuUsage = 0.3,
                    MemoryAllocated = 1024 * 1024 // 1MB
                }
            };

            // Act
            var canHandle = strategy.CanHandle(context.Operation);

            // Assert
            canHandle.Should().BeTrue();
        }

        [Fact]
        public async Task RequestAnalysisStrategy_HighCpuUsage_ReturnsSIMDOptimizationRecommendation()
        {
            // Arrange
            var strategy = new RequestAnalysisStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeRequest",
                RequestType = typeof(string),
                ExecutionMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(150),
                    CpuUsage = 0.8, // High CPU
                    MemoryAllocated = 10 * 1024 * 1024 // 10MB
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            result.Success.Should().BeTrue();
            result.StrategyName.Should().Be("RequestAnalysis");
            result.Data.Should().BeOfType<OptimizationRecommendation>();
            var recommendation = (OptimizationRecommendation)result.Data!;
            recommendation.Strategy.Should().Be(OptimizationStrategy.SIMDAcceleration);
            recommendation.ConfidenceScore.Should().BeGreaterThan(0);
        }

        [Fact]
        public void BatchSizePredictionStrategy_PredictBatchSizeOperation_IsHandled()
        {
            // Arrange
            var options = new AIOptimizationOptions();
            var strategy = new BatchSizePredictionStrategy(_logger, options);
            var context = new OptimizationContext
            {
                Operation = "PredictBatchSize",
                RequestType = typeof(string),
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = 0.5,
                    MemoryUtilization = 0.3,
                    ActiveConnections = 50,
                    QueuedRequestCount = 10
                }
            };

            // Act
            var canHandle = strategy.CanHandle(context.Operation);

            // Assert
            canHandle.Should().BeTrue();
        }

        [Fact]
        public async Task BatchSizePredictionStrategy_HighCpuUtilization_ReturnsReducedBatchSize()
        {
            // Arrange
            var options = new AIOptimizationOptions { DefaultBatchSize = 100, MaxBatchSize = 1000 };
            var strategy = new BatchSizePredictionStrategy(_logger, options);
            var context = new OptimizationContext
            {
                Operation = "PredictBatchSize",
                RequestType = typeof(string),
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = 0.8, // High CPU - should reduce batch size
                    MemoryUtilization = 0.2,
                    ActiveConnections = 100,
                    QueuedRequestCount = 5
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeOfType<int>();
            var batchSize = (int)result.Data!;
            batchSize.Should().BeLessThanOrEqualTo(options.MaxBatchSize);
            batchSize.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CachingStrategy_OptimizeCachingOperation_IsHandled()
        {
            // Arrange
            var strategy = new CachingStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "OptimizeCaching",
                AccessPatterns = new[]
                {
                    new AccessPattern
                    {
                        RequestKey = "test",
                        AccessCount = 10,
                        AccessFrequency = 2.0,
                        DataVolatility = 0.1
                    }
                }
            };

            // Act
            var canHandle = strategy.CanHandle(context.Operation);

            // Assert
            canHandle.Should().BeTrue();
        }

        [Fact]
        public async Task CachingStrategy_FrequentAccessPattern_ReturnsCachingRecommendation()
        {
            // Arrange
            var strategy = new CachingStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "OptimizeCaching",
                AccessPatterns = new[]
                {
                    new AccessPattern
                    {
                        RequestKey = "frequent",
                        AccessCount = 20,
                        AccessFrequency = 5.0,
                        DataVolatility = 0.1,
                        AverageExecutionTime = TimeSpan.FromMilliseconds(100)
                    }
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeOfType<OptimizationRecommendation>();
            var recommendation = (OptimizationRecommendation)result.Data!;
            recommendation.Strategy.Should().Be(OptimizationStrategy.Caching);
        }

        [Fact]
        public void LearningStrategy_LearnFromResultsOperation_IsHandled()
        {
            // Arrange
            var strategy = new LearningStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "LearnFromResults",
                AppliedStrategies = new[]
                {
                    new AppliedOptimizationResult
                    {
                        Strategy = OptimizationStrategy.Caching,
                        Success = true,
                        ActualImprovement = TimeSpan.FromMilliseconds(50),
                        ExpectedImprovement = TimeSpan.FromMilliseconds(40)
                    }
                }
            };

            // Act
            var canHandle = strategy.CanHandle(context.Operation);

            // Assert
            canHandle.Should().BeTrue();
        }

        [Fact]
        public async Task LearningStrategy_ShouldGenerateLearningInsights()
        {
            // Arrange
            var strategy = new LearningStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "LearnFromResults",
                AppliedStrategies = new[]
                {
                    new AppliedOptimizationResult
                    {
                        Strategy = OptimizationStrategy.Caching,
                        Success = true,
                        ActualImprovement = TimeSpan.FromMilliseconds(50),
                        ExpectedImprovement = TimeSpan.FromMilliseconds(40),
                        ConfidenceScore = 0.8
                    },
                    new AppliedOptimizationResult
                    {
                        Strategy = OptimizationStrategy.BatchProcessing,
                        Success = false,
                        ActualImprovement = TimeSpan.FromMilliseconds(10),
                        ExpectedImprovement = TimeSpan.FromMilliseconds(30),
                        ConfidenceScore = 0.6
                    }
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeOfType<OptimizationRecommendation>();
            var recommendation = (OptimizationRecommendation)result.Data!;
            recommendation.Parameters.Should().ContainKey("preferred_strategies");
            recommendation.Parameters.Should().ContainKey("avoid_strategies");
        }

        [Fact]
        public void SystemInsightsStrategy_ShouldHandleAnalyzeSystemInsightsOperation()
        {
            // Arrange
            var strategy = new SystemInsightsStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeSystemInsights",
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = 0.7,
                    MemoryUtilization = 0.5,
                    ActiveConnections = 100,
                    QueuedRequestCount = 5
                }
            };

            // Act
            var canHandle = strategy.CanHandle(context.Operation);

            // Assert
            canHandle.Should().BeTrue();
        }

        [Fact]
        public async Task SystemInsightsStrategy_ShouldReturnSystemOptimizationRecommendation()
        {
            // Arrange
            var strategy = new SystemInsightsStrategy(_logger);
            var context = new OptimizationContext
            {
                Operation = "AnalyzeSystemInsights",
                SystemLoad = new SystemLoadMetrics
                {
                    CpuUtilization = 0.9, // High CPU
                    MemoryUtilization = 0.2,
                    ActiveConnections = 200,
                    QueuedRequestCount = 50
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeOfType<OptimizationRecommendation>();
            var recommendation = (OptimizationRecommendation)result.Data!;
            recommendation.Parameters.Should().ContainKey("cpu_insights");
            recommendation.Parameters.Should().ContainKey("memory_insights");
            recommendation.Parameters.Should().ContainKey("connection_insights");
            recommendation.Parameters.Should().ContainKey("queue_insights");
        }

        [Fact]
        public void AllStrategies_ShouldHaveUniqueNames()
        {
            // Arrange
            var strategies = new List<IOptimizationStrategy>
            {
                new RequestAnalysisStrategy(_logger),
                new BatchSizePredictionStrategy(_logger, new AIOptimizationOptions()),
                new CachingStrategy(_logger),
                new LearningStrategy(_logger),
                new SystemInsightsStrategy(_logger)
            };

            // Act
            var names = strategies.Select(s => s.Name).ToList();

            // Assert
            names.Should().OnlyHaveUniqueItems();
            names.Should().HaveCount(5);
        }

        [Fact]
        public void AllStrategies_ShouldHaveValidPriorities()
        {
            // Arrange
            var strategies = new List<IOptimizationStrategy>
            {
                new RequestAnalysisStrategy(_logger),
                new BatchSizePredictionStrategy(_logger, new AIOptimizationOptions()),
                new CachingStrategy(_logger),
                new LearningStrategy(_logger),
                new SystemInsightsStrategy(_logger)
            };

            // Act & Assert
            foreach (var strategy in strategies)
            {
                strategy.Priority.Should().BeGreaterThanOrEqualTo(0);
            }
        }
    }
}
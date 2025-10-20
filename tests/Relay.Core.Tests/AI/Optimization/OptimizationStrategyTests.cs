using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Strategies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            Assert.True(canHandle);
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
            Assert.True(result.Success);
            Assert.Equal("RequestAnalysis", result.StrategyName);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.SIMDAcceleration, recommendation.Strategy);
            Assert.True(recommendation.ConfidenceScore > 0);
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
            Assert.True(canHandle);
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
            Assert.True(result.Success);
            Assert.IsType<int>(result.Data);
            var batchSize = (int)result.Data!;
            Assert.True(batchSize <= options.MaxBatchSize);
            Assert.True(batchSize > 0);
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
            Assert.True(canHandle);
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
            Assert.True(result.Success);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Equal(OptimizationStrategy.Caching, recommendation.Strategy);
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
            Assert.True(canHandle);
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
            Assert.True(result.Success);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Contains("preferred_strategies", recommendation.Parameters.Keys);
            Assert.Contains("avoid_strategies", recommendation.Parameters.Keys);
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
            Assert.True(canHandle);
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
            Assert.True(result.Success);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Contains("cpu_insights", recommendation.Parameters.Keys);
            Assert.Contains("memory_insights", recommendation.Parameters.Keys);
            Assert.Contains("connection_insights", recommendation.Parameters.Keys);
            Assert.Contains("queue_insights", recommendation.Parameters.Keys);
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
            Assert.Equal(5, names.Distinct().Count());
            Assert.Equal(5, names.Count);
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
                Assert.True(strategy.Priority >= 0);
            }
        }
    }
}
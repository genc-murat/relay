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
    public class LearningStrategyTests
    {
        private readonly ILogger _logger = NullLogger.Instance;

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
        public async Task LearningStrategy_ShouldCalculateConfidenceCorrelation_WithMultipleSuccessfulStrategies()
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
                        Strategy = OptimizationStrategy.MemoryPooling,
                        Success = true,
                        ActualImprovement = TimeSpan.FromMilliseconds(30),
                        ExpectedImprovement = TimeSpan.FromMilliseconds(35),
                        ConfidenceScore = 0.6
                    },
                    new AppliedOptimizationResult
                    {
                        Strategy = OptimizationStrategy.StreamingOptimization,
                        Success = true,
                        ActualImprovement = TimeSpan.FromMilliseconds(80),
                        ExpectedImprovement = TimeSpan.FromMilliseconds(70),
                        ConfidenceScore = 0.9
                    },
                    new AppliedOptimizationResult
                    {
                        Strategy = OptimizationStrategy.BatchProcessing,
                        Success = false,
                        ActualImprovement = TimeSpan.FromMilliseconds(10),
                        ExpectedImprovement = TimeSpan.FromMilliseconds(30),
                        ConfidenceScore = 0.5
                    }
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Contains("insights", recommendation.Parameters.Keys);

            var insights = recommendation.Parameters["insights"] as Dictionary<string, object>;
            Assert.NotNull(insights);
            Assert.Contains("confidence_correlation", insights.Keys);

            var correlation = (double)insights["confidence_correlation"];
            // Correlation should be a valid double value (between -1 and 1)
            Assert.True(correlation >= -1.0 && correlation <= 1.0);
        }

        [Fact]
        public async Task LearningStrategy_ShouldHandleCorrelationCalculation_WithInsufficientData()
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
                        ExpectedImprovement = null, // No expected improvement
                        ConfidenceScore = 0.8
                    }
                }
            };

            // Act
            var result = await strategy.ExecuteAsync(context);

            // Assert
            Assert.True(result.Success);
            Assert.IsType<OptimizationRecommendation>(result.Data);
            var recommendation = (OptimizationRecommendation)result.Data!;
            Assert.Contains("insights", recommendation.Parameters.Keys);

            var insights = recommendation.Parameters["insights"] as Dictionary<string, object>;
            Assert.NotNull(insights);
            Assert.Contains("confidence_correlation", insights.Keys);

            var correlation = (double)insights["confidence_correlation"];
            // With insufficient data, correlation should be 0
            Assert.Equal(0.0, correlation);
        }
    }
}
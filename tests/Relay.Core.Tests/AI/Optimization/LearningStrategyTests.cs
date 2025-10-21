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
    }
}
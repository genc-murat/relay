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
    public class CachingStrategyTests
    {
        private readonly ILogger _logger = NullLogger.Instance;

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
    }
}
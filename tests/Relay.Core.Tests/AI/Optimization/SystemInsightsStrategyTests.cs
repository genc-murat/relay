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
    public class SystemInsightsStrategyTests
    {
        private readonly ILogger _logger = NullLogger.Instance;

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
    }
}
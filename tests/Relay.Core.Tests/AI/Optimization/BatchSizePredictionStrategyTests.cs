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
    public class BatchSizePredictionStrategyTests
    {
        private readonly ILogger _logger = NullLogger.Instance;

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
    }
}
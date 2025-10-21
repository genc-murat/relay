using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Optimization.Data;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineTestBase : IDisposable
    {
        protected readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
        protected readonly AIOptimizationOptions _options;
        protected readonly AIOptimizationEngine _engine;

        public AIOptimizationEngineTestBase()
        {
            _loggerMock = new Mock<ILogger<AIOptimizationEngine>>();
            _options = new AIOptimizationOptions
            {
                DefaultBatchSize = 10,
                MaxBatchSize = 100,
                ModelUpdateInterval = TimeSpan.FromMinutes(5),
                ModelTrainingDate = DateTime.UtcNow,
                ModelVersion = "1.0.0",
                LastRetrainingDate = DateTime.UtcNow.AddDays(-1)
            };

            var optionsMock = new Mock<IOptions<AIOptimizationOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            _engine = new AIOptimizationEngine(_loggerMock.Object, optionsMock.Object);
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }

        protected RequestExecutionMetrics CreateMetrics(int executionCount = 100, TimeSpan? averageExecutionTime = null, int databaseCalls = 2, int externalApiCalls = 1, int failedExecutions = -1)
        {
            var avgTime = averageExecutionTime ?? TimeSpan.FromMilliseconds(100);
            var failed = failedExecutions >= 0 ? failedExecutions : executionCount / 10; // Default 10% failure rate
            return new RequestExecutionMetrics
            {
                AverageExecutionTime = avgTime,
                MedianExecutionTime = avgTime - TimeSpan.FromMilliseconds(5),
                P95ExecutionTime = avgTime + TimeSpan.FromMilliseconds(50),
                P99ExecutionTime = avgTime + TimeSpan.FromMilliseconds(100),
                TotalExecutions = executionCount,
                SuccessfulExecutions = executionCount - failed,
                FailedExecutions = failed,
                MemoryAllocated = 1024 * 1024,
                ConcurrentExecutions = 10,
                LastExecution = DateTime.UtcNow,
                SamplePeriod = TimeSpan.FromMinutes(5),
                CpuUsage = 0.45,
                MemoryUsage = 512 * 1024,
                DatabaseCalls = databaseCalls,
                ExternalApiCalls = externalApiCalls
            };
        }

        #region Test Types

        protected class TestRequest { }
        protected class OtherTestRequest { }
        protected class ThirdTestRequest { }
        protected class FourthTestRequest { }
        protected class FifthTestRequest { }
        protected class SixthTestRequest { }
        protected class SeventhTestRequest { }
        protected class EighthTestRequest { }

        #endregion
    }
}
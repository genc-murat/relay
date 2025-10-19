using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Batching;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class BatchCoordinatorDisposalTests
    {
        private readonly ILogger _logger;

        public BatchCoordinatorDisposalTests()
        {
            _logger = NullLogger.Instance;
        }

        [Fact]
        public void BatchCoordinator_Should_Implement_IDisposable()
        {
            // Arrange
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: 10,
                batchWindow: TimeSpan.FromSeconds(1),
                maxWaitTime: TimeSpan.FromSeconds(5),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            // Act & Assert (should not throw)
            coordinator.Dispose();
        }

        [Fact]
        public void Dispose_Should_Be_Idempotent()
        {
            // Arrange
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: 10,
                batchWindow: TimeSpan.FromSeconds(1),
                maxWaitTime: TimeSpan.FromSeconds(5),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            // Act & Assert - Multiple dispose calls should not throw
            coordinator.Dispose();
            coordinator.Dispose();
            coordinator.Dispose();
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
    }
}
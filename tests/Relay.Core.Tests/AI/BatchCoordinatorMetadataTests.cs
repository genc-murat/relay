using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Batching;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class BatchCoordinatorMetadataTests
    {
        private readonly ILogger _logger;

        public BatchCoordinatorMetadataTests()
        {
            _logger = NullLogger.Instance;
        }

        [Fact]
        public void GetMetadata_Should_Return_Null_Initially()
        {
            // Arrange
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: 10,
                batchWindow: TimeSpan.FromSeconds(1),
                maxWaitTime: TimeSpan.FromSeconds(5),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            // Act
            var metadata = coordinator.GetMetadata();

            // Assert - initially metadata is null until set
            Assert.Null(metadata);
        }

        [Fact]
        public void BatchCoordinatorMetadata_Should_Store_Configuration()
        {
            // Arrange
            var metadata = new BatchCoordinatorMetadata
            {
                BatchSize = 10,
                BatchWindow = TimeSpan.FromSeconds(1),
                MaxWaitTime = TimeSpan.FromSeconds(5),
                Strategy = BatchingStrategy.SizeAndTime,
                CreatedAt = DateTime.UtcNow,
                RequestCount = 100,
                LastUsed = DateTime.UtcNow,
                AverageWaitTime = 50.0,
                AverageBatchSize = 8.5
            };

            // Assert
            Assert.Equal(10, metadata.BatchSize);
            Assert.Equal(TimeSpan.FromSeconds(1), metadata.BatchWindow);
            Assert.Equal(BatchingStrategy.SizeAndTime, metadata.Strategy);
            Assert.Equal(100, metadata.RequestCount);
            Assert.Equal(8.5, metadata.AverageBatchSize);
        }

        [Fact]
        public void BatchCoordinatorMetadata_Should_Track_Usage_Metrics()
        {
            // Arrange
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: 10,
                batchWindow: TimeSpan.FromSeconds(1),
                maxWaitTime: TimeSpan.FromSeconds(5),
                strategy: BatchingStrategy.Adaptive,
                logger: _logger);

            var metadata = new BatchCoordinatorMetadata
            {
                BatchSize = 10,
                BatchWindow = TimeSpan.FromSeconds(1),
                MaxWaitTime = TimeSpan.FromSeconds(5),
                Strategy = BatchingStrategy.Adaptive,
                CreatedAt = DateTime.UtcNow,
                RequestCount = 0,
                LastUsed = DateTime.UtcNow,
                AverageWaitTime = 0.0,
                AverageBatchSize = 0.0
            };

            coordinator.Metadata = metadata;

            // Act
            var retrievedMetadata = coordinator.GetMetadata();

            // Assert
            Assert.NotNull(retrievedMetadata);
            Assert.Equal(BatchingStrategy.Adaptive, retrievedMetadata.Strategy);
            Assert.Equal(10, retrievedMetadata.BatchSize);
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
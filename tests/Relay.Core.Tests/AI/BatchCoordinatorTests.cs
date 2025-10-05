using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class BatchCoordinatorTests
    {
        private readonly ILogger _logger;

        public BatchCoordinatorTests()
        {
            _logger = NullLogger.Instance;
        }

        [Fact]
        public void Constructor_Should_Initialize_With_Valid_Parameters()
        {
            // Arrange & Act
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: 10,
                batchWindow: TimeSpan.FromSeconds(1),
                maxWaitTime: TimeSpan.FromSeconds(5),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            // Assert
            Assert.NotNull(coordinator);
        }

        [Fact]
        public async Task EnqueueAndWaitAsync_Should_Execute_Single_Item_On_Timeout()
        {
            // Arrange
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: 10,
                batchWindow: TimeSpan.FromSeconds(10),
                maxWaitTime: TimeSpan.FromMilliseconds(500),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            var request = new TestRequest { Value = "test" };
            var executed = false;

            RequestHandlerDelegate<TestResponse> handler = () =>
            {
                executed = true;
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            var item = new BatchItem<TestRequest, TestResponse>
            {
                Request = request,
                Handler = handler,
                CancellationToken = CancellationToken.None,
                EnqueueTime = DateTime.UtcNow,
                BatchId = Guid.NewGuid()
            };

            // Act
            var result = await coordinator.EnqueueAndWaitAsync(item, CancellationToken.None);

            // Assert
            Assert.True(executed);
            Assert.Equal("success", result.Response.Result);
            Assert.Equal(1, result.BatchSize);
        }

        [Fact]
        public async Task EnqueueAndWaitAsync_Should_Batch_Multiple_Items()
        {
            // Arrange
            var batchSize = 3;
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: batchSize,
                batchWindow: TimeSpan.FromSeconds(10),
                maxWaitTime: TimeSpan.FromSeconds(5),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            var tasks = new Task<BatchExecutionResult<TestResponse>>[batchSize];

            // Act
            for (int i = 0; i < batchSize; i++)
            {
                var index = i;
                var item = new BatchItem<TestRequest, TestResponse>
                {
                    Request = new TestRequest { Value = $"test{index}" },
                    Handler = () => new ValueTask<TestResponse>(new TestResponse { Result = $"result{index}" }),
                    CancellationToken = CancellationToken.None,
                    EnqueueTime = DateTime.UtcNow,
                    BatchId = Guid.NewGuid()
                };

                tasks[i] = coordinator.EnqueueAndWaitAsync(item, CancellationToken.None).AsTask();
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result =>
            {
                Assert.Equal(batchSize, result.BatchSize);
                Assert.True(result.Success);
            });
        }

        [Fact]
        public async Task EnqueueAndWaitAsync_Should_Handle_Handler_Exception()
        {
            // Arrange
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: 10,
                batchWindow: TimeSpan.FromSeconds(1),
                maxWaitTime: TimeSpan.FromMilliseconds(500),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            var request = new TestRequest { Value = "test" };
            RequestHandlerDelegate<TestResponse> handler = () =>
                throw new InvalidOperationException("Test exception");

            var item = new BatchItem<TestRequest, TestResponse>
            {
                Request = request,
                Handler = handler,
                CancellationToken = CancellationToken.None,
                EnqueueTime = DateTime.UtcNow,
                BatchId = Guid.NewGuid()
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await coordinator.EnqueueAndWaitAsync(item, CancellationToken.None));
        }

        [Fact]
        public async Task BatchCoordinator_Should_Calculate_Efficiency()
        {
            // Arrange
            var batchSize = 2;
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: batchSize,
                batchWindow: TimeSpan.FromSeconds(10),
                maxWaitTime: TimeSpan.FromSeconds(5),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            var tasks = new Task<BatchExecutionResult<TestResponse>>[batchSize];

            // Act
            for (int i = 0; i < batchSize; i++)
            {
                var item = new BatchItem<TestRequest, TestResponse>
                {
                    Request = new TestRequest { Value = $"test{i}" },
                    Handler = async () =>
                    {
                        await Task.Delay(10);
                        return new TestResponse { Result = $"result{i}" };
                    },
                    CancellationToken = CancellationToken.None,
                    EnqueueTime = DateTime.UtcNow,
                    BatchId = Guid.NewGuid()
                };

                tasks[i] = coordinator.EnqueueAndWaitAsync(item, CancellationToken.None).AsTask();
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result =>
            {
                Assert.InRange(result.Efficiency, 0.0, 1.0);
            });
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
        public void BatchExecutionResult_Should_Store_Execution_Details()
        {
            // Arrange
            var result = new BatchExecutionResult<TestResponse>
            {
                Response = new TestResponse { Result = "success" },
                BatchSize = 5,
                WaitTime = TimeSpan.FromMilliseconds(100),
                ExecutionTime = TimeSpan.FromMilliseconds(50),
                Success = true,
                Strategy = BatchingStrategy.SizeAndTime,
                Efficiency = 0.85
            };

            // Assert
            Assert.Equal("success", result.Response.Result);
            Assert.Equal(5, result.BatchSize);
            Assert.Equal(TimeSpan.FromMilliseconds(100), result.WaitTime);
            Assert.Equal(TimeSpan.FromMilliseconds(50), result.ExecutionTime);
            Assert.True(result.Success);
            Assert.Equal(0.85, result.Efficiency);
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

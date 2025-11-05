using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Batching;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class BatchCoordinatorEnqueueTests
    {
        private readonly ILogger _logger;

        public BatchCoordinatorEnqueueTests()
        {
            _logger = NullLogger.Instance;
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
            Relay.Core.Contracts.Pipeline.RequestHandlerDelegate<TestResponse> handler = () =>
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
        public async Task EnqueueAndWaitAsync_Should_Handle_Cancellation_Gracefully()
        {
            // Arrange
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: 10,
                batchWindow: TimeSpan.FromSeconds(10),
                maxWaitTime: TimeSpan.FromSeconds(10),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            var item = new BatchItem<TestRequest, TestResponse>
            {
                Request = new TestRequest { Value = "test" },
                Handler = () => new ValueTask<TestResponse>(new TestResponse { Result = "success" }),
                CancellationToken = CancellationToken.None,
                EnqueueTime = DateTime.UtcNow,
                BatchId = Guid.NewGuid()
            };

            // Act - When cancelled, the coordinator should fall back to individual execution
            var result = await coordinator.EnqueueAndWaitAsync(item, cts.Token);

            // Assert - Should execute individually with batch size 1
            Assert.NotNull(result);
            Assert.Equal(1, result.BatchSize);
            Assert.True(result.Success);
        }

        [Fact]
        public async Task EnqueueAndWaitAsync_Should_Process_Partial_Batch()
        {
            // Arrange
            var batchSize = 5;
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: batchSize,
                batchWindow: TimeSpan.FromMilliseconds(100),
                maxWaitTime: TimeSpan.FromSeconds(5),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            var itemsToEnqueue = 3; // Less than batch size
            var tasks = new Task<BatchExecutionResult<TestResponse>>[itemsToEnqueue];

            // Act
            for (int i = 0; i < itemsToEnqueue; i++)
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

            // Wait for batch window to trigger processing
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result =>
            {
                Assert.Equal(itemsToEnqueue, result.BatchSize);
                Assert.True(result.Success);
            });
        }

        [Fact]
        public async Task EnqueueAndWaitAsync_Should_Process_Multiple_Batches()
        {
            // Arrange
            var batchSize = 2;
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: batchSize,
                batchWindow: TimeSpan.FromSeconds(10),
                maxWaitTime: TimeSpan.FromSeconds(5),
                strategy: BatchingStrategy.Fixed,
                logger: _logger);

            var totalItems = batchSize * 3; // Create 3 full batches
            var tasks = new Task<BatchExecutionResult<TestResponse>>[totalItems];

            // Act
            for (int i = 0; i < totalItems; i++)
            {
                var index = i;
                var item = new BatchItem<TestRequest, TestResponse>
                {
                    Request = new TestRequest { Value = $"test{index}" },
                    Handler = async () =>
                    {
                        await Task.Delay(10);
                        return new TestResponse { Result = $"result{index}" };
                    },
                    CancellationToken = CancellationToken.None,
                    EnqueueTime = DateTime.UtcNow,
                    BatchId = Guid.NewGuid()
                };

                tasks[i] = coordinator.EnqueueAndWaitAsync(item, CancellationToken.None).AsTask();
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(totalItems, results.Length);
            Assert.All(results, result => Assert.True(result.Success));
        }

        [Fact]
        public async Task EnqueueAndWaitAsync_Should_Handle_Mixed_Success_And_Failure()
        {
            // Arrange
            var batchSize = 3;
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: batchSize,
                batchWindow: TimeSpan.FromSeconds(10),
                maxWaitTime: TimeSpan.FromSeconds(5),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            var tasks = new List<Task>();

            // Act - Add items that will succeed and fail
            for (int i = 0; i < batchSize; i++)
            {
                var index = i;
                var shouldFail = i == 1; // Make the middle one fail

                var item = new BatchItem<TestRequest, TestResponse>
                {
                    Request = new TestRequest { Value = $"test{index}" },
                    Handler = () =>
                    {
                        if (shouldFail)
                            throw new InvalidOperationException($"Intentional failure for item {index}");
                        return new ValueTask<TestResponse>(new TestResponse { Result = $"result{index}" });
                    },
                    CancellationToken = CancellationToken.None,
                    EnqueueTime = DateTime.UtcNow,
                    BatchId = Guid.NewGuid()
                };

                if (shouldFail)
                {
                    tasks.Add(Assert.ThrowsAsync<InvalidOperationException>(async () =>
                        await coordinator.EnqueueAndWaitAsync(item, CancellationToken.None)));
                }
                else
                {
                    tasks.Add(coordinator.EnqueueAndWaitAsync(item, CancellationToken.None).AsTask());
                }
            }

            // Assert - Should handle both successes and failures
            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task EnqueueAndWaitAsync_Should_Calculate_Correct_Efficiency()
        {
            // Arrange
            var batchSize = 5;
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: batchSize,
                batchWindow: TimeSpan.FromSeconds(10),
                maxWaitTime: TimeSpan.FromSeconds(2),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            var tasks = new Task<BatchExecutionResult<TestResponse>>[batchSize];

            // Act - Enqueue all items quickly to minimize wait time
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

            // Assert - Efficiency should be high when batch is full and wait time is low
            Assert.All(results, result =>
            {
                Assert.InRange(result.Efficiency, 0.0, 1.0);
                Assert.Equal(batchSize, result.BatchSize);
            });
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
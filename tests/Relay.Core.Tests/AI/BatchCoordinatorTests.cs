using System;
using System.Collections.Generic;
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
        public async Task BatchCoordinator_Should_Track_Wait_Times()
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
            var enqueueTime = DateTime.UtcNow;

            // Act
            for (int i = 0; i < batchSize; i++)
            {
                var item = new BatchItem<TestRequest, TestResponse>
                {
                    Request = new TestRequest { Value = $"test{i}" },
                    Handler = () => new ValueTask<TestResponse>(new TestResponse { Result = $"result{i}" }),
                    CancellationToken = CancellationToken.None,
                    EnqueueTime = enqueueTime,
                    BatchId = Guid.NewGuid()
                };

                tasks[i] = coordinator.EnqueueAndWaitAsync(item, CancellationToken.None).AsTask();
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, result =>
            {
                Assert.True(result.WaitTime >= TimeSpan.Zero);
                Assert.True(result.WaitTime < TimeSpan.FromSeconds(5)); // Should be less than maxWaitTime
            });
        }

        [Fact]
        public void BatchCoordinator_Should_Support_Different_Strategies()
        {
            // Arrange & Act
            var strategies = new[]
            {
                BatchingStrategy.Fixed,
                BatchingStrategy.Dynamic,
                BatchingStrategy.AIPredictive,
                BatchingStrategy.TimeBased,
                BatchingStrategy.Adaptive,
                BatchingStrategy.SizeAndTime
            };

            // Assert - All strategies should be supported
            foreach (var strategy in strategies)
            {
                var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                    batchSize: 10,
                    batchWindow: TimeSpan.FromSeconds(1),
                    maxWaitTime: TimeSpan.FromSeconds(5),
                    strategy: strategy,
                    logger: _logger);

                Assert.NotNull(coordinator);
                coordinator.Dispose();
            }
        }

        [Fact]
        public async Task BatchCoordinator_Should_Handle_Async_Handlers()
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
                var index = i;
                var item = new BatchItem<TestRequest, TestResponse>
                {
                    Request = new TestRequest { Value = $"test{index}" },
                    Handler = async () =>
                    {
                        await Task.Delay(50); // Simulate async work
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
            Assert.All(results, result =>
            {
                Assert.True(result.Success);
                Assert.True(result.ExecutionTime.TotalMilliseconds >= 0);
            });
        }

        [Fact]
        public void BatchCoordinator_Should_Be_Thread_Safe()
        {
            // Arrange
            var coordinator = new BatchCoordinator<TestRequest, TestResponse>(
                batchSize: 100,
                batchWindow: TimeSpan.FromSeconds(1),
                maxWaitTime: TimeSpan.FromSeconds(10),
                strategy: BatchingStrategy.SizeAndTime,
                logger: _logger);

            var itemCount = 50;
            var tasks = new Task<BatchExecutionResult<TestResponse>>[itemCount];

            // Act - Enqueue from multiple threads
            Parallel.For(0, itemCount, i =>
            {
                var item = new BatchItem<TestRequest, TestResponse>
                {
                    Request = new TestRequest { Value = $"test{i}" },
                    Handler = () => new ValueTask<TestResponse>(new TestResponse { Result = $"result{i}" }),
                    CancellationToken = CancellationToken.None,
                    EnqueueTime = DateTime.UtcNow,
                    BatchId = Guid.NewGuid()
                };

                tasks[i] = coordinator.EnqueueAndWaitAsync(item, CancellationToken.None).AsTask();
            });

            // Wait for all to complete
            Task.WaitAll(tasks, TimeSpan.FromSeconds(15));

            // Assert
            Assert.All(tasks, task => Assert.True(task.IsCompleted));
        }

        [Fact]
        public void BatchItem_Should_Store_Request_Details()
        {
            // Arrange
            var request = new TestRequest { Value = "test" };
            var enqueueTime = DateTime.UtcNow;
            var batchId = Guid.NewGuid();
            var cts = new CancellationTokenSource();

            RequestHandlerDelegate<TestResponse> handler = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = "success" });

            // Act
            var item = new BatchItem<TestRequest, TestResponse>
            {
                Request = request,
                Handler = handler,
                CancellationToken = cts.Token,
                EnqueueTime = enqueueTime,
                BatchId = batchId
            };

            // Assert
            Assert.Equal(request, item.Request);
            Assert.Equal(handler, item.Handler);
            Assert.Equal(cts.Token, item.CancellationToken);
            Assert.Equal(enqueueTime, item.EnqueueTime);
            Assert.Equal(batchId, item.BatchId);
            Assert.NotNull(item.CompletionSource);
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

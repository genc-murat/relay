using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Batching;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class BatchCoordinatorThreadSafetyTests
    {
        private readonly ILogger _logger;

        public BatchCoordinatorThreadSafetyTests()
        {
            _logger = NullLogger.Instance;
        }

        [Fact]
        public async Task BatchCoordinator_Should_Be_Thread_Safe()
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
            await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(15));

            // Assert
            Assert.All(tasks, task => Assert.True(task.IsCompleted));
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
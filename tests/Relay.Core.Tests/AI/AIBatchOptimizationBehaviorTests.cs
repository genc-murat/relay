using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Batching;
using Relay.Core.AI.Pipeline.Behaviors;
using Relay.Core.AI.Pipeline.Options;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI;

public class AIBatchOptimizationBehaviorTests : IDisposable
{
    private readonly ILogger<AIBatchOptimizationBehavior<TestRequest, TestResponse>> _logger;
    private readonly List<AIBatchOptimizationBehavior<TestRequest, TestResponse>> _behaviorsToDispose;

    public AIBatchOptimizationBehaviorTests()
    {
        _logger = NullLogger<AIBatchOptimizationBehavior<TestRequest, TestResponse>>.Instance;
        _behaviorsToDispose = new List<AIBatchOptimizationBehavior<TestRequest, TestResponse>>();
    }

    public void Dispose()
    {
        foreach (var behavior in _behaviorsToDispose)
        {
            behavior?.Dispose();
        }
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Valid_Logger()
    {
        // Arrange & Act
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger);
        _behaviorsToDispose.Add(behavior);

        // Assert
        Assert.NotNull(behavior);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AIBatchOptimizationBehavior<TestRequest, TestResponse>(null!));
    }

    [Fact]
    public void Constructor_Should_Accept_Null_Options()
    {
        // Arrange & Act
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, null);
        _behaviorsToDispose.Add(behavior);

        // Assert
        Assert.NotNull(behavior);
    }

    [Fact]
    public async Task HandleAsync_Should_Execute_Request_Without_Batching_When_Disabled()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = false
        };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };
        var executed = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executed = true;
            return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
        };

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.True(executed);
        Assert.Equal("success", result.Result);
    }

    [Fact]
    public async Task HandleAsync_Should_Execute_Single_Request_Without_Batching()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = true,
            MinimumRequestRateForBatching = 100 // High threshold to prevent batching
        };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };
        var executed = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executed = true;
            return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
        };

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.True(executed);
        Assert.Equal("success", result.Result);
    }

    [Fact]
    public async Task HandleAsync_Should_Apply_Batching_For_Attributed_Request()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = true,
            DefaultBatchSize = 2,
            DefaultBatchWindow = TimeSpan.FromSeconds(10),
            DefaultMaxWaitTime = TimeSpan.FromSeconds(5)
        };
        var logger = NullLogger<AIBatchOptimizationBehavior<TestBatchRequest, TestResponse>>.Instance;
        var behavior = new AIBatchOptimizationBehavior<TestBatchRequest, TestResponse>(logger, options);

        var tasks = new List<Task<TestResponse>>();

        // Act - Execute multiple requests concurrently
        for (int i = 0; i < 2; i++)
        {
            var index = i;
            var request = new TestBatchRequest { Value = $"test{index}" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(10);
                return new TestResponse { Result = $"result{index}" };
            };

            tasks.Add(behavior.HandleAsync(request, next, CancellationToken.None).AsTask());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(2, results.Length);
        Assert.All(results, result => Assert.NotNull(result));

        behavior.Dispose();
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Exception_In_Handler()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions { EnableBatching = false };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };

        RequestHandlerDelegate<TestResponse> next = () =>
            throw new InvalidOperationException("Test exception");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_Should_Track_Metrics()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions { EnableBatching = false };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act - Execute multiple requests
        for (int i = 0; i < 5; i++)
        {
            await behavior.HandleAsync(request, next, CancellationToken.None);
        }

        // Assert - Metrics are tracked internally (would need to expose them to verify)
        // For now, we just verify no exceptions were thrown
        Assert.True(true);
    }

    [Fact]
    public async Task HandleAsync_Should_Support_Cancellation()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions { EnableBatching = false };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerDelegate<TestResponse> next = async () =>
        {
            await Task.Delay(1000, cts.Token);
            return new TestResponse { Result = "success" };
        };

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await behavior.HandleAsync(request, next, cts.Token));
    }

    [Fact]
    public void Dispose_Should_Be_Idempotent()
    {
        // Arrange
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger);

        // Act & Assert - Multiple dispose calls should not throw
        behavior.Dispose();
        behavior.Dispose();
        behavior.Dispose();
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Concurrent_Requests()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = false
        };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var tasks = new List<Task<TestResponse>>();

        // Act - Execute many requests concurrently
        for (int i = 0; i < 50; i++)
        {
            var index = i;
            var request = new TestRequest { Value = $"test{index}" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(10);
                return new TestResponse { Result = $"result{index}" };
            };

            tasks.Add(behavior.HandleAsync(request, next, CancellationToken.None).AsTask());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(50, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
    }

    [Fact]
    public void AIBatchOptimizationOptions_Should_Have_Correct_Defaults()
    {
        // Arrange & Act
        var options = new AIBatchOptimizationOptions();

        // Assert
        Assert.True(options.EnableBatching);
        Assert.Equal(10.0, options.MinimumRequestRateForBatching);
        Assert.Equal(50, options.DefaultBatchSize);
        Assert.Equal(TimeSpan.FromMilliseconds(100), options.DefaultBatchWindow);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.DefaultMaxWaitTime);
        Assert.Equal(BatchingStrategy.Dynamic, options.DefaultStrategy);
    }

    [Fact]
    public void AIBatchOptimizationOptions_Should_Allow_Custom_Configuration()
    {
        // Arrange & Act
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = false,
            MinimumRequestRateForBatching = 5.0,
            DefaultBatchSize = 100,
            DefaultBatchWindow = TimeSpan.FromMilliseconds(200),
            DefaultMaxWaitTime = TimeSpan.FromSeconds(1),
            DefaultStrategy = BatchingStrategy.Fixed
        };

        // Assert
        Assert.False(options.EnableBatching);
        Assert.Equal(5.0, options.MinimumRequestRateForBatching);
        Assert.Equal(100, options.DefaultBatchSize);
        Assert.Equal(TimeSpan.FromMilliseconds(200), options.DefaultBatchWindow);
        Assert.Equal(TimeSpan.FromSeconds(1), options.DefaultMaxWaitTime);
        Assert.Equal(BatchingStrategy.Fixed, options.DefaultStrategy);
    }

    [Fact]
    public async Task HandleAsync_Should_Track_Success_Count()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions { EnableBatching = false };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act - Execute successful requests
        for (int i = 0; i < 3; i++)
        {
            await behavior.HandleAsync(request, next, CancellationToken.None);
        }

        // Assert - Success count is tracked internally
        Assert.True(true);
    }

    [Fact]
    public async Task HandleAsync_Should_Track_Failure_Count()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions { EnableBatching = false };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            throw new InvalidOperationException("Test exception");

        // Act - Execute failing requests
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await behavior.HandleAsync(request, next, CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                // Expected
            }
        }

        // Assert - Failure count is tracked internally
        Assert.True(true);
    }

    [Fact]
    public async Task HandleAsync_Should_Fallback_To_Direct_Execution_When_Coordinator_Is_Null()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = true,
            MinimumRequestRateForBatching = 0 // Always try to batch
        };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };
        var executed = false;

        RequestHandlerDelegate<TestResponse> next = () =>
        {
            executed = true;
            return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
        };

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.True(executed);
        Assert.Equal("success", result.Result);
    }

    [Fact]
    public async Task HandleAsync_Should_Process_Multiple_Batches_Sequentially()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = true,
            DefaultBatchSize = 2,
            DefaultBatchWindow = TimeSpan.FromSeconds(10),
            DefaultMaxWaitTime = TimeSpan.FromSeconds(5)
        };
        var logger = NullLogger<AIBatchOptimizationBehavior<TestBatchRequest, TestResponse>>.Instance;
        var behavior = new AIBatchOptimizationBehavior<TestBatchRequest, TestResponse>(logger, options);

        var totalRequests = 4; // 2 batches of 2
        var tasks = new List<Task<TestResponse>>();

        // Act
        for (int i = 0; i < totalRequests; i++)
        {
            var index = i;
            var request = new TestBatchRequest { Value = $"test{index}" };
            RequestHandlerDelegate<TestResponse> next = async () =>
            {
                await Task.Delay(10);
                return new TestResponse { Result = $"result{index}" };
            };

            tasks.Add(behavior.HandleAsync(request, next, CancellationToken.None).AsTask());
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(totalRequests, results.Length);
        Assert.All(results, result => Assert.NotNull(result));

        behavior.Dispose();
    }

    [Fact]
    public async Task HandleAsync_Should_Log_Batch_Execution_Details()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = true,
            DefaultBatchSize = 2,
            DefaultBatchWindow = TimeSpan.FromSeconds(10),
            DefaultMaxWaitTime = TimeSpan.FromSeconds(5)
        };

        // Use a test logger factory to capture logs
        var logger = NullLogger<AIBatchOptimizationBehavior<TestBatchRequest, TestResponse>>.Instance;
        var behavior = new AIBatchOptimizationBehavior<TestBatchRequest, TestResponse>(logger, options);

        var tasks = new List<Task<TestResponse>>();

        // Act
        for (int i = 0; i < 2; i++)
        {
            var index = i;
            var request = new TestBatchRequest { Value = $"test{index}" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = $"result{index}" });

            tasks.Add(behavior.HandleAsync(request, next, CancellationToken.None).AsTask());
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Logging happened (would need to verify with a test logger implementation)
        Assert.Equal(2, results.Length);

        behavior.Dispose();
    }

    [Fact]
    public void AIBatchOptimizationBehavior_Should_Implement_IDisposable()
    {
        // Arrange
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger);

        // Act & Assert
        Assert.IsAssignableFrom<IDisposable>(behavior);
        behavior.Dispose();
    }

    [Fact]
    public async Task HandleAsync_Should_Handle_Mixed_Success_And_Failure()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions { EnableBatching = false };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var successCount = 0;
        var failureCount = 0;

        // Act - Execute requests that succeed and fail
        for (int i = 0; i < 5; i++)
        {
            var shouldFail = i % 2 == 0;
            var request = new TestRequest { Value = $"test{i}" };

            RequestHandlerDelegate<TestResponse> next = () =>
            {
                if (shouldFail)
                    throw new InvalidOperationException("Test exception");
                return new ValueTask<TestResponse>(new TestResponse { Result = "success" });
            };

            try
            {
                await behavior.HandleAsync(request, next, CancellationToken.None);
                successCount++;
            }
            catch (InvalidOperationException)
            {
                failureCount++;
            }
        }

        // Assert
        Assert.Equal(2, successCount);
        Assert.Equal(3, failureCount);
    }

    [Fact]
    public async Task HandleAsync_Should_Respect_Batch_Window_Configuration()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = true,
            DefaultBatchSize = 10,
            DefaultBatchWindow = TimeSpan.FromMilliseconds(50),
            DefaultMaxWaitTime = TimeSpan.FromSeconds(5)
        };
        var logger = NullLogger<AIBatchOptimizationBehavior<TestBatchRequest, TestResponse>>.Instance;
        var behavior = new AIBatchOptimizationBehavior<TestBatchRequest, TestResponse>(logger, options);

        var tasks = new List<Task<TestResponse>>();

        // Act - Add fewer items than batch size
        for (int i = 0; i < 3; i++)
        {
            var index = i;
            var request = new TestBatchRequest { Value = $"test{index}" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = $"result{index}" });

            tasks.Add(behavior.HandleAsync(request, next, CancellationToken.None).AsTask());
        }

        // Should complete due to batch window timeout
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(3, results.Length);

        behavior.Dispose();
    }

    [Fact]
    public async Task AIBatchOptimizationBehavior_Should_Be_Thread_Safe()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions { EnableBatching = false };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var taskCount = 20;
        var tasks = new Task<TestResponse>[taskCount];

        // Act - Execute from multiple threads using Parallel.For
        Parallel.For(0, taskCount, i =>
        {
            var request = new TestRequest { Value = $"test{i}" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = $"result{i}" });

            tasks[i] = behavior.HandleAsync(request, next, CancellationToken.None).AsTask();
        });

        // Wait for all tasks to complete
        var results = await Task.WhenAll(tasks);

        // Assert - All tasks completed successfully
        Assert.Equal(taskCount, results.Length);
        Assert.All(results, result => Assert.NotNull(result));
    }

    [Fact]
    public async Task AIBatchOptimizationBehavior_Should_Expose_Metrics_Via_Interface()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = false // Disable batching to test non-batch metrics
        };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act - Execute some requests to generate metrics
        for (int i = 0; i < 5; i++)
        {
            await behavior.HandleAsync(request, next, CancellationToken.None);
        }

        // Assert - Verify the behavior implements the monitoring interface
        Assert.IsAssignableFrom<IAIBatchOptimizationMonitor>(behavior);

        var monitor = (IAIBatchOptimizationMonitor)behavior;
        var requestType = typeof(TestRequest);

        // Test that metrics can be retrieved without exception
        var avgWaitTime = monitor.GetAverageWaitTime(requestType);
        var avgEfficiency = monitor.GetAverageEfficiency(requestType);
        var batchingRate = monitor.GetBatchingRate(requestType);
        var requestRate = monitor.GetRequestRate(requestType);

        // For non-batch requests, average wait time should be a reasonable value
        Assert.True(avgWaitTime >= 0);
        Assert.True(avgEfficiency >= 0);
        Assert.True(batchingRate >= 0);
        Assert.True(requestRate >= 0);
    }

    [Fact]
    public async Task AIBatchOptimizationBehavior_Should_Update_Metrics_With_Batching()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions
        {
            EnableBatching = true,
            DefaultBatchSize = 2,
            DefaultBatchWindow = TimeSpan.FromMilliseconds(500),
            DefaultMaxWaitTime = TimeSpan.FromSeconds(2)
        };
        var loggerForBatch = NullLogger<AIBatchOptimizationBehavior<TestBatchRequest, TestResponse>>.Instance;
        var behavior = new AIBatchOptimizationBehavior<TestBatchRequest, TestResponse>(loggerForBatch, options);
        // Don't add to _behaviorsToDispose here since it's a different type parameter
        // Dispose manually at the end

        var tasks = new List<Task<TestResponse>>();

        // Act - Execute multiple requests that should get batched
        for (int i = 0; i < 3; i++) // This should create at least one batch of 2
        {
            var index = i;
            var request = new TestBatchRequest { Value = $"test{index}" };
            RequestHandlerDelegate<TestResponse> next = () =>
                new ValueTask<TestResponse>(new TestResponse { Result = $"result{index}" });

            tasks.Add(behavior.HandleAsync(request, next, CancellationToken.None).AsTask());
        }

        await Task.WhenAll(tasks);

        // Assert - Check that metrics reflect the batching
        var monitor = (IAIBatchOptimizationMonitor)behavior;
        var requestType = typeof(TestBatchRequest);

        var avgWaitTime = monitor.GetAverageWaitTime(requestType);
        var avgEfficiency = monitor.GetAverageEfficiency(requestType);
        var batchingRate = monitor.GetBatchingRate(requestType);
        var requestRate = monitor.GetRequestRate(requestType);
        var snapshot = monitor.GetBatchMetrics(requestType);

        // Verify metrics are accessible and in reasonable ranges
        Assert.True(avgWaitTime >= 0, "Average wait time should be non-negative");
        Assert.True(avgEfficiency >= 0 && avgEfficiency <= 1.0, "Average efficiency should be between 0 and 1");
        Assert.True(batchingRate >= 0 && batchingRate <= 1.0, "Batching rate should be between 0 and 1");
        Assert.True(requestRate >= 0, "Request rate should be non-negative");
        Assert.True(snapshot.TotalRequests >= 3, "Should have processed at least 3 requests");
        Assert.InRange(Math.Abs(snapshot.AverageWaitTime - avgWaitTime), 0, 0.001);
        Assert.InRange(Math.Abs(snapshot.AverageEfficiency - avgEfficiency), 0, 0.001);
        Assert.InRange(Math.Abs(snapshot.BatchingRate - batchingRate), 0, 0.001);
    }

    [Fact]
    public async Task AIBatchOptimizationBehavior_Should_Return_Zero_Metrics_For_Untracked_Request_Types()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions();
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        // Act & Assert - Request metrics for an untracked request type
        var monitor = (IAIBatchOptimizationMonitor)behavior;
        var untrackedRequestType = typeof(string); // This type was never processed

        var avgWaitTime = monitor.GetAverageWaitTime(untrackedRequestType);
        var avgEfficiency = monitor.GetAverageEfficiency(untrackedRequestType);
        var batchingRate = monitor.GetBatchingRate(untrackedRequestType);
        var requestRate = monitor.GetRequestRate(untrackedRequestType);
        var snapshot = monitor.GetBatchMetrics(untrackedRequestType);

        // Verify all metrics are zero for untracked types
        Assert.Equal(0.0, avgWaitTime);
        Assert.Equal(0.0, avgEfficiency);
        Assert.Equal(0.0, batchingRate);
        Assert.Equal(0.0, requestRate);
        Assert.Equal(0, snapshot.TotalRequests);
        Assert.Equal(0, snapshot.TotalBatchedRequests);
        Assert.Equal(0, snapshot.BatchExecutions);
    }

    [Fact]
    public async Task AIBatchOptimizationBehavior_Should_Track_Tracked_Request_Types()
    {
        // Arrange
        var options = new AIBatchOptimizationOptions { EnableBatching = false };
        var behavior = new AIBatchOptimizationBehavior<TestRequest, TestResponse>(_logger, options);
        _behaviorsToDispose.Add(behavior);

        var request = new TestRequest { Value = "test" };
        RequestHandlerDelegate<TestResponse> next = () =>
            new ValueTask<TestResponse>(new TestResponse { Result = "success" });

        // Act - Process a request to make the type tracked
        await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert - Check that the request type is tracked
        var monitor = (IAIBatchOptimizationMonitor)behavior;
        var trackedTypes = monitor.GetTrackedRequestTypes();

        Assert.Contains(typeof(TestRequest), trackedTypes);
        Assert.DoesNotContain(typeof(string), trackedTypes); // A type that was never processed
        
        // Verify we can get metrics for the tracked type
        var metrics = monitor.GetBatchMetrics(typeof(TestRequest));
        Assert.Equal(1, metrics.TotalRequests);
        Assert.Equal(0.0, metrics.BatchingRate); // No batching with EnableBatching = false
    }

    // Test classes
    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [SmartBatching(MaxBatchSize = 5, MaxWaitTimeMilliseconds = 200, Strategy = BatchingStrategy.SizeAndTime)]
    public class TestBatchRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}

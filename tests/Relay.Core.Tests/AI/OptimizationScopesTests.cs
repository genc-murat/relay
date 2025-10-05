using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class OptimizationScopesTests
    {
        private readonly ILogger _logger;

        public OptimizationScopesTests()
        {
            _logger = NullLogger.Instance;
        }

        #region CustomOptimizationScope Tests

        [Fact]
        public void CustomOptimizationScope_Should_Create_With_Valid_Context()
        {
            // Arrange
            var context = new CustomOptimizationContext
            {
                OptimizationType = "Test",
                OptimizationLevel = 1
            };

            // Act
            using var scope = CustomOptimizationScope.Create(context, _logger);

            // Assert
            Assert.NotNull(scope);
        }

        [Fact]
        public void CustomOptimizationScope_Should_Record_Actions()
        {
            // Arrange
            var context = new CustomOptimizationContext();
            using var scope = CustomOptimizationScope.Create(context, _logger);

            // Act
            scope.RecordAction("Test Action", "Description", success: true);
            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal(1, stats.OptimizationActionsApplied);
            Assert.Equal(1, stats.ActionsSucceeded);
            Assert.Equal(0, stats.ActionsFailed);
            Assert.Single(stats.Actions);
        }

        [Fact]
        public async Task CustomOptimizationScope_Should_Record_Timed_Actions()
        {
            // Arrange
            var context = new CustomOptimizationContext();
            using var scope = CustomOptimizationScope.Create(context, _logger);

            // Act
            var result = await scope.RecordTimedActionAsync("Timed Action", "Description",
                async () =>
                {
                    await Task.Delay(50);
                    return "success";
                });

            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal("success", result);
            Assert.Equal(1, stats.OptimizationActionsApplied);
            Assert.Equal(1, stats.ActionsSucceeded);
            Assert.Single(stats.Actions);
            Assert.True(stats.Actions[0].Duration > TimeSpan.Zero);
        }

        [Fact]
        public async Task CustomOptimizationScope_Should_Handle_Failed_Timed_Actions()
        {
            // Arrange
            var context = new CustomOptimizationContext();
            using var scope = CustomOptimizationScope.Create(context, _logger);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await scope.RecordTimedActionAsync<string>("Failing Action", "Description",
                    () => throw new InvalidOperationException("Test failure")));

            var stats = scope.GetStatistics();
            Assert.Equal(1, stats.OptimizationActionsApplied);
            Assert.Equal(0, stats.ActionsSucceeded);
            Assert.Equal(1, stats.ActionsFailed);
        }

        [Fact]
        public void CustomOptimizationScope_Should_Calculate_Effectiveness()
        {
            // Arrange
            var context = new CustomOptimizationContext();
            using var scope = CustomOptimizationScope.Create(context, _logger);

            // Act
            scope.RecordAction("Action1", "Desc", success: true);
            scope.RecordAction("Action2", "Desc", success: true);
            scope.RecordAction("Action3", "Desc", success: false);

            var stats = scope.GetStatistics();

            // Assert
            Assert.InRange(stats.OverallEffectiveness, 0.0, 1.0);
            Assert.Equal(2.0 / 3.0, stats.SuccessRate, 2);
        }

        [Fact]
        public void CustomOptimizationScope_Should_Return_Profiling_Data_When_Enabled()
        {
            // Arrange
            var context = new CustomOptimizationContext { EnableProfiling = true };
            using var scope = CustomOptimizationScope.Create(context, _logger);

            scope.RecordAction("Action1", "Desc", success: true);

            // Act
            var profilingData = scope.GetProfilingData();

            // Assert
            Assert.NotEmpty(profilingData);
            Assert.True(profilingData.ContainsKey("TotalDuration"));
            Assert.True(profilingData.ContainsKey("ActionsApplied"));
        }

        #endregion

        #region MemoryPoolScope Tests

        [Fact]
        public void MemoryPoolScope_Should_Create_With_Valid_Buffer_Size()
        {
            // Arrange & Act
            using var scope = MemoryPoolScope.Create(4096, _logger);

            // Assert
            Assert.NotNull(scope);
            Assert.NotNull(scope.Statistics);
        }

        [Fact]
        public void MemoryPoolScope_Should_Rent_And_Return_Buffers()
        {
            // Arrange
            using var scope = MemoryPoolScope.Create(1024, _logger);

            // Act
            var buffer = scope.RentBuffer(1024);
            Assert.NotNull(buffer);
            Assert.True(buffer.Length >= 1024);

            scope.ReturnBuffer(buffer);

            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal(1, stats.BuffersRented);
            Assert.Equal(1, stats.BuffersReturned);
            Assert.Equal(1.0, stats.PoolEfficiency);
        }

        [Fact]
        public void MemoryPoolScope_Should_Track_Multiple_Buffers()
        {
            // Arrange
            using var scope = MemoryPoolScope.Create(1024, _logger);

            // Act
            var buffer1 = scope.RentBuffer(512);
            var buffer2 = scope.RentBuffer(1024);
            var buffer3 = scope.RentBuffer(2048);

            scope.ReturnBuffer(buffer1);
            scope.ReturnBuffer(buffer2);

            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal(3, stats.BuffersRented);
            Assert.Equal(2, stats.BuffersReturned);
            Assert.True(stats.TotalBytesAllocated > 0);
        }

        #endregion

        #region ParallelProcessingScope Tests

        [Fact]
        public void ParallelProcessingScope_Should_Create_With_Valid_Parallelism()
        {
            // Arrange & Act
            using var scope = ParallelProcessingScope.Create(4, _logger);

            // Assert
            Assert.NotNull(scope);
            Assert.Equal(4, scope.MaxDegreeOfParallelism);
        }

        [Fact]
        public void ParallelProcessingScope_Should_Record_Task_Execution()
        {
            // Arrange
            using var scope = ParallelProcessingScope.Create(4, _logger);

            // Act
            scope.IncrementTasksStarted();
            scope.RecordTaskExecution(TimeSpan.FromMilliseconds(100));

            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal(1, stats.TasksStarted);
            Assert.Equal(1, stats.TasksCompleted);
            Assert.Equal(TimeSpan.FromMilliseconds(100), stats.AverageTaskDuration);
        }

        [Fact]
        public void ParallelProcessingScope_Should_Calculate_Speedup()
        {
            // Arrange
            using var scope = ParallelProcessingScope.Create(4, _logger);

            // Act
            for (int i = 0; i < 4; i++)
            {
                scope.IncrementTasksStarted();
                scope.RecordTaskExecution(TimeSpan.FromMilliseconds(100));
            }

            // Wait a bit to ensure total duration is measurable
            System.Threading.Thread.Sleep(50);

            // Assert - Speedup should be calculated on dispose
            Assert.NotNull(scope.Statistics);
        }

        #endregion

        #region DatabaseOptimizationScope Tests

        [Fact]
        public void DatabaseOptimizationScope_Should_Create_Successfully()
        {
            // Arrange & Act
            using var scope = DatabaseOptimizationScope.Create(_logger);

            // Assert
            Assert.NotNull(scope);
            Assert.NotNull(scope.Statistics);
        }

        [Fact]
        public void DatabaseOptimizationScope_Should_Record_Query_Execution()
        {
            // Arrange
            using var scope = DatabaseOptimizationScope.Create(_logger);

            // Act
            scope.RecordQueryExecution(TimeSpan.FromMilliseconds(50), wasRetried: false);
            scope.RecordQueryExecution(TimeSpan.FromMilliseconds(100), wasRetried: true);

            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal(2, stats.QueriesExecuted);
            Assert.Equal(1, stats.QueriesRetried);
        }

        [Fact]
        public void DatabaseOptimizationScope_Should_Record_Connection_Pool_Usage()
        {
            // Arrange
            using var scope = DatabaseOptimizationScope.Create(_logger);

            // Act
            scope.RecordConnectionPoolUsage(hit: true);
            scope.RecordConnectionPoolUsage(hit: true);
            scope.RecordConnectionPoolUsage(hit: false);

            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal(2, stats.ConnectionPoolHits);
            Assert.Equal(1, stats.ConnectionPoolMisses);
            Assert.Equal(2.0 / 3.0, stats.ConnectionPoolEfficiency, 2);
        }

        [Fact]
        public void DatabaseOptimizationScope_Should_Track_Slowest_Query()
        {
            // Arrange
            using var scope = DatabaseOptimizationScope.Create(_logger);

            // Act
            scope.RecordQueryExecution(TimeSpan.FromMilliseconds(50));
            scope.RecordQueryExecution(TimeSpan.FromMilliseconds(200));
            scope.RecordQueryExecution(TimeSpan.FromMilliseconds(100));

            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal(TimeSpan.FromMilliseconds(200), stats.SlowestQueryDuration);
        }

        #endregion

        #region SIMDOptimizationScope Tests

        [Fact]
        public void SIMDOptimizationScope_Should_Create_Successfully()
        {
            // Arrange & Act
            using var scope = SIMDOptimizationScope.Create(_logger);

            // Assert
            Assert.NotNull(scope);
            Assert.NotNull(scope.Statistics);
        }

        [Fact]
        public void SIMDOptimizationScope_Should_Record_Vector_Operations()
        {
            // Arrange
            using var scope = SIMDOptimizationScope.Create(_logger);

            // Act
            scope.RecordVectorOperation(8);
            scope.RecordVectorOperation(8);

            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal(2, stats.VectorOperations);
            Assert.Equal(16, stats.TotalElementsProcessed);
        }

        [Fact]
        public void SIMDOptimizationScope_Should_Record_Scalar_Operations()
        {
            // Arrange
            using var scope = SIMDOptimizationScope.Create(_logger);

            // Act
            scope.RecordScalarOperation(1);
            scope.RecordScalarOperation(1);
            scope.RecordScalarOperation(1);

            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal(3, stats.ScalarOperations);
            Assert.Equal(3, stats.TotalElementsProcessed);
        }

        [Fact]
        public void SIMDOptimizationScope_Should_Calculate_Vectorization_Ratio()
        {
            // Arrange
            using var scope = SIMDOptimizationScope.Create(_logger);

            // Act
            scope.RecordVectorOperation(8);
            scope.RecordVectorOperation(8);
            scope.RecordScalarOperation(1);
            scope.RecordScalarOperation(1);

            var stats = scope.GetStatistics();

            // Assert
            Assert.Equal(0.5, stats.VectorizationRatio); // 2 vector ops / 4 total ops
        }

        [Fact]
        public void SIMDOptimizationScope_Should_Process_Data_With_Vectors()
        {
            // Arrange
            using var scope = SIMDOptimizationScope.Create(_logger);
            var data = new int[100];
            for (int i = 0; i < data.Length; i++)
                data[i] = i;

            var vectorSum = 0;
            var scalarSum = 0;

            // Act
            scope.ProcessData<int>(
                data,
                vectorAction: vector =>
                {
                    vectorSum += Vector<int>.Count; // Count vector operations
                },
                scalarAction: scalar =>
                {
                    scalarSum += 1; // Count scalar operations
                });

            var stats = scope.GetStatistics();

            // Assert
            Assert.True(stats.VectorOperations > 0);
            Assert.True(stats.TotalElementsProcessed == 100);
        }

        #endregion

        #region Context Classes Tests

        [Fact]
        public void MemoryPoolingContext_Should_Store_Configuration()
        {
            // Arrange
            var context = new MemoryPoolingContext
            {
                EnableObjectPooling = true,
                EnableBufferPooling = true,
                EstimatedBufferSize = 8192
            };

            // Assert
            Assert.True(context.EnableObjectPooling);
            Assert.True(context.EnableBufferPooling);
            Assert.Equal(8192, context.EstimatedBufferSize);
        }

        [Fact]
        public void ParallelProcessingContext_Should_Store_Configuration()
        {
            // Arrange
            var context = new ParallelProcessingContext
            {
                MaxDegreeOfParallelism = 8,
                EnableWorkStealing = true,
                MinItemsForParallel = 10,
                CpuUtilization = 0.75,
                AvailableProcessors = 8
            };

            // Assert
            Assert.Equal(8, context.MaxDegreeOfParallelism);
            Assert.True(context.EnableWorkStealing);
            Assert.Equal(10, context.MinItemsForParallel);
            Assert.Equal(0.75, context.CpuUtilization);
        }

        [Fact]
        public void DatabaseOptimizationContext_Should_Store_Configuration()
        {
            // Arrange
            var context = new DatabaseOptimizationContext
            {
                EnableQueryOptimization = true,
                EnableConnectionPooling = true,
                MaxRetries = 3,
                RetryDelayMs = 100,
                QueryTimeoutSeconds = 30
            };

            // Assert
            Assert.True(context.EnableQueryOptimization);
            Assert.True(context.EnableConnectionPooling);
            Assert.Equal(3, context.MaxRetries);
            Assert.Equal(100, context.RetryDelayMs);
            Assert.Equal(30, context.QueryTimeoutSeconds);
        }

        [Fact]
        public void SIMDOptimizationContext_Should_Store_Configuration()
        {
            // Arrange
            var context = new SIMDOptimizationContext
            {
                EnableVectorization = true,
                VectorSize = 8,
                EnableUnrolling = true,
                UnrollFactor = 4,
                MinDataSize = 100,
                IsHardwareAccelerated = true,
                SupportedVectorTypes = new[] { "SSE", "AVX2" }
            };

            // Assert
            Assert.True(context.EnableVectorization);
            Assert.Equal(8, context.VectorSize);
            Assert.True(context.EnableUnrolling);
            Assert.Contains("AVX2", context.SupportedVectorTypes);
        }

        #endregion
    }
}

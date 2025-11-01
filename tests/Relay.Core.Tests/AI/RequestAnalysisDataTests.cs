using Relay.Core.AI;
using Relay.Core.AI.Models;
using System;
using Xunit;

namespace Relay.Core.Tests.AI;

public class RequestAnalysisDataTests
{
    [Fact]
    public void AddOptimizationResult_Should_Add_Result_To_Collection()
    {
        // Arrange
        var data = new RequestAnalysisData();
        var result = new OptimizationResult
        {
            Strategy = OptimizationStrategy.EnableCaching,
            ActualMetrics = new RequestExecutionMetrics
            {
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                FailedExecutions = 5,
                SuccessRate = 0.95,
                AverageExecutionTime = TimeSpan.FromMilliseconds(50)
            },
            Timestamp = DateTime.UtcNow
        };

        // Act
        data.AddOptimizationResult(result);

        // Assert
        Assert.Equal(1, data.OptimizationResultsCount);
    }

    [Fact]
    public void AddOptimizationResult_Should_Update_LastActivityTime()
    {
        // Arrange
        var data = new RequestAnalysisData();
        var beforeAdd = DateTime.UtcNow;
        var result = new OptimizationResult
        {
            Strategy = OptimizationStrategy.BatchProcessing,
            ActualMetrics = new RequestExecutionMetrics
            {
                TotalExecutions = 50,
                SuccessfulExecutions = 48,
                FailedExecutions = 2,
                SuccessRate = 0.96,
                AverageExecutionTime = TimeSpan.FromMilliseconds(75)
            },
            Timestamp = DateTime.UtcNow
        };

        // Act
        data.AddOptimizationResult(result);

        // Assert
        Assert.True(data.LastActivityTime >= beforeAdd);
    }

    [Fact]
    public void AddOptimizationResult_Should_Handle_Multiple_Results()
    {
        // Arrange
        var data = new RequestAnalysisData();
        var results = new[]
        {
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    FailedExecutions = 5,
                    SuccessRate = 0.95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50)
                },
                Timestamp = DateTime.UtcNow
            },
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 80,
                    SuccessfulExecutions = 78,
                    FailedExecutions = 2,
                    SuccessRate = 0.975,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(60)
                },
                Timestamp = DateTime.UtcNow
            },
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 120,
                    SuccessfulExecutions = 115,
                    FailedExecutions = 5,
                    SuccessRate = 0.9583333333333334,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(45)
                },
                Timestamp = DateTime.UtcNow
            }
        };

        // Act
        foreach (var result in results)
        {
            data.AddOptimizationResult(result);
        }

        // Assert
        Assert.Equal(3, data.OptimizationResultsCount);
    }

    [Fact]
    public void GetMostEffectiveStrategies_Should_Return_Empty_Array_When_No_Results()
    {
        // Arrange
        var data = new RequestAnalysisData();

        // Act
        var strategies = data.GetMostEffectiveStrategies();

        // Assert
        Assert.Empty(strategies);
    }

    [Fact]
    public void GetMostEffectiveStrategies_Should_Return_Top_3_Strategies_By_Success_Rate()
    {
        // Arrange
        var data = new RequestAnalysisData();

        // Add results with different success rates
        var results = new[]
        {
            // EnableCaching: 95% success rate (95/100)
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    FailedExecutions = 5,
                    SuccessRate = 0.95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50)
                },
                Timestamp = DateTime.UtcNow
            },
            // BatchProcessing: 97.5% success rate (78/80)
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 80,
                    SuccessfulExecutions = 78,
                    FailedExecutions = 2,
                    SuccessRate = 0.975,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(60)
                },
                Timestamp = DateTime.UtcNow
            },
            // ParallelProcessing: 90% success rate (90/100)
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.ParallelProcessing,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    SuccessRate = 0.9,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(40)
                },
                Timestamp = DateTime.UtcNow
            },
            // MemoryPooling: 85% success rate (85/100)
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.MemoryPooling,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 85,
                    FailedExecutions = 15,
                    SuccessRate = 0.85,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(55)
                },
                Timestamp = DateTime.UtcNow
            },
            // Another EnableCaching result: 96% success rate (96/100)
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 96,
                    FailedExecutions = 4,
                    SuccessRate = 0.96,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(48)
                },
                Timestamp = DateTime.UtcNow
            }
        };

        foreach (var result in results)
        {
            data.AddOptimizationResult(result);
        }

        // Act
        var strategies = data.GetMostEffectiveStrategies();

        // Assert
        Assert.Equal(3, strategies.Length);
        // Should be ordered by average success rate: BatchProcessing (97.5%), EnableCaching (95.5%), ParallelProcessing (90%)
        Assert.Equal(OptimizationStrategy.BatchProcessing, strategies[0]);
        Assert.Equal(OptimizationStrategy.EnableCaching, strategies[1]);
        Assert.Equal(OptimizationStrategy.ParallelProcessing, strategies[2]);
    }

    [Fact]
    public void GetMostEffectiveStrategies_Should_Return_All_Strategies_When_Less_Than_3()
    {
        // Arrange
        var data = new RequestAnalysisData();

        var results = new[]
        {
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    FailedExecutions = 5,
                    SuccessRate = 0.95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50)
                },
                Timestamp = DateTime.UtcNow
            },
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 80,
                    SuccessfulExecutions = 78,
                    FailedExecutions = 2,
                    SuccessRate = 0.975,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(60)
                },
                Timestamp = DateTime.UtcNow
            }
        };

        foreach (var result in results)
        {
            data.AddOptimizationResult(result);
        }

        // Act
        var strategies = data.GetMostEffectiveStrategies();

        // Assert
        Assert.Equal(2, strategies.Length);
        Assert.Equal(OptimizationStrategy.BatchProcessing, strategies[0]); // Higher success rate
        Assert.Equal(OptimizationStrategy.EnableCaching, strategies[1]);
    }

    [Fact]
    public void GetMostEffectiveStrategies_Should_Group_By_Strategy_And_Calculate_Average_Success_Rate()
    {
        // Arrange
        var data = new RequestAnalysisData();

        // Multiple results for the same strategy with different success rates
        var results = new[]
        {
            // EnableCaching: first result 95%, second result 90%, average 92.5%
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 95,
                    FailedExecutions = 5,
                    SuccessRate = 0.95,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(50)
                },
                Timestamp = DateTime.UtcNow
            },
            // BatchProcessing: 97.5%
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.BatchProcessing,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 80,
                    SuccessfulExecutions = 78,
                    FailedExecutions = 2,
                    SuccessRate = 0.975,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(60)
                },
                Timestamp = DateTime.UtcNow
            },
            // EnableCaching: second result 90%, average with first = 92.5%
            new OptimizationResult
            {
                Strategy = OptimizationStrategy.EnableCaching,
                ActualMetrics = new RequestExecutionMetrics
                {
                    TotalExecutions = 100,
                    SuccessfulExecutions = 90,
                    FailedExecutions = 10,
                    SuccessRate = 0.9,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(55)
                },
                Timestamp = DateTime.UtcNow
            }
        };

        foreach (var result in results)
        {
            data.AddOptimizationResult(result);
        }

        // Act
        var strategies = data.GetMostEffectiveStrategies();

        // Assert
        Assert.Equal(2, strategies.Length);
        // BatchProcessing (97.5%) should come first, then EnableCaching (92.5%)
        Assert.Equal(OptimizationStrategy.BatchProcessing, strategies[0]);
        Assert.Equal(OptimizationStrategy.EnableCaching, strategies[1]);
    }

    [Fact]
    public void TrimExecutionTimes_Should_Return_Zero_When_Count_Is_Less_Than_Or_Equal_To_MaxCount()
    {
        // Arrange
        var data = new RequestAnalysisData();
        var metrics = new RequestExecutionMetrics
        {
            TotalExecutions = 10,
            SuccessfulExecutions = 9,
            FailedExecutions = 1,
            SuccessRate = 0.9,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100)
        };

        // Add 3 execution times
        for (int i = 0; i < 3; i++)
        {
            data.AddMetrics(metrics);
        }

        // Act
        var removedCount = data.TrimExecutionTimes(5);

        // Assert
        Assert.Equal(0, removedCount);
        Assert.Equal(3, data.ExecutionTimesCount);
    }

    [Fact]
    public void TrimExecutionTimes_Should_Remove_Oldest_Items_When_Count_Exceeds_MaxCount()
    {
        // Arrange
        var data = new RequestAnalysisData();

        // Add 5 different execution times
        for (int i = 0; i < 5; i++)
        {
            var metrics = new RequestExecutionMetrics
            {
                TotalExecutions = 10,
                SuccessfulExecutions = 9,
                FailedExecutions = 1,
                SuccessRate = 0.9,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100 + i * 10) // 100, 110, 120, 130, 140
            };
            data.AddMetrics(metrics);
        }

        // Act
        var removedCount = data.TrimExecutionTimes(3);

        // Assert
        Assert.Equal(2, removedCount); // Should remove 2 items (5 - 3 = 2)
        Assert.Equal(3, data.ExecutionTimesCount);

        // The average should be calculated from the remaining 3 items (120, 130, 140)
        var expectedAverage = (120 + 130 + 140) / 3.0; // 130
        Assert.Equal(TimeSpan.FromMilliseconds(expectedAverage), data.AverageExecutionTime);
    }

    [Fact]
    public void TrimExecutionTimes_Should_Update_AverageExecutionTime_After_Trimming()
    {
        // Arrange
        var data = new RequestAnalysisData();

        // Add execution times: 50ms, 100ms, 150ms, 200ms
        var executionTimes = new[] { 50, 100, 150, 200 };
        foreach (var time in executionTimes)
        {
            var metrics = new RequestExecutionMetrics
            {
                TotalExecutions = 10,
                SuccessfulExecutions = 9,
                FailedExecutions = 1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(time)
            };
            data.AddMetrics(metrics);
        }

        // Initial average: (50 + 100 + 150 + 200) / 4 = 125ms
        Assert.Equal(TimeSpan.FromMilliseconds(125), data.AverageExecutionTime);

        // Act - trim to keep only 2 items (should keep the last 2: 150ms, 200ms)
        var removedCount = data.TrimExecutionTimes(2);

        // Assert
        Assert.Equal(2, removedCount);
        Assert.Equal(2, data.ExecutionTimesCount);
        // New average: (150 + 200) / 2 = 175ms
        Assert.Equal(TimeSpan.FromMilliseconds(175), data.AverageExecutionTime);
    }

    [Fact]
    public void TrimExecutionTimes_Should_Handle_Empty_Collection()
    {
        // Arrange
        var data = new RequestAnalysisData();

        // Act
        var removedCount = data.TrimExecutionTimes(5);

        // Assert
        Assert.Equal(0, removedCount);
        Assert.Equal(0, data.ExecutionTimesCount);
        Assert.Equal(TimeSpan.Zero, data.AverageExecutionTime);
    }

    [Fact]
    public void TrimExecutionTimes_Should_Handle_MaxCount_Zero()
    {
        // Arrange
        var data = new RequestAnalysisData();

        // Add some execution times
        for (int i = 0; i < 3; i++)
        {
            var metrics = new RequestExecutionMetrics
            {
                TotalExecutions = 10,
                SuccessfulExecutions = 9,
                FailedExecutions = 1,
                SuccessRate = 0.9,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100)
            };
            data.AddMetrics(metrics);
        }

        // Act
        var removedCount = data.TrimExecutionTimes(0);

        // Assert
        Assert.Equal(3, removedCount); // Should remove all items
        Assert.Equal(0, data.ExecutionTimesCount);
        Assert.Equal(TimeSpan.Zero, data.AverageExecutionTime);
    }

    [Fact]
    public void TrimExecutionTimes_Should_Preserve_Order_Of_Remaining_Items()
    {
        // Arrange
        var data = new RequestAnalysisData();

        // Add execution times in specific order
        var executionTimes = new[] { 10, 20, 30, 40, 50 };
        foreach (var time in executionTimes)
        {
            var metrics = new RequestExecutionMetrics
            {
                TotalExecutions = 10,
                SuccessfulExecutions = 9,
                FailedExecutions = 1,
                SuccessRate = 0.9,
                AverageExecutionTime = TimeSpan.FromMilliseconds(time)
            };
            data.AddMetrics(metrics);
        }

        // Act - trim to keep 3 items (should keep the most recent: 30, 40, 50)
        var removedCount = data.TrimExecutionTimes(3);

        // Assert
        Assert.Equal(2, removedCount);
        Assert.Equal(3, data.ExecutionTimesCount);

        // The average should be from the remaining items: (30 + 40 + 50) / 3 = 40
        Assert.Equal(TimeSpan.FromMilliseconds(40), data.AverageExecutionTime);
    }
}
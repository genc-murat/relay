using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Xunit;

namespace Relay.Core.Tests.AI;

public class SystemMetricsCalculatorTests
{
    private readonly ILogger<SystemMetricsCalculator> _logger;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;

    public SystemMetricsCalculatorTests()
    {
        _logger = NullLogger<SystemMetricsCalculator>.Instance;
        _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
    }

    private SystemMetricsCalculator CreateCalculator()
    {
        return new SystemMetricsCalculator(_logger, _requestAnalytics);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMetricsCalculator(null!, _requestAnalytics));
    }

    [Fact]
    public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SystemMetricsCalculator(_logger, null!));
    }

    [Fact]
    public async Task CalculateCpuUsageAsync_Should_Return_Value_Between_0_And_1()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var result = await calculator.CalculateCpuUsageAsync(CancellationToken.None);

        // Assert
        Assert.True(result >= 0.0 && result <= 1.0);
    }

    [Fact]
    public async Task CalculateCpuUsageAsync_Should_Respect_Cancellation()
    {
        // Arrange
        var calculator = CreateCalculator();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await calculator.CalculateCpuUsageAsync(cts.Token));
    }

    [Fact]
    public void CalculateMemoryUsage_Should_Return_Value_Between_0_And_1()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var result = calculator.CalculateMemoryUsage();

        // Assert
        Assert.True(result >= 0.0 && result <= 1.0);
    }

    [Fact]
    public void GetActiveRequestCount_Should_Return_Zero_When_No_Data()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var result = calculator.GetActiveRequestCount();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetActiveRequestCount_Should_Calculate_From_Request_Analytics()
    {
        // Arrange
        var calculator = CreateCalculator();
        var data1 = new RequestAnalysisData();
        data1.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50),
            ConcurrentExecutions = 10
        });
        var data2 = new RequestAnalysisData();
        data2.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50),
            ConcurrentExecutions = 20
        });
        _requestAnalytics[typeof(string)] = data1;
        _requestAnalytics[typeof(int)] = data2;

        // Act
        var result = calculator.GetActiveRequestCount();

        // Assert
        Assert.Equal(15, result); // (10 + 20) / 2
    }

    [Fact]
    public void GetQueuedRequestCount_Should_Return_Zero_When_No_Queue()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var result = calculator.GetQueuedRequestCount();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetQueuedRequestCount_Should_Calculate_Queue_Size()
    {
        // Arrange
        var calculator = CreateCalculator();
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50),
            ConcurrentExecutions = Environment.ProcessorCount * 4 // More than capacity
        });
        _requestAnalytics[typeof(string)] = data;

        // Act
        var result = calculator.GetQueuedRequestCount();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void CalculateCurrentThroughput_Should_Return_Zero_When_No_Data()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var result = calculator.CalculateCurrentThroughput();

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateCurrentThroughput_Should_Calculate_From_Executions()
    {
        // Arrange
        var calculator = CreateCalculator();
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 300, // 300 executions in 5 minutes = 1 per second
            SuccessfulExecutions = 280,
            FailedExecutions = 20,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 5
        });
        _requestAnalytics[typeof(string)] = data;

        // Act
        var result = calculator.CalculateCurrentThroughput();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void CalculateAverageResponseTime_Should_Return_Default_When_No_Data()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var result = calculator.CalculateAverageResponseTime();

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(100), result);
    }

    [Fact]
    public void CalculateAverageResponseTime_Should_Calculate_From_Request_Analytics()
    {
        // Arrange
        var calculator = CreateCalculator();
        var data1 = new RequestAnalysisData();
        data1.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50),
            ConcurrentExecutions = 5
        });
        var data2 = new RequestAnalysisData();
        data2.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10,
            AverageExecutionTime = TimeSpan.FromMilliseconds(150),
            ConcurrentExecutions = 5
        });
        _requestAnalytics[typeof(string)] = data1;
        _requestAnalytics[typeof(int)] = data2;

        // Act
        var result = calculator.CalculateAverageResponseTime();

        // Assert
        Assert.Equal(TimeSpan.FromMilliseconds(100), result); // Average of 50 and 150
    }

    [Fact]
    public void CalculateCurrentErrorRate_Should_Return_Zero_When_No_Data()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var result = calculator.CalculateCurrentErrorRate();

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void CalculateCurrentErrorRate_Should_Calculate_From_Error_Rates()
    {
        // Arrange
        var calculator = CreateCalculator();
        var data1 = new RequestAnalysisData();
        data1.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 90,
            FailedExecutions = 10, // 10% error rate
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 5
        });
        var data2 = new RequestAnalysisData();
        data2.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 50,
            SuccessfulExecutions = 45,
            FailedExecutions = 5, // 10% error rate
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 5
        });
        _requestAnalytics[typeof(string)] = data1;
        _requestAnalytics[typeof(int)] = data2;

        // Act
        var result = calculator.CalculateCurrentErrorRate();

        // Assert
        Assert.Equal(0.1, result); // Average of 0.1 and 0.1
    }

    [Fact]
    public void GetDatabasePoolUtilization_Should_Return_Zero()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var result = calculator.GetDatabasePoolUtilization();

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void GetThreadPoolUtilization_Should_Return_Value_Between_0_And_1()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var result = calculator.GetThreadPoolUtilization();

        // Assert
        Assert.True(result >= 0.0 && result <= 1.0);
    }

    [Fact]
    public void GetActiveRequestCount_Should_Handle_Empty_Request_Analytics()
    {
        // Arrange
        var calculator = CreateCalculator();
        // _requestAnalytics is empty

        // Act
        var result = calculator.GetActiveRequestCount();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateCurrentThroughput_Should_Handle_Large_Execution_Counts()
    {
        // Arrange
        var calculator = CreateCalculator();
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = int.MaxValue,
            SuccessfulExecutions = int.MaxValue - 100,
            FailedExecutions = 100,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 10
        });
        _requestAnalytics[typeof(string)] = data;

        // Act
        var result = calculator.CalculateCurrentThroughput();

        // Assert
        Assert.True(result > 0);
    }

    [Fact]
    public void CalculateAverageResponseTime_Should_Handle_Zero_Average_Time()
    {
        // Arrange
        var calculator = CreateCalculator();
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 100,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.Zero,
            ConcurrentExecutions = 5
        });
        _requestAnalytics[typeof(string)] = data;

        // Act
        var result = calculator.CalculateAverageResponseTime();

        // Assert
        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void CalculateCurrentErrorRate_Should_Handle_Zero_Total_Executions()
    {
        // Arrange
        var calculator = CreateCalculator();
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 0,
            SuccessfulExecutions = 0,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 0
        });
        _requestAnalytics[typeof(string)] = data;

        // Act
        var result = calculator.CalculateCurrentErrorRate();

        // Assert
        Assert.Equal(0.0, result);
    }

    [Fact]
    public void GetQueuedRequestCount_Should_Handle_Negative_Queue()
    {
        // Arrange
        var calculator = CreateCalculator();
        var data = new RequestAnalysisData();
        data.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 100,
            FailedExecutions = 0,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 1 // Less than capacity
        });
        _requestAnalytics[typeof(string)] = data;

        // Act
        var result = calculator.GetQueuedRequestCount();

        // Assert
        Assert.Equal(0, result); // Should clamp to 0
    }

    [Fact]
    public async Task CalculateCpuUsageAsync_Should_Handle_Multiple_Calls()
    {
        // Arrange
        var calculator = CreateCalculator();

        // Act
        var result1 = await calculator.CalculateCpuUsageAsync(CancellationToken.None);
        var result2 = await calculator.CalculateCpuUsageAsync(CancellationToken.None);
        var result3 = await calculator.CalculateCpuUsageAsync(CancellationToken.None);

        // Assert
        Assert.True(result1 >= 0.0 && result1 <= 1.0);
        Assert.True(result2 >= 0.0 && result2 <= 1.0);
        Assert.True(result3 >= 0.0 && result3 <= 1.0);
    }

    [Fact]
    public void CalculateMemoryUsage_Should_Handle_Low_Memory()
    {
        // Arrange
        var calculator = CreateCalculator();
        GC.Collect(); // Force GC to potentially lower memory

        // Act
        var result = calculator.CalculateMemoryUsage();

        // Assert
        Assert.True(result >= 0.0 && result <= 1.0);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class LoadTestResultTests
{
    [Fact]
    public void LoadTestResult_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var result = new LoadTestResult();

        // Assert
        Assert.Equal(string.Empty, result.RequestType);
        Assert.Equal(default(DateTime), result.StartedAt);
        Assert.Null(result.CompletedAt);
        Assert.Equal(TimeSpan.Zero, result.TotalDuration);
        Assert.NotNull(result.Configuration);
        Assert.Equal(0, result.SuccessfulRequests);
        Assert.Equal(0, result.FailedRequests);
        Assert.NotNull(result.ResponseTimes);
        Assert.Empty(result.ResponseTimes);
        Assert.Equal(0, result.AverageResponseTime);
        Assert.Equal(0, result.MedianResponseTime);
        Assert.Equal(0, result.P95ResponseTime);
        Assert.Equal(0, result.P99ResponseTime);
        Assert.Equal(0, result.PeakMemoryUsage);
        Assert.Equal(0, result.AverageMemoryUsage);
        Assert.False(result.MemoryLeakDetected);
    }

    [Fact]
    public void LoadTestResult_RequestsPerSecond_WithZeroDuration_ReturnsZero()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 10,
            FailedRequests = 5,
            TotalDuration = TimeSpan.Zero
        };

        // Act & Assert
        Assert.Equal(0, result.RequestsPerSecond);
    }

    [Fact]
    public void LoadTestResult_RequestsPerSecond_WithValidDuration_CalculatesCorrectly()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 8,
            FailedRequests = 2,
            TotalDuration = TimeSpan.FromSeconds(2)
        };

        // Act & Assert
        Assert.Equal(5, result.RequestsPerSecond);
    }

    [Fact]
    public void LoadTestResult_SuccessRate_WithNoRequests_ReturnsZero()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 0,
            FailedRequests = 0
        };

        // Act & Assert
        Assert.Equal(0, result.SuccessRate);
    }

    [Fact]
    public void LoadTestResult_SuccessRate_WithMixedResults_CalculatesCorrectly()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 7,
            FailedRequests = 3
        };

        // Act & Assert
        Assert.Equal(0.7, result.SuccessRate, 0.01);
    }

    [Fact]
    public void LoadTestResult_SuccessRate_WithAllSuccessful_ReturnsOne()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 10,
            FailedRequests = 0
        };

        // Act & Assert
        Assert.Equal(1.0, result.SuccessRate);
    }

    [Fact]
    public void LoadTestResult_SuccessRate_WithAllFailed_ReturnsZero()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 0,
            FailedRequests = 5
        };

        // Act & Assert
        Assert.Equal(0, result.SuccessRate);
    }

    [Fact]
    public void LoadTestResult_TotalRequests_CalculatesCorrectly()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 12,
            FailedRequests = 8
        };

        // Act & Assert
        Assert.Equal(20, result.TotalRequests);
    }

    [Fact]
    public void LoadTestResult_ErrorRate_CalculatesCorrectly()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 6,
            FailedRequests = 4
        };

        // Act & Assert
        Assert.Equal(0.4, result.ErrorRate, 0.01);
    }

    [Fact]
    public void LoadTestResult_ErrorRate_WithNoRequests_ReturnsZero()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 0,
            FailedRequests = 0
        };

        // Act & Assert
        Assert.Equal(0, result.ErrorRate);
    }

    [Fact]
    public void LoadTestResult_Throughput_AliasForRequestsPerSecond()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 15,
            FailedRequests = 5,
            TotalDuration = TimeSpan.FromSeconds(1)
        };

        // Act & Assert
        Assert.Equal(result.RequestsPerSecond, result.Throughput);
        Assert.Equal(20, result.Throughput);
    }

    [Fact]
    public void LoadTestResult_WithResponseTimes_CalculatesAverage()
    {
        // Arrange
        var result = new LoadTestResult
        {
            ResponseTimes = new List<double> { 100, 200, 150, 300 }
        };

        // Act
        result.AverageResponseTime = result.ResponseTimes.Count != 0 ? result.ResponseTimes.Average() : 0;

        // Assert
        Assert.Equal(187.5, result.AverageResponseTime);
    }

    [Fact]
    public void LoadTestResult_WithResponseTimes_CalculatesMedian_OddCount()
    {
        // Arrange
        var result = new LoadTestResult
        {
            ResponseTimes = new List<double> { 100, 200, 150, 300, 250 }
        };

        // Act
        var sorted = result.ResponseTimes.OrderBy(x => x).ToList();
        result.MedianResponseTime = sorted.Count != 0 ? sorted[sorted.Count / 2] : 0;

        // Assert
        Assert.Equal(200, result.MedianResponseTime);
    }

    [Fact]
    public void LoadTestResult_WithResponseTimes_CalculatesMedian_EvenCount()
    {
        // Arrange
        var result = new LoadTestResult
        {
            ResponseTimes = new List<double> { 100, 200, 150, 300 }
        };

        // Act
        var sorted = result.ResponseTimes.OrderBy(x => x).ToList();
        result.MedianResponseTime = sorted.Count != 0 ?
            (sorted[sorted.Count / 2 - 1] + sorted[sorted.Count / 2]) / 2.0 : 0;

        // Assert
        Assert.Equal(175, result.MedianResponseTime);
    }

    [Fact]
    public void LoadTestResult_WithEmptyResponseTimes_ReturnsZeroMetrics()
    {
        // Arrange
        var result = new LoadTestResult
        {
            ResponseTimes = new List<double>()
        };

        // Act
        result.AverageResponseTime = result.ResponseTimes.Count != 0 ? result.ResponseTimes.Average() : 0;
        result.MedianResponseTime = result.ResponseTimes.Count != 0 ? result.ResponseTimes.First() : 0;
        result.P95ResponseTime = result.ResponseTimes.Count != 0 ? result.ResponseTimes.First() : 0;
        result.P99ResponseTime = result.ResponseTimes.Count != 0 ? result.ResponseTimes.First() : 0;

        // Assert
        Assert.Equal(0, result.AverageResponseTime);
        Assert.Equal(0, result.MedianResponseTime);
        Assert.Equal(0, result.P95ResponseTime);
        Assert.Equal(0, result.P99ResponseTime);
    }

    [Fact]
    public void LoadTestResult_WithMemoryUsage_SetsValuesCorrectly()
    {
        // Arrange
        var result = new LoadTestResult
        {
            PeakMemoryUsage = 1024 * 1024, // 1MB
            AverageMemoryUsage = 512 * 1024, // 512KB
            MemoryLeakDetected = true
        };

        // Assert
        Assert.Equal(1024 * 1024, result.PeakMemoryUsage);
        Assert.Equal(512 * 1024, result.AverageMemoryUsage);
        Assert.True(result.MemoryLeakDetected);
    }

    [Fact]
    public void LoadTestResult_WithConfiguration_SetsConfigurationCorrectly()
    {
        // Arrange
        var config = new LoadTestConfiguration
        {
            TotalRequests = 100,
            MaxConcurrency = 10
        };
        var result = new LoadTestResult
        {
            Configuration = config
        };

        // Assert
        Assert.Equal(config, result.Configuration);
        Assert.Equal(100, result.Configuration.TotalRequests);
        Assert.Equal(10, result.Configuration.MaxConcurrency);
    }

    [Fact]
    public void LoadTestResult_WithTimestamps_SetsValuesCorrectly()
    {
        // Arrange
        var startTime = new DateTime(2023, 1, 1, 10, 0, 0);
        var endTime = new DateTime(2023, 1, 1, 10, 0, 30);
        var result = new LoadTestResult
        {
            StartedAt = startTime,
            CompletedAt = endTime,
            TotalDuration = TimeSpan.FromSeconds(30)
        };

        // Assert
        Assert.Equal(startTime, result.StartedAt);
        Assert.Equal(endTime, result.CompletedAt);
        Assert.Equal(TimeSpan.FromSeconds(30), result.TotalDuration);
    }

    [Fact]
    public void LoadTestResult_RequestType_SetsAndGetsCorrectly()
    {
        // Arrange
        var result = new LoadTestResult();

        // Act
        result.RequestType = "TestRequest";

        // Assert
        Assert.Equal("TestRequest", result.RequestType);
    }
}
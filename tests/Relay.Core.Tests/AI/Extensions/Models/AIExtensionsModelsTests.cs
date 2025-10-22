using System;
using System.Collections.Generic;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Extensions.Models;

/// <summary>
/// Tests for AI extension model classes.
/// </summary>
public class AIExtensionsModelsTests
{


    [Fact]
    public void AIHealthCheckResult_Should_Initialize_With_Default_Values()
    {
        // Act
        var result = new AIHealthCheckResult();

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Equal(default(DateTime), result.Timestamp);
        Assert.Equal(TimeSpan.Zero, result.Duration);
        Assert.NotNull(result.ComponentResults);
        Assert.Empty(result.ComponentResults);
        Assert.Equal(string.Empty, result.Summary);
        Assert.Null(result.Exception);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
    }

    [Fact]
    public void AIHealthCheckResult_Should_Allow_Setting_All_Properties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var duration = TimeSpan.FromSeconds(5);
        var componentResults = new List<ComponentHealthResult>
        {
            new ComponentHealthResult { ComponentName = "Test1", IsHealthy = true },
            new ComponentHealthResult { ComponentName = "Test2", IsHealthy = false }
        };
        var summary = "Overall health check summary";
        var exception = new Exception("Test exception");
        var data = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        var result = new AIHealthCheckResult
        {
            IsHealthy = true,
            Timestamp = timestamp,
            Duration = duration,
            ComponentResults = componentResults,
            Summary = summary,
            Exception = exception,
            Data = data
        };

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal(timestamp, result.Timestamp);
        Assert.Equal(duration, result.Duration);
        Assert.Equal(componentResults, result.ComponentResults);
        Assert.Equal(summary, result.Summary);
        Assert.Equal(exception, result.Exception);
        Assert.Equal(data, result.Data);
    }

    [Fact]
    public void ComponentHealthResult_Should_Initialize_With_Default_Values()
    {
        // Act
        var result = new ComponentHealthResult();

        // Assert
        Assert.Equal(string.Empty, result.ComponentName);
        Assert.False(result.IsHealthy);
        Assert.Equal(string.Empty, result.Status);
        Assert.Equal(string.Empty, result.Description);
        Assert.Equal(0.0, result.HealthScore);
        Assert.NotNull(result.Warnings);
        Assert.Empty(result.Warnings);
        Assert.NotNull(result.Errors);
        Assert.Empty(result.Errors);
        Assert.Equal(TimeSpan.Zero, result.Duration);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
        Assert.Null(result.Exception);
    }

    [Fact]
    public void ComponentHealthResult_Should_Allow_Setting_All_Properties()
    {
        // Arrange
        var componentName = "TestComponent";
        var status = "Healthy";
        var description = "Component is functioning normally";
        var healthScore = 0.95;
        var warnings = new List<string> { "Minor warning 1", "Minor warning 2" };
        var errors = new List<string> { "Error 1" };
        var duration = TimeSpan.FromMilliseconds(150);
        var data = new Dictionary<string, object>
        {
            ["metric1"] = 123.45,
            ["metric2"] = "value"
        };
        var exception = new InvalidOperationException("Test exception");

        // Act
        var result = new ComponentHealthResult
        {
            ComponentName = componentName,
            IsHealthy = true,
            Status = status,
            Description = description,
            HealthScore = healthScore,
            Warnings = warnings,
            Errors = errors,
            Duration = duration,
            Data = data,
            Exception = exception
        };

        // Assert
        Assert.Equal(componentName, result.ComponentName);
        Assert.True(result.IsHealthy);
        Assert.Equal(status, result.Status);
        Assert.Equal(description, result.Description);
        Assert.Equal(healthScore, result.HealthScore);
        Assert.Equal(warnings, result.Warnings);
        Assert.Equal(errors, result.Errors);
        Assert.Equal(duration, result.Duration);
        Assert.Equal(data, result.Data);
        Assert.Equal(exception, result.Exception);
    }

    [Fact]
    public void ComponentHealthResult_Should_Support_Health_Score_Range()
    {
        // Arrange & Act
        var result1 = new ComponentHealthResult { HealthScore = 0.0 };
        var result2 = new ComponentHealthResult { HealthScore = 0.5 };
        var result3 = new ComponentHealthResult { HealthScore = 1.0 };

        // Assert
        Assert.Equal(0.0, result1.HealthScore);
        Assert.Equal(0.5, result2.HealthScore);
        Assert.Equal(1.0, result3.HealthScore);
    }

    [Fact]
    public void AIHealthCheckResult_Should_Support_Complex_Component_Results()
    {
        // Arrange
        var component1 = new ComponentHealthResult
        {
            ComponentName = "Database",
            IsHealthy = true,
            HealthScore = 0.98,
            Status = "Connected",
            Warnings = new List<string> { "High latency detected" }
        };

        var component2 = new ComponentHealthResult
        {
            ComponentName = "Cache",
            IsHealthy = false,
            HealthScore = 0.2,
            Status = "Disconnected",
            Errors = new List<string> { "Connection timeout", "Service unavailable" },
            Exception = new TimeoutException("Cache service timeout")
        };

        // Act
        var healthResult = new AIHealthCheckResult
        {
            IsHealthy = false,
            ComponentResults = new List<ComponentHealthResult> { component1, component2 },
            Summary = "One or more components are unhealthy"
        };

        // Assert
        Assert.False(healthResult.IsHealthy);
        Assert.Equal(2, healthResult.ComponentResults.Count);
        Assert.Contains(healthResult.ComponentResults, c => c.ComponentName == "Database");
        Assert.Contains(healthResult.ComponentResults, c => c.ComponentName == "Cache");
        Assert.Equal("One or more components are unhealthy", healthResult.Summary);
    }
}
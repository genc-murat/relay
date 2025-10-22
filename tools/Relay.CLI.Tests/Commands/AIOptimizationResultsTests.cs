using Relay.CLI.Commands;

using Xunit;

namespace Relay.CLI.Tests.Commands;

public class AIOptimizationResultsTests
{
    [Fact]
    public void AIOptimizationResults_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var results = new AIOptimizationResults();

        // Assert
        Assert.NotNull(results.AppliedOptimizations);
        Assert.Empty(results.AppliedOptimizations);
        Assert.Equal(0.0, results.OverallImprovement);
    }

    [Fact]
    public void AIOptimizationResults_CanSetAppliedOptimizations()
    {
        // Arrange
        var results = new AIOptimizationResults();
        var optimizations = new[]
        {
            new OptimizationResult { Strategy = "Caching", FilePath = "Services/UserService.cs", Description = "Added cache", Success = true, PerformanceGain = 0.6 },
            new OptimizationResult { Strategy = "Async", FilePath = "Services/OrderService.cs", Description = "Converted to ValueTask", Success = true, PerformanceGain = 0.1 }
        };

        // Act
        results.AppliedOptimizations = optimizations;

        // Assert
        Assert.Equal(optimizations, results.AppliedOptimizations);
        Assert.Equal(2, results.AppliedOptimizations.Length);
    }

    [Fact]
    public void AIOptimizationResults_CanSetOverallImprovement()
    {
        // Arrange
        var results = new AIOptimizationResults();
        var expectedImprovement = 0.35;

        // Act
        results.OverallImprovement = expectedImprovement;

        // Assert
        Assert.Equal(expectedImprovement, results.OverallImprovement);
    }

    [Fact]
    public void AIOptimizationResults_OverallImprovement_AcceptsValidRange()
    {
        // Arrange
        var results = new AIOptimizationResults
        {
            // Act & Assert
            OverallImprovement = 0.0
        };
        Assert.Equal(0.0, results.OverallImprovement);

        results.OverallImprovement = 1.0;
        Assert.Equal(1.0, results.OverallImprovement);

        results.OverallImprovement = 0.5;
        Assert.Equal(0.5, results.OverallImprovement);
    }

    [Fact]
    public void AIOptimizationResults_AppliedOptimizations_CanBeNullInitially()
    {
        // Arrange
        var results = new AIOptimizationResults
        {
            // Act
            AppliedOptimizations = null!
        };

        // Assert
        Assert.Null(results.AppliedOptimizations);
    }

    [Fact]
    public void AIOptimizationResults_WithTypicalValues_HasExpectedStructure()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations =
            [
                new OptimizationResult { Strategy = "Caching", FilePath = "Services/UserService.cs", Description = "Added [DistributedCache] attribute", Success = true, PerformanceGain = 0.6 },
                new OptimizationResult { Strategy = "Async", FilePath = "Services/OrderService.cs", Description = "Converted Task to ValueTask", Success = true, PerformanceGain = 0.1 }
            ],
            OverallImprovement = 0.35
        };

        // Assert
        Assert.Equal(2, results.AppliedOptimizations.Length);
        Assert.Equal("Caching", results.AppliedOptimizations[0].Strategy);
        Assert.Equal("Services/UserService.cs", results.AppliedOptimizations[0].FilePath);
        Assert.Equal("Added [DistributedCache] attribute", results.AppliedOptimizations[0].Description);
        Assert.True(results.AppliedOptimizations[0].Success);
        Assert.Equal(0.6, results.AppliedOptimizations[0].PerformanceGain);

        Assert.Equal("Async", results.AppliedOptimizations[1].Strategy);
        Assert.Equal("Services/OrderService.cs", results.AppliedOptimizations[1].FilePath);
        Assert.Equal("Converted Task to ValueTask", results.AppliedOptimizations[1].Description);
        Assert.True(results.AppliedOptimizations[1].Success);
        Assert.Equal(0.1, results.AppliedOptimizations[1].PerformanceGain);

        Assert.Equal(0.35, results.OverallImprovement);
    }

    [Fact]
    public void AIOptimizationResults_AppliedOptimizations_AreIndependentObjects()
    {
        // Arrange
        var results1 = new AIOptimizationResults
        {
            AppliedOptimizations =
            [
                new OptimizationResult { Strategy = "Test1", Success = true }
            ]
        };

        var results2 = new AIOptimizationResults
        {
            AppliedOptimizations =
            [
                new OptimizationResult { Strategy = "Test2", Success = false }
            ]
        };

        // Act & Assert
        Assert.Equal("Test1", results1.AppliedOptimizations[0].Strategy);
        Assert.Equal("Test2", results2.AppliedOptimizations[0].Strategy);
        Assert.NotSame(results1.AppliedOptimizations[0], results2.AppliedOptimizations[0]);
    }

    [Fact]
    public void AIOptimizationResults_CanBeSerializedToJson()
    {
        // Arrange
        var results = new AIOptimizationResults
        {
            AppliedOptimizations =
            [
                new OptimizationResult { Strategy = "Caching", FilePath = "Services/UserService.cs", Description = "Added cache", Success = true, PerformanceGain = 0.6 }
            ],
            OverallImprovement = 0.35
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(results);

        // Assert
        Assert.Contains("Caching", json);
        Assert.Contains("Services/UserService.cs", json);
        Assert.Contains("Added cache", json);
        Assert.Contains("true", json);
        Assert.Contains("0.6", json);
        Assert.Contains("0.35", json);
    }

    [Fact]
    public void AIOptimizationResults_CanBeDeserializedFromJson()
    {
        // Arrange
        var json = @"{
            ""AppliedOptimizations"": [
                {
                    ""Strategy"": ""Caching"",
                    ""FilePath"": ""Services/UserService.cs"",
                    ""Description"": ""Added cache"",
                    ""Success"": true,
                    ""PerformanceGain"": 0.6
                }
            ],
            ""OverallImprovement"": 0.35
        }";

        // Act
        var results = System.Text.Json.JsonSerializer.Deserialize<AIOptimizationResults>(json);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results!.AppliedOptimizations);
        Assert.Equal("Caching", results.AppliedOptimizations[0].Strategy);
        Assert.Equal("Services/UserService.cs", results.AppliedOptimizations[0].FilePath);
        Assert.Equal("Added cache", results.AppliedOptimizations[0].Description);
        Assert.True(results.AppliedOptimizations[0].Success);
        Assert.Equal(0.6, results.AppliedOptimizations[0].PerformanceGain);
        Assert.Equal(0.35, results.OverallImprovement);
    }

    [Fact]
    public void AIOptimizationResults_EmptyAppliedOptimizations_SerializesCorrectly()
    {
        // Arrange
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = [],
            OverallImprovement = 0.0
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(results);

        // Assert
        Assert.Contains("[]", json);
        Assert.Contains("0", json);
    }

    [Fact]
    public void AIOptimizationResults_WithZeroValues_IsValid()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = [],
            OverallImprovement = 0.0
        };

        // Assert
        Assert.Empty(results.AppliedOptimizations);
        Assert.Equal(0.0, results.OverallImprovement);
    }

    [Fact]
    public void AIOptimizationResults_WithMaximumValues_IsValid()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations =
            [
                new OptimizationResult { Strategy = "Test", FilePath = "Test.cs", Description = "Test", Success = true, PerformanceGain = double.MaxValue }
            ],
            OverallImprovement = double.MaxValue
        };

        // Assert
        Assert.Equal(double.MaxValue, results.AppliedOptimizations[0].PerformanceGain);
        Assert.Equal(double.MaxValue, results.OverallImprovement);
    }

    [Fact]
    public void AIOptimizationResults_AppliedOptimizations_CanContainMultipleItems()
    {
        // Arrange
        var optimizations = new OptimizationResult[100];
        for (int i = 0; i < optimizations.Length; i++)
        {
            optimizations[i] = new OptimizationResult
            {
                Strategy = $"Strategy{i}",
                FilePath = $"File{i}.cs",
                Description = $"Description{i}",
                Success = i % 2 == 0,
                PerformanceGain = i * 0.01
            };
        }

        var results = new AIOptimizationResults
        {
            AppliedOptimizations = optimizations
        };

        // Act & Assert
        Assert.Equal(100, results.AppliedOptimizations.Length);
        for (int i = 0; i < optimizations.Length; i++)
        {
            Assert.Equal($"Strategy{i}", results.AppliedOptimizations[i].Strategy);
            Assert.Equal($"File{i}.cs", results.AppliedOptimizations[i].FilePath);
            Assert.Equal($"Description{i}", results.AppliedOptimizations[i].Description);
            Assert.Equal(i % 2 == 0, results.AppliedOptimizations[i].Success);
            Assert.Equal(i * 0.01, results.AppliedOptimizations[i].PerformanceGain);
        }
    }

    [Fact]
    public void AIOptimizationResults_IsReferenceType()
    {
        // Arrange & Act
        var results1 = new AIOptimizationResults();
        var results2 = results1;

        // Assert
        Assert.Same(results1, results2);
    }

    [Fact]
    public void AIOptimizationResults_PropertiesAreIndependent()
    {
        // Arrange
        var results1 = new AIOptimizationResults { OverallImprovement = 0.5 };
        var results2 = new AIOptimizationResults { OverallImprovement = 0.7 };

        // Act
        results1.OverallImprovement = 0.8;

        // Assert
        Assert.Equal(0.8, results1.OverallImprovement);
        Assert.Equal(0.7, results2.OverallImprovement);
    }

    [Fact]
    public void AIOptimizationResults_AppliedOptimizations_ArrayIsMutable()
    {
        // Arrange
        var results = new AIOptimizationResults
        {
            AppliedOptimizations =
            [
                new OptimizationResult { Strategy = "Initial", Success = true }
            ]
        };

        // Act
        results.AppliedOptimizations[0].Strategy = "Modified";

        // Assert
        Assert.Equal("Modified", results.AppliedOptimizations[0].Strategy);
    }

    [Fact]
    public void AIOptimizationResults_CanHaveMixedSuccessStates()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations =
            [
                new OptimizationResult { Strategy = "SuccessOpt", Success = true, PerformanceGain = 0.5 },
                new OptimizationResult { Strategy = "FailedOpt", Success = false, PerformanceGain = 0.0 }
            ],
            OverallImprovement = 0.25
        };

        // Assert
        Assert.Contains(results.AppliedOptimizations, o => o.Success == true);
        Assert.Contains(results.AppliedOptimizations, o => o.Success == false);
        Assert.Equal(1, results.AppliedOptimizations.Count(o => o.Success));
        Assert.Equal(1, results.AppliedOptimizations.Count(o => !o.Success));
    }

    [Fact]
    public void AIOptimizationResults_PerformanceGains_CanBeNegative()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations =
            [
                new OptimizationResult { Strategy = "BadOpt", Success = false, PerformanceGain = -0.1 }
            ],
            OverallImprovement = -0.05
        };

        // Assert
        Assert.Equal(-0.1, results.AppliedOptimizations[0].PerformanceGain);
        Assert.Equal(-0.05, results.OverallImprovement);
    }

    [Fact]
    public void AIOptimizationResults_FilePaths_CanBeEmpty()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations =
            [
                new OptimizationResult { Strategy = "Test", FilePath = "", Description = "Test", Success = true, PerformanceGain = 0.1 }
            ]
        };

        // Assert
        Assert.Empty(results.AppliedOptimizations[0].FilePath);
    }

    [Fact]
    public void AIOptimizationResults_Descriptions_CanBeEmpty()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations =
            [
                new OptimizationResult { Strategy = "Test", FilePath = "Test.cs", Description = "", Success = true, PerformanceGain = 0.1 }
            ]
        };

        // Assert
        Assert.Empty(results.AppliedOptimizations[0].Description);
    }
}

public class OptimizationResultTests
{
    [Fact]
    public void OptimizationResult_DefaultConstructor_SetsDefaultValues()
    {
        // Act
        var result = new OptimizationResult();

        // Assert
        Assert.Empty(result.Strategy);
        Assert.Empty(result.FilePath);
        Assert.Empty(result.Description);
        Assert.False(result.Success);
        Assert.Equal(0.0, result.PerformanceGain);
    }

    [Fact]
    public void OptimizationResult_CanSetAllProperties()
    {
        // Arrange & Act
        var result = new OptimizationResult
        {
            Strategy = "Caching",
            FilePath = "Services/UserService.cs",
            Description = "Added distributed cache",
            Success = true,
            PerformanceGain = 0.6
        };

        // Assert
        Assert.Equal("Caching", result.Strategy);
        Assert.Equal("Services/UserService.cs", result.FilePath);
        Assert.Equal("Added distributed cache", result.Description);
        Assert.True(result.Success);
        Assert.Equal(0.6, result.PerformanceGain);
    }

    [Fact]
    public void OptimizationResult_PerformanceGain_AcceptsDecimalValues()
    {
        // Arrange
        var result = new OptimizationResult
        {
            // Act
            PerformanceGain = 0.123
        };

        // Assert
        Assert.Equal(0.123, result.PerformanceGain);
    }

    [Fact]
    public void OptimizationResult_CanBeSerializedToJson()
    {
        // Arrange
        var result = new OptimizationResult
        {
            Strategy = "Async",
            FilePath = "Handlers/TestHandler.cs",
            Description = "Converted to ValueTask",
            Success = true,
            PerformanceGain = 0.15
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(result);

        // Assert
        Assert.Contains("Async", json);
        Assert.Contains("Handlers/TestHandler.cs", json);
        Assert.Contains("Converted to ValueTask", json);
        Assert.Contains("true", json);
        Assert.Contains("0.15", json);
    }

    [Fact]
    public void OptimizationResult_CanBeDeserializedFromJson()
    {
        // Arrange
        var json = @"{
            ""Strategy"": ""Database"",
            ""FilePath"": ""Repositories/UserRepository.cs"",
            ""Description"": ""Added query optimization"",
            ""Success"": true,
            ""PerformanceGain"": 0.3
        }";

        // Act
        var result = System.Text.Json.JsonSerializer.Deserialize<OptimizationResult>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Database", result!.Strategy);
        Assert.Equal("Repositories/UserRepository.cs", result.FilePath);
        Assert.Equal("Added query optimization", result.Description);
        Assert.True(result.Success);
        Assert.Equal(0.3, result.PerformanceGain);
    }

    [Fact]
    public void OptimizationResult_IsReferenceType()
    {
        // Arrange & Act
        var result1 = new OptimizationResult();
        var result2 = result1;

        // Assert
        Assert.Same(result1, result2);
    }

    [Fact]
    public void OptimizationResult_PropertiesAreIndependent()
    {
        // Arrange
        var result1 = new OptimizationResult { PerformanceGain = 0.5 };
        var result2 = new OptimizationResult { PerformanceGain = 0.7 };

        // Act
        result1.PerformanceGain = 0.8;

        // Assert
        Assert.Equal(0.8, result1.PerformanceGain);
        Assert.Equal(0.7, result2.PerformanceGain);
    }
}
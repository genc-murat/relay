using Relay.CLI.Commands;
using FluentAssertions;
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
        results.AppliedOptimizations.Should().NotBeNull();
        results.AppliedOptimizations.Should().BeEmpty();
        results.OverallImprovement.Should().Be(0.0);
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
        results.AppliedOptimizations.Should().BeEquivalentTo(optimizations);
        results.AppliedOptimizations.Should().HaveCount(2);
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
        results.OverallImprovement.Should().Be(expectedImprovement);
    }

    [Fact]
    public void AIOptimizationResults_OverallImprovement_AcceptsValidRange()
    {
        // Arrange
        var results = new AIOptimizationResults();

        // Act & Assert
        results.OverallImprovement = 0.0;
        results.OverallImprovement.Should().Be(0.0);

        results.OverallImprovement = 1.0;
        results.OverallImprovement.Should().Be(1.0);

        results.OverallImprovement = 0.5;
        results.OverallImprovement.Should().Be(0.5);
    }

    [Fact]
    public void AIOptimizationResults_AppliedOptimizations_CanBeNullInitially()
    {
        // Arrange
        var results = new AIOptimizationResults();

        // Act
        results.AppliedOptimizations = null!;

        // Assert
        results.AppliedOptimizations.Should().BeNull();
    }

    [Fact]
    public void AIOptimizationResults_WithTypicalValues_HasExpectedStructure()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = new[]
            {
                new OptimizationResult { Strategy = "Caching", FilePath = "Services/UserService.cs", Description = "Added [DistributedCache] attribute", Success = true, PerformanceGain = 0.6 },
                new OptimizationResult { Strategy = "Async", FilePath = "Services/OrderService.cs", Description = "Converted Task to ValueTask", Success = true, PerformanceGain = 0.1 }
            },
            OverallImprovement = 0.35
        };

        // Assert
        results.AppliedOptimizations.Should().HaveCount(2);
        results.AppliedOptimizations[0].Strategy.Should().Be("Caching");
        results.AppliedOptimizations[0].FilePath.Should().Be("Services/UserService.cs");
        results.AppliedOptimizations[0].Description.Should().Be("Added [DistributedCache] attribute");
        results.AppliedOptimizations[0].Success.Should().BeTrue();
        results.AppliedOptimizations[0].PerformanceGain.Should().Be(0.6);

        results.AppliedOptimizations[1].Strategy.Should().Be("Async");
        results.AppliedOptimizations[1].FilePath.Should().Be("Services/OrderService.cs");
        results.AppliedOptimizations[1].Description.Should().Be("Converted Task to ValueTask");
        results.AppliedOptimizations[1].Success.Should().BeTrue();
        results.AppliedOptimizations[1].PerformanceGain.Should().Be(0.1);

        results.OverallImprovement.Should().Be(0.35);
    }

    [Fact]
    public void AIOptimizationResults_AppliedOptimizations_AreIndependentObjects()
    {
        // Arrange
        var results1 = new AIOptimizationResults
        {
            AppliedOptimizations = new[]
            {
                new OptimizationResult { Strategy = "Test1", Success = true }
            }
        };

        var results2 = new AIOptimizationResults
        {
            AppliedOptimizations = new[]
            {
                new OptimizationResult { Strategy = "Test2", Success = false }
            }
        };

        // Act & Assert
        results1.AppliedOptimizations[0].Strategy.Should().Be("Test1");
        results2.AppliedOptimizations[0].Strategy.Should().Be("Test2");
        results1.AppliedOptimizations[0].Should().NotBeSameAs(results2.AppliedOptimizations[0]);
    }

    [Fact]
    public void AIOptimizationResults_CanBeSerializedToJson()
    {
        // Arrange
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = new[]
            {
                new OptimizationResult { Strategy = "Caching", FilePath = "Services/UserService.cs", Description = "Added cache", Success = true, PerformanceGain = 0.6 }
            },
            OverallImprovement = 0.35
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(results);

        // Assert
        json.Should().Contain("Caching");
        json.Should().Contain("Services/UserService.cs");
        json.Should().Contain("Added cache");
        json.Should().Contain("true");
        json.Should().Contain("0.6");
        json.Should().Contain("0.35");
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
        results.Should().NotBeNull();
        results!.AppliedOptimizations.Should().HaveCount(1);
        results.AppliedOptimizations[0].Strategy.Should().Be("Caching");
        results.AppliedOptimizations[0].FilePath.Should().Be("Services/UserService.cs");
        results.AppliedOptimizations[0].Description.Should().Be("Added cache");
        results.AppliedOptimizations[0].Success.Should().BeTrue();
        results.AppliedOptimizations[0].PerformanceGain.Should().Be(0.6);
        results.OverallImprovement.Should().Be(0.35);
    }

    [Fact]
    public void AIOptimizationResults_EmptyAppliedOptimizations_SerializesCorrectly()
    {
        // Arrange
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = Array.Empty<OptimizationResult>(),
            OverallImprovement = 0.0
        };

        // Act
        var json = System.Text.Json.JsonSerializer.Serialize(results);

        // Assert
        json.Should().Contain("[]");
        json.Should().Contain("0");
    }

    [Fact]
    public void AIOptimizationResults_WithZeroValues_IsValid()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = Array.Empty<OptimizationResult>(),
            OverallImprovement = 0.0
        };

        // Assert
        results.AppliedOptimizations.Should().BeEmpty();
        results.OverallImprovement.Should().Be(0.0);
    }

    [Fact]
    public void AIOptimizationResults_WithMaximumValues_IsValid()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = new[]
            {
                new OptimizationResult { Strategy = "Test", FilePath = "Test.cs", Description = "Test", Success = true, PerformanceGain = double.MaxValue }
            },
            OverallImprovement = double.MaxValue
        };

        // Assert
        results.AppliedOptimizations[0].PerformanceGain.Should().Be(double.MaxValue);
        results.OverallImprovement.Should().Be(double.MaxValue);
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
        results.AppliedOptimizations.Should().HaveCount(100);
        for (int i = 0; i < optimizations.Length; i++)
        {
            results.AppliedOptimizations[i].Strategy.Should().Be($"Strategy{i}");
            results.AppliedOptimizations[i].FilePath.Should().Be($"File{i}.cs");
            results.AppliedOptimizations[i].Description.Should().Be($"Description{i}");
            results.AppliedOptimizations[i].Success.Should().Be(i % 2 == 0);
            results.AppliedOptimizations[i].PerformanceGain.Should().Be(i * 0.01);
        }
    }

    [Fact]
    public void AIOptimizationResults_IsReferenceType()
    {
        // Arrange & Act
        var results1 = new AIOptimizationResults();
        var results2 = results1;

        // Assert
        results1.Should().BeSameAs(results2);
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
        results1.OverallImprovement.Should().Be(0.8);
        results2.OverallImprovement.Should().Be(0.7);
    }

    [Fact]
    public void AIOptimizationResults_AppliedOptimizations_ArrayIsMutable()
    {
        // Arrange
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = new[]
            {
                new OptimizationResult { Strategy = "Initial", Success = true }
            }
        };

        // Act
        results.AppliedOptimizations[0].Strategy = "Modified";

        // Assert
        results.AppliedOptimizations[0].Strategy.Should().Be("Modified");
    }

    [Fact]
    public void AIOptimizationResults_CanHaveMixedSuccessStates()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = new[]
            {
                new OptimizationResult { Strategy = "SuccessOpt", Success = true, PerformanceGain = 0.5 },
                new OptimizationResult { Strategy = "FailedOpt", Success = false, PerformanceGain = 0.0 }
            },
            OverallImprovement = 0.25
        };

        // Assert
        results.AppliedOptimizations.Should().Contain(o => o.Success == true);
        results.AppliedOptimizations.Should().Contain(o => o.Success == false);
        results.AppliedOptimizations.Count(o => o.Success).Should().Be(1);
        results.AppliedOptimizations.Count(o => !o.Success).Should().Be(1);
    }

    [Fact]
    public void AIOptimizationResults_PerformanceGains_CanBeNegative()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = new[]
            {
                new OptimizationResult { Strategy = "BadOpt", Success = false, PerformanceGain = -0.1 }
            },
            OverallImprovement = -0.05
        };

        // Assert
        results.AppliedOptimizations[0].PerformanceGain.Should().Be(-0.1);
        results.OverallImprovement.Should().Be(-0.05);
    }

    [Fact]
    public void AIOptimizationResults_FilePaths_CanBeEmpty()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = new[]
            {
                new OptimizationResult { Strategy = "Test", FilePath = "", Description = "Test", Success = true, PerformanceGain = 0.1 }
            }
        };

        // Assert
        results.AppliedOptimizations[0].FilePath.Should().BeEmpty();
    }

    [Fact]
    public void AIOptimizationResults_Descriptions_CanBeEmpty()
    {
        // Arrange & Act
        var results = new AIOptimizationResults
        {
            AppliedOptimizations = new[]
            {
                new OptimizationResult { Strategy = "Test", FilePath = "Test.cs", Description = "", Success = true, PerformanceGain = 0.1 }
            }
        };

        // Assert
        results.AppliedOptimizations[0].Description.Should().BeEmpty();
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
        result.Strategy.Should().BeEmpty();
        result.FilePath.Should().BeEmpty();
        result.Description.Should().BeEmpty();
        result.Success.Should().BeFalse();
        result.PerformanceGain.Should().Be(0.0);
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
        result.Strategy.Should().Be("Caching");
        result.FilePath.Should().Be("Services/UserService.cs");
        result.Description.Should().Be("Added distributed cache");
        result.Success.Should().BeTrue();
        result.PerformanceGain.Should().Be(0.6);
    }

    [Fact]
    public void OptimizationResult_PerformanceGain_AcceptsDecimalValues()
    {
        // Arrange
        var result = new OptimizationResult();

        // Act
        result.PerformanceGain = 0.123;

        // Assert
        result.PerformanceGain.Should().Be(0.123);
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
        json.Should().Contain("Async");
        json.Should().Contain("Handlers/TestHandler.cs");
        json.Should().Contain("Converted to ValueTask");
        json.Should().Contain("true");
        json.Should().Contain("0.15");
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
        result.Should().NotBeNull();
        result!.Strategy.Should().Be("Database");
        result.FilePath.Should().Be("Repositories/UserRepository.cs");
        result.Description.Should().Be("Added query optimization");
        result.Success.Should().BeTrue();
        result.PerformanceGain.Should().Be(0.3);
    }

    [Fact]
    public void OptimizationResult_IsReferenceType()
    {
        // Arrange & Act
        var result1 = new OptimizationResult();
        var result2 = result1;

        // Assert
        result1.Should().BeSameAs(result2);
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
        result1.PerformanceGain.Should().Be(0.8);
        result2.PerformanceGain.Should().Be(0.7);
    }
}
using Relay.CLI.Commands.Models.Pipeline;

namespace Relay.CLI.Tests.Commands;

public class PipelineResultTests
{
    [Fact]
    public void PipelineResult_ShouldHaveStagesProperty()
    {
        // Arrange & Act
        var result = new PipelineResult();
        var stages = new List<PipelineStageResult>
        {
            new() { StageName = "Build" },
            new() { StageName = "Test" }
        };

        // Act
        result.Stages = stages;

        // Assert
        Assert.Equal(stages, result.Stages);
    }

    [Fact]
    public void PipelineResult_ShouldHaveTotalDurationProperty()
    {
        // Arrange & Act
        var duration = TimeSpan.FromSeconds(45.5);
        var result = new PipelineResult { TotalDuration = duration };

        // Assert
        Assert.Equal(duration, result.TotalDuration);
    }

    [Fact]
    public void PipelineResult_ShouldHaveSuccessProperty()
    {
        // Arrange & Act
        var result = new PipelineResult { Success = true };

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void PipelineResult_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var result = new PipelineResult();

        // Assert
        Assert.NotNull(result.Stages);
        Assert.Empty(result.Stages);
        Assert.Equal(TimeSpan.Zero, result.TotalDuration);
        Assert.False(result.Success);
    }

    [Fact]
    public void PipelineResult_CanSetAllPropertiesViaInitializer()
    {
        // Arrange
        var stages = new List<PipelineStageResult>
        {
            new() { StageName = "Compile", Success = true, Duration = TimeSpan.FromSeconds(10) },
            new() { StageName = "Test", Success = true, Duration = TimeSpan.FromSeconds(20) }
        };
        var totalDuration = TimeSpan.FromSeconds(35);

        // Act
        var result = new PipelineResult
        {
            Stages = stages,
            TotalDuration = totalDuration,
            Success = true
        };

        // Assert
        Assert.Equal(stages, result.Stages);
        Assert.Equal(totalDuration, result.TotalDuration);
        Assert.True(result.Success);
    }

    [Fact]
    public void PipelineResult_CanAddStages()
    {
        // Arrange
        var result = new PipelineResult();
        var stage1 = new PipelineStageResult { StageName = "Init", Success = true };
        var stage2 = new PipelineStageResult { StageName = "Build", Success = true };

        // Act
        result.Stages.Add(stage1);
        result.Stages.Add(stage2);

        // Assert
        Assert.Equal(2, result.Stages.Count);
        Assert.Equal(stage1, result.Stages[0]);
        Assert.Equal(stage2, result.Stages[1]);
    }

    [Fact]
    public void PipelineResult_Stages_CanBeEmpty()
    {
        // Arrange & Act
        var result = new PipelineResult();

        // Assert
        Assert.Empty(result.Stages);
    }

    [Fact]
    public void PipelineResult_Stages_CanContainMultipleStages()
    {
        // Arrange
        var result = new PipelineResult();
        for (int i = 1; i <= 10; i++)
        {
            result.Stages.Add(new PipelineStageResult
            {
                StageName = $"Stage{i}",
                Success = true,
                Duration = TimeSpan.FromSeconds(i)
            });
        }

        // Assert
        Assert.Equal(10, result.Stages.Count);
        Assert.Equal(55, result.Stages.Sum(s => s.Duration.TotalSeconds)); // Sum of 1-10
    }

    [Fact]
    public void PipelineResult_TotalDuration_CanBeZero()
    {
        // Arrange & Act
        var result = new PipelineResult { TotalDuration = TimeSpan.Zero };

        // Assert
        Assert.Equal(TimeSpan.Zero, result.TotalDuration);
    }

    [Fact]
    public void PipelineResult_TotalDuration_CanBeLarge()
    {
        // Arrange & Act
        var result = new PipelineResult { TotalDuration = TimeSpan.FromHours(2) };

        // Assert
        Assert.Equal(TimeSpan.FromHours(2), result.TotalDuration);
    }

    [Fact]
    public void PipelineResult_Success_CanBeTrue()
    {
        // Arrange & Act
        var result = new PipelineResult { Success = true };

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void PipelineResult_Success_CanBeFalse()
    {
        // Arrange & Act
        var result = new PipelineResult { Success = false };

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void PipelineResult_CanBeUsedInCollections()
    {
        // Arrange & Act
        var results = new List<PipelineResult>
        {
            new PipelineResult { Success = true, TotalDuration = TimeSpan.FromSeconds(30) },
            new PipelineResult { Success = false, TotalDuration = TimeSpan.FromSeconds(45) },
            new PipelineResult { Success = true, TotalDuration = TimeSpan.FromSeconds(25) }
        };

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(2, results.Count(r => r.Success));
        Assert.Equal(27.5, results.Where(r => r.Success).Average(r => r.TotalDuration.TotalSeconds));
    }

    [Fact]
    public void PipelineResult_CanFilterSuccessfulResults()
    {
        // Arrange
        var results = new List<PipelineResult>
        {
            new() { Success = true, TotalDuration = TimeSpan.FromSeconds(30) },
            new() { Success = false, TotalDuration = TimeSpan.FromSeconds(45) },
            new() { Success = true, TotalDuration = TimeSpan.FromSeconds(25) },
            new() { Success = false, TotalDuration = TimeSpan.FromSeconds(60) }
        };

        // Act
        var successfulResults = results.Where(r => r.Success).ToList();
        var failedResults = results.Where(r => !r.Success).ToList();

        // Assert
        Assert.Equal(2, successfulResults.Count);
        Assert.Equal(2, failedResults.Count);
        Assert.True(successfulResults.All(r => r.Success));
        Assert.True(failedResults.All(r => !r.Success));
    }

    [Fact]
    public void PipelineResult_CanCalculateAggregates()
    {
        // Arrange
        var results = new List<PipelineResult>
        {
            new PipelineResult
            {
                Success = true,
                TotalDuration = TimeSpan.FromSeconds(30),
                Stages = new List<PipelineStageResult>
                {
                    new PipelineStageResult { StageName = "Build", Duration = TimeSpan.FromSeconds(10) },
                    new PipelineStageResult { StageName = "Test", Duration = TimeSpan.FromSeconds(20) }
                }
            },
            new PipelineResult
            {
                Success = true,
                TotalDuration = TimeSpan.FromSeconds(45),
                Stages = new List<PipelineStageResult>
                {
                    new PipelineStageResult { StageName = "Build", Duration = TimeSpan.FromSeconds(15) },
                    new PipelineStageResult { StageName = "Test", Duration = TimeSpan.FromSeconds(30) }
                }
            }
        };

        // Act
        var totalStages = results.Sum(r => r.Stages.Count);
        var averageDuration = results.Average(r => r.TotalDuration.TotalSeconds);
        var successRate = (double)results.Count(r => r.Success) / results.Count * 100;

        // Assert
        Assert.Equal(4, totalStages);
        Assert.Equal(37.5, averageDuration);
        Assert.Equal(100.0, successRate);
    }

    [Fact]
    public void PipelineResult_ShouldBeClass()
    {
        // Arrange & Act
        var result = new PipelineResult();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.GetType().IsClass);
    }

    [Fact]
    public void PipelineResult_WithSuccessfulPipeline_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new PipelineResult
        {
            Stages = new List<PipelineStageResult>
            {
                new PipelineStageResult
                {
                    StageName = "Initialize",
                    StageEmoji = "🚀",
                    Success = true,
                    Message = "Project initialized successfully",
                    Duration = TimeSpan.FromSeconds(2)
                },
                new PipelineStageResult
                {
                    StageName = "Build",
                    StageEmoji = "🔨",
                    Success = true,
                    Message = "Build completed without errors",
                    Duration = TimeSpan.FromSeconds(15)
                },
                new PipelineStageResult
                {
                    StageName = "Test",
                    StageEmoji = "🧪",
                    Success = true,
                    Message = "All tests passed",
                    Duration = TimeSpan.FromSeconds(25)
                }
            },
            TotalDuration = TimeSpan.FromSeconds(42),
            Success = true
        };

        // Assert
        Assert.Equal(3, result.Stages.Count());
        Assert.Equal(TimeSpan.FromSeconds(42), result.TotalDuration);
        Assert.True(result.Success);
        Assert.True(result.Stages.All(s => s.Success));
        Assert.Equal(42, result.Stages.Sum(s => s.Duration.TotalSeconds));
    }

    [Fact]
    public void PipelineResult_WithFailedPipeline_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new PipelineResult
        {
            Stages = new List<PipelineStageResult>
            {
                new PipelineStageResult
                {
                    StageName = "Initialize",
                    Success = true,
                    Message = "Project initialized successfully",
                    Duration = TimeSpan.FromSeconds(2)
                },
                new PipelineStageResult
                {
                    StageName = "Build",
                    Success = false,
                    Message = "Build failed",
                    Error = "Compilation error in Main.cs",
                    Duration = TimeSpan.FromSeconds(8)
                },
                new PipelineStageResult
                {
                    StageName = "Test",
                    Success = false,
                    Message = "Tests skipped due to build failure",
                    Duration = TimeSpan.Zero
                }
            },
            TotalDuration = TimeSpan.FromSeconds(10),
            Success = false
        };

        // Assert
        Assert.Equal(3, result.Stages.Count());
        Assert.Equal(TimeSpan.FromSeconds(10), result.TotalDuration);
        Assert.False(result.Success);
        Assert.Equal(1, result.Stages.Count(s => s.Success));
        Assert.Equal(2, result.Stages.Count(s => !s.Success));
        Assert.Equal("Compilation error in Main.cs", result.Stages.First(s => s.Error != null).Error);
    }

    [Fact]
    public void PipelineResult_CanBeOrderedByDuration()
    {
        // Arrange
        var results = new List<PipelineResult>
        {
            new PipelineResult { Success = true, TotalDuration = TimeSpan.FromSeconds(60) },
            new PipelineResult { Success = true, TotalDuration = TimeSpan.FromSeconds(30) },
            new PipelineResult { Success = false, TotalDuration = TimeSpan.FromSeconds(45) }
        };

        // Act
        var orderedByDuration = results.OrderBy(r => r.TotalDuration).ToList();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(30), orderedByDuration[0].TotalDuration);
        Assert.Equal(TimeSpan.FromSeconds(45), orderedByDuration[1].TotalDuration);
        Assert.Equal(TimeSpan.FromSeconds(60), orderedByDuration[2].TotalDuration);
    }

    [Fact]
    public void PipelineResult_CanBeGroupedBySuccess()
    {
        // Arrange
        var results = new List<PipelineResult>
        {
            new PipelineResult { Success = true, TotalDuration = TimeSpan.FromSeconds(30) },
            new PipelineResult { Success = false, TotalDuration = TimeSpan.FromSeconds(45) },
            new PipelineResult { Success = true, TotalDuration = TimeSpan.FromSeconds(25) },
            new PipelineResult { Success = false, TotalDuration = TimeSpan.FromSeconds(60) }
        };

        // Act
        var grouped = results.GroupBy(r => r.Success);

        // Assert
        Assert.Equal(2, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key).Count()); // Successful
        Assert.Equal(2, grouped.First(g => !g.Key).Count()); // Failed
    }

    [Fact]
    public void PipelineResult_PropertiesCanBeModified()
    {
        // Arrange
        var result = new PipelineResult
        {
            Stages = new List<PipelineStageResult> { new PipelineStageResult { StageName = "Initial" } },
            TotalDuration = TimeSpan.FromSeconds(10),
            Success = false
        };

        // Act
        result.Stages = new List<PipelineStageResult> { new PipelineStageResult { StageName = "Modified" } };
        result.TotalDuration = TimeSpan.FromSeconds(20);
        result.Success = true;

        // Assert
        Assert.Single(result.Stages);
        Assert.Equal("Modified", result.Stages[0].StageName);
        Assert.Equal(TimeSpan.FromSeconds(20), result.TotalDuration);
        Assert.True(result.Success);
    }

    [Fact]
    public void PipelineResult_WithEmptyStages_ShouldHandleOperations()
    {
        // Arrange
        var result = new PipelineResult
        {
            Stages = new List<PipelineStageResult>(),
            TotalDuration = TimeSpan.FromSeconds(5),
            Success = true
        };

        // Act & Assert
        Assert.Empty(result.Stages);
        Assert.Equal(TimeSpan.FromSeconds(5), result.TotalDuration);
        Assert.True(result.Success);
    }

    [Fact]
    public void PipelineResult_CanCalculateStageDurations()
    {
        // Arrange
        var result = new PipelineResult
        {
            Stages = new List<PipelineStageResult>
            {
                new PipelineStageResult { StageName = "Stage1", Duration = TimeSpan.FromSeconds(10) },
                new PipelineStageResult { StageName = "Stage2", Duration = TimeSpan.FromSeconds(15) },
                new PipelineStageResult { StageName = "Stage3", Duration = TimeSpan.FromSeconds(5) }
            },
            TotalDuration = TimeSpan.FromSeconds(30),
            Success = true
        };

        // Act
        var totalStageDuration = result.Stages.Sum(s => s.Duration.TotalSeconds);
        var stageCount = result.Stages.Count;

        // Assert
        Assert.Equal(30, totalStageDuration);
        Assert.Equal(3, stageCount);
        Assert.Equal(30, result.TotalDuration.TotalSeconds);
    }

    [Fact]
    public void PipelineResult_CanIdentifyFailedStages()
    {
        // Arrange
        var result = new PipelineResult
        {
            Stages = new List<PipelineStageResult>
            {
                new PipelineStageResult { StageName = "Init", Success = true },
                new PipelineStageResult { StageName = "Build", Success = false, Error = "Build failed" },
                new PipelineStageResult { StageName = "Test", Success = false, Message = "Skipped" }
            },
            Success = false
        };

        // Act
        var failedStages = result.Stages.Where(s => !s.Success).ToList();
        var stagesWithErrors = result.Stages.Where(s => s.Error != null).ToList();

        // Assert
        Assert.Equal(2, failedStages.Count());
        Assert.Single(stagesWithErrors);
        Assert.Equal("Build failed", stagesWithErrors[0].Error);
    }

    [Fact]
    public void PipelineResult_WithComplexPipelineData()
    {
        // Arrange & Act
        var result = new PipelineResult
        {
            Stages = new List<PipelineStageResult>
            {
                new PipelineStageResult
                {
                    StageName = "Dependencies",
                    StageEmoji = "📦",
                    Success = true,
                    Message = "All dependencies resolved",
                    Duration = TimeSpan.FromSeconds(3),
                    Details = new List<string> { "Restored 45 packages", "No conflicts detected" }
                },
                new PipelineStageResult
                {
                    StageName = "Compilation",
                    StageEmoji = "⚙️",
                    Success = true,
                    Message = "Code compiled successfully",
                    Duration = TimeSpan.FromSeconds(12),
                    Details = new List<string> { "Compiled 15 source files", "0 warnings, 0 errors" }
                },
                new PipelineStageResult
                {
                    StageName = "Unit Tests",
                    StageEmoji = "🧪",
                    Success = true,
                    Message = "All unit tests passed",
                    Duration = TimeSpan.FromSeconds(8),
                    Details = new List<string> { "Ran 150 tests", "Coverage: 95%" }
                },
                new PipelineStageResult
                {
                    StageName = "Integration Tests",
                    StageEmoji = "🔗",
                    Success = false,
                    Message = "Integration tests failed",
                    Error = "Database connection timeout",
                    Duration = TimeSpan.FromSeconds(5),
                    Details = new List<string> { "Failed test: UserLoginTest", "Timeout after 30s" }
                }
            },
            TotalDuration = TimeSpan.FromSeconds(28),
            Success = false
        };

        // Assert
        Assert.Equal(4, result.Stages.Count());
        Assert.Equal(TimeSpan.FromSeconds(28), result.TotalDuration);
        Assert.False(result.Success);
        Assert.Equal(3, result.Stages.Count(s => s.Success));
        Assert.Equal(1, result.Stages.Count(s => !s.Success));
        Assert.Equal("Integration Tests", result.Stages.First(s => s.Error != null).StageName);
    }
}


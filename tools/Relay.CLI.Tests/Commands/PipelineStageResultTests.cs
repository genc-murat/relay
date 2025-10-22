using Relay.CLI.Commands.Models.Pipeline;

namespace Relay.CLI.Tests.Commands;

public class PipelineStageResultTests
{
    [Fact]
    public void PipelineStageResult_ShouldHaveStageNameProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { StageName = "Build" };

        // Assert
        Assert.Equal("Build", result.StageName);
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveStageEmojiProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { StageEmoji = "🔨" };

        // Assert
        Assert.Equal("🔨", result.StageEmoji);
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveSuccessProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Success = true };

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveMessageProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Message = "Stage completed successfully" };

        // Assert
        Assert.Equal("Stage completed successfully", result.Message);
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveErrorProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Error = "Compilation failed" };

        // Assert
        Assert.Equal("Compilation failed", result.Error);
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveDurationProperty()
    {
        // Arrange & Act
        var duration = TimeSpan.FromSeconds(15.5);
        var result = new PipelineStageResult { Duration = duration };

        // Assert
        Assert.Equal(duration, result.Duration);
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveDetailsProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult();
        List<string> details = [ "Detail 1", "Detail 2" ];

        // Act
        result.Details = details;

        // Assert
        Assert.Equal(details, result.Details);
    }

    [Fact]
    public void PipelineStageResult_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var result = new PipelineStageResult();

        // Assert
        Assert.Equal("", result.StageName);
        Assert.Equal("", result.StageEmoji);
        Assert.False(result.Success);
        Assert.Equal("", result.Message);
        Assert.Null(result.Error);
        Assert.Equal(TimeSpan.Zero, result.Duration);
        Assert.NotNull(result.Details);
        Assert.Empty(result.Details);
    }

    [Fact]
    public void PipelineStageResult_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var result = new PipelineStageResult
        {
            Details = [
                "Step 1: Initialize",
                "Step 2: Process",
                "Step 3: Finalize",
                "Step 4: Cleanup"
            ]
        };

        // Assert
        Assert.Equal(4, result.Details.Count);
        Assert.True(result.Details.All(d => d.StartsWith("Step")));
    }

    [Fact]
    public void PipelineStageResult_CanBeUsedInCollections()
    {
        // Arrange & Act
        var results = new List<PipelineStageResult>
        {
            new() { StageName = "Init", Success = true, Duration = TimeSpan.FromSeconds(2) },
            new() { StageName = "Build", Success = true, Duration = TimeSpan.FromSeconds(15) },
            new() { StageName = "Test", Success = false, Duration = TimeSpan.FromSeconds(8) }
        };

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(2, results.Count(r => r.Success));
        Assert.Equal(25, results.Sum(r => r.Duration.TotalSeconds));
    }

    [Fact]
    public void PipelineStageResult_CanBeFilteredBySuccess()
    {
        // Arrange
        var results = new List<PipelineStageResult>
        {
            new PipelineStageResult { StageName = "Stage1", Success = true },
            new PipelineStageResult { StageName = "Stage2", Success = false, Error = "Failed" },
            new PipelineStageResult { StageName = "Stage3", Success = true },
            new PipelineStageResult { StageName = "Stage4", Success = false }
        };

        // Act
        var successfulResults = results.Where(r => r.Success).ToList();
        var failedResults = results.Where(r => !r.Success).ToList();

        // Assert
        Assert.Equal(2, successfulResults.Count);
        Assert.Equal(2, failedResults.Count);
        Assert.Equal(1, failedResults.Count(r => r.Error != null));
    }

    [Fact]
    public void PipelineStageResult_CanBeOrderedByDuration()
    {
        // Arrange
        var results = new List<PipelineStageResult>
        {
            new PipelineStageResult { StageName = "Fast", Duration = TimeSpan.FromSeconds(1) },
            new PipelineStageResult { StageName = "Slow", Duration = TimeSpan.FromSeconds(30) },
            new PipelineStageResult { StageName = "Medium", Duration = TimeSpan.FromSeconds(10) }
        };

        // Act
        var ordered = results.OrderBy(r => r.Duration).ToList();

        // Assert
        Assert.Equal("Fast", ordered[0].StageName);
        Assert.Equal("Medium", ordered[1].StageName);
        Assert.Equal("Slow", ordered[2].StageName);
    }

    [Fact]
    public void PipelineStageResult_CanBeGroupedBySuccess()
    {
        // Arrange
        var results = new List<PipelineStageResult>
        {
            new PipelineStageResult { StageName = "Stage1", Success = true },
            new PipelineStageResult { StageName = "Stage2", Success = false },
            new PipelineStageResult { StageName = "Stage3", Success = true },
            new PipelineStageResult { StageName = "Stage4", Success = false }
        };

        // Act
        var grouped = results.GroupBy(r => r.Success).ToList();

        // Assert
        Assert.Equal(2, grouped.Count);
        Assert.Equal(2, grouped.First(g => g.Key).Count()); // Successful
        Assert.Equal(2, grouped.First(g => !g.Key).Count()); // Failed
    }

    [Fact]
    public void PipelineStageResult_ShouldBeClass()
    {
        // Arrange & Act
        var result = new PipelineStageResult();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.GetType().IsClass);
    }

    [Fact]
    public void PipelineStageResult_WithSuccessfulStage_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new PipelineStageResult
        {
            StageName = "Build",
            StageEmoji = "🔨",
            Success = true,
            Message = "Build completed successfully",
            Error = null,
            Duration = TimeSpan.FromSeconds(45),
            Details = [
                "Compiled 25 source files",
                "Generated assembly: MyApp.dll",
                "0 errors, 3 warnings"
            ]
        };

        // Assert
        Assert.Equal("Build", result.StageName);
        Assert.Equal("🔨", result.StageEmoji);
        Assert.True(result.Success);
        Assert.Equal("Build completed successfully", result.Message);
        Assert.Null(result.Error);
        Assert.Equal(TimeSpan.FromSeconds(45), result.Duration);
        Assert.Equal(3, result.Details.Count);
    }

    [Fact]
    public void PipelineStageResult_WithFailedStage_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new PipelineStageResult
        {
            StageName = "Unit Tests",
            StageEmoji = "🧪",
            Success = false,
            Message = "Tests failed",
            Error = "3 tests failed, 1 test inconclusive",
            Duration = TimeSpan.FromSeconds(12),
            Details = [
                "TestUserLogin: FAILED",
                "TestPasswordReset: FAILED",
                "TestProfileUpdate: FAILED",
                "TestEmailValidation: INCONCLUSIVE"
            ]
        };

        // Assert
        Assert.Equal("Unit Tests", result.StageName);
        Assert.False(result.Success);
        Assert.Contains("3 tests failed", result.Error);
        Assert.Equal(4, result.Details.Count);
        Assert.Equal(3, result.Details.Count(d => d.Contains("FAILED")));
    }

    [Fact]
    public void PipelineStageResult_PropertiesCanBeModified()
    {
        // Arrange
        var result = new PipelineStageResult
        {
            StageName = "Initial",
            Success = false,
            Message = "Initial message",
            Duration = TimeSpan.FromSeconds(5)
        };

        // Act
        result.StageName = "Modified";
        result.Success = true;
        result.Message = "Modified message";
        result.Duration = TimeSpan.FromSeconds(10);

        // Assert
        Assert.Equal("Modified", result.StageName);
        Assert.True(result.Success);
        Assert.Equal("Modified message", result.Message);
        Assert.Equal(TimeSpan.FromSeconds(10), result.Duration);
    }

    [Fact]
    public void PipelineStageResult_WithEmptyDetails_ShouldHandleOperations()
    {
        // Arrange
        var result = new PipelineStageResult
        {
            StageName = "Simple Stage",
            Success = true,
            Details = []
        };

        // Act & Assert
        Assert.Empty(result.Details);
        Assert.True(result.Success);
    }

    [Fact]
    public void PipelineStageResult_CanCalculateDurationStatistics()
    {
        // Arrange
        var results = new List<PipelineStageResult>
        {
            new() { StageName = "Fast", Duration = TimeSpan.FromSeconds(1) },
            new() { StageName = "Medium", Duration = TimeSpan.FromSeconds(5) },
            new() { StageName = "Slow", Duration = TimeSpan.FromSeconds(15) }
        };

        // Act
        var totalDuration = results.Sum(r => r.Duration.TotalSeconds);
        var averageDuration = results.Average(r => r.Duration.TotalSeconds);
        var maxDuration = results.Max(r => r.Duration.TotalSeconds);

        // Assert
        Assert.Equal(21, totalDuration);
        Assert.Equal(7, averageDuration);
        Assert.Equal(15, maxDuration);
    }

    [Fact]
    public void PipelineStageResult_CanIdentifyStagesWithErrors()
    {
        // Arrange
        var results = new List<PipelineStageResult>
        {
            new() { StageName = "Init", Success = true },
            new() { StageName = "Build", Success = false, Error = "Compile error" },
            new() { StageName = "Test", Success = false, Message = "Skipped" },
            new() { StageName = "Deploy", Success = true }
        };

        // Act
        var stagesWithErrors = results.Where(r => r.Error != null).ToList();
        var failedStages = results.Where(r => !r.Success).ToList();

        // Assert
        Assert.Single(stagesWithErrors);
        Assert.Equal("Build", stagesWithErrors[0].StageName);
        Assert.Equal(2, failedStages.Count);
    }

    [Fact]
    public void PipelineStageResult_CanBeFilteredByStageName()
    {
        // Arrange
        var results = new List<PipelineStageResult>
        {
            new() { StageName = "Build", Success = true },
            new() { StageName = "Test", Success = true },
            new() { StageName = "Build", Success = false },
            new() { StageName = "Deploy", Success = true }
        };

        // Act
        var buildStages = results.Where(r => r.StageName == "Build").ToList();

        // Assert
        Assert.Equal(2, buildStages.Count);
        Assert.Equal(1, buildStages.Count(r => r.Success));
        Assert.Equal(1, buildStages.Count(r => !r.Success));
    }

    [Fact]
    public void PipelineStageResult_WithComplexData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var result = new PipelineStageResult
        {
            StageName = "Integration Tests",
            StageEmoji = "🔗",
            Success = false,
            Message = "Integration tests failed due to database timeout",
            Error = "System.TimeoutException: Connection timeout after 00:00:30",
            Duration = TimeSpan.FromSeconds(35),
            Details = [
                "Starting database connection...",
                "Connection established in 2.3s",
                "Running test suite...",
                "Test 'UserAuthenticationTest' started",
                "Database query timeout at 00:00:30",
                "Test 'UserAuthenticationTest' failed",
                "Remaining tests skipped"
            ]
        };

        // Assert
        Assert.Equal("Integration Tests", result.StageName);
        Assert.False(result.Success);
        Assert.Contains("TimeoutException", result.Error);
        Assert.Equal(TimeSpan.FromSeconds(35), result.Duration);
        Assert.Equal(7, result.Details.Count);
        Assert.Equal(1, result.Details.Count(d => d.Contains("failed")));
        Assert.Equal(1, result.Details.Count(d => d.Contains("skipped")));
    }

    [Fact]
    public void PipelineStageResult_CanBeUsedInReporting()
    {
        // Arrange
        var results = new List<PipelineStageResult>
        {
            new PipelineStageResult
            {
                StageName = "Dependencies",
                StageEmoji = "📦",
                Success = true,
                Message = "Dependencies installed",
                Duration = TimeSpan.FromSeconds(8),
                Details = [ "Restored 45 NuGet packages" ]
            },
            new PipelineStageResult
            {
                StageName = "Compilation",
                StageEmoji = "⚙️",
                Success = true,
                Message = "Code compiled",
                Duration = TimeSpan.FromSeconds(12),
                Details = [ "Compiled 18 C# files", "Generated MyApp.dll" ]
            },
            new PipelineStageResult
            {
                StageName = "Testing",
                StageEmoji = "🧪",
                Success = false,
                Message = "Tests failed",
                Error = "2 unit tests failed",
                Duration = TimeSpan.FromSeconds(15),
                Details = [ "UserServiceTest.LoginTest: FAILED", "UserServiceTest.LogoutTest: FAILED" ]
            }
        };

        // Act - Simulate report generation
        var report = results.Select(r => new
        {
            Stage = r.StageName,
            Status = r.Success ? "✅" : "❌",
            Duration = r.Duration.TotalSeconds,
            HasError = r.Error != null,
            DetailCount = r.Details.Count
        }).ToList();

        // Assert
        Assert.Equal(3, report.Count);
        Assert.Equal("✅", report[0].Status);
        Assert.Equal("✅", report[1].Status);
        Assert.Equal("❌", report[2].Status);
        Assert.True(report[2].HasError);
        Assert.Equal(35, report.Sum(r => r.Duration));
    }
}


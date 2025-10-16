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
        result.StageName.Should().Be("Build");
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveStageEmojiProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { StageEmoji = "🔨" };

        // Assert
        result.StageEmoji.Should().Be("🔨");
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveSuccessProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Success = true };

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveMessageProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Message = "Stage completed successfully" };

        // Assert
        result.Message.Should().Be("Stage completed successfully");
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveErrorProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Error = "Compilation failed" };

        // Assert
        result.Error.Should().Be("Compilation failed");
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveDurationProperty()
    {
        // Arrange & Act
        var duration = TimeSpan.FromSeconds(15.5);
        var result = new PipelineStageResult { Duration = duration };

        // Assert
        result.Duration.Should().Be(duration);
    }

    [Fact]
    public void PipelineStageResult_ShouldHaveDetailsProperty()
    {
        // Arrange & Act
        var result = new PipelineStageResult();
        var details = new List<string> { "Detail 1", "Detail 2" };

        // Act
        result.Details = details;

        // Assert
        result.Details.Should().BeEquivalentTo(details);
    }

    [Fact]
    public void PipelineStageResult_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var result = new PipelineStageResult();

        // Assert
        result.StageName.Should().Be("");
        result.StageEmoji.Should().Be("");
        result.Success.Should().BeFalse();
        result.Message.Should().Be("");
        result.Error.Should().BeNull();
        result.Duration.Should().Be(TimeSpan.Zero);
        result.Details.Should().NotBeNull();
        result.Details.Should().BeEmpty();
    }

    [Fact]
    public void PipelineStageResult_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var result = new PipelineStageResult
        {
            StageName = "Unit Tests",
            StageEmoji = "🧪",
            Success = true,
            Message = "All tests passed",
            Error = null,
            Duration = TimeSpan.FromSeconds(25),
            Details = new List<string> { "Ran 150 tests", "Coverage: 95%" }
        };

        // Assert
        result.StageName.Should().Be("Unit Tests");
        result.StageEmoji.Should().Be("🧪");
        result.Success.Should().BeTrue();
        result.Message.Should().Be("All tests passed");
        result.Error.Should().BeNull();
        result.Duration.Should().Be(TimeSpan.FromSeconds(25));
        result.Details.Should().BeEquivalentTo(new[] { "Ran 150 tests", "Coverage: 95%" });
    }

    [Fact]
    public void PipelineStageResult_StageName_CanBeEmpty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { StageName = "" };

        // Assert
        result.StageName.Should().BeEmpty();
    }

    [Fact]
    public void PipelineStageResult_StageName_CanContainSpacesAndSpecialChars()
    {
        // Arrange & Act
        var result = new PipelineStageResult { StageName = "Build & Package" };

        // Assert
        result.StageName.Should().Be("Build & Package");
    }

    [Fact]
    public void PipelineStageResult_StageEmoji_CanBeEmpty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { StageEmoji = "" };

        // Assert
        result.StageEmoji.Should().BeEmpty();
    }

    [Fact]
    public void PipelineStageResult_StageEmoji_CanContainEmoji()
    {
        // Arrange & Act
        var result = new PipelineStageResult { StageEmoji = "🚀" };

        // Assert
        result.StageEmoji.Should().Be("🚀");
    }

    [Fact]
    public void PipelineStageResult_Success_CanBeTrue()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Success = true };

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void PipelineStageResult_Success_CanBeFalse()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Success = false };

        // Assert
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void PipelineStageResult_Message_CanBeEmpty()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Message = "" };

        // Assert
        result.Message.Should().BeEmpty();
    }

    [Fact]
    public void PipelineStageResult_Message_CanBeLong()
    {
        // Arrange
        var longMessage = new string('A', 1000);

        // Act
        var result = new PipelineStageResult { Message = longMessage };

        // Assert
        result.Message.Should().Be(longMessage);
        result.Message.Length.Should().Be(1000);
    }

    [Fact]
    public void PipelineStageResult_Error_CanBeNull()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Error = null };

        // Assert
        result.Error.Should().BeNull();
    }

    [Fact]
    public void PipelineStageResult_Error_CanContainErrorMessage()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Error = "NullReferenceException in Main.cs:42" };

        // Assert
        result.Error.Should().Be("NullReferenceException in Main.cs:42");
    }

    [Fact]
    public void PipelineStageResult_Duration_CanBeZero()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Duration = TimeSpan.Zero };

        // Assert
        result.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void PipelineStageResult_Duration_CanBeLarge()
    {
        // Arrange & Act
        var result = new PipelineStageResult { Duration = TimeSpan.FromHours(1) };

        // Assert
        result.Duration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void PipelineStageResult_CanAddDetails()
    {
        // Arrange
        var result = new PipelineStageResult();

        // Act
        result.Details.Add("Compiled 15 files");
        result.Details.Add("0 errors, 2 warnings");

        // Assert
        result.Details.Should().HaveCount(2);
        result.Details[0].Should().Be("Compiled 15 files");
        result.Details[1].Should().Be("0 errors, 2 warnings");
    }

    [Fact]
    public void PipelineStageResult_Details_CanBeEmpty()
    {
        // Arrange & Act
        var result = new PipelineStageResult();

        // Assert
        result.Details.Should().BeEmpty();
    }

    [Fact]
    public void PipelineStageResult_Details_CanContainMultipleItems()
    {
        // Arrange
        var result = new PipelineStageResult
        {
            Details = new List<string>
            {
                "Step 1: Initialize",
                "Step 2: Process",
                "Step 3: Finalize",
                "Step 4: Cleanup"
            }
        };

        // Assert
        result.Details.Should().HaveCount(4);
        result.Details.All(d => d.StartsWith("Step")).Should().BeTrue();
    }

    [Fact]
    public void PipelineStageResult_CanBeUsedInCollections()
    {
        // Arrange & Act
        var results = new List<PipelineStageResult>
        {
            new PipelineStageResult { StageName = "Init", Success = true, Duration = TimeSpan.FromSeconds(2) },
            new PipelineStageResult { StageName = "Build", Success = true, Duration = TimeSpan.FromSeconds(15) },
            new PipelineStageResult { StageName = "Test", Success = false, Duration = TimeSpan.FromSeconds(8) }
        };

        // Assert
        results.Should().HaveCount(3);
        results.Count(r => r.Success).Should().Be(2);
        results.Sum(r => r.Duration.TotalSeconds).Should().Be(25);
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
        successfulResults.Should().HaveCount(2);
        failedResults.Should().HaveCount(2);
        failedResults.Count(r => r.Error != null).Should().Be(1);
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
        ordered[0].StageName.Should().Be("Fast");
        ordered[1].StageName.Should().Be("Medium");
        ordered[2].StageName.Should().Be("Slow");
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
        var grouped = results.GroupBy(r => r.Success);

        // Assert
        grouped.Should().HaveCount(2);
        grouped.First(g => g.Key).Should().HaveCount(2); // Successful
        grouped.First(g => !g.Key).Should().HaveCount(2); // Failed
    }

    [Fact]
    public void PipelineStageResult_ShouldBeClass()
    {
        // Arrange & Act
        var result = new PipelineStageResult();

        // Assert
        result.Should().NotBeNull();
        result.GetType().IsClass.Should().BeTrue();
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
            Details = new List<string>
            {
                "Compiled 25 source files",
                "Generated assembly: MyApp.dll",
                "0 errors, 3 warnings"
            }
        };

        // Assert
        result.StageName.Should().Be("Build");
        result.StageEmoji.Should().Be("🔨");
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Build completed successfully");
        result.Error.Should().BeNull();
        result.Duration.Should().Be(TimeSpan.FromSeconds(45));
        result.Details.Should().HaveCount(3);
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
            Details = new List<string>
            {
                "TestUserLogin: FAILED",
                "TestPasswordReset: FAILED",
                "TestProfileUpdate: FAILED",
                "TestEmailValidation: INCONCLUSIVE"
            }
        };

        // Assert
        result.StageName.Should().Be("Unit Tests");
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("3 tests failed");
        result.Details.Should().HaveCount(4);
        result.Details.Count(d => d.Contains("FAILED")).Should().Be(3);
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
        result.StageName.Should().Be("Modified");
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Modified message");
        result.Duration.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void PipelineStageResult_WithEmptyDetails_ShouldHandleOperations()
    {
        // Arrange
        var result = new PipelineStageResult
        {
            StageName = "Simple Stage",
            Success = true,
            Details = new List<string>()
        };

        // Act & Assert
        result.Details.Should().BeEmpty();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void PipelineStageResult_CanCalculateDurationStatistics()
    {
        // Arrange
        var results = new List<PipelineStageResult>
        {
            new PipelineStageResult { StageName = "Fast", Duration = TimeSpan.FromSeconds(1) },
            new PipelineStageResult { StageName = "Medium", Duration = TimeSpan.FromSeconds(5) },
            new PipelineStageResult { StageName = "Slow", Duration = TimeSpan.FromSeconds(15) }
        };

        // Act
        var totalDuration = results.Sum(r => r.Duration.TotalSeconds);
        var averageDuration = results.Average(r => r.Duration.TotalSeconds);
        var maxDuration = results.Max(r => r.Duration.TotalSeconds);

        // Assert
        totalDuration.Should().Be(21);
        averageDuration.Should().Be(7);
        maxDuration.Should().Be(15);
    }

    [Fact]
    public void PipelineStageResult_CanIdentifyStagesWithErrors()
    {
        // Arrange
        var results = new List<PipelineStageResult>
        {
            new PipelineStageResult { StageName = "Init", Success = true },
            new PipelineStageResult { StageName = "Build", Success = false, Error = "Compile error" },
            new PipelineStageResult { StageName = "Test", Success = false, Message = "Skipped" },
            new PipelineStageResult { StageName = "Deploy", Success = true }
        };

        // Act
        var stagesWithErrors = results.Where(r => r.Error != null).ToList();
        var failedStages = results.Where(r => !r.Success).ToList();

        // Assert
        stagesWithErrors.Should().HaveCount(1);
        stagesWithErrors[0].StageName.Should().Be("Build");
        failedStages.Should().HaveCount(2);
    }

    [Fact]
    public void PipelineStageResult_CanBeFilteredByStageName()
    {
        // Arrange
        var results = new List<PipelineStageResult>
        {
            new PipelineStageResult { StageName = "Build", Success = true },
            new PipelineStageResult { StageName = "Test", Success = true },
            new PipelineStageResult { StageName = "Build", Success = false },
            new PipelineStageResult { StageName = "Deploy", Success = true }
        };

        // Act
        var buildStages = results.Where(r => r.StageName == "Build").ToList();

        // Assert
        buildStages.Should().HaveCount(2);
        buildStages.Count(r => r.Success).Should().Be(1);
        buildStages.Count(r => !r.Success).Should().Be(1);
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
            Details = new List<string>
            {
                "Starting database connection...",
                "Connection established in 2.3s",
                "Running test suite...",
                "Test 'UserAuthenticationTest' started",
                "Database query timeout at 00:00:30",
                "Test 'UserAuthenticationTest' failed",
                "Remaining tests skipped"
            }
        };

        // Assert
        result.StageName.Should().Be("Integration Tests");
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("TimeoutException");
        result.Duration.Should().Be(TimeSpan.FromSeconds(35));
        result.Details.Should().HaveCount(7);
        result.Details.Count(d => d.Contains("failed")).Should().Be(1);
        result.Details.Count(d => d.Contains("skipped")).Should().Be(1);
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
                Details = new List<string> { "Restored 45 NuGet packages" }
            },
            new PipelineStageResult
            {
                StageName = "Compilation",
                StageEmoji = "⚙️",
                Success = true,
                Message = "Code compiled",
                Duration = TimeSpan.FromSeconds(12),
                Details = new List<string> { "Compiled 18 C# files", "Generated MyApp.dll" }
            },
            new PipelineStageResult
            {
                StageName = "Testing",
                StageEmoji = "🧪",
                Success = false,
                Message = "Tests failed",
                Error = "2 unit tests failed",
                Duration = TimeSpan.FromSeconds(15),
                Details = new List<string> { "UserServiceTest.LoginTest: FAILED", "UserServiceTest.LogoutTest: FAILED" }
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
        report.Should().HaveCount(3);
        report[0].Status.Should().Be("✅");
        report[1].Status.Should().Be("✅");
        report[2].Status.Should().Be("❌");
        report[2].HasError.Should().BeTrue();
        report.Sum(r => r.Duration).Should().Be(35);
    }
}
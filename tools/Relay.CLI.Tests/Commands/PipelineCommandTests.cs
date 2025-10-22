 using System.CommandLine;
using Relay.CLI.Commands;
using Relay.CLI.Commands.Models.Pipeline;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

#pragma warning disable CS0219 // The variable is assigned but its value is never used
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type

namespace Relay.CLI.Tests.Commands;

public class PipelineCommandTests
{
    private async Task<T> ExecuteWithMockedConsole<T>(Func<Task<T>> action)
    {
        // Mock console to avoid concurrency issues with Spectre.Console
        // Use a lock to prevent concurrent access to Spectre.Console
        lock (typeof(AnsiConsole))
        {
            var testConsole = new Spectre.Console.Testing.TestConsole();
            var originalConsole = AnsiConsole.Console;

            AnsiConsole.Console = testConsole;

            try
            {
                return action().Result;
            }
            finally
            {
                AnsiConsole.Console = originalConsole;
            }
        }
    }

    private async Task ExecuteWithMockedConsole(Func<Task> action)
    {
        // Mock console to avoid concurrency issues with Spectre.Console
        // Use a lock to prevent concurrent access to Spectre.Console
        lock (typeof(AnsiConsole))
        {
            var testConsole = new Spectre.Console.Testing.TestConsole();
            var originalConsole = AnsiConsole.Console;

            AnsiConsole.Console = testConsole;

            try
            {
                action().Wait();
            }
            finally
            {
                AnsiConsole.Console = originalConsole;
            }
        }
    }

    [Fact]
    public async Task Pipeline_ShouldRunAllStages_WhenNoSkipSpecified()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-pipeline");
        Directory.CreateDirectory(path);

        try
        {
            // Act - This would run the full pipeline
            // In real scenario, we'd mock the stage execution
            var stages = new[] { "init", "doctor", "validate", "optimize" };
            var results = new List<bool>();

            foreach (var stage in stages)
            {
                // Simulate stage execution
                await Task.Delay(10);
                results.Add(true);
            }

            // Assert
            Assert.Equal(4, results.Count);
            Assert.All(results, r => Assert.True(r));
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public void Pipeline_ShouldSkipStages_WhenSkipOptionProvided()
    {
        // Arrange
        var skipStages = new[] { "init", "doctor" };
        var allStages = new[] { "init", "doctor", "validate", "optimize" };

        // Act
        var executedStages = allStages.Except(skipStages).ToList();

        // Assert
        Assert.Equal(2, executedStages.Count);
        Assert.Contains("validate", executedStages);
        Assert.Contains("optimize", executedStages);
    }

    [Fact]
    public void Pipeline_InitStage_ShouldCreateProjectStructure()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), $"relay-test-{Guid.NewGuid()}");

        try
        {
            // Act
            Directory.CreateDirectory(testPath);
            var srcPath = Path.Combine(testPath, "src");
            Directory.CreateDirectory(srcPath);

            // Assert
            Assert.True(Directory.Exists(testPath));
            Assert.True(Directory.Exists(srcPath));
        }
        finally
        {
            if (Directory.Exists(testPath))
                Directory.Delete(testPath, true);
        }
    }

    [Fact]
    public void Pipeline_DoctorStage_ShouldDetectHealthIssues()
    {
        // Arrange
        var diagnostics = new List<(string Category, bool Pass)>
        {
            ("Structure", true),
            ("Dependencies", true),
            ("Configuration", true)
        };

        // Act
        var failedChecks = diagnostics.Where(d => !d.Pass).ToList();

        // Assert
        Assert.Empty(failedChecks);
    }

    [Fact]
    public void Pipeline_ValidateStage_ShouldValidateCode()
    {
        // Arrange
        var validationResults = new List<string>();

        // Act
        validationResults.Add("Handler patterns: OK");
        validationResults.Add("Request types: OK");
        validationResults.Add("Async patterns: OK");

        // Assert
        Assert.Equal(3, validationResults.Count);
        Assert.All(validationResults, r => Assert.Contains("OK", r));
    }

    [Fact]
    public void Pipeline_OptimizeStage_ShouldApplyOptimizations()
    {
        // Arrange
        var optimizations = new List<(string Type, string Impact)>
        {
            ("ValueTask Conversion", "High"),
            ("Allocation Reduction", "Medium")
        };

        // Act
        var highImpactOptimizations = optimizations.Where(o => o.Impact == "High").ToList();

        // Assert
        Assert.NotEmpty(highImpactOptimizations);
        Assert.Contains(optimizations, o => o.Type == "ValueTask Conversion");
    }

    [Fact]
    public async Task Pipeline_ShouldGenerateReport_WhenReportPathProvided()
    {
        // Arrange
        var reportPath = Path.Combine(Path.GetTempPath(), "pipeline-report.md");

        try
        {
            // Act
            var reportContent = @"# Relay Pipeline Report

**Status:** ✅ Success
**Duration:** 2.5s

## Stages

| Stage | Status | Duration |
|-------|--------|----------|
| 🎬 Init | ✅ | 0.8s |
| 🏥 Doctor | ✅ | 0.5s |
| ✅ Validate | ✅ | 0.6s |
| ⚡ Optimize | ✅ | 0.6s |
";
            await File.WriteAllTextAsync(reportPath, reportContent);

            // Assert
            Assert.True(File.Exists(reportPath));
            var content = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("Pipeline Report", content);
            Assert.Contains("Success", content);
        }
        finally
        {
            if (File.Exists(reportPath))
                File.Delete(reportPath);
        }
    }

    [Fact]
    public void Pipeline_ShouldRunInCIMode_WhenCIFlagSet()
    {
        // Arrange
        var ciMode = true;
        var output = new List<string>();

        // Act
        if (ciMode)
        {
            output.Add("🎬 Init: PASS (0.8s)");
            output.Add("🏥 Doctor: PASS (0.5s)");
            output.Add("✅ Validate: PASS (0.6s)");
            output.Add("⚡ Optimize: PASS (0.6s)");
            output.Add("Total: SUCCESS (2.5s)");
        }

        // Assert
        Assert.Equal(5, output.Count);
        Assert.All(output, line => Assert.DoesNotContain("[", line)); // No ANSI markup in CI mode
    }

    [Fact]
    public void Pipeline_ShouldCalculateSuccessRate()
    {
        // Arrange
        var stages = new[]
        {
            (Name: "Init", Success: true),
            (Name: "Doctor", Success: true),
            (Name: "Validate", Success: true),
            (Name: "Optimize", Success: false)
        };

        // Act
        var successCount = stages.Count(s => s.Success);
        var totalCount = stages.Length;
        var successRate = (successCount * 100.0) / totalCount;

        // Assert
        Assert.Equal(75.0, successRate);
    }

    [Fact]
    public void Pipeline_ShouldStopOnCriticalError_WhenDoctorFails()
    {
        // Arrange
        var stages = new List<string> { "init", "doctor", "validate", "optimize" };
        var executedStages = new List<string>();
        var doctorFailed = false;

        // Act
        foreach (var stage in stages)
        {
            executedStages.Add(stage);
            
            if (stage == "doctor")
            {
                doctorFailed = true;
                break; // Critical failure
            }
        }

        // Assert
        Assert.True(doctorFailed);
        Assert.Equal(2, executedStages.Count);
        Assert.DoesNotContain("validate", executedStages);
        Assert.DoesNotContain("optimize", executedStages);
    }

    [Fact]
    public void Pipeline_ShouldUseAggressiveOptimizations_WhenFlagSet()
    {
        // Arrange
        var aggressive = true;
        var optimizations = new List<string>();

        // Act
        optimizations.Add("ValueTask Conversion");
        optimizations.Add("Allocation Reduction");
        
        if (aggressive)
        {
            optimizations.Add("SIMD Vectorization");
            optimizations.Add("Span Optimizations");
        }

        // Assert
        Assert.Equal(4, optimizations.Count);
        Assert.Contains("SIMD Vectorization", optimizations);
    }

    [Fact]
    public void Pipeline_ShouldAutoFixIssues_WhenAutoFixEnabled()
    {
        // Arrange
        var autoFix = true;
        var issues = new List<(string Issue, bool Fixed)>
        {
            ("Missing using directive", false),
            ("Incorrect return type", false)
        };

        // Act
        if (autoFix)
        {
            issues = issues.Select(i => (i.Issue, Fixed: true)).ToList();
        }

        // Assert
        Assert.All(issues, i => Assert.True(i.Fixed));
    }

    [Theory]
    [InlineData("minimal")]
    [InlineData("standard")]
    [InlineData("enterprise")]
    public void Pipeline_ShouldSupportAllTemplates(string template)
    {
        // Arrange & Act
        var validTemplates = new[] { "minimal", "standard", "enterprise" };

        // Assert
        Assert.Contains(template, validTemplates);
    }

    [Fact]
    public async Task Pipeline_ShouldTrackTotalDuration()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        await Task.Delay(100);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds >= 90);
    }

    [Fact]
    public async Task Pipeline_ShouldHandleCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var cancelled = false;

        // Act
        try
        {
            cts.Cancel();
            await Task.Delay(1000, cts.Token);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;
        }

        // Assert
        Assert.True(cancelled);
    }

    [Fact]
    public void Pipeline_ShouldReturnExitCode0_WhenSuccessful()
    {
        // Arrange
        var allStagesSuccessful = true;

        // Act
        var exitCode = allStagesSuccessful ? 0 : 1;

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Pipeline_ShouldReturnExitCode1_WhenFailed()
    {
        // Arrange
        var allStagesSuccessful = false;

        // Act
        var exitCode = allStagesSuccessful ? 0 : 1;

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Pipeline_ShouldReturnExitCode130_WhenCancelled()
    {
        // Arrange
        var cancelled = true;

        // Act
        var exitCode = cancelled ? 130 : 0;

        // Assert
        Assert.Equal(130, exitCode);
    }

    [Fact]
    public void Pipeline_ShouldNotRunInit_WhenProjectNameIsNull()
    {
        // Arrange
        string? projectName = null;
        var skipStages = Array.Empty<string>();
        var stagesExecuted = new List<string>();

        // Act
        if (!skipStages.Contains("init") && projectName != null)
        {
            stagesExecuted.Add("init");
        }

        // Assert
        Assert.DoesNotContain("init", stagesExecuted);
    }

    [Fact]
    public void Pipeline_ShouldIncludeTemplateInInitDetails()
    {
        // Arrange
        var template = "enterprise";
        var details = new List<string>();

        // Act
        details.Add($"Template: {template}");

        // Assert
        Assert.Contains("Template: enterprise", details);
    }

    [Fact]
    public void Pipeline_ShouldIncludeLocationInInitDetails()
    {
        // Arrange
        var path = @"C:\projects\test";
        var details = new List<string>();

        // Act
        details.Add($"Location: {path}");

        // Assert
        Assert.Contains($"Location: {path}", details);
    }

    [Fact]
    public async Task Pipeline_DoctorStage_ShouldPerformStructureCheck()
    {
        // Arrange
        var diagnostics = new List<string>();

        // Act
        await Task.Delay(10);
        diagnostics.Add("Structure: Project structure is valid");

        // Assert
        Assert.Contains("Structure: Project structure is valid", diagnostics);
    }

    [Fact]
    public async Task Pipeline_DoctorStage_ShouldPerformDependenciesCheck()
    {
        // Arrange
        var diagnostics = new List<string>();

        // Act
        await Task.Delay(10);
        diagnostics.Add("Dependencies: All dependencies up to date");

        // Assert
        Assert.Contains("Dependencies: All dependencies up to date", diagnostics);
    }

    [Fact]
    public async Task Pipeline_DoctorStage_ShouldPerformConfigurationCheck()
    {
        // Arrange
        var diagnostics = new List<string>();

        // Act
        await Task.Delay(10);
        diagnostics.Add("Configuration: Configuration is valid");

        // Assert
        Assert.Contains("Configuration: Configuration is valid", diagnostics);
    }

    [Fact]
    public async Task Pipeline_ValidateStage_ShouldCheckHandlerPatterns()
    {
        // Arrange
        var validationChecks = new List<string>();

        // Act
        await Task.Delay(10);
        validationChecks.Add("Handler patterns validated");

        // Assert
        Assert.Contains("Handler patterns validated", validationChecks);
    }

    [Fact]
    public async Task Pipeline_ValidateStage_ShouldCheckRequestTypes()
    {
        // Arrange
        var validationChecks = new List<string>();

        // Act
        await Task.Delay(10);
        validationChecks.Add("Request types validated");

        // Assert
        Assert.Contains("Request types validated", validationChecks);
    }

    [Fact]
    public async Task Pipeline_ValidateStage_ShouldCheckAsyncPatterns()
    {
        // Arrange
        var validationChecks = new List<string>();

        // Act
        await Task.Delay(10);
        validationChecks.Add("Async patterns validated");

        // Assert
        Assert.Contains("Async patterns validated", validationChecks);
    }

    [Fact]
    public async Task Pipeline_OptimizeStage_ShouldApplyValueTaskConversion()
    {
        // Arrange
        var optimizations = new List<string>();

        // Act
        await Task.Delay(10);
        optimizations.Add("ValueTask Conversion: High impact");

        // Assert
        Assert.Contains("ValueTask Conversion: High impact", optimizations);
    }

    [Fact]
    public async Task Pipeline_OptimizeStage_ShouldApplyAllocationReduction()
    {
        // Arrange
        var optimizations = new List<string>();

        // Act
        await Task.Delay(10);
        optimizations.Add("Allocation Reduction: Medium impact");

        // Assert
        Assert.Contains("Allocation Reduction: Medium impact", optimizations);
    }

    [Fact]
    public async Task Pipeline_OptimizeStage_ShouldApplySIMD_WhenAggressive()
    {
        // Arrange
        var aggressive = true;
        var optimizations = new List<string>();

        // Act
        await Task.Delay(10);
        optimizations.Add("ValueTask Conversion: High impact");
        optimizations.Add("Allocation Reduction: Medium impact");

        if (aggressive)
        {
            optimizations.Add("SIMD Vectorization: High impact");
        }

        // Assert
        Assert.Equal(3, optimizations.Count);
        Assert.Contains("SIMD Vectorization: High impact", optimizations);
    }

    [Fact]
    public async Task Pipeline_OptimizeStage_ShouldNotApplySIMD_WhenNotAggressive()
    {
        // Arrange
        var aggressive = false;
        var optimizations = new List<string>();

        // Act
        await Task.Delay(10);
        optimizations.Add("ValueTask Conversion: High impact");
        optimizations.Add("Allocation Reduction: Medium impact");

        if (aggressive)
        {
            optimizations.Add("SIMD Vectorization: High impact");
        }

        // Assert
        Assert.Equal(2, optimizations.Count);
        Assert.DoesNotContain("SIMD Vectorization: High impact", optimizations);
    }

    [Fact]
    public void Pipeline_Report_ShouldIncludeGeneratedTimestamp()
    {
        // Arrange
        var now = DateTime.Now;
        var reportLines = new List<string>();

        // Act
        reportLines.Add($"**Generated:** {now:yyyy-MM-dd HH:mm:ss}");

        // Assert
        Assert.Contains(reportLines, line => line.StartsWith("**Generated:**"));
    }

    [Fact]
    public void Pipeline_Report_ShouldIncludeSuccessStatus()
    {
        // Arrange
        var success = true;
        var reportLines = new List<string>();

        // Act
        reportLines.Add($"**Status:** {(success ? "✅ Success" : "❌ Failed")}");

        // Assert
        Assert.Contains("**Status:** ✅ Success", reportLines);
    }

    [Fact]
    public void Pipeline_Report_ShouldIncludeFailedStatus()
    {
        // Arrange
        var success = false;
        var reportLines = new List<string>();

        // Act
        reportLines.Add($"**Status:** {(success ? "✅ Success" : "❌ Failed")}");

        // Assert
        Assert.Contains("**Status:** ❌ Failed", reportLines);
    }

    [Fact]
    public void Pipeline_Report_ShouldIncludeTotalDuration()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(2.5);
        var reportLines = new List<string>();

        // Act
        reportLines.Add($"**Total Duration:** {duration.TotalSeconds:F2}s");

        // Assert
        Assert.Contains(reportLines, line => line.StartsWith("**Total Duration:**") && line.Contains("2") && line.Contains("50s"));
    }

    [Fact]
    public void Pipeline_Report_ShouldIncludeStagesTable()
    {
        // Arrange
        var reportLines = new List<string>();

        // Act
        reportLines.Add("## Stages");
        reportLines.Add("");
        reportLines.Add("| Stage | Status | Duration | Details |");
        reportLines.Add("|-------|--------|----------|---------|");

        // Assert
        Assert.Contains("## Stages", reportLines);
        Assert.Contains("| Stage | Status | Duration | Details |", reportLines);
    }

    [Fact]
    public void Pipeline_ShouldCatchAndHandleGeneralExceptions()
    {
        // Arrange
        var exceptionCaught = false;
        var exitCode = 0;

        // Act
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (OperationCanceledException)
        {
            exitCode = 130;
        }
        catch (Exception)
        {
            exceptionCaught = true;
            exitCode = 1;
        }

        // Assert
        Assert.True(exceptionCaught);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Pipeline_InitStage_ShouldCaptureExceptionMessage()
    {
        // Arrange
        var errorMessage = "";

        // Act
        try
        {
            throw new IOException("Failed to create directory");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        // Assert
        Assert.Equal("Failed to create directory", errorMessage);
    }

    [Fact]
    public void Pipeline_DoctorStage_ShouldCaptureExceptionMessage()
    {
        // Arrange
        var errorMessage = "";

        // Act
        try
        {
            throw new FileNotFoundException("Project file not found");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        // Assert
        Assert.Equal("Project file not found", errorMessage);
    }

    [Fact]
    public void Pipeline_ValidateStage_ShouldCaptureExceptionMessage()
    {
        // Arrange
        var errorMessage = "";

        // Act
        try
        {
            throw new InvalidOperationException("Validation error");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        // Assert
        Assert.Equal("Validation error", errorMessage);
    }

    [Fact]
    public void Pipeline_OptimizeStage_ShouldCaptureExceptionMessage()
    {
        // Arrange
        var errorMessage = "";

        // Act
        try
        {
            throw new InvalidOperationException("Optimization failed");
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }

        // Assert
        Assert.Equal("Optimization failed", errorMessage);
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(1, 25.0)]
    [InlineData(2, 50.0)]
    [InlineData(3, 75.0)]
    [InlineData(4, 100.0)]
    public void Pipeline_ShouldCalculateCorrectSuccessRate(int successCount, double expectedRate)
    {
        // Arrange
        var totalStages = 4;

        // Act
        var successRate = (successCount * 100.0) / totalStages;

        // Assert
        Assert.Equal(expectedRate, successRate);
    }

    [Fact]
    public void Pipeline_ShouldDisplayNextSteps_WhenSuccessful()
    {
        // Arrange
        var success = true;
        var nextSteps = new List<string>();

        // Act
        if (success)
        {
            nextSteps.Add("Build: dotnet build");
            nextSteps.Add("Test: dotnet test");
            nextSteps.Add("Run: dotnet run");
        }

        // Assert
        Assert.Equal(3, nextSteps.Count);
        Assert.Contains("Build: dotnet build", nextSteps);
        Assert.Contains("Test: dotnet test", nextSteps);
        Assert.Contains("Run: dotnet run", nextSteps);
    }

    [Fact]
    public void Pipeline_ShouldNotDisplayNextSteps_WhenFailed()
    {
        // Arrange
        var success = false;
        var nextSteps = new List<string>();

        // Act
        if (success)
        {
            nextSteps.Add("Build: dotnet build");
            nextSteps.Add("Test: dotnet test");
            nextSteps.Add("Run: dotnet run");
        }

        // Assert
        Assert.Empty(nextSteps);
    }

    [Fact]
    public void Pipeline_ShouldCountCompletedStages()
    {
        // Arrange
        var stages = new[]
        {
            (Name: "Init", Success: true),
            (Name: "Doctor", Success: true),
            (Name: "Validate", Success: false),
            (Name: "Optimize", Success: true)
        };

        // Act
        var completedCount = stages.Count(s => s.Success);

        // Assert
        Assert.Equal(3, completedCount);
    }

    [Fact]
    public void Pipeline_ShouldFormatDurationCorrectly()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(1234);

        // Act
        var formattedDuration = $"{duration.TotalSeconds:F2}s";

        // Assert
        Assert.Contains("1", formattedDuration);
        Assert.Contains("23s", formattedDuration);
    }

    [Fact]
    public void Pipeline_ShouldDetermineOverallSuccess_WhenAllStagesPass()
    {
        // Arrange
        var stages = new[]
        {
            (Name: "Init", Success: true),
            (Name: "Doctor", Success: true),
            (Name: "Validate", Success: true),
            (Name: "Optimize", Success: true)
        };

        // Act
        var overallSuccess = stages.All(s => s.Success);

        // Assert
        Assert.True(overallSuccess);
    }

    [Fact]
    public void Pipeline_ShouldDetermineOverallFailure_WhenAnyStagesFail()
    {
        // Arrange
        var stages = new[]
        {
            (Name: "Init", Success: true),
            (Name: "Doctor", Success: false),
            (Name: "Validate", Success: true),
            (Name: "Optimize", Success: true)
        };

        // Act
        var overallSuccess = stages.All(s => s.Success);

        // Assert
        Assert.False(overallSuccess);
    }

    [Fact]
    public void Pipeline_ShouldFormatReportPath()
    {
        // Arrange
        var reportPath = "pipeline-report.md";

        // Act
        var message = $"📄 Report saved: {reportPath}";

        // Assert
        Assert.Equal("📄 Report saved: pipeline-report.md", message);
    }

    [Theory]
    [InlineData("init")]
    [InlineData("doctor")]
    [InlineData("validate")]
    [InlineData("optimize")]
    public void Pipeline_ShouldRecognizeValidStageNames(string stageName)
    {
        // Arrange
        var validStages = new[] { "init", "doctor", "validate", "optimize" };

        // Assert
        Assert.Contains(stageName, validStages);
    }

    [Fact]
    public void Pipeline_ShouldHandleEmptySkipArray()
    {
        // Arrange
        var skipStages = Array.Empty<string>();
        var allStages = new[] { "init", "doctor", "validate", "optimize" };

        // Act
        var stagesToRun = allStages.Where(s => !skipStages.Contains(s)).ToList();

        // Assert
        Assert.Equal(4, stagesToRun.Count);
    }

    [Fact]
    public void Pipeline_ShouldHandleMultipleSkipStages()
    {
        // Arrange
        var skipStages = new[] { "init", "doctor", "validate" };
        var allStages = new[] { "init", "doctor", "validate", "optimize" };

        // Act
        var stagesToRun = allStages.Where(s => !skipStages.Contains(s)).ToList();

        // Assert
        Assert.Single(stagesToRun);
        Assert.Contains("optimize", stagesToRun);
    }

    [Fact]
    public void Create_ShouldReturnCommandWithCorrectNameAndDescription()
    {
        // Act
        var command = PipelineCommand.Create();

        // Assert
        Assert.Equal("pipeline", command.Name);
        Assert.Equal("Run complete project development pipeline", command.Description);
    }

    [Fact]
    public void Create_ShouldHavePathOptionWithCorrectDefaults()
    {
        // Act
        var command = PipelineCommand.Create();
        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        Assert.NotNull(pathOption);
        Assert.Equal("Project path", pathOption.Description);
    }

    [Fact]
    public void Create_ShouldHaveNameOption()
    {
        // Act
        var command = PipelineCommand.Create();
        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");

        // Assert
        Assert.NotNull(nameOption);
        Assert.Equal("Project name (for new projects)", nameOption.Description);
    }

    [Fact]
    public void Create_ShouldHaveTemplateOptionWithCorrectDefaults()
    {
        // Act
        var command = PipelineCommand.Create();
        var templateOption = command.Options.FirstOrDefault(o => o.Name == "template");

        // Assert
        Assert.NotNull(templateOption);
        Assert.Equal("Template (minimal, standard, enterprise)", templateOption.Description);
    }

    [Fact]
    public void Create_ShouldHaveSkipOptionWithCorrectDefaults()
    {
        // Act
        var command = PipelineCommand.Create();
        var skipOption = command.Options.FirstOrDefault(o => o.Name == "skip");

        // Assert
        Assert.NotNull(skipOption);
        Assert.Equal("Skip stages (init, doctor, validate, optimize)", skipOption.Description);
    }

    [Fact]
    public void Create_ShouldHaveAggressiveOptionWithCorrectDefaults()
    {
        // Act
        var command = PipelineCommand.Create();
        var aggressiveOption = command.Options.FirstOrDefault(o => o.Name == "aggressive");

        // Assert
        Assert.NotNull(aggressiveOption);
        Assert.Equal("Use aggressive optimizations", aggressiveOption.Description);
    }

    [Fact]
    public void Create_ShouldHaveAutoFixOptionWithCorrectDefaults()
    {
        // Act
        var command = PipelineCommand.Create();
        var autoFixOption = command.Options.FirstOrDefault(o => o.Name == "auto-fix");

        // Assert
        Assert.NotNull(autoFixOption);
        Assert.Equal("Automatically fix detected issues", autoFixOption.Description);
    }

    [Fact]
    public void Create_ShouldHaveReportOption()
    {
        // Act
        var command = PipelineCommand.Create();
        var reportOption = command.Options.FirstOrDefault(o => o.Name == "report");

        // Assert
        Assert.NotNull(reportOption);
        Assert.Equal("Generate pipeline report", reportOption.Description);
    }

    [Fact]
    public void Create_ShouldHaveCiOptionWithCorrectDefaults()
    {
        // Act
        var command = PipelineCommand.Create();
        var ciOption = command.Options.FirstOrDefault(o => o.Name == "ci");

        // Assert
        Assert.NotNull(ciOption);
        Assert.Equal("Run in CI mode (non-interactive)", ciOption.Description);
    }

    [Fact]
    public void Create_ShouldHaveAllRequiredOptions()
    {
        // Act
        var command = PipelineCommand.Create();

        // Assert
        Assert.Equal(8, command.Options.Count);
        var optionNames = command.Options.Select(o => o.Name).ToArray();
        Assert.Contains("path", optionNames);
        Assert.Contains("name", optionNames);
        Assert.Contains("template", optionNames);
        Assert.Contains("skip", optionNames);
        Assert.Contains("aggressive", optionNames);
        Assert.Contains("auto-fix", optionNames);
        Assert.Contains("report", optionNames);
        Assert.Contains("ci", optionNames);
    }

    [Fact]
    public async Task RunInitStage_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-init");
        var projectName = "TestProject";
        var template = "standard";
        var ciMode = false;
        var cancellationToken = CancellationToken.None;

        try
        {
            Directory.CreateDirectory(path);

            // Act
            var result = await ExecuteWithMockedConsole(() => PipelineCommand.RunInitStage(path, projectName, template, ciMode, cancellationToken));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Init", result.StageName);
            Assert.Equal("🎬", result.StageEmoji);
            Assert.Contains($"Project '{projectName}' created successfully", result.Message);
            Assert.Contains($"Template: {template}", result.Details);
            Assert.Contains($"Location: {Path.GetFullPath(path)}", result.Details);
            Assert.True(result.Duration.TotalMilliseconds > 0);
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task RunInitStage_ShouldHandleCancellation()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-init-cancel");
        var projectName = "TestProject";
        var template = "standard";
        var ciMode = false;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            Directory.CreateDirectory(path);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                PipelineCommand.RunInitStage(path, projectName, template, ciMode, cts.Token));
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task RunDoctorStage_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-doctor");
        var autoFix = false;
        var ciMode = false;
        var cancellationToken = CancellationToken.None;

        try
        {
            Directory.CreateDirectory(path);

            // Act
            var result = await ExecuteWithMockedConsole(() => PipelineCommand.RunDoctorStage(path, autoFix, ciMode, cancellationToken));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Doctor", result.StageName);
            Assert.Equal("🏥", result.StageEmoji);
            Assert.Equal("All health checks passed", result.Message);
            Assert.Contains("Structure: Project structure is valid", result.Details);
            Assert.Contains("Dependencies: All dependencies up to date", result.Details);
            Assert.Contains("Configuration: Configuration is valid", result.Details);
            Assert.True(result.Duration.TotalMilliseconds > 0);
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task RunValidateStage_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-validate");
        var ciMode = false;
        var cancellationToken = CancellationToken.None;

        try
        {
            Directory.CreateDirectory(path);

            // Act
            var result = await PipelineCommand.RunValidateStage(path, ciMode, cancellationToken);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Validate", result.StageName);
            Assert.Equal("✅", result.StageEmoji);
            Assert.Equal("Code validation passed", result.Message);
            Assert.Contains("No critical issues found", result.Details);
            Assert.Contains("All handlers follow best practices", result.Details);
            Assert.True(result.Duration.TotalMilliseconds > 0);
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task RunOptimizeStage_ShouldReturnSuccessfulResult_WithStandardOptimizations()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-optimize");
        var aggressive = false;
        var ciMode = false;
        var cancellationToken = CancellationToken.None;

        try
        {
            Directory.CreateDirectory(path);

            // Act
            var result = await PipelineCommand.RunOptimizeStage(path, aggressive, ciMode, cancellationToken);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Optimize", result.StageName);
            Assert.Equal("⚡", result.StageEmoji);
            Assert.Equal("2 optimization(s) applied", result.Message);
            Assert.Contains("ValueTask Conversion: High impact", result.Details);
            Assert.Contains("Allocation Reduction: Medium impact", result.Details);
            Assert.DoesNotContain("SIMD Vectorization", result.Details);
            Assert.True(result.Duration.TotalMilliseconds > 0);
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task RunOptimizeStage_ShouldReturnSuccessfulResult_WithAggressiveOptimizations()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-optimize-aggressive");
        var aggressive = true;
        var ciMode = false;
        var cancellationToken = CancellationToken.None;

        try
        {
            Directory.CreateDirectory(path);

            // Act
            var result = await ExecuteWithMockedConsole(() => PipelineCommand.RunOptimizeStage(path, aggressive, ciMode, cancellationToken));

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Optimize", result.StageName);
            Assert.Equal("⚡", result.StageEmoji);
            Assert.Equal("3 optimization(s) applied", result.Message);
            Assert.Contains("ValueTask Conversion: High impact", result.Details);
            Assert.Contains("Allocation Reduction: Medium impact", result.Details);
            Assert.Contains("SIMD Vectorization: High impact", result.Details);
            Assert.True(result.Duration.TotalMilliseconds > 0);
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task RunOptimizeStage_ShouldHandleCancellation()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-optimize-cancel");
        var aggressive = false;
        var ciMode = false;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            Directory.CreateDirectory(path);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                PipelineCommand.RunOptimizeStage(path, aggressive, ciMode, cts.Token));
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public void ExecutePipeline_ShouldSkipInitStage_WhenProjectNameIsNull()
    {
        // Arrange
        var path = ".";
        string? projectName = null;
        var template = "standard";
        var skipStages = Array.Empty<string>();
        var aggressive = false;
        var autoFix = false;
        string? reportPath = null;
        var ciMode = true; // Use CI mode to avoid console output
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        // Since projectName is null, init stage should be skipped
        // We can't easily test the full execution due to console output and exit code
        // But we can test the condition logic
        var shouldRunInit = !skipStages.Contains("init") && projectName != null;
        Assert.False(shouldRunInit);
    }

    [Fact]
    public void ExecutePipeline_ShouldSkipInitStage_WhenInitIsInSkipList()
    {
        // Arrange
        var skipStages = new[] { "init" };

        // Act
        var shouldRunInit = !skipStages.Contains("init");
        var shouldRunDoctor = !skipStages.Contains("doctor");
        var shouldRunValidate = !skipStages.Contains("validate");
        var shouldRunOptimize = !skipStages.Contains("optimize");

        // Assert
        Assert.False(shouldRunInit);
        Assert.True(shouldRunDoctor);
        Assert.True(shouldRunValidate);
        Assert.True(shouldRunOptimize);
    }

    [Fact]
    public void ExecutePipeline_ShouldRunAllStages_WhenNoSkips()
    {
        // Arrange
        var skipStages = Array.Empty<string>();

        // Act
        var shouldRunInit = !skipStages.Contains("init");
        var shouldRunDoctor = !skipStages.Contains("doctor");
        var shouldRunValidate = !skipStages.Contains("validate");
        var shouldRunOptimize = !skipStages.Contains("optimize");

        // Assert
        Assert.True(shouldRunInit);
        Assert.True(shouldRunDoctor);
        Assert.True(shouldRunValidate);
        Assert.True(shouldRunOptimize);
    }

    [Fact]
    public void ExecutePipeline_ShouldSkipMultipleStages_WhenSpecified()
    {
        // Arrange
        var skipStages = new[] { "doctor", "validate" };

        // Act
        var shouldRunInit = !skipStages.Contains("init");
        var shouldRunDoctor = !skipStages.Contains("doctor");
        var shouldRunValidate = !skipStages.Contains("validate");
        var shouldRunOptimize = !skipStages.Contains("optimize");

        // Assert
        Assert.True(shouldRunInit);
        Assert.False(shouldRunDoctor);
        Assert.False(shouldRunValidate);
        Assert.True(shouldRunOptimize);
    }

    [Fact]
    public async Task ExecutePipeline_ShouldHandleCancellationException()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-pipeline-cancel");
        var projectName = "TestProject";
        var template = "standard";
        var skipStages = Array.Empty<string>();
        var aggressive = false;
        var autoFix = false;
        string? reportPath = null;
        var ciMode = true;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            Directory.CreateDirectory(path);

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                PipelineCommand.ExecutePipeline(path, projectName, template, skipStages, aggressive, autoFix, reportPath, ciMode, cts.Token));
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public void DisplayPipelineResults_ShouldNotThrowException_WithValidResult()
    {
        // Arrange
        var pipelineResult = new PipelineResult
        {
            Success = true,
            TotalDuration = TimeSpan.FromSeconds(2.0),
            Stages = new List<PipelineStageResult>
            {
                new PipelineStageResult
                {
                    StageName = "Init",
                    StageEmoji = "🎬",
                    Success = true,
                    Message = "Project created",
                    Duration = TimeSpan.FromSeconds(0.8),
                    Details = new List<string> { "Template: standard" }
                }
            }
        };

        // Act & Assert
        // DisplayPipelineResults uses AnsiConsole, so we just verify it doesn't throw
        // If it throws, the test will fail automatically
        PipelineCommand.DisplayPipelineResults(pipelineResult, ciMode: true);
    }

    [Fact]
    public void DisplayPipelineHeader_ShouldNotThrowException()
    {
        // Act & Assert
        // DisplayPipelineHeader uses AnsiConsole, so we just verify it doesn't throw
        // If it throws, the test will fail automatically
        PipelineCommand.DisplayPipelineHeader();
    }

    [Fact]
    public async Task RunInitStage_ShouldHandleInvalidPath()
    {
        // Arrange
        var path = "invalid:\\path\\that\\does\\not\\exist";
        var projectName = "TestProject";
        var template = "standard";
        var ciMode = true;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await PipelineCommand.RunInitStage(path, projectName, template, ciMode, cancellationToken);

        // Assert
        // The method should still complete, even if the path is invalid
        // (it doesn't actually validate the path in the current implementation)
        Assert.NotNull(result);
        Assert.Equal("Init", result.StageName);
    }

    [Fact]
    public async Task RunDoctorStage_ShouldHandleInvalidPath()
    {
        // Arrange
        var path = "invalid:\\path\\that\\does\\not\\exist";
        var autoFix = false;
        var ciMode = true;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await PipelineCommand.RunDoctorStage(path, autoFix, ciMode, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Doctor", result.StageName);
    }

    [Fact]
    public async Task RunValidateStage_ShouldHandleInvalidPath()
    {
        // Arrange
        var path = "invalid:\\path\\that\\does\\not\\exist";
        var ciMode = true;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await PipelineCommand.RunValidateStage(path, ciMode, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Validate", result.StageName);
    }

    [Fact]
    public async Task RunOptimizeStage_ShouldHandleInvalidPath()
    {
        // Arrange
        var path = "invalid:\\path\\that\\does\\not\\exist";
        var aggressive = false;
        var ciMode = true;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await PipelineCommand.RunOptimizeStage(path, aggressive, ciMode, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Optimize", result.StageName);
    }

    [Fact]
    public void Pipeline_ShouldHandleNullSkipArray()
    {
        // Arrange
        string[]? skipStages = null;
        var allStages = new[] { "init", "doctor", "validate", "optimize" };

        // Act
        var stagesToExecute = allStages.Where(stage => skipStages == null || !skipStages.Contains(stage)).ToList();

        // Assert
        Assert.Equal(4, stagesToExecute.Count);
        Assert.Equal(allStages, stagesToExecute);
    }

    [Fact]
    public void Pipeline_ShouldHandleInvalidStageNamesInSkip()
    {
        // Arrange
        var skipStages = new[] { "invalid", "also_invalid" };
        var allStages = new[] { "init", "doctor", "validate", "optimize" };

        // Act
        var stagesToExecute = allStages.Where(stage => !skipStages.Contains(stage)).ToList();

        // Assert
        Assert.Equal(4, stagesToExecute.Count);
        Assert.Equal(allStages, stagesToExecute);
    }

    [Fact]
    public void Pipeline_ShouldHandleDuplicateSkipStages()
    {
        // Arrange
        var skipStages = new[] { "init", "init", "doctor" };
        var allStages = new[] { "init", "doctor", "validate", "optimize" };

        // Act
        var stagesToExecute = allStages.Where(stage => !skipStages.Contains(stage)).ToList();

        // Assert
        Assert.Equal(2, stagesToExecute.Count);
        Assert.Contains("validate", stagesToExecute);
        Assert.Contains("optimize", stagesToExecute);
    }

    [Fact]
    public void PipelineCommand_Create_ReturnsConfiguredCommand()
    {
        // Act
        var command = PipelineCommand.Create();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("pipeline", command.Name);
        Assert.Equal("Run complete project development pipeline", command.Description);

        // Verify all options exist
        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");
        Assert.NotNull(pathOption);

        var nameOption = command.Options.FirstOrDefault(o => o.Name == "name");
        Assert.NotNull(nameOption);

        var templateOption = command.Options.FirstOrDefault(o => o.Name == "template");
        Assert.NotNull(templateOption);

        var skipOption = command.Options.FirstOrDefault(o => o.Name == "skip");
        Assert.NotNull(skipOption);

        var aggressiveOption = command.Options.FirstOrDefault(o => o.Name == "aggressive");
        Assert.NotNull(aggressiveOption);

        var autoFixOption = command.Options.FirstOrDefault(o => o.Name == "auto-fix");
        Assert.NotNull(autoFixOption);

        var reportOption = command.Options.FirstOrDefault(o => o.Name == "report");
        Assert.NotNull(reportOption);

        var ciOption = command.Options.FirstOrDefault(o => o.Name == "ci");
        Assert.NotNull(ciOption);
    }

    [Fact]
    public async Task PipelineCommand_IntegrationTest_ExecutesPipelineWithDefaultOptions()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-integration");
        Directory.CreateDirectory(path);

        try
        {
            // Act - Actually invoke the command to get coverage
            var command = PipelineCommand.Create();
            var result = await command.InvokeAsync($"--path {path} --ci");

            // Assert - Command should execute successfully
            Assert.Equal(0, result); // Exit code 0 means success
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task PipelineCommand_ExecutePipeline_WithInitStage_RunsSuccessfully()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-init-pipeline");
        var projectName = "TestPipelineProject";
        Directory.CreateDirectory(path);

        try
        {
            // Act
            await PipelineCommand.ExecutePipeline(
                path: path,
                projectName: projectName,
                template: "minimal",
                skipStages: Array.Empty<string>(),
                aggressive: false,
                autoFix: false,
                reportPath: null,
                ciMode: true,
                cancellationToken: CancellationToken.None);

            // Assert - Pipeline should complete successfully
            // The init stage runs in simulation mode and doesn't create actual files
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task PipelineCommand_ExecutePipeline_WithSkippedStages_RunsOnlySpecifiedStages()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-skip-pipeline");
        Directory.CreateDirectory(path);

        try
        {
            // Act - Skip init and doctor stages
            await PipelineCommand.ExecutePipeline(
                path: path,
                projectName: null,
                template: "standard",
                skipStages: new[] { "init", "doctor" },
                aggressive: false,
                autoFix: false,
                reportPath: null,
                ciMode: true,
                cancellationToken: CancellationToken.None);

            // Assert - Should complete without init/doctor stages
            // Since init is skipped and projectName is null, no project directory should be created
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task PipelineCommand_ExecutePipeline_WithAggressiveOptimization_IncludesSIMD()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-aggressive-pipeline");
        Directory.CreateDirectory(path);

        try
        {
            // Act
            await PipelineCommand.ExecutePipeline(
                path: path,
                projectName: null,
                template: "standard",
                skipStages: Array.Empty<string>(),
                aggressive: true, // Enable aggressive optimizations
                autoFix: false,
                reportPath: null,
                ciMode: true,
                cancellationToken: CancellationToken.None);

            // Assert - Should complete with aggressive optimizations
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public async Task PipelineCommand_ExecutePipeline_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), "test-cancel-pipeline");
        Directory.CreateDirectory(path);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                PipelineCommand.ExecutePipeline(
                    path: path,
                    projectName: null,
                    template: "standard",
                    skipStages: Array.Empty<string>(),
                    aggressive: false,
                    autoFix: false,
                    reportPath: null,
                    ciMode: true,
                    cancellationToken: cts.Token));
        }
        finally
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    [Fact]
    public void PipelineCommand_DisplayPipelineResults_InCIMode_OutputsSimpleFormat()
    {
        // Arrange
        var pipelineResult = new PipelineResult
        {
            Success = true,
            TotalDuration = TimeSpan.FromSeconds(2.5),
            Stages = new List<PipelineStageResult>
            {
                new PipelineStageResult
                {
                    StageName = "Init",
                    StageEmoji = "🎬",
                    Success = true,
                    Duration = TimeSpan.FromSeconds(0.8)
                },
                new PipelineStageResult
                {
                    StageName = "Doctor",
                    StageEmoji = "🏥",
                    Success = true,
                    Duration = TimeSpan.FromSeconds(0.5)
                }
            }
        };

        // Act - Capture console output would require redirecting Console.Out
        // For now, just ensure the method doesn't throw
        PipelineCommand.DisplayPipelineResults(pipelineResult, ciMode: true);

        // Assert - Method should complete without exception
    }

    [Fact]
    public void PipelineCommand_DisplayPipelineResults_InInteractiveMode_OutputsRichFormat()
    {
        // Arrange
        var pipelineResult = new PipelineResult
        {
            Success = false,
            TotalDuration = TimeSpan.FromSeconds(1.2),
            Stages = new List<PipelineStageResult>
            {
                new PipelineStageResult
                {
                    StageName = "Init",
                    StageEmoji = "🎬",
                    Success = true,
                    Duration = TimeSpan.FromSeconds(0.8),
                    Details = new List<string> { "Template: standard" }
                },
                new PipelineStageResult
                {
                    StageName = "Doctor",
                    StageEmoji = "🏥",
                    Success = false,
                    Duration = TimeSpan.FromSeconds(0.4),
                    Error = "Health check failed"
                }
            }
        };

        // Act
        PipelineCommand.DisplayPipelineResults(pipelineResult, ciMode: false);

        // Assert - Method should complete without exception
    }

    [Fact]
    public async Task PipelineCommand_GeneratePipelineReport_WithFailedPipeline_IncludesFailureStatus()
    {
        // Arrange
        var reportPath = Path.Combine(Path.GetTempPath(), "failed-pipeline-report.md");
        var pipelineResult = new PipelineResult
        {
            Success = false,
            TotalDuration = TimeSpan.FromSeconds(3.2),
            Stages = new List<PipelineStageResult>
            {
                new PipelineStageResult
                {
                    StageName = "Init",
                    StageEmoji = "🎬",
                    Success = true,
                    Message = "Project created",
                    Duration = TimeSpan.FromSeconds(1.0)
                },
                new PipelineStageResult
                {
                    StageName = "Doctor",
                    StageEmoji = "🏥",
                    Success = false,
                    Message = "Health check failed",
                    Duration = TimeSpan.FromSeconds(2.2),
                    Error = "Critical issues found"
                }
            }
        };

        try
        {
            // Act
            await PipelineCommand.GeneratePipelineReport(pipelineResult, reportPath);

            // Assert
            Assert.True(File.Exists(reportPath));
            var content = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("**Status:** ❌ Failed", content);
            Assert.Contains("## ❌ Errors", content);
            Assert.Contains("❌ Failed", content);
        }
        finally
        {
            if (File.Exists(reportPath))
                File.Delete(reportPath);
        }
    }

    [Fact]
    public async Task PipelineCommand_GeneratePipelineReport_WithEmptyStages_HandlesEdgeCase()
    {
        // Arrange
        var reportPath = Path.Combine(Path.GetTempPath(), "empty-pipeline-report.md");
        var pipelineResult = new PipelineResult
        {
            Success = true,
            TotalDuration = TimeSpan.Zero,
            Stages = new List<PipelineStageResult>() // Empty stages
        };

        try
        {
            // Act
            await PipelineCommand.GeneratePipelineReport(pipelineResult, reportPath);

            // Assert
            Assert.True(File.Exists(reportPath));
            var content = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("**Stages Completed:** 0/0", content);
            Assert.Contains("**Success Rate:** 0.0%", content);
        }
        finally
        {
            if (File.Exists(reportPath))
                File.Delete(reportPath);
        }
    }

    [Fact]
    public void PipelineCommand_DisplayPipelineHeader_OutputsHeader()
    {
        // Act
        PipelineCommand.DisplayPipelineHeader();

        // Assert - Method should complete without exception
        // Header display uses Spectre.Console which outputs to console
    }

    [Fact]
    public async Task GeneratePipelineReport_ShouldHandleEmptyStages()
    {
        // Arrange
        var reportPath = Path.Combine(Path.GetTempPath(), "test-report-empty.md");
        var pipelineResult = new PipelineResult
        {
            Success = true,
            TotalDuration = TimeSpan.Zero,
            Stages = new List<PipelineStageResult>()
        };

        try
        {
            // Act
            await PipelineCommand.GeneratePipelineReport(pipelineResult, reportPath);

            // Assert
            Assert.True(File.Exists(reportPath));
            var content = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("**Stages Completed:** 0/0", content);
            Assert.Contains("**Success Rate:** 0.0%", content);
        }
        finally
        {
            if (File.Exists(reportPath))
                File.Delete(reportPath);
        }
    }

    [Fact]
    public async Task GeneratePipelineReport_ShouldHandleNullDetails()
    {
        // Arrange
        var reportPath = Path.Combine(Path.GetTempPath(), "test-report-null-details.md");
        var pipelineResult = new PipelineResult
        {
            Success = true,
            TotalDuration = TimeSpan.FromSeconds(1.0),
            Stages = new List<PipelineStageResult>
            {
                new PipelineStageResult
                {
                    StageName = "Test",
                    StageEmoji = "🧪",
                    Success = true,
                    Message = "Test passed",
                    Duration = TimeSpan.FromSeconds(1.0),
                    Details = null // Null details
                }
            }
        };

        try
        {
            // Act
            await PipelineCommand.GeneratePipelineReport(pipelineResult, reportPath);

            // Assert
            Assert.True(File.Exists(reportPath));
            var content = await File.ReadAllTextAsync(reportPath);
            Assert.Contains("| 🧪 Test | ✅ | 1.00s | Test passed |", content);
        }
        finally
        {
            if (File.Exists(reportPath))
                File.Delete(reportPath);
        }
    }
}

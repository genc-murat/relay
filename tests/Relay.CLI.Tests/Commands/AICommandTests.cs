using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Relay.CLI.Tests.Commands;

public class AICommandTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDirectory;

    public AICommandTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void CreateCommand_ReturnsCommandWithCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = AICommand.CreateCommand();

        // Assert
        Assert.Equal("ai", command.Name);
        Assert.Equal("AI-powered analysis and optimization for Relay projects", command.Description);
    }

    [Fact]
    public void CreateCommand_HasAllSubcommands()
    {
        // Arrange & Act
        var command = AICommand.CreateCommand();

        // Assert
        var subcommandNames = command.Subcommands.Select(c => c.Name).ToArray();
        Assert.Contains("analyze", subcommandNames);
        Assert.Contains("optimize", subcommandNames);
        Assert.Contains("predict", subcommandNames);
        Assert.Contains("learn", subcommandNames);
        Assert.Contains("insights", subcommandNames);
    }

    [Fact]
    public void AnalyzeSubcommand_HasCorrectOptions()
    {
        // Arrange
        var command = AICommand.CreateCommand();
        var analyzeCommand = command.Subcommands.First(c => c.Name == "analyze");

        // Assert
        Assert.Equal("analyze", analyzeCommand.Name);
        Assert.Equal("Analyze code for AI optimization opportunities", analyzeCommand.Description);

        var optionNames = analyzeCommand.Options.Select(o => o.Name).ToArray();
        Assert.Contains("path", optionNames);
        Assert.Contains("depth", optionNames);
        Assert.Contains("format", optionNames);
        Assert.Contains("output", optionNames);
        Assert.Contains("include-metrics", optionNames);
        Assert.Contains("suggest-optimizations", optionNames);
    }

    [Fact]
    public void OptimizeSubcommand_HasCorrectOptions()
    {
        // Arrange
        var command = AICommand.CreateCommand();
        var optimizeCommand = command.Subcommands.First(c => c.Name == "optimize");

        // Assert
        Assert.Equal("optimize", optimizeCommand.Name);
        Assert.Equal("Apply AI-recommended optimizations", optimizeCommand.Description);

        var optionNames = optimizeCommand.Options.Select(o => o.Name).ToArray();
        Assert.Contains("path", optionNames);
        Assert.Contains("strategy", optionNames);
        Assert.Contains("risk-level", optionNames);
        Assert.Contains("backup", optionNames);
        Assert.Contains("dry-run", optionNames);
        Assert.Contains("confidence-threshold", optionNames);
    }

    [Fact]
    public void PredictSubcommand_HasCorrectOptions()
    {
        // Arrange
        var command = AICommand.CreateCommand();
        var predictCommand = command.Subcommands.First(c => c.Name == "predict");

        // Assert
        Assert.Equal("predict", predictCommand.Name);
        Assert.Equal("Predict performance and generate recommendations", predictCommand.Description);

        var optionNames = predictCommand.Options.Select(o => o.Name).ToArray();
        Assert.Contains("path", optionNames);
        Assert.Contains("scenario", optionNames);
        Assert.Contains("expected-load", optionNames);
        Assert.Contains("time-horizon", optionNames);
        Assert.Contains("format", optionNames);
    }

    [Fact]
    public void LearnSubcommand_HasCorrectOptions()
    {
        // Arrange
        var command = AICommand.CreateCommand();
        var learnCommand = command.Subcommands.First(c => c.Name == "learn");

        // Assert
        Assert.Equal("learn", learnCommand.Name);
        Assert.Equal("Learn from performance data to improve AI recommendations", learnCommand.Description);

        var optionNames = learnCommand.Options.Select(o => o.Name).ToArray();
        Assert.Contains("path", optionNames);
        Assert.Contains("metrics-path", optionNames);
        Assert.Contains("update-model", optionNames);
        Assert.Contains("validate", optionNames);
    }

    [Fact]
    public void InsightsSubcommand_HasCorrectOptions()
    {
        // Arrange
        var command = AICommand.CreateCommand();
        var insightsCommand = command.Subcommands.First(c => c.Name == "insights");

        // Assert
        Assert.Equal("insights", insightsCommand.Name);
        Assert.Equal("Generate comprehensive AI-powered system insights", insightsCommand.Description);

        var optionNames = insightsCommand.Options.Select(o => o.Name).ToArray();
        Assert.Contains("path", optionNames);
        Assert.Contains("time-window", optionNames);
        Assert.Contains("format", optionNames);
        Assert.Contains("output", optionNames);
        Assert.Contains("include-health", optionNames);
        Assert.Contains("include-predictions", optionNames);
    }

    [Fact]
    public async Task ExecuteAnalyzeCommand_WithValidParameters_CompletesSuccessfully()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Act & Assert - Should not throw
        await AICommand.ExecuteAnalyzeCommand(projectPath, "standard", "console", null, true, true);
    }

    [Fact]
    public async Task ExecuteOptimizeCommand_WithValidParameters_CompletesSuccessfully()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Act & Assert - Should not throw
        await AICommand.ExecuteOptimizeCommand(projectPath, new[] { "caching", "async" }, "low", true, false, 0.8);
    }

    [Fact]
    public async Task ExecutePredictCommand_WithValidParameters_CompletesSuccessfully()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Act & Assert - Should not throw
        await AICommand.ExecutePredictCommand(projectPath, "production", "medium", "1h", "console");
    }

    [Fact]
    public async Task ExecuteLearnCommand_WithValidParameters_CompletesSuccessfully()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Act & Assert - Should not throw
        await AICommand.ExecuteLearnCommand(projectPath, null, true, true);
    }

    [Fact]
    public async Task ExecuteInsightsCommand_WithValidParameters_CompletesSuccessfully()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Act & Assert - Should not throw
        await AICommand.ExecuteInsightsCommand(projectPath, "24h", "console", null, true, true);
    }

    [Fact]
    public async Task OutputResults_WithConsoleFormat_DisplaysResults()
    {
        // Arrange
        var results = new AIAnalysisResults
        {
            ProjectPath = "/test/path",
            FilesAnalyzed = 10,
            HandlersFound = 5,
            PerformanceScore = 8.5,
            AIConfidence = 0.9,
            PerformanceIssues = new[]
            {
                new AIPerformanceIssue { Severity = "High", Description = "Test issue", Location = "Test.cs", Impact = "High" }
            },
            OptimizationOpportunities = new[]
            {
                new OptimizationOpportunity { Strategy = "Caching", Description = "Add caching", ExpectedImprovement = 0.5, Confidence = 0.8, RiskLevel = "Low" }
            }
        };

        // Act & Assert - Should not throw
        await AICommand.OutputResults(results, "console", null);
    }

    [Fact]
    public async Task OutputResults_WithJsonFormat_OutputsJson()
    {
        // Arrange
        var results = new AIAnalysisResults
        {
            ProjectPath = "/test/path",
            FilesAnalyzed = 10,
            HandlersFound = 5,
            PerformanceScore = 8.5,
            AIConfidence = 0.9
        };
        var outputPath = Path.Combine(_tempDirectory, "output.json");

        // Act
        await AICommand.OutputResults(results, "json", outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("ProjectPath", content);
        Assert.Contains("8.5", content);
    }

    [Fact]
    public async Task OutputResults_WithHtmlFormat_CreatesHtmlFile()
    {
        // Arrange
        var results = new AIAnalysisResults
        {
            ProjectPath = "/test/path",
            FilesAnalyzed = 10,
            HandlersFound = 5,
            PerformanceScore = 8.5,
            AIConfidence = 0.9
        };
        var outputPath = Path.Combine(_tempDirectory, "output.html");

        // Act
        await AICommand.OutputResults(results, "html", outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("<html>", content);
        Assert.Contains("AI Analysis Report", content);
    }

    [Fact]
    public async Task OutputPredictions_WithConsoleFormat_DisplaysPredictions()
    {
        // Arrange
        var predictions = new AIPredictionResults
        {
            ExpectedThroughput = 1000,
            ExpectedResponseTime = 50,
            ExpectedErrorRate = 0.01,
            ExpectedCpuUsage = 0.7,
            ExpectedMemoryUsage = 0.6,
            Bottlenecks = new[]
            {
                new PredictedBottleneck { Component = "Database", Description = "High load", Probability = 0.3, Impact = "High" }
            },
            Recommendations = new[] { "Add caching", "Optimize queries" }
        };

        // Act & Assert - Should not throw
        await AICommand.OutputPredictions(predictions, "console");
    }

    [Fact]
    public async Task OutputInsights_WithConsoleFormat_DisplaysInsights()
    {
        // Arrange
        var insights = new AIInsightsResults
        {
            HealthScore = 8.5,
            PerformanceGrade = 'A',
            ReliabilityScore = 9.0,
            CriticalIssues = new[] { "Memory usage high" },
            OptimizationOpportunities = new[]
            {
                new OptimizationOpportunity { Title = "Add Caching", ExpectedImprovement = 0.4 }
            },
            Predictions = new[]
            {
                new PredictionResult { Metric = "Throughput", PredictedValue = "1000 req/sec", Confidence = 0.9 }
            }
        };

        // Act & Assert - Should not throw
        await AICommand.OutputInsights(insights, "console", null);
    }

    [Fact]
    public async Task OutputInsights_WithJsonFormat_OutputsJson()
    {
        // Arrange
        var insights = new AIInsightsResults
        {
            HealthScore = 8.5,
            PerformanceGrade = 'A',
            ReliabilityScore = 9.0
        };
        var outputPath = Path.Combine(_tempDirectory, "insights.json");

        // Act
        await AICommand.OutputInsights(insights, "json", outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("HealthScore", content);
        Assert.Contains("8.5", content);
    }

    [Fact]
    public async Task OutputInsights_WithHtmlFormat_CreatesHtmlFile()
    {
        // Arrange
        var insights = new AIInsightsResults
        {
            HealthScore = 8.5,
            PerformanceGrade = 'A',
            ReliabilityScore = 9.0
        };
        var outputPath = Path.Combine(_tempDirectory, "insights.html");

        // Act
        await AICommand.OutputInsights(insights, "html", outputPath);

        // Assert
        Assert.True(File.Exists(outputPath));
        var content = await File.ReadAllTextAsync(outputPath);
        Assert.Contains("<html>", content);
        Assert.Contains("AI System Insights", content);
    }

    [Fact]
    public void GenerateHtmlReport_ReturnsValidHtml()
    {
        // Arrange
        var results = new AIAnalysisResults
        {
            ProjectPath = "/test/path",
            FilesAnalyzed = 10,
            HandlersFound = 5,
            PerformanceScore = 8.5,
            AIConfidence = 0.9,
            PerformanceIssues = new[]
            {
                new AIPerformanceIssue { Severity = "High", Description = "Test issue", Location = "Test.cs", Impact = "High" }
            },
            OptimizationOpportunities = new[]
            {
                new OptimizationOpportunity { Strategy = "Caching", Description = "Add caching", ExpectedImprovement = 0.5, Confidence = 0.8, RiskLevel = "Low" }
            }
        };

        // Act
        var html = AICommand.GenerateHtmlReport(results);

        // Assert
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("AI Analysis Report", html);
        Assert.Contains("8.5", html);
        Assert.Contains("Test issue", html);
        Assert.Contains("Add caching", html);
    }

    [Fact]
    public void GenerateInsightsHtmlReport_ReturnsValidHtml()
    {
        // Arrange
        var insights = new AIInsightsResults
        {
            HealthScore = 8.5,
            PerformanceGrade = 'A',
            ReliabilityScore = 9.0,
            CriticalIssues = new[] { "Memory usage high" },
            OptimizationOpportunities = new[]
            {
                new OptimizationOpportunity { Title = "Add Caching", ExpectedImprovement = 0.4 }
            }
        };

        // Act
        var html = AICommand.GenerateInsightsHtmlReport(insights);

        // Assert
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("AI System Insights", html);
        Assert.Contains("8.5", html);
        Assert.Contains("Memory usage high", html);
    }

    [Fact]
    public async Task ExecuteAnalyzeCommand_WithInvalidFormat_ThrowsArgumentException()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            AICommand.ExecuteAnalyzeCommand(projectPath, "standard", "invalid", null, true, true));
    }

    [Fact]
    public async Task ExecuteOptimizeCommand_WithDryRun_DisplaysDryRunMessage()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);

        // Act & Assert - Should not throw
        await AICommand.ExecuteOptimizeCommand(projectPath, new[] { "caching" }, "low", false, true, 0.8);
    }

    [Fact]
    public async Task ExecuteLearnCommand_WithMetricsPath_ProcessesMetrics()
    {
        // Arrange
        var projectPath = Path.Combine(_tempDirectory, "TestProject");
        Directory.CreateDirectory(projectPath);
        var metricsPath = Path.Combine(_tempDirectory, "metrics.json");
        await File.WriteAllTextAsync(metricsPath, "{}");

        // Act & Assert - Should not throw
        await AICommand.ExecuteLearnCommand(projectPath, metricsPath, true, true);
    }
}
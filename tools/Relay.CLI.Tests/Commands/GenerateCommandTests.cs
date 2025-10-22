using Relay.CLI.Commands;
using System.CommandLine;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class GenerateCommandTests : IDisposable
{
    private readonly string _testPath;

    public GenerateCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-generate-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public void Create_ReturnsConfiguredCommand()
    {
        // Act
        var command = GenerateCommand.Create();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("generate", command.Name);
        Assert.Equal("Generate additional components and utilities", command.Description);

        var typeOption = command.Options.FirstOrDefault(o => o.Name == "type");
        Assert.NotNull(typeOption);
        Assert.True(typeOption.IsRequired);

        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");
        Assert.NotNull(outputOption);
        Assert.False(outputOption.IsRequired);
    }

    [Theory]
    [InlineData("docs")]
    [InlineData("config")]
    [InlineData("benchmark")]
    public async Task ExecuteGenerate_WithValidType_CreatesFile(string type)
    {
        // Act
        await GenerateCommand.ExecuteGenerate(type, _testPath);

        // Assert
        var files = Directory.GetFiles(_testPath);
        Assert.Single(files);
    }

    [Fact]
    public async Task ExecuteGenerate_WithInvalidType_ShowsErrorMessage()
    {
        // Act
        await GenerateCommand.ExecuteGenerate("invalid", _testPath);

        // Assert
        var files = Directory.GetFiles(_testPath);
        Assert.Empty(files);
    }

    [Fact]
    public async Task ExecuteGenerate_Docs_CreatesReadmeFile()
    {
        // Act
        await GenerateCommand.ExecuteGenerate("docs", _testPath);

        // Assert
        var readmePath = Path.Combine(_testPath, "README.md");
        Assert.True(File.Exists(readmePath));

        var content = await File.ReadAllTextAsync(readmePath);
        Assert.Contains("# Project Documentation", content);
        Assert.Contains("## Overview", content);
        Assert.Contains("## Getting Started", content);
        Assert.Contains("## Performance", content);
        Assert.Contains("dotnet restore", content);
        Assert.Contains("dotnet run", content);
        Assert.Contains("dotnet test", content);
        Assert.Contains("80%+ better performance", content);
    }

    [Fact]
    public async Task ExecuteGenerate_Config_CreatesAppsettingsFile()
    {
        // Act
        await GenerateCommand.ExecuteGenerate("config", _testPath);

        // Assert
        var configPath = Path.Combine(_testPath, "appsettings.relay.json");
        Assert.True(File.Exists(configPath));

        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("\"Relay\"", content);
        Assert.Contains("\"EnablePerformanceOptimizations\"", content);
        Assert.Contains("\"UseValueTask\"", content);
        Assert.Contains("\"EnableSourceGeneration\"", content);
        Assert.Contains("\"Telemetry\"", content);
        Assert.Contains("\"Enabled\"", content);
        Assert.Contains("\"CollectMetrics\"", content);
    }

    [Fact]
    public async Task ExecuteGenerate_Benchmark_CreatesBenchmarkFile()
    {
        // Act
        await GenerateCommand.ExecuteGenerate("benchmark", _testPath);

        // Assert
        var benchmarkPath = Path.Combine(_testPath, "RelayBenchmark.cs");
        Assert.True(File.Exists(benchmarkPath));

        var content = await File.ReadAllTextAsync(benchmarkPath);
        Assert.Contains("using BenchmarkDotNet.Attributes;", content);
        Assert.Contains("using BenchmarkDotNet.Running;", content);
        Assert.Contains("[MemoryDiagnoser]", content);
        Assert.Contains("[SimpleJob]", content);
        Assert.Contains("public class RelayBenchmark", content);
        Assert.Contains("[Benchmark]", content);
        Assert.Contains("public async Task SendRequest()", content);
        Assert.Contains("await Task.Delay(1);", content);
        Assert.Contains("public class Program", content);
        Assert.Contains("BenchmarkRunner.Run<RelayBenchmark>()", content);
    }

    [Fact]
    public async Task ExecuteGenerate_CreatesFilesInSpecifiedOutputDirectory()
    {
        // Arrange
        var subDir = Path.Combine(_testPath, "output");
        Directory.CreateDirectory(subDir);

        // Act
        await GenerateCommand.ExecuteGenerate("docs", subDir);

        // Assert
        var readmePath = Path.Combine(subDir, "README.md");
        Assert.True(File.Exists(readmePath));
    }

    [Fact]
    public async Task ExecuteGenerate_OverwritesExistingFiles()
    {
        // Arrange
        var readmePath = Path.Combine(_testPath, "README.md");
        await File.WriteAllTextAsync(readmePath, "existing content");

        // Act
        await GenerateCommand.ExecuteGenerate("docs", _testPath);

        // Assert
        var content = await File.ReadAllTextAsync(readmePath);
        Assert.NotEqual("existing content", content);
        Assert.Contains("# Project Documentation", content);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPath))
                Directory.Delete(_testPath, true);
        }
        catch { }
    }
}



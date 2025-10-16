using Relay.CLI.Commands;
using System.CommandLine;

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
        command.Should().NotBeNull();
        command.Name.Should().Be("generate");
        command.Description.Should().Be("Generate additional components and utilities");

        var typeOption = command.Options.FirstOrDefault(o => o.Name == "type");
        typeOption.Should().NotBeNull();
        typeOption.IsRequired.Should().BeTrue();

        var outputOption = command.Options.FirstOrDefault(o => o.Name == "output");
        outputOption.Should().NotBeNull();
        outputOption.IsRequired.Should().BeFalse();
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
        files.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecuteGenerate_WithInvalidType_ShowsErrorMessage()
    {
        // Act
        await GenerateCommand.ExecuteGenerate("invalid", _testPath);

        // Assert
        var files = Directory.GetFiles(_testPath);
        files.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteGenerate_Docs_CreatesReadmeFile()
    {
        // Act
        await GenerateCommand.ExecuteGenerate("docs", _testPath);

        // Assert
        var readmePath = Path.Combine(_testPath, "README.md");
        File.Exists(readmePath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(readmePath);
        content.Should().Contain("# Project Documentation");
        content.Should().Contain("## Overview");
        content.Should().Contain("## Getting Started");
        content.Should().Contain("## Performance");
        content.Should().Contain("dotnet restore");
        content.Should().Contain("dotnet run");
        content.Should().Contain("dotnet test");
        content.Should().Contain("80%+ better performance");
    }

    [Fact]
    public async Task ExecuteGenerate_Config_CreatesAppsettingsFile()
    {
        // Act
        await GenerateCommand.ExecuteGenerate("config", _testPath);

        // Assert
        var configPath = Path.Combine(_testPath, "appsettings.relay.json");
        File.Exists(configPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(configPath);
        content.Should().Contain("\"Relay\"");
        content.Should().Contain("\"EnablePerformanceOptimizations\"");
        content.Should().Contain("\"UseValueTask\"");
        content.Should().Contain("\"EnableSourceGeneration\"");
        content.Should().Contain("\"Telemetry\"");
        content.Should().Contain("\"Enabled\"");
        content.Should().Contain("\"CollectMetrics\"");
    }

    [Fact]
    public async Task ExecuteGenerate_Benchmark_CreatesBenchmarkFile()
    {
        // Act
        await GenerateCommand.ExecuteGenerate("benchmark", _testPath);

        // Assert
        var benchmarkPath = Path.Combine(_testPath, "RelayBenchmark.cs");
        File.Exists(benchmarkPath).Should().BeTrue();

        var content = await File.ReadAllTextAsync(benchmarkPath);
        content.Should().Contain("using BenchmarkDotNet.Attributes;");
        content.Should().Contain("using BenchmarkDotNet.Running;");
        content.Should().Contain("[MemoryDiagnoser]");
        content.Should().Contain("[SimpleJob]");
        content.Should().Contain("public class RelayBenchmark");
        content.Should().Contain("[Benchmark]");
        content.Should().Contain("public async Task SendRequest()");
        content.Should().Contain("await Task.Delay(1);");
        content.Should().Contain("public class Program");
        content.Should().Contain("BenchmarkRunner.Run<RelayBenchmark>()");
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
        File.Exists(readmePath).Should().BeTrue();
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
        content.Should().NotBe("existing content");
        content.Should().Contain("# Project Documentation");
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

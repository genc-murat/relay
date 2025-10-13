using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Relay.CLI.Tests.Commands;

public class GenerateCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = GenerateCommand.Create();

        // Assert
        Assert.Equal("generate", command.Name);
        Assert.Equal("Generate additional components and utilities", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = GenerateCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "type");
        Assert.Contains(command.Options, o => o.Name == "output");

        var typeOption = command.Options.First(o => o.Name == "type");
        Assert.True(typeOption.IsRequired);
    }

    [Fact]
    public async Task ExecuteGenerate_WithDocsType_GeneratesDocumentation()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayGenerateDocsTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await GenerateCommand.ExecuteGenerate("docs", tempDir);

            // Assert
            var docsPath = Path.Combine(tempDir, "README.md");
            Assert.True(File.Exists(docsPath));

            var content = await File.ReadAllTextAsync(docsPath);
            Assert.Contains("# Project Documentation", content);
            Assert.Contains("Relay framework", content);
            Assert.Contains("Performance", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteGenerate_WithConfigType_GeneratesConfiguration()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayGenerateConfigTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await GenerateCommand.ExecuteGenerate("config", tempDir);

            // Assert
            var configPath = Path.Combine(tempDir, "appsettings.relay.json");
            Assert.True(File.Exists(configPath));

            var content = await File.ReadAllTextAsync(configPath);
            Assert.Contains("Relay", content);
            Assert.Contains("EnablePerformanceOptimizations", content);
            Assert.Contains("UseValueTask", content);
            Assert.Contains("EnableSourceGeneration", content);
            Assert.Contains("Telemetry", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteGenerate_WithBenchmarkType_GeneratesBenchmark()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayGenerateBenchmarkTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await GenerateCommand.ExecuteGenerate("benchmark", tempDir);

            // Assert
            var benchmarkPath = Path.Combine(tempDir, "RelayBenchmark.cs");
            Assert.True(File.Exists(benchmarkPath));

            var content = await File.ReadAllTextAsync(benchmarkPath);
            Assert.Contains("BenchmarkDotNet", content);
            Assert.Contains("[MemoryDiagnoser]", content);
            Assert.Contains("RelayBenchmark", content);
            Assert.Contains("BenchmarkRunner", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteGenerate_WithUnknownType_HandlesError()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayGenerateUnknownTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await GenerateCommand.ExecuteGenerate("unknown", tempDir);

            // Assert - Method should complete without throwing
            // The unknown type case is handled gracefully
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteGenerate_WithCaseInsensitiveType_Works()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayGenerateCaseTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await GenerateCommand.ExecuteGenerate("DOCS", tempDir);

            // Assert
            var docsPath = Path.Combine(tempDir, "README.md");
            Assert.True(File.Exists(docsPath));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GenerateDocs_CreatesExpectedContent()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayDocsTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await GenerateCommand.GenerateDocs(tempDir);

            // Assert
            var docsPath = Path.Combine(tempDir, "README.md");
            Assert.True(File.Exists(docsPath));

            var content = await File.ReadAllTextAsync(docsPath);
            Assert.Contains("# Project Documentation", content);
            Assert.Contains("## Overview", content);
            Assert.Contains("## Getting Started", content);
            Assert.Contains("## Performance", content);
            Assert.Contains("dotnet restore", content);
            Assert.Contains("dotnet run", content);
            Assert.Contains("dotnet test", content);
            Assert.Contains("80%+", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GenerateConfig_CreatesExpectedContent()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayConfigTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await GenerateCommand.GenerateConfig(tempDir);

            // Assert
            var configPath = Path.Combine(tempDir, "appsettings.relay.json");
            Assert.True(File.Exists(configPath));

            var content = await File.ReadAllTextAsync(configPath);
            Assert.Contains("\"Relay\"", content);
            Assert.Contains("\"EnablePerformanceOptimizations\": true", content);
            Assert.Contains("\"UseValueTask\": true", content);
            Assert.Contains("\"EnableSourceGeneration\": true", content);
            Assert.Contains("\"Telemetry\"", content);
            Assert.Contains("\"Enabled\": true", content);
            Assert.Contains("\"CollectMetrics\": true", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GenerateBenchmark_CreatesExpectedContent()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayBenchmarkTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await GenerateCommand.GenerateBenchmark(tempDir);

            // Assert
            var benchmarkPath = Path.Combine(tempDir, "RelayBenchmark.cs");
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
            Assert.Contains("BenchmarkRunner.Run<RelayBenchmark>();", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GenerateDocs_OverwritesExistingFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayOverwriteTest");
        Directory.CreateDirectory(tempDir);
        var docsPath = Path.Combine(tempDir, "README.md");

        // Create existing file with different content
        await File.WriteAllTextAsync(docsPath, "Existing content");

        try
        {
            // Act
            await GenerateCommand.GenerateDocs(tempDir);

            // Assert
            var content = await File.ReadAllTextAsync(docsPath);
            Assert.Contains("# Project Documentation", content);
            Assert.DoesNotContain("Existing content", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
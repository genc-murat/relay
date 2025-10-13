using System.CommandLine;
using Relay.CLI.Commands;
using Relay.CLI.Commands.Models.Benchmark;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Relay.CLI.Tests.Commands;

public class BenchmarkCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = BenchmarkCommand.Create();

        // Assert
        Assert.Equal("benchmark", command.Name);
        Assert.Equal("Run comprehensive performance benchmarks", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = BenchmarkCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "iterations");
        Assert.Contains(command.Options, o => o.Name == "output");
        Assert.Contains(command.Options, o => o.Name == "format");
        Assert.Contains(command.Options, o => o.Name == "tests");
        Assert.Contains(command.Options, o => o.Name == "warmup");
        Assert.Contains(command.Options, o => o.Name == "threads");
    }

    [Fact]
    public async Task ExecuteBenchmark_WithDefaultParameters_CompletesSuccessfully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayBenchmarkTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "results.json");

            // Act - This should not throw
            await BenchmarkCommand.ExecuteBenchmark(1000, outputPath, "json", new[] { "relay" }, 100, 1);

            // Assert
            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("TestConfiguration", content);
            Assert.Contains("RelayResults", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteBenchmark_WithConsoleFormat_DoesNotCreateOutputFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayBenchmarkConsoleTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await BenchmarkCommand.ExecuteBenchmark(100, null, "console", new[] { "relay" }, 10, 1);

            // Assert - No file should be created since output is null
            var files = Directory.GetFiles(tempDir);
            Assert.Empty(files);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteBenchmark_WithSpecificTests_RunsOnlyRequestedTests()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayBenchmarkSpecificTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "results.json");

            // Act
            await BenchmarkCommand.ExecuteBenchmark(100, outputPath, "json", new[] { "relay" }, 10, 1);

            // Assert
            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("Standard", content);
            Assert.Contains("UltraFast", content);
            Assert.Contains("SIMD", content);
            Assert.Contains("AOT", content);
            // Should not contain comparison results
            Assert.DoesNotContain("DirectCall", content);
            Assert.DoesNotContain("MediatR", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteBenchmark_WithAllTests_IncludesComparisonBenchmarks()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayBenchmarkAllTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "results.json");

            // Act
            await BenchmarkCommand.ExecuteBenchmark(100, outputPath, "json", new[] { "all" }, 10, 1);

            // Assert
            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("Standard", content);
            Assert.Contains("UltraFast", content);
            Assert.Contains("DirectCall", content);
            Assert.Contains("MediatR", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteBenchmark_WithHtmlFormat_CreatesHtmlFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayBenchmarkHtmlTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "results.html");

            // Act
            await BenchmarkCommand.ExecuteBenchmark(100, outputPath, "html", new[] { "relay" }, 10, 1);

            // Assert
            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("Relay Performance Benchmark Results", content);
            Assert.Contains("performanceChart", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteBenchmark_WithCsvFormat_CreatesCsvFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayBenchmarkCsvTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "results.csv");

            // Act
            await BenchmarkCommand.ExecuteBenchmark(100, outputPath, "csv", new[] { "relay" }, 10, 1);

            // Assert
            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("Implementation,Average Time", content);
            Assert.Contains("Standard Relay", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task ExecuteBenchmark_WithMultipleThreads_CompletesSuccessfully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayBenchmarkMultiThreadTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, "results.json");

            // Act
            await BenchmarkCommand.ExecuteBenchmark(1000, outputPath, "json", new[] { "relay" }, 100, 4);

            // Assert
            Assert.True(File.Exists(outputPath));
            var content = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("Threads", content);
            Assert.Contains("\"Threads\":4", content);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
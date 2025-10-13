using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Relay.CLI.Tests.Commands;

public class OptimizeCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = OptimizeCommand.Create();

        // Assert
        Assert.Equal("optimize", command.Name);
        Assert.Equal("Apply automatic optimizations to improve performance", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = OptimizeCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "path");
        Assert.Contains(command.Options, o => o.Name == "dry-run");
        Assert.Contains(command.Options, o => o.Name == "target");
        Assert.Contains(command.Options, o => o.Name == "aggressive");
        Assert.Contains(command.Options, o => o.Name == "backup");
    }

    [Fact]
    public async Task ExecuteOptimize_WithDryRun_DoesNotModifyFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayOptimizeDryRunTest");
        Directory.CreateDirectory(tempDir);

        // Create a handler file with Task<T>
        var handlerContent = @"using Relay.Core;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}
public record TestRequest : IRequest<string>;";
        var filePath = Path.Combine(tempDir, "TestHandler.cs");
        await File.WriteAllTextAsync(filePath, handlerContent);

        try
        {
            // Act
            await OptimizeCommand.ExecuteOptimize(tempDir, true, "all", false, false);

            // Assert - File should remain unchanged
            var contentAfter = await File.ReadAllTextAsync(filePath);
            Assert.Contains("Task<string>", contentAfter);
            Assert.DoesNotContain("ValueTask<string>", contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteOptimize_WithHandlersTarget_OptimizesHandlerFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayOptimizeHandlersTest");
        Directory.CreateDirectory(tempDir);

        // Create a handler file with Task<T>
        var handlerContent = @"using Relay.Core;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}
public record TestRequest : IRequest<string>;";
        var filePath = Path.Combine(tempDir, "TestHandler.cs");
        await File.WriteAllTextAsync(filePath, handlerContent);

        try
        {
            // Act
            await OptimizeCommand.ExecuteOptimize(tempDir, false, "handlers", false, false);

            // Assert - File should be optimized
            var contentAfter = await File.ReadAllTextAsync(filePath);
            Assert.Contains("ValueTask<string>", contentAfter);
            Assert.DoesNotContain("Task<string>", contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteOptimize_WithAllTarget_IncludesAllOptimizations()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayOptimizeAllTest");
        Directory.CreateDirectory(tempDir);

        // Create a handler file with Task<T>
        var handlerContent = @"using Relay.Core;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}
public record TestRequest : IRequest<string>;";
        var filePath = Path.Combine(tempDir, "TestHandler.cs");
        await File.WriteAllTextAsync(filePath, handlerContent);

        try
        {
            // Act
            await OptimizeCommand.ExecuteOptimize(tempDir, false, "all", false, false);

            // Assert - File should be optimized
            var contentAfter = await File.ReadAllTextAsync(filePath);
            Assert.Contains("ValueTask<string>", contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteOptimize_WithNoOptimizableFiles_ShowsNoOptimizationsMessage()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayOptimizeNoFilesTest");
        Directory.CreateDirectory(tempDir);

        // Create a file that doesn't need optimization
        var content = @"using Relay.Core;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async ValueTask<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}
public record TestRequest : IRequest<string>;";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "TestHandler.cs"), content);

        try
        {
            // Act
            await OptimizeCommand.ExecuteOptimize(tempDir, true, "all", false, false);

            // Assert - Should complete without errors
            // The method should handle the case where no optimizations are needed
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteOptimize_WithNonExistentPath_HandlesGracefully()
    {
        // Arrange
        var invalidPath = "/nonexistent/path/that/does/not/exist";

        // Act & Assert - Should not throw
        await OptimizeCommand.ExecuteOptimize(invalidPath, true, "all", false, false);
    }

    [Fact]
    public async Task ExecuteOptimize_WithAggressiveFlag_ProcessesAggressively()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayOptimizeAggressiveTest");
        Directory.CreateDirectory(tempDir);

        // Create a handler file
        var handlerContent = @"using Relay.Core;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}
public record TestRequest : IRequest<string>;";
        var filePath = Path.Combine(tempDir, "TestHandler.cs");
        await File.WriteAllTextAsync(filePath, handlerContent);

        try
        {
            // Act
            await OptimizeCommand.ExecuteOptimize(tempDir, false, "all", true, false);

            // Assert - File should be optimized (aggressive flag doesn't change the logic in current implementation)
            var contentAfter = await File.ReadAllTextAsync(filePath);
            Assert.Contains("ValueTask<string>", contentAfter);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
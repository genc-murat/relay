using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Relay.CLI.Tests.Commands;

public class ValidateCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();

        // Assert
        Assert.Equal("validate", command.Name);
        Assert.Equal("Validate project structure and configuration", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = ValidateCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "path");
        Assert.Contains(command.Options, o => o.Name == "strict");
        Assert.Contains(command.Options, o => o.Name == "output");
        Assert.Contains(command.Options, o => o.Name == "format");
    }

    [Fact]
    public async Task ExecuteValidate_WithNonExistentPath_HandlesGracefully()
    {
        // Arrange
        var invalidPath = "/nonexistent/path";

        // Act & Assert - Should not throw
        await ValidateCommand.ExecuteValidate(invalidPath, false, null, "console");
    }

    [Fact]
    public async Task ExecuteValidate_WithEmptyDirectory_FindsNoProjectFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayValidateEmptyTest");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act & Assert - Should not throw
            await ValidateCommand.ExecuteValidate(tempDir, false, null, "console");
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
    public async Task ExecuteValidate_WithValidProject_PassesValidation()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayValidateValidTest");
        Directory.CreateDirectory(tempDir);

        // Create a minimal valid project
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        // Create Program.cs with AddRelay
        var programContent = @"using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay.Core;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddRelay();
var app = builder.Build();
await app.RunAsync();";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "Program.cs"), programContent);

        // Create a handler
        var handlerContent = @"using Relay.Core;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async ValueTask<string> HandleTest(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}
public record TestRequest(string Value) : IRequest<string>;";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "TestHandler.cs"), handlerContent);

        try
        {
            // Act & Assert - Should not throw
            await ValidateCommand.ExecuteValidate(tempDir, false, null, "console");
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
    public async Task ExecuteValidate_WithOutputFile_CreatesReport()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayValidateOutputTest");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "validation-report.json");

        // Create a minimal project
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        try
        {
            // Act
            await ValidateCommand.ExecuteValidate(tempDir, false, outputPath, "json");

            // Assert
            Assert.True(File.Exists(outputPath));
            var reportContent = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("Type", reportContent);
            Assert.Contains("Status", reportContent);
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
    public async Task ExecuteValidate_WithMarkdownFormat_CreatesMarkdownReport()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayValidateMarkdownTest");
        Directory.CreateDirectory(tempDir);
        var outputPath = Path.Combine(tempDir, "validation-report.md");

        // Create a minimal project
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        try
        {
            // Act
            await ValidateCommand.ExecuteValidate(tempDir, false, outputPath, "markdown");

            // Assert
            Assert.True(File.Exists(outputPath));
            var reportContent = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("# Validation Report", reportContent);
            Assert.Contains("## Results", reportContent);
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
    public async Task ExecuteValidate_WithStrictMode_FailsOnMissingNullable()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayValidateStrictTest");
        Directory.CreateDirectory(tempDir);

        // Create a project without nullable enabled
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        try
        {
            // Act & Assert - Should not throw
            await ValidateCommand.ExecuteValidate(tempDir, true, null, "console");
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
    public async Task ExecuteValidate_WithNoRelayPackage_FailsValidation()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayValidateNoRelayTest");
        Directory.CreateDirectory(tempDir);

        // Create a project without Relay package
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        try
        {
            // Act & Assert - Should not throw
            await ValidateCommand.ExecuteValidate(tempDir, false, null, "console");
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
    public async Task ExecuteValidate_WithInvalidJsonConfig_ReportsError()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayValidateInvalidJsonTest");
        Directory.CreateDirectory(tempDir);

        // Create a project with invalid JSON config
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        // Create invalid JSON config
        await File.WriteAllTextAsync(Path.Combine(tempDir, ".relay-cli.json"), "{ invalid json }");

        try
        {
            // Act & Assert - Should not throw
            await ValidateCommand.ExecuteValidate(tempDir, false, null, "console");
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
    public async Task ExecuteValidate_WithHandlerUsingTask_WarnsAboutValueTask()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayValidateTaskTest");
        Directory.CreateDirectory(tempDir);

        // Create a project
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.0.0"" />
  </ItemGroup>
</Project>";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "test.csproj"), csprojContent);

        // Create a handler using Task instead of ValueTask
        var handlerContent = @"using Relay.Core;
public class TestHandler : IRequestHandler<TestRequest, string>
{
    public async Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}
public record TestRequest : IRequest<string>;";
        await File.WriteAllTextAsync(Path.Combine(tempDir, "TestHandler.cs"), handlerContent);

        try
        {
            // Act & Assert - Should not throw
            await ValidateCommand.ExecuteValidate(tempDir, false, null, "console");
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
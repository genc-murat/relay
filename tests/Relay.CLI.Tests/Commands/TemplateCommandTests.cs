using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Relay.CLI.Tests.Commands;

public class TemplateCommandTests
{
    [Fact]
    public void Constructor_CreatesCommandWithCorrectName()
    {
        // Arrange & Act
        var command = new TemplateCommand();

        // Assert
        Assert.Equal("template", command.Name);
        Assert.Equal("Manage Relay project templates", command.Description);
    }

    [Fact]
    public void Constructor_HasAllSubcommands()
    {
        // Arrange & Act
        var command = new TemplateCommand();

        // Assert
        Assert.Contains(command.Subcommands, c => c.Name == "validate");
        Assert.Contains(command.Subcommands, c => c.Name == "pack");
        Assert.Contains(command.Subcommands, c => c.Name == "publish");
        Assert.Contains(command.Subcommands, c => c.Name == "list");
        Assert.Contains(command.Subcommands, c => c.Name == "create");
    }

    [Fact]
    public void ValidateSubcommand_HasRequiredOptions()
    {
        // Arrange
        var command = new TemplateCommand();
        var validateCommand = command.Subcommands.First(c => c.Name == "validate");

        // Assert
        Assert.Contains(validateCommand.Options, o => o.Name == "path");
    }

    [Fact]
    public void PackSubcommand_HasRequiredOptions()
    {
        // Arrange
        var command = new TemplateCommand();
        var packCommand = command.Subcommands.First(c => c.Name == "pack");

        // Assert
        Assert.Contains(packCommand.Options, o => o.Name == "path");
        Assert.Contains(packCommand.Options, o => o.Name == "output");
    }

    [Fact]
    public void PublishSubcommand_HasRequiredOptions()
    {
        // Arrange
        var command = new TemplateCommand();
        var publishCommand = command.Subcommands.First(c => c.Name == "publish");

        // Assert
        Assert.Contains(publishCommand.Options, o => o.Name == "package");
        Assert.Contains(publishCommand.Options, o => o.Name == "registry");
    }

    [Fact]
    public void CreateSubcommand_HasRequiredOptions()
    {
        // Arrange
        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(c => c.Name == "create");

        // Assert
        Assert.Contains(createCommand.Options, o => o.Name == "name");
        Assert.Contains(createCommand.Options, o => o.Name == "from");
        Assert.Contains(createCommand.Options, o => o.Name == "output");
    }

    [Fact]
    public async Task ValidateTemplateAsync_WithInvalidPath_HandlesGracefully()
    {
        // Arrange
        var invalidPath = "/nonexistent/path";

        // Act & Assert - Should not throw
        await TemplateCommand.ValidateTemplateAsync(invalidPath);
    }

    [Fact]
    public async Task PackTemplateAsync_WithInvalidPath_HandlesGracefully()
    {
        // Arrange
        var invalidPath = "/nonexistent/path";
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayTemplatePackTest");

        try
        {
            // Act & Assert - Should not throw
            await TemplateCommand.PackTemplateAsync(invalidPath, tempDir);
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
    public async Task PublishTemplateAsync_WithInvalidPath_HandlesGracefully()
    {
        // Arrange
        var invalidPath = "/nonexistent/path";

        // Act & Assert - Should not throw
        await TemplateCommand.PublishTemplateAsync(invalidPath, "https://example.com");
    }

    [Fact]
    public async Task ListTemplatesAsync_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await TemplateCommand.ListTemplatesAsync();
    }

    [Fact]
    public async Task CreateCustomTemplateAsync_WithInvalidSourcePath_HandlesGracefully()
    {
        // Arrange
        var invalidPath = "/nonexistent/path";
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayTemplateCreateTest");

        try
        {
            // Act & Assert - Should not throw
            await TemplateCommand.CreateCustomTemplateAsync("TestTemplate", invalidPath, tempDir);
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
    public async Task CreateCustomTemplateAsync_WithExistingTemplatePath_HandlesGracefully()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayTemplateCreateExistingTest");
        var templateName = "ExistingTemplate";
        var templatePath = Path.Combine(tempDir, templateName);

        Directory.CreateDirectory(templatePath);

        try
        {
            // Create a minimal source project
            var sourcePath = Path.Combine(tempDir, "SourceProject");
            Directory.CreateDirectory(sourcePath);
            await File.WriteAllTextAsync(Path.Combine(sourcePath, "test.cs"), "public class Test {}");

            // Act & Assert - Should not throw
            await TemplateCommand.CreateCustomTemplateAsync(templateName, sourcePath, tempDir);
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
    public async Task CreateCustomTemplateAsync_WithValidPaths_CreatesTemplateStructure()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayTemplateCreateValidTest");
        var templateName = "ValidTemplate";

        // Create a minimal source project
        var sourcePath = Path.Combine(tempDir, "SourceProject");
        Directory.CreateDirectory(sourcePath);
        await File.WriteAllTextAsync(Path.Combine(sourcePath, "Program.cs"), "Console.WriteLine(\"Hello\");");

        try
        {
            // Act
            await TemplateCommand.CreateCustomTemplateAsync(templateName, sourcePath, tempDir);

            // Assert
            var templatePath = Path.Combine(tempDir, templateName);
            Assert.True(Directory.Exists(templatePath));
            Assert.True(Directory.Exists(Path.Combine(templatePath, ".template.config")));
            Assert.True(Directory.Exists(Path.Combine(templatePath, "content")));
            Assert.True(File.Exists(Path.Combine(templatePath, ".template.config", "template.json")));
            Assert.True(File.Exists(Path.Combine(templatePath, "content", "Program.cs")));
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
    public async Task CreateCustomTemplateAsync_GeneratesValidTemplateJson()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayTemplateJsonTest");
        var templateName = "JsonTemplate";

        // Create a minimal source project
        var sourcePath = Path.Combine(tempDir, "SourceProject");
        Directory.CreateDirectory(sourcePath);
        await File.WriteAllTextAsync(Path.Combine(sourcePath, "test.cs"), "public class Test {}");

        try
        {
            // Act
            await TemplateCommand.CreateCustomTemplateAsync(templateName, sourcePath, tempDir);

            // Assert
            var templateJsonPath = Path.Combine(tempDir, templateName, ".template.config", "template.json");
            var templateJson = await File.ReadAllTextAsync(templateJsonPath);
            Assert.Contains(templateName, templateJson);
            Assert.Contains("Relay.Templates." + templateName, templateJson);
            Assert.Contains("$schema", templateJson);
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
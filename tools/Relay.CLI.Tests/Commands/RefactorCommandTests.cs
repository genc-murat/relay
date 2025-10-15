using Relay.CLI.Commands;
using System.CommandLine;

namespace Relay.CLI.Tests.Commands;

public class RefactorCommandTests
{
    [Fact]
    public void RefactorCommand_Create_ShouldReturnCommand()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<Command>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        command.Name.Should().Be("refactor");
    }

    [Fact]
    public void RefactorCommand_ShouldHaveDescription()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        command.Description.Should().Be("Automated code refactoring and modernization");
    }

    [Fact]
    public void RefactorCommand_ShouldHavePathOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var pathOption = command.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        pathOption.Should().NotBeNull();
        pathOption!.Name.Should().Be("path");
        pathOption.Description.Should().Be("Project path to refactor");
        pathOption.Should().BeOfType<Option<string>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveAnalyzeOnlyOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "analyze-only");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("analyze-only");
        option.Description.Should().Be("Only analyze without applying changes");
        option.Should().BeOfType<Option<bool>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveDryRunOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "dry-run");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("dry-run");
        option.Description.Should().Be("Show changes without applying them");
        option.Should().BeOfType<Option<bool>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveInteractiveOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "interactive");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("interactive");
        option.Description.Should().Be("Prompt for each refactoring");
        option.Should().BeOfType<Option<bool>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveRulesOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "rules");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("rules");
        option.Description.Should().Be("Specific rules to apply");
        option.Should().BeOfType<Option<string[]>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveCategoryOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "category");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("category");
        option.Description.Should().Be("Refactoring categories (Performance, Readability, etc.)");
        option.Should().BeOfType<Option<string[]>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveMinSeverityOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "min-severity");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("min-severity");
        option.Description.Should().Be("Minimum severity (Info, Suggestion, Warning, Error)");
        option.Should().BeOfType<Option<string>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveOutputOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "output");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("output");
        option.Description.Should().Be("Refactoring report output path");
        option.Should().BeOfType<Option<string?>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveFormatOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "format");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("format");
        option.Description.Should().Be("Report format (markdown, json, html)");
        option.Should().BeOfType<Option<string>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveBackupOption()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();
        var option = command.Options.FirstOrDefault(o => o.Name == "backup");

        // Assert
        option.Should().NotBeNull();
        option!.Name.Should().Be("backup");
        option.Description.Should().Be("Create backup before refactoring");
        option.Should().BeOfType<Option<bool>>();
    }

    [Fact]
    public void RefactorCommand_ShouldHaveTenOptions()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        command.Options.Should().HaveCount(10);
    }

    [Fact]
    public void RefactorCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = RefactorCommand.Create();

        // Assert
        command.Handler.Should().NotBeNull();
    }
}
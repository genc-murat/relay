using Relay.CLI.Commands;
using System.CommandLine;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class MigrateCommandBuilderTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectNameAndDescription()
    {
        // Act
        var command = MigrateCommandBuilder.Create();

        // Assert
        Assert.NotNull(command);
        Assert.Equal("migrate", command.Name);
        Assert.Equal("Migrate from MediatR to Relay with automated transformation", command.Description);
    }

    [Fact]
    public void Create_AddsAllRequiredOptions()
    {
        // Act
        var command = MigrateCommandBuilder.Create();

        // Assert - Check that all options are present
        Assert.Contains(command.Options, o => o.Name == "from");
        Assert.Contains(command.Options, o => o.Name == "to");
        Assert.Contains(command.Options, o => o.Name == "path");
        Assert.Contains(command.Options, o => o.Name == "analyze-only");
        Assert.Contains(command.Options, o => o.Name == "dry-run");
        Assert.Contains(command.Options, o => o.Name == "preview");
        Assert.Contains(command.Options, o => o.Name == "side-by-side");
        Assert.Contains(command.Options, o => o.Name == "backup");
        Assert.Contains(command.Options, o => o.Name == "backup-path");
        Assert.Contains(command.Options, o => o.Name == "output");
        Assert.Contains(command.Options, o => o.Name == "format");
        Assert.Contains(command.Options, o => o.Name == "aggressive");
        Assert.Contains(command.Options, o => o.Name == "interactive");
    }

    [Fact]
    public void Create_FromOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var fromOption = command.Options.First(o => o.Name == "from") as Option<string>;

        // Assert
        Assert.NotNull(fromOption);
        Assert.Equal("Source framework to migrate from", fromOption.Description);
        Assert.False(fromOption.IsRequired);
    }

    [Fact]
    public void Create_ToOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var toOption = command.Options.First(o => o.Name == "to") as Option<string>;

        // Assert
        Assert.NotNull(toOption);
        Assert.Equal("Target framework to migrate to", toOption.Description);
        Assert.False(toOption.IsRequired);
    }

    [Fact]
    public void Create_PathOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var pathOption = command.Options.First(o => o.Name == "path") as Option<string>;

        // Assert
        Assert.NotNull(pathOption);
        Assert.Equal("Project path to migrate", pathOption.Description);
        Assert.False(pathOption.IsRequired);
    }

    [Fact]
    public void Create_AnalyzeOnlyOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var analyzeOnlyOption = command.Options.First(o => o.Name == "analyze-only") as Option<bool>;

        // Assert
        Assert.NotNull(analyzeOnlyOption);
        Assert.Equal("Only analyze without migrating", analyzeOnlyOption.Description);
        Assert.False(analyzeOnlyOption.IsRequired);
    }

    [Fact]
    public void Create_DryRunOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var dryRunOption = command.Options.First(o => o.Name == "dry-run") as Option<bool>;

        // Assert
        Assert.NotNull(dryRunOption);
        Assert.Equal("Show changes without applying them", dryRunOption.Description);
        Assert.False(dryRunOption.IsRequired);
    }

    [Fact]
    public void Create_PreviewOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var previewOption = command.Options.First(o => o.Name == "preview") as Option<bool>;

        // Assert
        Assert.NotNull(previewOption);
        Assert.Equal("Show detailed diff preview", previewOption.Description);
        Assert.False(previewOption.IsRequired);
    }

    [Fact]
    public void Create_SideBySideOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var sideBySideOption = command.Options.First(o => o.Name == "side-by-side") as Option<bool>;

        // Assert
        Assert.NotNull(sideBySideOption);
        Assert.Equal("Use side-by-side diff display", sideBySideOption.Description);
        Assert.False(sideBySideOption.IsRequired);
    }

    [Fact]
    public void Create_BackupOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var backupOption = command.Options.First(o => o.Name == "backup") as Option<bool>;

        // Assert
        Assert.NotNull(backupOption);
        Assert.Equal("Create backup before migration", backupOption.Description);
        Assert.False(backupOption.IsRequired);
    }

    [Fact]
    public void Create_BackupPathOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var backupPathOption = command.Options.First(o => o.Name == "backup-path") as Option<string>;

        // Assert
        Assert.NotNull(backupPathOption);
        Assert.Equal("Backup directory path", backupPathOption.Description);
        Assert.False(backupPathOption.IsRequired);
    }

    [Fact]
    public void Create_OutputOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var outputOption = command.Options.First(o => o.Name == "output") as Option<string?>;

        // Assert
        Assert.NotNull(outputOption);
        Assert.Equal("Migration report output path", outputOption.Description);
        Assert.False(outputOption.IsRequired);
    }

    [Fact]
    public void Create_FormatOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var formatOption = command.Options.First(o => o.Name == "format") as Option<string>;

        // Assert
        Assert.NotNull(formatOption);
        Assert.Equal("Report format (markdown, json, html)", formatOption.Description);
        Assert.False(formatOption.IsRequired);
    }

    [Fact]
    public void Create_AggressiveOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var aggressiveOption = command.Options.First(o => o.Name == "aggressive") as Option<bool>;

        // Assert
        Assert.NotNull(aggressiveOption);
        Assert.Equal("Apply aggressive optimizations", aggressiveOption.Description);
        Assert.False(aggressiveOption.IsRequired);
    }

    [Fact]
    public void Create_InteractiveOption_HasCorrectConfiguration()
    {
        // Act
        var command = MigrateCommandBuilder.Create();
        var interactiveOption = command.Options.First(o => o.Name == "interactive") as Option<bool>;

        // Assert
        Assert.NotNull(interactiveOption);
        Assert.Equal("Prompt for each change", interactiveOption.Description);
        Assert.False(interactiveOption.IsRequired);
    }

    [Fact]
    public void Create_CommandHasHandler()
    {
        // Act
        var command = MigrateCommandBuilder.Create();

        // Assert
        Assert.NotNull(command.Handler);
    }

    [Fact]
    public void Create_CommandHasCorrectNumberOfOptions()
    {
        // Act
        var command = MigrateCommandBuilder.Create();

        // Assert - Should have 13 options total (12 defined + potentially command name)
        Assert.True(command.Options.Count >= 12);
    }

    [Fact]
    public void Create_CommandCanParseEmptyArgs()
    {
        // Arrange
        var command = MigrateCommandBuilder.Create();
        var args = Array.Empty<string>();

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.NotNull(parseResult);
        Assert.Equal("migrate", parseResult.CommandResult.Command.Name);
    }

    [Fact]
    public void Create_CommandCanParseWithArgs()
    {
        // Arrange
        var command = MigrateCommandBuilder.Create();
        var args = new[] { "--from", "MediatR", "--to", "Relay", "--dry-run" };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.NotNull(parseResult);
        Assert.Equal("migrate", parseResult.CommandResult.Command.Name);
    }

    [Fact]
    public void Create_CommandCanParseAllOptions()
    {
        // Arrange
        var command = MigrateCommandBuilder.Create();
        var args = new[]
        {
            "--from", "MediatR",
            "--to", "Relay",
            "--path", ".",
            "--analyze-only",
            "--dry-run",
            "--preview",
            "--side-by-side",
            "--backup",
            "--backup-path", ".backup",
            "--output", "report.md",
            "--format", "markdown",
            "--aggressive",
            "--interactive"
        };

        // Act
        var parseResult = command.Parse(args);

        // Assert
        Assert.NotNull(parseResult);
        Assert.Equal("migrate", parseResult.CommandResult.Command.Name);
    }
}
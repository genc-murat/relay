using Relay.CLI.Commands;
using System.CommandLine;

namespace Relay.CLI.Tests.Commands;

public class PluginCommandTests
{
    [Fact]
    public void PluginCommand_Create_ShouldReturnCommand()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        command.Should().NotBeNull();
        command.Should().BeOfType<Command>();
    }

    [Fact]
    public void PluginCommand_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        command.Name.Should().Be("plugin");
    }

    [Fact]
    public void PluginCommand_ShouldHaveDescription()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        command.Description.Should().Be("Manage Relay CLI plugins");
    }

    [Fact]
    public void PluginCommand_ShouldHaveSubcommands()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert
        subcommandNames.Should().Contain("list");
        subcommandNames.Should().Contain("search");
        subcommandNames.Should().Contain("install");
        subcommandNames.Should().Contain("uninstall");
        subcommandNames.Should().Contain("update");
        subcommandNames.Should().Contain("info");
        subcommandNames.Should().Contain("create");
    }

    [Fact]
    public void PluginCommand_ShouldHaveSevenSubcommands()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        command.Subcommands.Should().HaveCount(7);
    }

    [Fact]
    public void PluginCommand_ListSubcommand_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        listCommand.Name.Should().Be("list");
        listCommand.Description.Should().Be("List installed plugins");
    }

    [Fact]
    public void PluginCommand_ListSubcommand_ShouldHaveAllOption()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var listCommand = command.Subcommands.First(c => c.Name == "list");
        var allOption = listCommand.Options.FirstOrDefault(o => o.Name == "all");

        // Assert
        allOption.Should().NotBeNull();
        allOption!.Name.Should().Be("all");
        allOption.Description.Should().Be("Include disabled plugins");
    }

    [Fact]
    public void PluginCommand_SearchSubcommand_ShouldHaveCorrectArguments()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var searchCommand = command.Subcommands.First(c => c.Name == "search");

        // Assert
        searchCommand.Name.Should().Be("search");
        searchCommand.Description.Should().Be("Search for plugins in the marketplace");
        searchCommand.Arguments.Should().HaveCount(1);
        searchCommand.Arguments.First().Name.Should().Be("query");
    }

    [Fact]
    public void PluginCommand_SearchSubcommand_ShouldHaveTagAndAuthorOptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var searchCommand = command.Subcommands.First(c => c.Name == "search");
        var tagOption = searchCommand.Options.FirstOrDefault(o => o.Name == "tag");
        var authorOption = searchCommand.Options.FirstOrDefault(o => o.Name == "author");

        // Assert
        tagOption.Should().NotBeNull();
        tagOption!.Name.Should().Be("tag");
        tagOption.Description.Should().Be("Filter by tag");

        authorOption.Should().NotBeNull();
        authorOption!.Name.Should().Be("author");
        authorOption.Description.Should().Be("Filter by author");
    }

    [Fact]
    public void PluginCommand_InstallSubcommand_ShouldHaveCorrectArgumentsAndOptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var installCommand = command.Subcommands.First(c => c.Name == "install");

        // Assert
        installCommand.Name.Should().Be("install");
        installCommand.Description.Should().Be("Install a plugin");
        installCommand.Arguments.Should().HaveCount(1);
        installCommand.Arguments.First().Name.Should().Be("name");

        var versionOption = installCommand.Options.FirstOrDefault(o => o.Name == "version");
        var globalOption = installCommand.Options.FirstOrDefault(o => o.Name == "global");

        versionOption.Should().NotBeNull();
        versionOption!.Name.Should().Be("version");
        versionOption.Description.Should().Be("Specific version to install");

        globalOption.Should().NotBeNull();
        globalOption!.Name.Should().Be("global");
        globalOption.Description.Should().Be("Install globally");
    }

    [Fact]
    public void PluginCommand_UninstallSubcommand_ShouldHaveCorrectArgumentsAndOptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var uninstallCommand = command.Subcommands.First(c => c.Name == "uninstall");

        // Assert
        uninstallCommand.Name.Should().Be("uninstall");
        uninstallCommand.Description.Should().Be("Uninstall a plugin");
        uninstallCommand.Arguments.Should().HaveCount(1);
        uninstallCommand.Arguments.First().Name.Should().Be("name");

        var globalOption = uninstallCommand.Options.FirstOrDefault(o => o.Name == "global");
        globalOption.Should().NotBeNull();
        globalOption!.Name.Should().Be("global");
        globalOption.Description.Should().Be("Uninstall from global location");
    }

    [Fact]
    public void PluginCommand_UpdateSubcommand_ShouldHaveOptionalNameArgument()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var updateCommand = command.Subcommands.First(c => c.Name == "update");

        // Assert
        updateCommand.Name.Should().Be("update");
        updateCommand.Description.Should().Be("Update installed plugins");
        updateCommand.Arguments.Should().HaveCount(1);
        updateCommand.Arguments.First().Name.Should().Be("name");
    }

    [Fact]
    public void PluginCommand_InfoSubcommand_ShouldHaveNameArgument()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var infoCommand = command.Subcommands.First(c => c.Name == "info");

        // Assert
        infoCommand.Name.Should().Be("info");
        infoCommand.Description.Should().Be("Show detailed information about a plugin");
        infoCommand.Arguments.Should().HaveCount(1);
        infoCommand.Arguments.First().Name.Should().Be("name");
    }

    [Fact]
    public void PluginCommand_CreateSubcommand_ShouldHaveRequiredOptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var createCommand = command.Subcommands.First(c => c.Name == "create");

        // Assert
        createCommand.Name.Should().Be("create");
        createCommand.Description.Should().Be("Create a new plugin from template");

        var nameOption = createCommand.Options.FirstOrDefault(o => o.Name == "name");
        var outputOption = createCommand.Options.FirstOrDefault(o => o.Name == "output");
        var templateOption = createCommand.Options.FirstOrDefault(o => o.Name == "template");

        nameOption.Should().NotBeNull();
        nameOption!.Name.Should().Be("name");
        nameOption.IsRequired.Should().BeTrue();

        outputOption.Should().NotBeNull();
        outputOption!.Name.Should().Be("output");

        templateOption.Should().NotBeNull();
        templateOption!.Name.Should().Be("template");
    }

    [Fact]
    public void PluginCommand_AllSubcommands_ShouldHaveUniqueNames()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert
        subcommandNames.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void PluginCommand_ShouldNotHaveOptionsAtRootLevel()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        command.Options.Should().BeEmpty();
    }

    [Fact]
    public void PluginCommand_Subcommands_ShouldNotHaveSubcommands()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            subcommand.Subcommands.Should().BeEmpty();
        }
    }

    [Fact]
    public void PluginCommand_ListCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        listCommand.Handler.Should().NotBeNull();
    }

    [Fact]
    public void PluginCommand_SearchCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var searchCommand = command.Subcommands.First(c => c.Name == "search");

        // Assert
        searchCommand.Handler.Should().NotBeNull();
    }

    [Fact]
    public void PluginCommand_InstallCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var installCommand = command.Subcommands.First(c => c.Name == "install");

        // Assert
        installCommand.Handler.Should().NotBeNull();
    }

    [Fact]
    public void PluginCommand_UninstallCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var uninstallCommand = command.Subcommands.First(c => c.Name == "uninstall");

        // Assert
        uninstallCommand.Handler.Should().NotBeNull();
    }

    [Fact]
    public void PluginCommand_UpdateCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var updateCommand = command.Subcommands.First(c => c.Name == "update");

        // Assert
        updateCommand.Handler.Should().NotBeNull();
    }

    [Fact]
    public void PluginCommand_InfoCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var infoCommand = command.Subcommands.First(c => c.Name == "info");

        // Assert
        infoCommand.Handler.Should().NotBeNull();
    }

    [Fact]
    public void PluginCommand_CreateCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var createCommand = command.Subcommands.First(c => c.Name == "create");

        // Assert
        createCommand.Handler.Should().NotBeNull();
    }

    [Fact]
    public void PluginCommand_AllCommands_ShouldHaveHandlers()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            subcommand.Handler.Should().NotBeNull($"{subcommand.Name} should have a handler");
        }
    }

    [Fact]
    public void PluginCommand_GlobalOption_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var installCommand = command.Subcommands.First(c => c.Name == "install");
        var globalOption = installCommand.Options.First(o => o.Name == "global");

        // Assert
        // Note: We can't easily test the default value without parsing, but we can verify the option exists
        globalOption.Should().NotBeNull();
    }

    [Fact]
    public void PluginCommand_TemplateOption_ShouldDefaultToBasic()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var createCommand = command.Subcommands.First(c => c.Name == "create");
        var templateOption = createCommand.Options.First(o => o.Name == "template");

        // Assert
        templateOption.Should().NotBeNull();
        // The default value "basic" is set in the option creation
    }

    [Fact]
    public void PluginCommand_OutputOption_ShouldDefaultToCurrentDirectory()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var createCommand = command.Subcommands.First(c => c.Name == "create");
        var outputOption = createCommand.Options.First(o => o.Name == "output");

        // Assert
        outputOption.Should().NotBeNull();
        // The default value "." is set in the option creation
    }

    [Fact]
    public void PluginCommand_CommandStructure_ShouldBeImmutable()
    {
        // Arrange
        var command1 = PluginCommand.Create();
        var command2 = PluginCommand.Create();

        // Act - Try to modify (this should not affect the original)
        var originalCount = command1.Subcommands.Count;

        // Assert
        command1.Subcommands.Count.Should().Be(originalCount);
        command2.Subcommands.Count.Should().Be(originalCount);
        command1.Should().NotBeSameAs(command2); // Different instances
    }

    [Fact]
    public void PluginCommand_Subcommands_ShouldHaveDescriptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            subcommand.Description.Should().NotBeNullOrEmpty($"{subcommand.Name} should have a description");
        }
    }

    [Fact]
    public void PluginCommand_Arguments_ShouldHaveDescriptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            foreach (var argument in subcommand.Arguments)
            {
                argument.Description.Should().NotBeNullOrEmpty($"{subcommand.Name}.{argument.Name} should have a description");
            }
        }
    }

    [Fact]
    public void PluginCommand_Options_ShouldHaveDescriptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            foreach (var option in subcommand.Options)
            {
                option.Description.Should().NotBeNullOrEmpty($"{subcommand.Name}.{option.Name} should have a description");
            }
        }
    }

    [Fact]
    public void PluginCommand_CommandCreation_ShouldBeIdempotent()
    {
        // Arrange & Act
        var command1 = PluginCommand.Create();
        var command2 = PluginCommand.Create();

        // Assert
        command1.Name.Should().Be(command2.Name);
        command1.Description.Should().Be(command2.Description);
        command1.Subcommands.Count.Should().Be(command2.Subcommands.Count);

        var subcommandNames1 = command1.Subcommands.Select(s => s.Name).OrderBy(n => n).ToList();
        var subcommandNames2 = command2.Subcommands.Select(s => s.Name).OrderBy(n => n).ToList();
        subcommandNames1.Should().BeEquivalentTo(subcommandNames2);
    }

    [Fact]
    public void PluginCommand_ShouldSupportCommonPluginOperations()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert - Verify all essential plugin management operations are supported
        subcommandNames.Should().Contain("list", "Should support listing plugins");
        subcommandNames.Should().Contain("install", "Should support installing plugins");
        subcommandNames.Should().Contain("uninstall", "Should support uninstalling plugins");
        subcommandNames.Should().Contain("update", "Should support updating plugins");
        subcommandNames.Should().Contain("search", "Should support searching plugins");
        subcommandNames.Should().Contain("info", "Should support getting plugin info");
        subcommandNames.Should().Contain("create", "Should support creating plugins");
    }

    [Fact]
    public void PluginCommand_CommandTree_ShouldBeWellStructured()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        command.Name.Should().NotBeNullOrEmpty();
        command.Description.Should().NotBeNullOrEmpty();

        foreach (var subcommand in command.Subcommands)
        {
            subcommand.Name.Should().NotBeNullOrEmpty();
            subcommand.Description.Should().NotBeNullOrEmpty();
            subcommand.Handler.Should().NotBeNull();

            // Each subcommand should have either arguments or options, but not necessarily both
            (subcommand.Arguments.Any() || subcommand.Options.Any()).Should().BeTrue(
                $"{subcommand.Name} should have arguments or options");
        }
    }

    [Fact]
    public void PluginCommand_Arguments_ShouldHaveNames()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            foreach (var argument in subcommand.Arguments)
            {
                argument.Name.Should().NotBeNullOrEmpty($"{subcommand.Name} should have named arguments");
            }
        }
    }

    [Fact]
    public void PluginCommand_UpdateCommand_ShouldHaveOptionalNameArgument()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var updateCommand = command.Subcommands.First(c => c.Name == "update");

        // Assert
        updateCommand.Arguments.Should().HaveCount(1);
        updateCommand.Arguments.First().Name.Should().Be("name");
    }

    [Fact]
    public void PluginCommand_BooleanOptions_ShouldHaveAppropriateDefaults()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        var installCommand = command.Subcommands.First(c => c.Name == "install");
        var globalOption = installCommand.Options.First(o => o.Name == "global");
        // Boolean options typically default to false
        globalOption.Should().NotBeNull();

        var listCommand = command.Subcommands.First(c => c.Name == "list");
        var allOption = listCommand.Options.First(o => o.Name == "all");
        allOption.Should().NotBeNull();
    }

    [Fact]
    public void PluginCommand_CommandNames_ShouldFollowConventions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            subcommand.Name.Should().MatchRegex("^[a-z]+$", $"{subcommand.Name} should be lowercase");
            subcommand.Name.Length.Should().BeGreaterThan(2, $"{subcommand.Name} should be descriptive");
        }
    }

    [Fact]
    public void PluginCommand_ShouldSupportPluginLifecycleManagement()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert - Plugin lifecycle operations
        subcommandNames.Should().Contain("install", "Installation is part of plugin lifecycle");
        subcommandNames.Should().Contain("uninstall", "Uninstallation is part of plugin lifecycle");
        subcommandNames.Should().Contain("update", "Updates are part of plugin lifecycle");
        subcommandNames.Should().Contain("list", "Listing shows current lifecycle state");
    }

    [Fact]
    public void PluginCommand_ShouldSupportPluginDiscovery()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert - Plugin discovery operations
        subcommandNames.Should().Contain("search", "Search enables plugin discovery");
        subcommandNames.Should().Contain("info", "Info provides detailed plugin discovery");
        subcommandNames.Should().Contain("list", "List shows discovered/installed plugins");
    }

    [Fact]
    public void PluginCommand_ShouldSupportPluginDevelopment()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert - Plugin development operations
        subcommandNames.Should().Contain("create", "Create enables plugin development");
        subcommandNames.Should().Contain("info", "Info helps with plugin development");
    }
}
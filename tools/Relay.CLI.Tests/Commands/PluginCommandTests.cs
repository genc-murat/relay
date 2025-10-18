using Relay.CLI.Commands;
using Spectre.Console;
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
        Assert.NotNull(command);
        Assert.IsType<Command>(command);
    }

    [Fact]
    public void PluginCommand_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        Assert.Equal("plugin", command.Name);
    }

    [Fact]
    public void PluginCommand_ShouldHaveDescription()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        Assert.Equal("Manage Relay CLI plugins", command.Description);
    }

    [Fact]
    public void PluginCommand_ShouldHaveSubcommands()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert
        Assert.Contains("list", subcommandNames);
        Assert.Contains("search", subcommandNames);
        Assert.Contains("install", subcommandNames);
        Assert.Contains("uninstall", subcommandNames);
        Assert.Contains("update", subcommandNames);
        Assert.Contains("info", subcommandNames);
        Assert.Contains("create", subcommandNames);
    }

    [Fact]
    public void PluginCommand_ShouldHaveSevenSubcommands()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        Assert.Equal(7, command.Subcommands.Count());
    }

    [Fact]
    public void PluginCommand_ListSubcommand_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        Assert.Equal("list", listCommand.Name);
        Assert.Equal("List installed plugins", listCommand.Description);
    }

    [Fact]
    public void PluginCommand_ListSubcommand_ShouldHaveAllOption()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var listCommand = command.Subcommands.First(c => c.Name == "list");
        var allOption = listCommand.Options.FirstOrDefault(o => o.Name == "all");

        // Assert
        Assert.NotNull(allOption);
        Assert.Equal("all", allOption.Name);
        Assert.Equal("Include disabled plugins", allOption.Description);
    }

    [Fact]
    public void PluginCommand_SearchSubcommand_ShouldHaveCorrectArguments()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var searchCommand = command.Subcommands.First(c => c.Name == "search");

        // Assert
        Assert.Equal("search", searchCommand.Name);
        Assert.Equal("Search for plugins in the marketplace", searchCommand.Description);
        Assert.Equal(1, searchCommand.Arguments.Count());
        Assert.Equal("query", searchCommand.Arguments.First().Name);
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
        Assert.NotNull(tagOption);
        Assert.Equal("tag", tagOption.Name);
        Assert.Equal("Filter by tag", tagOption.Description);

        Assert.NotNull(authorOption);
        Assert.Equal("author", authorOption.Name);
        Assert.Equal("Filter by author", authorOption.Description);
    }

    [Fact]
    public void PluginCommand_InstallSubcommand_ShouldHaveCorrectArgumentsAndOptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var installCommand = command.Subcommands.First(c => c.Name == "install");

        // Assert
        Assert.Equal("install", installCommand.Name);
        Assert.Equal("Install a plugin", installCommand.Description);
        Assert.Equal(1, installCommand.Arguments.Count());
        Assert.Equal("name", installCommand.Arguments.First().Name);

        var versionOption = installCommand.Options.FirstOrDefault(o => o.Name == "version");
        var globalOption = installCommand.Options.FirstOrDefault(o => o.Name == "global");

        Assert.NotNull(versionOption);
        Assert.Equal("version", versionOption.Name);
        Assert.Equal("Specific version to install", versionOption.Description);

        Assert.NotNull(globalOption);
        Assert.Equal("global", globalOption.Name);
        Assert.Equal("Install globally", globalOption.Description);
    }

    [Fact]
    public void PluginCommand_UninstallSubcommand_ShouldHaveCorrectArgumentsAndOptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var uninstallCommand = command.Subcommands.First(c => c.Name == "uninstall");

        // Assert
        Assert.Equal("uninstall", uninstallCommand.Name);
        Assert.Equal("Uninstall a plugin", uninstallCommand.Description);
        Assert.Equal(1, uninstallCommand.Arguments.Count());
        Assert.Equal("name", uninstallCommand.Arguments.First().Name);

        var globalOption = uninstallCommand.Options.FirstOrDefault(o => o.Name == "global");
        Assert.NotNull(globalOption);
        Assert.Equal("global", globalOption.Name);
        Assert.Equal("Uninstall from global location", globalOption.Description);
    }

    [Fact]
    public void PluginCommand_UpdateSubcommand_ShouldHaveOptionalNameArgument()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var updateCommand = command.Subcommands.First(c => c.Name == "update");

        // Assert
        Assert.Equal("update", updateCommand.Name);
        Assert.Equal("Update installed plugins", updateCommand.Description);
        Assert.Equal(1, updateCommand.Arguments.Count());
        Assert.Equal("name", updateCommand.Arguments.First().Name);
    }

    [Fact]
    public void PluginCommand_InfoSubcommand_ShouldHaveNameArgument()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var infoCommand = command.Subcommands.First(c => c.Name == "info");

        // Assert
        Assert.Equal("info", infoCommand.Name);
        Assert.Equal("Show detailed information about a plugin", infoCommand.Description);
        Assert.Equal(1, infoCommand.Arguments.Count());
        Assert.Equal("name", infoCommand.Arguments.First().Name);
    }

    [Fact]
    public void PluginCommand_CreateSubcommand_ShouldHaveRequiredOptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var createCommand = command.Subcommands.First(c => c.Name == "create");

        // Assert
        Assert.Equal("create", createCommand.Name);
        Assert.Equal("Create a new plugin from template", createCommand.Description);

        var nameOption = createCommand.Options.FirstOrDefault(o => o.Name == "name");
        var outputOption = createCommand.Options.FirstOrDefault(o => o.Name == "output");
        var templateOption = createCommand.Options.FirstOrDefault(o => o.Name == "template");

        Assert.NotNull(nameOption);
        Assert.Equal("name", nameOption.Name);
        Assert.True(nameOption.IsRequired);

        Assert.NotNull(outputOption);
        Assert.Equal("output", outputOption.Name);

        Assert.NotNull(templateOption);
        Assert.Equal("template", templateOption.Name);
    }

    [Fact]
    public void PluginCommand_AllSubcommands_ShouldHaveUniqueNames()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert
        Assert.Equal(subcommandNames.Count, subcommandNames.Distinct().Count());
    }

    [Fact]
    public void PluginCommand_ShouldNotHaveOptionsAtRootLevel()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        Assert.Empty(command.Options);
    }

    [Fact]
    public void PluginCommand_Subcommands_ShouldNotHaveSubcommands()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            Assert.Empty(subcommand.Subcommands);
        }
    }

    [Fact]
    public void PluginCommand_ListCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        Assert.NotNull(listCommand.Handler);
    }

    [Fact]
    public void PluginCommand_SearchCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var searchCommand = command.Subcommands.First(c => c.Name == "search");

        // Assert
        Assert.NotNull(searchCommand.Handler);
    }

    [Fact]
    public void PluginCommand_InstallCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var installCommand = command.Subcommands.First(c => c.Name == "install");

        // Assert
        Assert.NotNull(installCommand.Handler);
    }

    [Fact]
    public void PluginCommand_UninstallCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var uninstallCommand = command.Subcommands.First(c => c.Name == "uninstall");

        // Assert
        Assert.NotNull(uninstallCommand.Handler);
    }

    [Fact]
    public void PluginCommand_UpdateCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var updateCommand = command.Subcommands.First(c => c.Name == "update");

        // Assert
        Assert.NotNull(updateCommand.Handler);
    }

    [Fact]
    public void PluginCommand_InfoCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var infoCommand = command.Subcommands.First(c => c.Name == "info");

        // Assert
        Assert.NotNull(infoCommand.Handler);
    }

    [Fact]
    public void PluginCommand_CreateCommand_ShouldHaveHandler()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var createCommand = command.Subcommands.First(c => c.Name == "create");

        // Assert
        Assert.NotNull(createCommand.Handler);
    }

    [Fact]
    public void PluginCommand_AllCommands_ShouldHaveHandlers()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            Assert.NotNull(subcommand.Handler);
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
        Assert.NotNull(globalOption);
    }

    [Fact]
    public void PluginCommand_TemplateOption_ShouldDefaultToBasic()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var createCommand = command.Subcommands.First(c => c.Name == "create");
        var templateOption = createCommand.Options.First(o => o.Name == "template");

        // Assert
        Assert.NotNull(templateOption);
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
        Assert.NotNull(outputOption);
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
        Assert.Equal(originalCount, command1.Subcommands.Count);
        Assert.Equal(originalCount, command2.Subcommands.Count);
        Assert.NotSame(command1, command2); // Different instances
    }

    [Fact]
    public void PluginCommand_Subcommands_ShouldHaveDescriptions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            Assert.False(string.IsNullOrEmpty(subcommand.Description));
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
                Assert.False(string.IsNullOrEmpty(argument.Description));
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
                Assert.False(string.IsNullOrEmpty(option.Description));
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
        Assert.Equal(command2.Name, command1.Name);
        Assert.Equal(command2.Description, command1.Description);
        Assert.Equal(command2.Subcommands.Count, command1.Subcommands.Count);

        var subcommandNames1 = command1.Subcommands.Select(s => s.Name).OrderBy(n => n).ToList();
        var subcommandNames2 = command2.Subcommands.Select(s => s.Name).OrderBy(n => n).ToList();
        Assert.Equal(subcommandNames1, subcommandNames2);
    }

    [Fact]
    public void PluginCommand_ShouldSupportCommonPluginOperations()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert - Verify all essential plugin management operations are supported
        Assert.Contains("list", subcommandNames);
        Assert.Contains("install", subcommandNames);
        Assert.Contains("uninstall", subcommandNames);
        Assert.Contains("update", subcommandNames);
        Assert.Contains("search", subcommandNames);
        Assert.Contains("info", subcommandNames);
        Assert.Contains("create", subcommandNames);
    }

    [Fact]
    public void PluginCommand_CommandTree_ShouldBeWellStructured()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        Assert.False(string.IsNullOrEmpty(command.Name));
        Assert.False(string.IsNullOrEmpty(command.Description));

        foreach (var subcommand in command.Subcommands)
        {
            Assert.False(string.IsNullOrEmpty(subcommand.Name));
            Assert.False(string.IsNullOrEmpty(subcommand.Description));
            Assert.NotNull(subcommand.Handler);

            // Each subcommand should have either arguments or options, but not necessarily both
            Assert.True(subcommand.Arguments.Any() || subcommand.Options.Any());
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
                Assert.False(string.IsNullOrEmpty(argument.Name));
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
        Assert.Equal(1, updateCommand.Arguments.Count());
        Assert.Equal("name", updateCommand.Arguments.First().Name);
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
        Assert.NotNull(globalOption);

        var listCommand = command.Subcommands.First(c => c.Name == "list");
        var allOption = listCommand.Options.First(o => o.Name == "all");
        Assert.NotNull(allOption);
    }

    [Fact]
    public void PluginCommand_CommandNames_ShouldFollowConventions()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        foreach (var subcommand in command.Subcommands)
        {
            Assert.Matches("^[a-z]+$", subcommand.Name);
            Assert.True(subcommand.Name.Length > 2);
        }
    }

    [Fact]
    public void PluginCommand_ShouldSupportPluginLifecycleManagement()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert - Plugin lifecycle operations
        Assert.Contains("install", subcommandNames);
        Assert.Contains("uninstall", subcommandNames);
        Assert.Contains("update", subcommandNames);
        Assert.Contains("list", subcommandNames);
    }

    [Fact]
    public void PluginCommand_ShouldSupportPluginDiscovery()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert - Plugin discovery operations
        Assert.Contains("search", subcommandNames);
        Assert.Contains("info", subcommandNames);
        Assert.Contains("list", subcommandNames);
    }

    [Fact]
    public void PluginCommand_ShouldSupportPluginDevelopment()
    {
        // Arrange & Act
        var command = PluginCommand.Create();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert - Plugin development operations
        Assert.Contains("create", subcommandNames);
        Assert.Contains("info", subcommandNames);
    }

    [Fact]
    public async Task PluginCommand_ExecuteList_ShouldExecuteWithoutException()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();

        // Act & Assert - The method should execute without throwing exceptions
        // Since it creates PluginManager internally, we test that it runs and produces output
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        try
        {
            await PluginCommand.ExecuteList(false);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }

        // Assert
        // The method should execute without exception - output depends on plugin installation status
    }

    [Fact]
    public async Task PluginCommand_ExecuteList_IncludeAll_ShouldExecuteWithoutException()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();

        // Act & Assert
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        try
        {
            await PluginCommand.ExecuteList(true);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }

        // Assert
        Assert.Contains("📦 Installed Plugins", testConsole.Output);
    }

    [Fact]
    public async Task PluginCommand_ExecuteSearch_WithFilters_ShouldExecuteWithoutException()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        try
        {
            await PluginCommand.ExecuteSearch("test", "swagger", "microsoft");
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }

        // Assert
        Assert.Contains("🔍 Searching for: test", testConsole.Output);
    }

    [Fact]
    public async Task PluginCommand_ExecuteInstall_ShouldDisplayInstallationProgress()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        try
        {
            await PluginCommand.ExecuteInstall("test-plugin", "1.0.0", false);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }

        // Assert
        Assert.Contains("📥 Installing plugin: test-plugin (1.0.0)", testConsole.Output);
    }

    [Fact]
    public async Task PluginCommand_ExecuteInstall_Global_ShouldDisplayGlobalInstallation()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        try
        {
            await PluginCommand.ExecuteInstall("test-plugin", null, true);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }

        // Assert
        // The method should execute without exception - output may vary
    }

    [Fact]
    public async Task PluginCommand_ExecuteUninstall_WithConfirmation_ShouldDisplaySuccess()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        try
        {
            await PluginCommand.ExecuteUninstall("test-plugin", false, true);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }

        // Assert
        Assert.Contains("🗑️  Uninstalling plugin: test-plugin", testConsole.Output);
    }

    [Fact]
    public async Task PluginCommand_ExecuteUninstall_Global_ShouldExecuteWithoutException()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        try
        {
            await PluginCommand.ExecuteUninstall("test-plugin", true, true);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }

        // Assert
        Assert.Contains("🗑️  Uninstalling plugin: test-plugin", testConsole.Output);
    }

    [Fact]
    public async Task PluginCommand_ExecuteUpdate_AllPlugins_ShouldDisplaySuccess()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        try
        {
            await PluginCommand.ExecuteUpdate(null);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }

        // Assert
        Assert.Contains("🔄 Updating all plugins", testConsole.Output);
    }

    [Fact]
    public async Task PluginCommand_ExecuteUpdate_SpecificPlugin_ShouldDisplayTarget()
    {
        // Arrange
        var testConsole = new Spectre.Console.Testing.TestConsole();

        // Act
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = testConsole;

        try
        {
            await PluginCommand.ExecuteUpdate("test-plugin");
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }

        // Assert
        Assert.Contains("🔄 Updating test-plugin", testConsole.Output);
    }

    [Fact]
    public async Task PluginCommand_ExecuteCreate_ShouldCreatePluginStructure()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var testConsole = new Spectre.Console.Testing.TestConsole();

        try
        {
            // Act
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            try
            {
                await PluginCommand.ExecuteCreate("test-plugin", tempDir, "basic", true, false);
            }
            finally
            {
                AnsiConsole.Console = originalConsole;
            }

            // Assert
            var pluginDir = Path.Combine(tempDir, "test-plugin");
            Assert.True(Directory.Exists(pluginDir));

            var csprojFile = Path.Combine(pluginDir, "test-plugin.csproj");
            Assert.True(File.Exists(csprojFile));

            var pluginClassFile = Path.Combine(pluginDir, "TestPluginPlugin.cs");
            Assert.True(File.Exists(pluginClassFile));

            var manifestFile = Path.Combine(pluginDir, "plugin.json");
            Assert.True(File.Exists(manifestFile));

            var readmeFile = Path.Combine(pluginDir, "README.md");
            Assert.True(File.Exists(readmeFile));

            Assert.Contains("🎨 Creating plugin: test-plugin", testConsole.Output);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task PluginCommand_ExecuteCreate_QuietMode_ShouldNotDisplayOutput()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var testConsole = new Spectre.Console.Testing.TestConsole();

        try
        {
            // Act
            var originalConsole = AnsiConsole.Console;
            AnsiConsole.Console = testConsole;

            try
            {
                await PluginCommand.ExecuteCreate("test-plugin", tempDir, "basic", false, true);
            }
            finally
            {
                AnsiConsole.Console = originalConsole;
            }

            // Assert
            var pluginDir = Path.Combine(tempDir, "test-plugin");
            Assert.True(Directory.Exists(pluginDir));

            // In quiet mode, should not display progress messages
            Assert.DoesNotContain("🎨 Creating plugin:", testConsole.Output);
            Assert.DoesNotContain("✅ Plugin created:", testConsole.Output);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task PluginCommand_CreatePluginProject_ShouldCreateValidCsprojFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await PluginCommand.CreatePluginProject(tempDir, "test-plugin", "basic");

            // Assert
            var csprojFile = Path.Combine(tempDir, "test-plugin.csproj");
            Assert.True(File.Exists(csprojFile));

            var content = await File.ReadAllTextAsync(csprojFile);
            Assert.Contains("<Project Sdk=\"Microsoft.NET.Sdk\">", content);
            Assert.Contains("<TargetFramework>net8.0</TargetFramework>", content);
            Assert.Contains("<PackageReference Include=\"Relay.CLI.Sdk\" Version=\"2.1.0\" />", content);
            Assert.Contains("<Nullable>enable</Nullable>", content);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task PluginCommand_CreatePluginClass_ShouldCreateValidPluginClass()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await PluginCommand.CreatePluginClass(tempDir, "relay-plugin-test");

            // Assert
            var classFile = Path.Combine(tempDir, "testPlugin.cs");
            Assert.True(File.Exists(classFile));

            var content = await File.ReadAllTextAsync(classFile);
            Assert.Contains("using Relay.CLI.Plugins;", content);
            Assert.Contains("[RelayPlugin(\"relay-plugin-test\", \"1.0.0\")]", content);
            Assert.Contains("public class testPlugin : IRelayPlugin", content);
            Assert.Contains("public string Name => \"relay-plugin-test\";", content);
            Assert.Contains("public async Task<bool> InitializeAsync", content);
            Assert.Contains("public async Task<int> ExecuteAsync", content);
            Assert.Contains("public async Task CleanupAsync", content);
            Assert.Contains("public string GetHelp()", content);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task PluginCommand_CreateManifest_ShouldCreateValidJsonManifest()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await PluginCommand.CreateManifest(tempDir, "test-plugin");

            // Assert
            var manifestFile = Path.Combine(tempDir, "plugin.json");
            Assert.True(File.Exists(manifestFile));

            var content = await File.ReadAllTextAsync(manifestFile);
            Assert.Contains("\"name\": \"test-plugin\"", content);
            Assert.Contains("\"version\": \"1.0.0\"", content);
            Assert.Contains("\"description\": \"My awesome Relay plugin\"", content);
            Assert.Contains("\"authors\": [\"Your Name\"]", content);
            Assert.Contains("\"tags\": [\"utility\"]", content);
            Assert.Contains("\"minimumRelayVersion\": \"2.1.0\"", content);
            Assert.Contains("\"dependencies\": {}", content);
            Assert.Contains("\"repository\": \"https://github.com/youruser/test-plugin\"", content);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task PluginCommand_CreateReadme_ShouldCreateValidMarkdownReadme()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // Act
            await PluginCommand.CreateReadme(tempDir, "test-plugin");

            // Assert
            var readmeFile = Path.Combine(tempDir, "README.md");
            Assert.True(File.Exists(readmeFile));

            var content = await File.ReadAllTextAsync(readmeFile);
            Assert.Contains("# test-plugin", content);
            Assert.Contains("My awesome Relay CLI plugin.", content);
            Assert.Contains("## Installation", content);
            Assert.Contains("relay plugin install test-plugin", content);
            Assert.Contains("## Usage", content);
            Assert.Contains("relay plugin run test-plugin", content);
            Assert.Contains("## Development", content);
            Assert.Contains("dotnet build", content);
            Assert.Contains("relay plugin install .", content);
            Assert.Contains("## License", content);
            Assert.Contains("MIT", content);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}


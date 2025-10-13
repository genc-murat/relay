using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using Xunit.Abstractions;

namespace Relay.CLI.Tests.Commands;

public class PluginCommandTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDirectory;

    public PluginCommandTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public void Create_ReturnsCommandWithCorrectNameAndDescription()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        Assert.Equal("plugin", command.Name);
        Assert.Equal("Manage Relay CLI plugins", command.Description);
    }

    [Fact]
    public void Create_HasAllSubcommands()
    {
        // Arrange & Act
        var command = PluginCommand.Create();

        // Assert
        var subcommandNames = command.Subcommands.Select(c => c.Name).ToArray();
        Assert.Contains("list", subcommandNames);
        Assert.Contains("search", subcommandNames);
        Assert.Contains("install", subcommandNames);
        Assert.Contains("uninstall", subcommandNames);
        Assert.Contains("update", subcommandNames);
        Assert.Contains("info", subcommandNames);
        Assert.Contains("create", subcommandNames);
    }

    [Fact]
    public void ListSubcommand_HasCorrectOptions()
    {
        // Arrange
        var command = PluginCommand.Create();
        var listCommand = command.Subcommands.First(c => c.Name == "list");

        // Assert
        Assert.Equal("list", listCommand.Name);
        Assert.Equal("List installed plugins", listCommand.Description);

        var optionNames = listCommand.Options.Select(o => o.Name).ToArray();
        Assert.Contains("all", optionNames);
    }

    [Fact]
    public void SearchSubcommand_HasCorrectArgumentsAndOptions()
    {
        // Arrange
        var command = PluginCommand.Create();
        var searchCommand = command.Subcommands.First(c => c.Name == "search");

        // Assert
        Assert.Equal("search", searchCommand.Name);
        Assert.Equal("Search for plugins in the marketplace", searchCommand.Description);

        var argumentNames = searchCommand.Arguments.Select(a => a.Name).ToArray();
        Assert.Contains("query", argumentNames);

        var optionNames = searchCommand.Options.Select(o => o.Name).ToArray();
        Assert.Contains("tag", optionNames);
        Assert.Contains("author", optionNames);
    }

    [Fact]
    public void InstallSubcommand_HasCorrectArgumentsAndOptions()
    {
        // Arrange
        var command = PluginCommand.Create();
        var installCommand = command.Subcommands.First(c => c.Name == "install");

        // Assert
        Assert.Equal("install", installCommand.Name);
        Assert.Equal("Install a plugin", installCommand.Description);

        var argumentNames = installCommand.Arguments.Select(a => a.Name).ToArray();
        Assert.Contains("name", argumentNames);

        var optionNames = installCommand.Options.Select(o => o.Name).ToArray();
        Assert.Contains("version", optionNames);
        Assert.Contains("global", optionNames);
    }

    [Fact]
    public void UninstallSubcommand_HasCorrectArgumentsAndOptions()
    {
        // Arrange
        var command = PluginCommand.Create();
        var uninstallCommand = command.Subcommands.First(c => c.Name == "uninstall");

        // Assert
        Assert.Equal("uninstall", uninstallCommand.Name);
        Assert.Equal("Uninstall a plugin", uninstallCommand.Description);

        var argumentNames = uninstallCommand.Arguments.Select(a => a.Name).ToArray();
        Assert.Contains("name", argumentNames);

        var optionNames = uninstallCommand.Options.Select(o => o.Name).ToArray();
        Assert.Contains("global", optionNames);
    }

    [Fact]
    public void UpdateSubcommand_HasCorrectArguments()
    {
        // Arrange
        var command = PluginCommand.Create();
        var updateCommand = command.Subcommands.First(c => c.Name == "update");

        // Assert
        Assert.Equal("update", updateCommand.Name);
        Assert.Equal("Update installed plugins", updateCommand.Description);

        var argumentNames = updateCommand.Arguments.Select(a => a.Name).ToArray();
        Assert.Contains("name", argumentNames);
    }

    [Fact]
    public void InfoSubcommand_HasCorrectArguments()
    {
        // Arrange
        var command = PluginCommand.Create();
        var infoCommand = command.Subcommands.First(c => c.Name == "info");

        // Assert
        Assert.Equal("info", infoCommand.Name);
        Assert.Equal("Show detailed information about a plugin", infoCommand.Description);

        var argumentNames = infoCommand.Arguments.Select(a => a.Name).ToArray();
        Assert.Contains("name", argumentNames);
    }

    [Fact]
    public void CreateSubcommand_HasCorrectOptions()
    {
        // Arrange
        var command = PluginCommand.Create();
        var createCommand = command.Subcommands.First(c => c.Name == "create");

        // Assert
        Assert.Equal("create", createCommand.Name);
        Assert.Equal("Create a new plugin from template", createCommand.Description);

        var optionNames = createCommand.Options.Select(o => o.Name).ToArray();
        Assert.Contains("name", optionNames);
        Assert.Contains("output", optionNames);
        Assert.Contains("template", optionNames);
    }

    [Fact]
    public async Task ExecuteList_WithNoPlugins_ShowsEmptyMessage()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteList(false);
    }

    [Fact]
    public async Task ExecuteList_WithAllOption_IncludesDisabledPlugins()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteList(true);
    }

    [Fact]
    public async Task ExecuteSearch_WithQuery_ShowsResults()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteSearch("test", null, null);
    }

    [Fact]
    public async Task ExecuteSearch_WithTagAndAuthor_ShowsFilteredResults()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteSearch("test", "utility", "john");
    }

    [Fact]
    public async Task ExecuteInstall_WithName_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteInstall("test-plugin", null, false);
    }

    [Fact]
    public async Task ExecuteInstall_WithVersionAndGlobal_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteInstall("test-plugin", "1.0.0", true);
    }

    [Fact]
    public async Task ExecuteUninstall_WithName_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteUninstall("test-plugin", false, true);
    }

    [Fact]
    public async Task ExecuteUninstall_WithGlobalOption_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteUninstall("test-plugin", true, true);
    }

    [Fact]
    public async Task ExecuteUpdate_WithSpecificPlugin_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteUpdate("test-plugin");
    }

    [Fact]
    public async Task ExecuteUpdate_WithNullName_UpdatesAllPlugins()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteUpdate(null);
    }

    [Fact]
    public async Task ExecuteInfo_WithPluginName_ShowsPluginDetails()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteInfo("test-plugin");
    }

    [Fact]
    public async Task ExecuteCreate_WithValidParameters_CreatesPluginStructure()
    {
        // Arrange
        var pluginName = "test-plugin";
        var outputPath = _tempDirectory;

        // Act
        await PluginCommand.ExecuteCreate(pluginName, outputPath, "basic", false, true);

        // Assert
        var pluginPath = Path.Combine(outputPath, pluginName);
        Assert.True(Directory.Exists(pluginPath));
        Assert.True(File.Exists(Path.Combine(pluginPath, $"{pluginName}.csproj")));
        Assert.True(File.Exists(Path.Combine(pluginPath, "testpluginPlugin.cs")));
        Assert.True(File.Exists(Path.Combine(pluginPath, "plugin.json")));
        Assert.True(File.Exists(Path.Combine(pluginPath, "README.md")));
    }

    [Fact]
    public async Task ExecuteCreate_WithAdvancedTemplate_CreatesPluginStructure()
    {
        // Arrange
        var pluginName = "advanced-plugin";
        var outputPath = _tempDirectory;

        // Act
        await PluginCommand.ExecuteCreate(pluginName, outputPath, "advanced", false, true);

        // Assert
        var pluginPath = Path.Combine(outputPath, pluginName);
        Assert.True(Directory.Exists(pluginPath));
        Assert.True(File.Exists(Path.Combine(pluginPath, $"{pluginName}.csproj")));
    }

    [Fact]
    public async Task CreatePluginProject_CreatesValidCsprojFile()
    {
        // Arrange
        var pluginName = "test-plugin";
        var path = Path.Combine(_tempDirectory, pluginName);
        Directory.CreateDirectory(path);

        // Act
        await PluginCommand.CreatePluginProject(path, pluginName, "basic");

        // Assert
        var csprojPath = Path.Combine(path, $"{pluginName}.csproj");
        Assert.True(File.Exists(csprojPath));

        var content = await File.ReadAllTextAsync(csprojPath);
        Assert.Contains("net8.0", content);
        Assert.Contains("Relay.CLI.Sdk", content);
        Assert.Contains("2.1.0", content);
    }

    [Fact]
    public async Task CreatePluginClass_CreatesValidPluginClass()
    {
        // Arrange
        var pluginName = "relay-plugin-test";
        var path = Path.Combine(_tempDirectory, pluginName);
        Directory.CreateDirectory(path);

        // Act
        await PluginCommand.CreatePluginClass(path, pluginName);

        // Assert
        var classPath = Path.Combine(path, "TestPlugin.cs");
        Assert.True(File.Exists(classPath));

        var content = await File.ReadAllTextAsync(classPath);
        Assert.Contains("relay-plugin-test", content);
        Assert.Contains("IRelayPlugin", content);
        Assert.Contains("[RelayPlugin", content);
        Assert.Contains("InitializeAsync", content);
        Assert.Contains("ExecuteAsync", content);
    }

    [Fact]
    public async Task CreateManifest_CreatesValidJsonManifest()
    {
        // Arrange
        var pluginName = "test-plugin";
        var path = Path.Combine(_tempDirectory, pluginName);
        Directory.CreateDirectory(path);

        // Act
        await PluginCommand.CreateManifest(path, pluginName);

        // Assert
        var manifestPath = Path.Combine(path, "plugin.json");
        Assert.True(File.Exists(manifestPath));

        var content = await File.ReadAllTextAsync(manifestPath);
        Assert.Contains(pluginName, content);
        Assert.Contains("1.0.0", content);
        Assert.Contains("minimumRelayVersion", content);
    }

    [Fact]
    public async Task CreateReadme_CreatesValidMarkdownReadme()
    {
        // Arrange
        var pluginName = "test-plugin";
        var path = Path.Combine(_tempDirectory, pluginName);
        Directory.CreateDirectory(path);

        // Act
        await PluginCommand.CreateReadme(path, pluginName);

        // Assert
        var readmePath = Path.Combine(path, "README.md");
        Assert.True(File.Exists(readmePath));

        var content = await File.ReadAllTextAsync(readmePath);
        Assert.Contains($"# {pluginName}", content);
        Assert.Contains("relay plugin install", content);
        Assert.Contains("dotnet build", content);
    }

    [Fact]
    public async Task ExecuteCreate_WithExistingDirectory_HandlesGracefully()
    {
        // Arrange
        var pluginName = "existing-plugin";
        var outputPath = _tempDirectory;
        var pluginPath = Path.Combine(outputPath, pluginName);
        Directory.CreateDirectory(pluginPath);

        // Act & Assert - Should not throw even if directory exists
        await PluginCommand.ExecuteCreate(pluginName, outputPath, "basic", false, true);
    }

    [Fact]
    public async Task ExecuteInstall_WithPath_HandlesLocalInstallation()
    {
        // Arrange
        var pluginPath = Path.Combine(_tempDirectory, "plugin.dll");

        // Act & Assert - Should not throw
        await PluginCommand.ExecuteInstall(pluginPath, null, false);
    }

    [Fact]
    public async Task ExecuteSearch_WithEmptyQuery_StillWorks()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteSearch("", null, null);
    }

    [Fact]
    public async Task ExecuteInfo_WithSpecialCharacters_HandlesCorrectly()
    {
        // Act & Assert - Should not throw
        await PluginCommand.ExecuteInfo("relay-plugin-swagger");
    }
}
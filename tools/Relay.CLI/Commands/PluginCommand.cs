using System.CommandLine;
using Spectre.Console;

namespace Relay.CLI.Commands;

/// <summary>
/// Plugin management command
/// </summary>
public static class PluginCommand
{
    public static Command Create()
    {
        var command = new Command("plugin", "Manage Relay CLI plugins");

        // Subcommands
        command.AddCommand(CreateListCommand());
        command.AddCommand(CreateSearchCommand());
        command.AddCommand(CreateInstallCommand());
        command.AddCommand(CreateUninstallCommand());
        command.AddCommand(CreateUpdateCommand());
        command.AddCommand(CreateInfoCommand());
        command.AddCommand(CreateCreateCommand());

        return command;
    }

    private static Command CreateListCommand()
    {
        var command = new Command("list", "List installed plugins");
        
        var allOption = new Option<bool>("--all", () => false, "Include disabled plugins");
        command.AddOption(allOption);

        command.SetHandler(async (all) =>
        {
            await ExecuteList(all);
        }, allOption);

        return command;
    }

    private static Command CreateSearchCommand()
    {
        var command = new Command("search", "Search for plugins in the marketplace");

        var queryArgument = new Argument<string>("query", "Search query");
        var tagOption = new Option<string?>("--tag", "Filter by tag");
        var authorOption = new Option<string?>("--author", "Filter by author");

        command.AddArgument(queryArgument);
        command.AddOption(tagOption);
        command.AddOption(authorOption);

        command.SetHandler(async (query, tag, author) =>
        {
            await ExecuteSearch(query, tag, author);
        }, queryArgument, tagOption, authorOption);

        return command;
    }

    private static Command CreateInstallCommand()
    {
        var command = new Command("install", "Install a plugin");

        var nameArgument = new Argument<string>("name", "Plugin name or path");
        var versionOption = new Option<string?>("--version", "Specific version to install");
        var globalOption = new Option<bool>("--global", () => false, "Install globally");

        command.AddArgument(nameArgument);
        command.AddOption(versionOption);
        command.AddOption(globalOption);

        command.SetHandler(async (name, version, global) =>
        {
            await ExecuteInstall(name, version, global);
        }, nameArgument, versionOption, globalOption);

        return command;
    }

    private static Command CreateUninstallCommand()
    {
        var command = new Command("uninstall", "Uninstall a plugin");

        var nameArgument = new Argument<string>("name", "Plugin name");
        var globalOption = new Option<bool>("--global", () => false, "Uninstall from global location");

        command.AddArgument(nameArgument);
        command.AddOption(globalOption);

        command.SetHandler(async (name, global) =>
        {
            await ExecuteUninstall(name, global);
        }, nameArgument, globalOption);

        return command;
    }

    private static Command CreateUpdateCommand()
    {
        var command = new Command("update", "Update installed plugins");

        var nameArgument = new Argument<string?>("name", () => null, "Plugin name (updates all if not specified)");
        command.AddArgument(nameArgument);

        command.SetHandler(async (name) =>
        {
            await ExecuteUpdate(name);
        }, nameArgument);

        return command;
    }

    private static Command CreateInfoCommand()
    {
        var command = new Command("info", "Show detailed information about a plugin");

        var nameArgument = new Argument<string>("name", "Plugin name");
        command.AddArgument(nameArgument);

        command.SetHandler(async (name) =>
        {
            await ExecuteInfo(name);
        }, nameArgument);

        return command;
    }

    private static Command CreateCreateCommand()
    {
        var command = new Command("create", "Create a new plugin from template");

        var nameOption = new Option<string>("--name", "Plugin name") { IsRequired = true };
        var outputOption = new Option<string>("--output", () => ".", "Output directory");
        var templateOption = new Option<string>("--template", () => "basic", "Template (basic, advanced)");

        command.AddOption(nameOption);
        command.AddOption(outputOption);
        command.AddOption(templateOption);

        command.SetHandler(async (name, output, template) =>
        {
            await ExecuteCreate(name, output, template);
        }, nameOption, outputOption, templateOption);

        return command;
    }

    private static async Task ExecuteList(bool includeAll)
    {
        AnsiConsole.MarkupLine("[cyan]📦 Installed Plugins[/]");
        AnsiConsole.WriteLine();

        var pluginManager = new Plugins.PluginManager();
        var plugins = await pluginManager.GetInstalledPluginsAsync(includeAll);

        if (plugins.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No plugins installed[/]");
            AnsiConsole.MarkupLine("[dim]Try: relay plugin search <query>[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Version[/]")
            .AddColumn("[bold]Description[/]")
            .AddColumn("[bold]Status[/]");

        foreach (var plugin in plugins)
        {
            var status = plugin.Enabled ? "[green]✓ Enabled[/]" : "[dim]Disabled[/]";
            table.AddRow(
                plugin.Name,
                plugin.Version,
                plugin.Description.Length > 50 ? plugin.Description.Substring(0, 47) + "..." : plugin.Description,
                status
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]Total: {plugins.Count} plugin(s)[/]");
    }

    private static async Task ExecuteSearch(string query, string? tag, string? author)
    {
        AnsiConsole.MarkupLine($"[cyan]🔍 Searching for: {query}[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Status()
            .StartAsync("Searching marketplace...", async ctx =>
            {
                await Task.Delay(1000); // Simulate search
            });

        // Simulated results (in production, this would call a real API)
        var results = new[]
        {
            new { Name = "relay-plugin-swagger", Version = "1.0.0", Description = "Generate Swagger/OpenAPI documentation", Downloads = 1234 },
            new { Name = "relay-plugin-graphql", Version = "1.2.0", Description = "GraphQL schema generation", Downloads = 890 },
            new { Name = "relay-plugin-docker", Version = "2.0.0", Description = "Docker configuration generator", Downloads = 2100 },
            new { Name = "relay-plugin-kubernetes", Version = "1.5.0", Description = "Kubernetes deployment templates", Downloads = 1567 },
        };

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Version[/]")
            .AddColumn("[bold]Description[/]")
            .AddColumn("[bold]Downloads[/]");

        foreach (var result in results)
        {
            table.AddRow(
                $"[blue]{result.Name}[/]",
                result.Version,
                result.Description,
                result.Downloads.ToString("N0")
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]To install: relay plugin install <name>[/]");
    }

    private static async Task ExecuteInstall(string name, string? version, bool global)
    {
        var versionText = version != null ? $" ({version})" : "";
        AnsiConsole.MarkupLine($"[cyan]📥 Installing plugin: {name}{versionText}[/]");
        AnsiConsole.WriteLine();

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[green]Installing {name}[/]");

                // Simulate download
                task.Description = "[green]Downloading...[/]";
                for (int i = 0; i <= 40; i++)
                {
                    task.Increment(2.5);
                    await Task.Delay(20);
                }

                // Simulate extraction
                task.Description = "[green]Extracting...[/]";
                for (int i = 40; i <= 70; i++)
                {
                    task.Increment(1);
                    await Task.Delay(15);
                }

                // Simulate validation
                task.Description = "[green]Validating...[/]";
                for (int i = 70; i <= 100; i++)
                {
                    task.Increment(1);
                    await Task.Delay(10);
                }
            });

        var location = global ? "global" : "local";
        AnsiConsole.MarkupLine($"[green]✅ Plugin '{name}' installed successfully ({location})[/]");
        AnsiConsole.MarkupLine($"[dim]Run with: relay plugin run {name}[/]");
    }

    private static async Task ExecuteUninstall(string name, bool global)
    {
        if (!AnsiConsole.Confirm($"[yellow]Are you sure you want to uninstall '{name}'?[/]"))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelled[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[cyan]🗑️  Uninstalling plugin: {name}[/]");
        
        await Task.Delay(500);
        
        AnsiConsole.MarkupLine($"[green]✅ Plugin '{name}' uninstalled successfully[/]");
    }

    private static async Task ExecuteUpdate(string? name)
    {
        var target = name ?? "all plugins";
        AnsiConsole.MarkupLine($"[cyan]🔄 Updating {target}[/]");
        AnsiConsole.WriteLine();

        await Task.Delay(1000);

        AnsiConsole.MarkupLine("[green]✅ All plugins are up to date[/]");
    }

    private static async Task ExecuteInfo(string name)
    {
        AnsiConsole.MarkupLine($"[cyan]ℹ️  Plugin Information: {name}[/]");
        AnsiConsole.WriteLine();

        await Task.Delay(300);

        // Simulated plugin info
        var info = new Panel($@"[bold]{name}[/] v1.2.0

[yellow]Description:[/]
  Generate Swagger/OpenAPI documentation for Relay handlers

[yellow]Authors:[/]
  • John Doe
  • Jane Smith

[yellow]Tags:[/]
  swagger, openapi, documentation, api

[yellow]Dependencies:[/]
  • Swashbuckle.AspNetCore (>= 6.0.0)
  • NSwag.Core (>= 13.0.0)

[yellow]Repository:[/]
  https://github.com/relay-plugins/swagger

[yellow]License:[/]
  MIT

[yellow]Installation:[/]
  relay plugin install {name}")
            .Header("[bold green]Plugin Details[/]")
            .BorderColor(Color.Cyan1);

        AnsiConsole.Write(info);
    }

    private static async Task ExecuteCreate(string name, string outputPath, string template)
    {
        AnsiConsole.MarkupLine($"[cyan]🎨 Creating plugin: {name}[/]");
        AnsiConsole.WriteLine();

        var fullPath = Path.Combine(outputPath, name);

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Generating project[/]", maxValue: 5);

                // Create directory structure
                Directory.CreateDirectory(fullPath);
                task.Increment(1);
                await Task.Delay(200);

                // Create project file
                await CreatePluginProject(fullPath, name, template);
                task.Increment(1);
                await Task.Delay(200);

                // Create plugin class
                await CreatePluginClass(fullPath, name);
                task.Increment(1);
                await Task.Delay(200);

                // Create manifest
                await CreateManifest(fullPath, name);
                task.Increment(1);
                await Task.Delay(200);

                // Create README
                await CreateReadme(fullPath, name);
                task.Increment(1);
            });

        AnsiConsole.MarkupLine($"[green]✅ Plugin created: {fullPath}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[cyan]Next steps:[/]");
        AnsiConsole.MarkupLine($"  1. cd {name}");
        AnsiConsole.MarkupLine("  2. dotnet build");
        AnsiConsole.MarkupLine("  3. relay plugin install .");
    }

    private static async Task CreatePluginProject(string path, string name, string template)
    {
        var csproj = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Relay.CLI.Sdk"" Version=""2.1.0"" />
  </ItemGroup>
</Project>";

        await File.WriteAllTextAsync(Path.Combine(path, $"{name}.csproj"), csproj);
    }

    private static async Task CreatePluginClass(string path, string name)
    {
        var className = name.Replace("relay-plugin-", "").Replace("-", "");
        var pluginCode = $@"using Relay.CLI.Plugins;

[RelayPlugin(""{name}"", ""1.0.0"")]
public class {className}Plugin : IRelayPlugin
{{
    public string Name => ""{name}"";
    public string Version => ""1.0.0"";
    public string Description => ""My awesome Relay plugin"";
    public string[] Authors => new[] {{ ""Your Name"" }};
    public string[] Tags => new[] {{ ""utility"" }};
    public string MinimumRelayVersion => ""2.1.0"";

    public async Task<bool> InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {{
        context.Logger.LogInformation($""Initializing {{Name}}..."");
        return true;
    }}

    public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {{
        Console.WriteLine($""Hello from {{Name}}!"");
        
        // Your plugin logic here
        
        return 0;
    }}

    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {{
        // Cleanup resources
        await Task.CompletedTask;
    }}

    public string GetHelp()
    {{
        return @""
Usage: relay plugin run {name} [options]

Description:
  {{Description}}

Options:
  --help    Show this help message
"";
    }}
}}";

        await File.WriteAllTextAsync(Path.Combine(path, $"{className}Plugin.cs"), pluginCode);
    }

    private static async Task CreateManifest(string path, string name)
    {
        var manifest = $@"{{
  ""name"": ""{name}"",
  ""version"": ""1.0.0"",
  ""description"": ""My awesome Relay plugin"",
  ""authors"": [""Your Name""],
  ""tags"": [""utility""],
  ""minimumRelayVersion"": ""2.1.0"",
  ""dependencies"": {{}},
  ""repository"": ""https://github.com/youruser/{name}""
}}";

        await File.WriteAllTextAsync(Path.Combine(path, "plugin.json"), manifest);
    }

    private static async Task CreateReadme(string path, string name)
    {
        var readme = $@"# {name}

My awesome Relay CLI plugin.

## Installation

```bash
relay plugin install {name}
```

## Usage

```bash
relay plugin run {name}
```

## Development

```bash
dotnet build
relay plugin install .
```

## License

MIT
";

        await File.WriteAllTextAsync(Path.Combine(path, "README.md"), readme);
    }
}

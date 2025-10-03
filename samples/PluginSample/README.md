# Relay CLI Plugin Sample

This sample demonstrates how to create custom plugins for the Relay CLI. Plugins allow you to extend the CLI with custom commands and functionality.

## What's Included

This sample includes:

1. **CodeGeneratorPlugin** - A plugin that generates boilerplate code
2. **DatabaseMigrationPlugin** - A plugin that manages database migrations
3. **CustomValidatorPlugin** - A plugin that adds custom validation rules

## Plugin Architecture

Relay CLI uses a plugin architecture that allows developers to extend the CLI with custom functionality. Each plugin:

- Implements the `IRelayPlugin` interface
- Has a `plugin.json` manifest file
- Can access CLI services through `IPluginContext`
- Runs in an isolated `AssemblyLoadContext`

## Creating a Plugin

### 1. Create a Class Library Project

```bash
dotnet new classlib -n MyCustomPlugin
cd MyCustomPlugin
```

### 2. Add Required References

```xml
<ItemGroup>
  <PackageReference Include="Relay.CLI.Sdk" Version="2.1.0" />
</ItemGroup>
```

### 3. Implement IRelayPlugin

```csharp
using Relay.CLI.Plugins;

[RelayPlugin("my-custom-plugin", "1.0.0")]
public class MyCustomPlugin : IRelayPlugin
{
    public string Name => "My Custom Plugin";
    public string Version => "1.0.0";
    public string Description => "A custom plugin for Relay CLI";
    public string[] Authors => new[] { "Your Name" };
    public string[] Tags => new[] { "custom", "example" };
    public string MinimumRelayVersion => "2.1.0";

    private IPluginContext? _context;

    public async Task<bool> InitializeAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        _context = context;
        _context.Logger.LogInformation($"{Name} initialized");
        return true;
    }

    public async Task<int> ExecuteAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (_context == null)
            return 1;

        _context.Logger.LogInformation($"Executing {Name} with {args.Length} arguments");
        
        // Your plugin logic here
        
        return 0; // Success
    }

    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        _context?.Logger.LogInformation($"{Name} cleaned up");
        await Task.CompletedTask;
    }

    public string GetHelp()
    {
        return @"
My Custom Plugin - Help

Usage:
  relay plugin run my-custom-plugin [options]

Options:
  --option1 <value>    Description of option 1
  --option2 <value>    Description of option 2

Examples:
  relay plugin run my-custom-plugin --option1 value1
";
    }
}
```

### 4. Create plugin.json Manifest

```json
{
  "name": "my-custom-plugin",
  "version": "1.0.0",
  "description": "A custom plugin for Relay CLI",
  "authors": ["Your Name"],
  "tags": ["custom", "example"],
  "minimumRelayVersion": "2.1.0",
  "repository": "https://github.com/yourusername/my-custom-plugin",
  "license": "MIT"
}
```

### 5. Build the Plugin

```bash
dotnet build -c Release
```

## Installing a Plugin

### From Local Directory

```bash
relay plugin install /path/to/plugin/directory
```

### From ZIP File

```bash
relay plugin install /path/to/plugin.zip
```

### Global Installation

```bash
relay plugin install /path/to/plugin --global
```

## Using a Plugin

### List Installed Plugins

```bash
relay plugin list
```

### Run a Plugin

```bash
relay plugin run <plugin-name> [args]
```

### Get Plugin Help

```bash
relay plugin help <plugin-name>
```

### Uninstall a Plugin

```bash
relay plugin uninstall <plugin-name>
```

## Plugin Context Services

Plugins have access to various CLI services through `IPluginContext`:

### Logger

```csharp
_context.Logger.LogInformation("Information message");
_context.Logger.LogWarning("Warning message");
_context.Logger.LogError("Error message");
```

### File System

```csharp
var exists = await _context.FileSystem.FileExistsAsync("file.txt");
var content = await _context.FileSystem.ReadFileAsync("file.txt");
await _context.FileSystem.WriteFileAsync("output.txt", "content");
```

### Configuration

```csharp
var value = await _context.GetSettingAsync("key");
await _context.SetSettingAsync("key", "value");
```

### Service Provider

```csharp
var service = await _context.GetServiceAsync<IMyService>();
```

## Best Practices

1. **Error Handling**: Always handle exceptions and return appropriate exit codes
2. **Logging**: Use the provided logger for consistent output
3. **Async/Await**: Use async patterns for I/O operations
4. **Cleanup**: Implement proper cleanup in `CleanupAsync`
5. **Versioning**: Follow semantic versioning for your plugins
6. **Documentation**: Provide clear help text in `GetHelp()`
7. **Testing**: Write unit tests for your plugin logic

## Plugin Examples in This Sample

### Code Generator Plugin

Generates boilerplate code based on templates:

```bash
relay plugin run code-generator --type controller --name ProductController
```

### Database Migration Plugin

Manages database migrations:

```bash
relay plugin run db-migration create AddProductTable
relay plugin run db-migration up
relay plugin run db-migration down
```

### Custom Validator Plugin

Adds custom validation rules:

```bash
relay plugin run custom-validator --rule naming --path ./src
```

## Additional Resources

- [Plugin API Documentation](https://docs.relay.dev/plugins)
- [Sample Plugins Repository](https://github.com/relay/sample-plugins)
- [Plugin Development Guide](https://docs.relay.dev/plugins/development)

## License

This sample is provided under the MIT License.

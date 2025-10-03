using System.CommandLine;
using Relay.CLI.TemplateEngine;

namespace Relay.CLI.Commands;

/// <summary>
/// Command for managing templates (validate, pack, publish, etc.)
/// </summary>
public class TemplateCommand : Command
{
    public TemplateCommand() : base("template", "Manage Relay project templates")
    {
        // Subcommands
        AddCommand(CreateValidateCommand());
        AddCommand(CreatePackCommand());
        AddCommand(CreatePublishCommand());
        AddCommand(CreateListCommand());
        AddCommand(CreateCreateCommand());
    }

    private Command CreateValidateCommand()
    {
        var validateCommand = new Command("validate", "Validate a template structure");
        
        var pathOption = new Option<string>(
            "--path",
            description: "Path to the template directory"
        ) { IsRequired = true };

        validateCommand.AddOption(pathOption);

        validateCommand.SetHandler(async (path) =>
        {
            await ValidateTemplateAsync(path);
        }, pathOption);

        return validateCommand;
    }

    private Command CreatePackCommand()
    {
        var packCommand = new Command("pack", "Package a template for distribution");
        
        var pathOption = new Option<string>(
            "--path",
            description: "Path to the template directory"
        ) { IsRequired = true };

        var outputOption = new Option<string>(
            "--output",
            description: "Output directory for the package",
            getDefaultValue: () => Directory.GetCurrentDirectory()
        );

        packCommand.AddOption(pathOption);
        packCommand.AddOption(outputOption);

        packCommand.SetHandler(async (path, output) =>
        {
            await PackTemplateAsync(path, output!);
        }, pathOption, outputOption);

        return packCommand;
    }

    private Command CreatePublishCommand()
    {
        var publishCommand = new Command("publish", "Publish a template to a registry");
        
        var packageOption = new Option<string>(
            "--package",
            description: "Path to the template package"
        ) { IsRequired = true };

        var registryOption = new Option<string>(
            "--registry",
            description: "Registry URL",
            getDefaultValue: () => "https://api.nuget.org/v3/index.json"
        );

        publishCommand.AddOption(packageOption);
        publishCommand.AddOption(registryOption);

        publishCommand.SetHandler(async (package, registry) =>
        {
            await PublishTemplateAsync(package, registry!);
        }, packageOption, registryOption);

        return publishCommand;
    }

    private Command CreateListCommand()
    {
        var listCommand = new Command("list", "List all available templates");

        listCommand.SetHandler(async () =>
        {
            await ListTemplatesAsync();
        });

        return listCommand;
    }

    private Command CreateCreateCommand()
    {
        var createCommand = new Command("create", "Create a new custom template from existing project");
        
        var nameOption = new Option<string>(
            "--name",
            description: "Name of the new template"
        ) { IsRequired = true };

        var fromOption = new Option<string>(
            "--from",
            description: "Path to the source project"
        ) { IsRequired = true };

        var outputOption = new Option<string>(
            "--output",
            description: "Output directory for the template",
            getDefaultValue: () => Path.Combine(Directory.GetCurrentDirectory(), "Templates")
        );

        createCommand.AddOption(nameOption);
        createCommand.AddOption(fromOption);
        createCommand.AddOption(outputOption);

        createCommand.SetHandler(async (name, from, output) =>
        {
            await CreateCustomTemplateAsync(name, from, output!);
        }, nameOption, fromOption, outputOption);

        return createCommand;
    }

    private static async Task ValidateTemplateAsync(string templatePath)
    {
        Console.WriteLine();
        Console.WriteLine($"🔍 Validating template: {templatePath}");
        Console.WriteLine("".PadRight(80, '='));

        var validator = new TemplateValidator();
        var result = await validator.ValidateAsync(templatePath);

        result.DisplayResults();
    }

    private static async Task PackTemplateAsync(string templatePath, string outputPath)
    {
        Console.WriteLine();
        Console.WriteLine("📦 Packaging Template");
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine();

        var publisher = new TemplatePublisher(templatePath);
        var result = await publisher.PackTemplateAsync(templatePath, outputPath);

        Console.WriteLine();
        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(result.Message);
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("📁 Package Details:");
            Console.WriteLine($"  Location: {result.PackagePath}");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine($"  • Test: relay template validate --path {templatePath}");
            Console.WriteLine($"  • Publish: relay template publish --package {result.PackagePath}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Message);
            Console.ResetColor();
            
            if (result.Errors.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Errors:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  • {error}");
                }
            }
        }
        Console.WriteLine();
    }

    private static async Task PublishTemplateAsync(string packagePath, string registryUrl)
    {
        Console.WriteLine();
        Console.WriteLine("📤 Publishing Template");
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine();

        var publisher = new TemplatePublisher(string.Empty);
        var result = await publisher.PublishTemplateAsync(packagePath, registryUrl);

        Console.WriteLine();
        if (result.Success)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(result.Message);
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(result.Message);
            Console.ResetColor();
            
            if (result.Errors.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Errors:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  • {error}");
                }
            }
        }
        Console.WriteLine();
    }

    private static async Task ListTemplatesAsync()
    {
        Console.WriteLine();
        Console.WriteLine("📋 Available Relay Templates");
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine();

        var templatesPath = Path.Combine(AppContext.BaseDirectory, "Templates");
        var publisher = new TemplatePublisher(templatesPath);
        var templates = await publisher.ListAvailableTemplatesAsync();

        if (!templates.Any())
        {
            Console.WriteLine("No templates found.");
            Console.WriteLine();
            Console.WriteLine("To create a new template:");
            Console.WriteLine("  relay template create --name my-template --from ./MyProject");
            Console.WriteLine();
            return;
        }

        foreach (var template in templates)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"📦 {template.Id}");
            Console.ResetColor();
            Console.WriteLine($"   Name: {template.Name}");
            Console.WriteLine($"   Description: {template.Description}");
            Console.WriteLine($"   Author: {template.Author}");
            Console.WriteLine($"   Version: {template.Version}");
            Console.WriteLine($"   Path: {template.Path}");
            Console.WriteLine();
        }

        Console.WriteLine("Usage:");
        Console.WriteLine("  relay new --name MyProject --template <template-id>");
        Console.WriteLine();
    }

    private static async Task CreateCustomTemplateAsync(string name, string sourcePath, string outputPath)
    {
        Console.WriteLine();
        Console.WriteLine($"🎨 Creating Custom Template: {name}");
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine();

        try
        {
            if (!Directory.Exists(sourcePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Source directory not found: {sourcePath}");
                Console.ResetColor();
                return;
            }

            var templatePath = Path.Combine(outputPath, name);
            
            if (Directory.Exists(templatePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Template directory already exists: {templatePath}");
                Console.ResetColor();
                return;
            }

            Console.WriteLine("📁 Creating template structure...");
            
            // Create template directories
            var configDir = Path.Combine(templatePath, ".template.config");
            var contentDir = Path.Combine(templatePath, "content");
            Directory.CreateDirectory(configDir);
            Directory.CreateDirectory(contentDir);

            Console.WriteLine("  ✓ Created .template.config/");
            Console.WriteLine("  ✓ Created content/");

            // Copy source files to content directory
            Console.WriteLine();
            Console.WriteLine("📄 Copying source files...");
            await CopyDirectoryAsync(sourcePath, contentDir);
            Console.WriteLine("  ✓ Files copied");

            // Generate template.json
            Console.WriteLine();
            Console.WriteLine("📝 Generating template.json...");
            await GenerateTemplateJsonAsync(name, configDir);
            Console.WriteLine("  ✓ template.json created");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ Template created successfully: {templatePath}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine($"  1. Edit {Path.Combine(configDir, "template.json")} to customize your template");
            Console.WriteLine($"  2. Validate: relay template validate --path {templatePath}");
            Console.WriteLine($"  3. Test: relay new --name TestProject --template {name}");
            Console.WriteLine($"  4. Package: relay template pack --path {templatePath}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error creating template: {ex.Message}");
            Console.ResetColor();
        }
    }

    private static async Task CopyDirectoryAsync(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var fileName = Path.GetFileName(file);
            
            // Skip certain files
            if (fileName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
                fileName.Equals(".git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var destFile = Path.Combine(destDir, fileName);
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var dirName = Path.GetFileName(dir);
            
            // Skip certain directories
            if (dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                dirName.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                dirName.Equals(".vs", StringComparison.OrdinalIgnoreCase) ||
                dirName.Equals(".git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await CopyDirectoryAsync(dir, Path.Combine(destDir, dirName));
        }
    }

    private static async Task GenerateTemplateJsonAsync(string name, string configDir)
    {
        var templateJson = $$"""
        {
          "$schema": "http://json.schemastore.org/template",
          "author": "{{Environment.UserName}}",
          "classifications": ["Custom", "Relay"],
          "identity": "Relay.Templates.{{name}}",
          "name": "{{name}} Template",
          "shortName": "{{name.ToLower()}}",
          "description": "Custom Relay template: {{name}}",
          "tags": {
            "language": "C#",
            "type": "project"
          },
          "sourceName": "{{name}}",
          "preferNameDirectory": true,
          "symbols": {
            "ProjectName": {
              "type": "parameter",
              "datatype": "string",
              "isRequired": true,
              "replaces": "{{name}}",
              "fileRename": "{{name}}",
              "description": "The name of the project"
            }
          }
        }
        """;

        var templateJsonPath = Path.Combine(configDir, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, templateJson);
    }
}

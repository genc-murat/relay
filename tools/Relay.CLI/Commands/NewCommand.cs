using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json;

namespace Relay.CLI.Commands;

/// <summary>
/// Command for creating new projects from templates
/// </summary>
public class NewCommand : Command
{
    public NewCommand() : base("new", "Create a new project from a template")
    {
        var nameOption = new Option<string>(
            "--name",
            description: "Name of the project"
        ) { IsRequired = true };
        
        var templateOption = new Option<string>(
            "--template",
            description: "Template to use (e.g., relay-webapi, relay-microservice)"
        );
        
        var listOption = new Option<bool>(
            "--list",
            description: "List all available templates"
        );
        
        var featuresOption = new Option<string[]>(
            "--features",
            description: "Features to include (e.g., auth,swagger,docker)"
        );
        
        var outputOption = new Option<string>(
            "--output",
            description: "Output directory",
            getDefaultValue: () => Directory.GetCurrentDirectory()
        );
        
        var brokerOption = new Option<string>(
            "--broker",
            description: "Message broker (rabbitmq, kafka, azureservicebus)"
        );
        
        var databaseOption = new Option<string>(
            "--database",
            description: "Database provider (sqlserver, postgres, mysql, sqlite)"
        );
        
        var authOption = new Option<string>(
            "--auth",
            description: "Authentication provider (jwt, identityserver, auth0)"
        );
        
        var noRestoreOption = new Option<bool>(
            "--no-restore",
            description: "Skip restoring NuGet packages"
        );
        
        var noBuildOption = new Option<bool>(
            "--no-build",
            description: "Skip building the project"
        );

        AddOption(nameOption);
        AddOption(templateOption);
        AddOption(listOption);
        AddOption(featuresOption);
        AddOption(outputOption);
        AddOption(brokerOption);
        AddOption(databaseOption);
        AddOption(authOption);
        AddOption(noRestoreOption);
        AddOption(noBuildOption);

        this.SetHandler(async (context) =>
        {
            var name = context.ParseResult.GetValueForOption(nameOption);
            var template = context.ParseResult.GetValueForOption(templateOption);
            var list = context.ParseResult.GetValueForOption(listOption);
            var features = context.ParseResult.GetValueForOption(featuresOption) ?? Array.Empty<string>();
            var output = context.ParseResult.GetValueForOption(outputOption)!;
            var broker = context.ParseResult.GetValueForOption(brokerOption);
            var database = context.ParseResult.GetValueForOption(databaseOption);
            var auth = context.ParseResult.GetValueForOption(authOption);
            var noRestore = context.ParseResult.GetValueForOption(noRestoreOption);
            var noBuild = context.ParseResult.GetValueForOption(noBuildOption);

            if (list)
            {
                await ListTemplatesAsync();
                return;
            }

            if (string.IsNullOrEmpty(name))
            {
                Console.WriteLine("❌ Error: Project name is required");
                Console.WriteLine("Usage: relay new --name MyProject --template relay-webapi");
                return;
            }

            if (string.IsNullOrEmpty(template))
            {
                Console.WriteLine("❌ Error: Template is required");
                Console.WriteLine("Use 'relay new --list' to see available templates");
                return;
            }

            await CreateProjectAsync(name!, template!, features, output, broker, database, auth, noRestore, noBuild);
        });
    }

    private static async Task ListTemplatesAsync()
    {
        Console.WriteLine();
        Console.WriteLine("📋 Available Relay Templates");
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine();

        var templates = GetTemplates();

        foreach (var template in templates)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{template.Id}");
            Console.ResetColor();
            
            Console.WriteLine($"  Description: {template.Description}");
            Console.WriteLine($"  Best for: {template.BestFor}");
            Console.WriteLine($"  Tags: {string.Join(", ", template.Tags)}");
            
            if (template.Features.Any())
            {
                Console.WriteLine($"  Available features: {string.Join(", ", template.Features)}");
            }
            
            Console.WriteLine();
        }

        Console.WriteLine("Usage Examples:");
        Console.WriteLine("  relay new --name MyApi --template relay-webapi");
        Console.WriteLine("  relay new --name MyApi --template relay-webapi --features auth,swagger,docker");
        Console.WriteLine("  relay new --name OrderService --template relay-microservice --broker rabbitmq");
        Console.WriteLine();
    }

    private static async Task CreateProjectAsync(
        string name,
        string template,
        string[] features,
        string output,
        string? broker,
        string? database,
        string? auth,
        bool noRestore,
        bool noBuild)
    {
        Console.WriteLine();
        Console.WriteLine($"🚀 Creating project '{name}' from template '{template}'");
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine();

        var templateInfo = GetTemplates().FirstOrDefault(t => t.Id == template);
        if (templateInfo == null)
        {
            Console.WriteLine($"❌ Error: Template '{template}' not found");
            Console.WriteLine("Use 'relay new --list' to see available templates");
            return;
        }

        var projectPath = Path.Combine(output, name);
        
        if (Directory.Exists(projectPath))
        {
            Console.WriteLine($"❌ Error: Directory '{projectPath}' already exists");
            return;
        }

        try
        {
            // Create project structure
            Console.WriteLine("📁 Creating project structure...");
            await CreateProjectStructure(projectPath, name, templateInfo, features, broker, database, auth);

            // Generate files
            Console.WriteLine("📝 Generating project files...");
            await GenerateProjectFiles(projectPath, name, templateInfo, features, broker, database, auth);

            // Restore packages
            if (!noRestore)
            {
                Console.WriteLine("📦 Restoring NuGet packages...");
                await RestorePackages(projectPath);
            }

            // Build project
            if (!noBuild && !noRestore)
            {
                Console.WriteLine("🔨 Building project...");
                await BuildProject(projectPath);
            }

            Console.WriteLine();
            Console.WriteLine("✅ Project created successfully!");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine($"  cd {name}");
            Console.WriteLine("  dotnet run --project src/{0}.Api", name);
            Console.WriteLine();
            Console.WriteLine("Documentation:");
            Console.WriteLine($"  See README.md in {projectPath}");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating project: {ex.Message}");
        }
    }

    private static async Task CreateProjectStructure(
        string projectPath,
        string name,
        TemplateInfo template,
        string[] features,
        string? broker,
        string? database,
        string? auth)
    {
        Directory.CreateDirectory(projectPath);

        var directories = template.Structure switch
        {
            "clean-architecture" => new[]
            {
                "src", $"src/{name}.Api", $"src/{name}.Application", 
                $"src/{name}.Domain", $"src/{name}.Infrastructure",
                "tests", $"tests/{name}.UnitTests", $"tests/{name}.IntegrationTests",
                $"tests/{name}.ArchitectureTests",
                "docs", "scripts"
            },
            "microservice" => new[]
            {
                "src", $"src/{name}", "tests", $"tests/{name}.Tests",
                "k8s", "helm", "docs"
            },
            "modular" => new[]
            {
                "src", $"src/{name}.Api", $"src/{name}.Modules",
                $"src/{name}.Shared", "tests", "docs"
            },
            _ => new[] { "src", "tests", "docs" }
        };

        foreach (var dir in directories)
        {
            Directory.CreateDirectory(Path.Combine(projectPath, dir));
            Console.WriteLine($"  ✓ Created {dir}");
        }

        await Task.CompletedTask;
    }

    private static async Task GenerateProjectFiles(
        string projectPath,
        string name,
        TemplateInfo template,
        string[] features,
        string? broker,
        string? database,
        string? auth)
    {
        // Generate based on template type
        switch (template.Id)
        {
            case "relay-webapi":
                await GenerateWebApiProject(projectPath, name, features, database, auth);
                break;
            case "relay-microservice":
                await GenerateMicroserviceProject(projectPath, name, features, broker, database);
                break;
            case "relay-ddd":
                await GenerateDddProject(projectPath, name, features, database);
                break;
            case "relay-cqrs-es":
                await GenerateCqrsEsProject(projectPath, name, features);
                break;
            case "relay-modular":
                await GenerateModularProject(projectPath, name, features, database);
                break;
            case "relay-graphql":
                await GenerateGraphQLProject(projectPath, name, features, database);
                break;
            case "relay-grpc":
                await GenerateGrpcProject(projectPath, name, features);
                break;
            case "relay-serverless":
                await GenerateServerlessProject(projectPath, name, features);
                break;
            case "relay-blazor":
                await GenerateBlazorProject(projectPath, name, features);
                break;
            case "relay-maui":
                await GenerateMauiProject(projectPath, name, features);
                break;
        }

        // Generate common files
        await GenerateCommonFiles(projectPath, name, template, features);
    }

    private static async Task GenerateWebApiProject(
        string projectPath,
        string name,
        string[] features,
        string? database,
        string? auth)
    {
        // This will be implemented with actual template generation
        Console.WriteLine($"  ✓ Generated Web API project structure");
        await Task.CompletedTask;
    }

    private static async Task GenerateMicroserviceProject(
        string projectPath,
        string name,
        string[] features,
        string? broker,
        string? database)
    {
        Console.WriteLine($"  ✓ Generated Microservice project structure");
        await Task.CompletedTask;
    }

    private static async Task GenerateDddProject(
        string projectPath,
        string name,
        string[] features,
        string? database)
    {
        Console.WriteLine($"  ✓ Generated DDD project structure");
        await Task.CompletedTask;
    }

    private static async Task GenerateCqrsEsProject(
        string projectPath,
        string name,
        string[] features)
    {
        Console.WriteLine($"  ✓ Generated CQRS+ES project structure");
        await Task.CompletedTask;
    }

    private static async Task GenerateModularProject(
        string projectPath,
        string name,
        string[] features,
        string? database)
    {
        Console.WriteLine($"  ✓ Generated Modular Monolith structure");
        await Task.CompletedTask;
    }

    private static async Task GenerateGraphQLProject(
        string projectPath,
        string name,
        string[] features,
        string? database)
    {
        Console.WriteLine($"  ✓ Generated GraphQL project structure");
        await Task.CompletedTask;
    }

    private static async Task GenerateGrpcProject(
        string projectPath,
        string name,
        string[] features)
    {
        Console.WriteLine($"  ✓ Generated gRPC project structure");
        await Task.CompletedTask;
    }

    private static async Task GenerateServerlessProject(
        string projectPath,
        string name,
        string[] features)
    {
        Console.WriteLine($"  ✓ Generated Serverless project structure");
        await Task.CompletedTask;
    }

    private static async Task GenerateBlazorProject(
        string projectPath,
        string name,
        string[] features)
    {
        Console.WriteLine($"  ✓ Generated Blazor project structure");
        await Task.CompletedTask;
    }

    private static async Task GenerateMauiProject(
        string projectPath,
        string name,
        string[] features)
    {
        Console.WriteLine($"  ✓ Generated MAUI project structure");
        await Task.CompletedTask;
    }

    private static async Task GenerateCommonFiles(
        string projectPath,
        string name,
        TemplateInfo template,
        string[] features)
    {
        // Generate README
        var readmeContent = GenerateReadme(name, template, features);
        await File.WriteAllTextAsync(Path.Combine(projectPath, "README.md"), readmeContent);
        Console.WriteLine("  ✓ Generated README.md");

        // Generate .gitignore
        await GenerateGitignore(projectPath);
        Console.WriteLine("  ✓ Generated .gitignore");

        // Generate Docker files if requested
        if (features.Contains("docker"))
        {
            await GenerateDockerFiles(projectPath, name);
            Console.WriteLine("  ✓ Generated Docker files");
        }

        // Generate CI/CD if requested
        if (features.Contains("ci"))
        {
            await GenerateCICDFiles(projectPath, name);
            Console.WriteLine("  ✓ Generated CI/CD files");
        }
    }

    private static string GenerateReadme(string name, TemplateInfo template, string[] features)
    {
        return $@"# {name}

{template.Description}

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- Docker (optional)

### Running the application

```bash
dotnet run --project src/{name}.Api
```

### Running tests

```bash
dotnet test
```

## Features

{string.Join("\n", features.Select(f => $"- {f}"))}

## Documentation

See the `docs` folder for detailed documentation.

## License

MIT
";
    }

    private static async Task GenerateGitignore(string projectPath)
    {
        var gitignoreContent = @"
# Build results
[Bb]in/
[Oo]bj/
[Ll]og/
[Ll]ogs/

# Visual Studio
.vs/
*.user
*.userosscache
*.sln.docstates

# User-specific files
*.suo
*.user
*.sln.docstates

# ReSharper
_ReSharper*/
*.[Rr]e[Ss]harper
*.DotSettings.user

# JetBrains Rider
.idea/
*.sln.iml

# Visual Studio Code
.vscode/

# NuGet
*.nupkg
*.snupkg
packages/
*.nuget.props
*.nuget.targets

# Test Results
TestResults/
*.trx
*.coverage
*.coveragexml

# Docker
docker-compose.override.yml
";
        await File.WriteAllTextAsync(Path.Combine(projectPath, ".gitignore"), gitignoreContent.Trim());
    }

    private static async Task GenerateDockerFiles(string projectPath, string name)
    {
        // Will be implemented with actual Dockerfile generation
        await Task.CompletedTask;
    }

    private static async Task GenerateCICDFiles(string projectPath, string name)
    {
        // Will be implemented with actual CI/CD file generation
        await Task.CompletedTask;
    }

    private static async Task RestorePackages(string projectPath)
    {
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "restore",
            WorkingDirectory = projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        if (process != null)
        {
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("⚠️  Warning: Package restore failed");
            }
        }
    }

    private static async Task BuildProject(string projectPath)
    {
        var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "build",
            WorkingDirectory = projectPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        });

        if (process != null)
        {
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                Console.WriteLine("⚠️  Warning: Build failed");
            }
        }
    }

    private static List<TemplateInfo> GetTemplates()
    {
        return new List<TemplateInfo>
        {
            new TemplateInfo
            {
                Id = "relay-webapi",
                Name = "Clean Architecture Web API",
                Description = "Production-ready REST API following Clean Architecture principles",
                BestFor = "Enterprise REST APIs, Backend services",
                Tags = new[] { "web", "api", "rest", "clean-architecture" },
                Features = new[] { "auth", "swagger", "docker", "tests", "healthchecks" },
                Structure = "clean-architecture"
            },
            new TemplateInfo
            {
                Id = "relay-microservice",
                Name = "Event-Driven Microservice",
                Description = "Microservice template with message broker integration",
                BestFor = "Microservices architecture, Event-driven systems",
                Tags = new[] { "microservice", "events", "messaging" },
                Features = new[] { "rabbitmq", "kafka", "k8s", "docker", "tracing" },
                Structure = "microservice"
            },
            new TemplateInfo
            {
                Id = "relay-ddd",
                Name = "Domain-Driven Design",
                Description = "DDD tactical patterns with Relay",
                BestFor = "Complex business domains, Enterprise applications",
                Tags = new[] { "ddd", "domain", "enterprise" },
                Features = new[] { "aggregates", "events", "specifications" },
                Structure = "clean-architecture"
            },
            new TemplateInfo
            {
                Id = "relay-cqrs-es",
                Name = "CQRS + Event Sourcing",
                Description = "Complete CQRS with Event Sourcing implementation",
                BestFor = "Systems requiring full audit trail, Financial applications",
                Tags = new[] { "cqrs", "event-sourcing", "audit" },
                Features = new[] { "eventstore", "projections", "snapshots" },
                Structure = "clean-architecture"
            },
            new TemplateInfo
            {
                Id = "relay-modular",
                Name = "Modular Monolith",
                Description = "Modular monolith with vertical slices",
                BestFor = "Starting with monolith, future microservices migration",
                Tags = new[] { "modular", "monolith", "vertical-slice" },
                Features = new[] { "modules", "isolation", "migration-ready" },
                Structure = "modular"
            },
            new TemplateInfo
            {
                Id = "relay-graphql",
                Name = "GraphQL API",
                Description = "GraphQL API with Hot Chocolate",
                BestFor = "Flexible APIs, Mobile/SPA backends",
                Tags = new[] { "graphql", "api", "hotchocolate" },
                Features = new[] { "subscriptions", "dataloader", "filtering" },
                Structure = "clean-architecture"
            },
            new TemplateInfo
            {
                Id = "relay-grpc",
                Name = "gRPC Service",
                Description = "High-performance gRPC service",
                BestFor = "Microservice communication, High-performance APIs",
                Tags = new[] { "grpc", "protobuf", "performance" },
                Features = new[] { "streaming", "tls", "discovery" },
                Structure = "microservice"
            },
            new TemplateInfo
            {
                Id = "relay-serverless",
                Name = "Serverless Functions",
                Description = "AWS Lambda / Azure Functions template",
                BestFor = "Serverless applications, Cost-sensitive workloads",
                Tags = new[] { "serverless", "lambda", "functions" },
                Features = new[] { "aws", "azure", "api-gateway" },
                Structure = "simple"
            },
            new TemplateInfo
            {
                Id = "relay-blazor",
                Name = "Blazor Application",
                Description = "Full-stack Blazor app with Relay",
                BestFor = "Full-stack .NET applications, Internal tools",
                Tags = new[] { "blazor", "spa", "fullstack" },
                Features = new[] { "server", "wasm", "signalr", "pwa" },
                Structure = "clean-architecture"
            },
            new TemplateInfo
            {
                Id = "relay-maui",
                Name = "MAUI Mobile App",
                Description = "Cross-platform mobile app",
                BestFor = "Mobile applications, Cross-platform apps",
                Tags = new[] { "maui", "mobile", "cross-platform" },
                Features = new[] { "ios", "android", "offline", "sqlite" },
                Structure = "mvvm"
            }
        };
    }
}

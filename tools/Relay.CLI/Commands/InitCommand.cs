using System.CommandLine;
using System.Text;
using Spectre.Console;

namespace Relay.CLI.Commands;

/// <summary>
/// Init command - initializes a new Relay project with scaffolding
/// </summary>
public static class InitCommand
{
    public static Command Create()
    {
        var command = new Command("init", "Initialize a new Relay project with complete scaffolding");

        var nameOption = new Option<string>("--name", "Project name") { IsRequired = true };
        var templateOption = new Option<string>("--template", () => "standard", "Project template (minimal, standard, enterprise)");
        var outputOption = new Option<string>("--output", () => ".", "Output directory");
        var frameworkOption = new Option<string>("--framework", () => "net8.0", "Target framework (net6.0, net8.0, net9.0)");
        var gitOption = new Option<bool>("--git", () => true, "Initialize git repository");
        var dockerOption = new Option<bool>("--docker", () => false, "Include Docker support");
        var ciOption = new Option<bool>("--ci", () => false, "Include CI/CD configuration");

        command.AddOption(nameOption);
        command.AddOption(templateOption);
        command.AddOption(outputOption);
        command.AddOption(frameworkOption);
        command.AddOption(gitOption);
        command.AddOption(dockerOption);
        command.AddOption(ciOption);

        command.SetHandler(async (name, template, output, framework, git, docker, ci) =>
        {
            await ExecuteInit(name, template, output, framework, git, docker, ci);
        }, nameOption, templateOption, outputOption, frameworkOption, gitOption, dockerOption, ciOption);

        return command;
    }

    private static async Task ExecuteInit(
        string projectName,
        string template,
        string outputPath,
        string framework,
        bool initGit,
        bool includeDocker,
        bool includeCI)
    {
        var rule = new Rule("[cyan]🚀 Initializing {projectName}[/]");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var fullPath = Path.Combine(outputPath, projectName);

        // Validate
        if (Directory.Exists(fullPath))
        {
            AnsiConsole.MarkupLine("[red]❌ Directory already exists![/]");
            return;
        }

        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var totalSteps = 7 + (includeDocker ? 1 : 0) + (includeCI ? 1 : 0);
                var task = ctx.AddTask("[cyan]Creating project structure[/]", maxValue: totalSteps);

                // Step 1: Create directories
                await CreateDirectoryStructure(fullPath, template);
                task.Increment(1);

                // Step 2: Create solution
                await CreateSolution(fullPath, projectName, framework);
                task.Increment(1);

                // Step 3: Create main project
                await CreateMainProject(fullPath, projectName, framework, template);
                task.Increment(1);

                // Step 4: Create test project
                await CreateTestProject(fullPath, projectName, framework);
                task.Increment(1);

                // Step 5: Create sample handlers
                await CreateSampleCode(fullPath, projectName, template);
                task.Increment(1);

                // Step 6: Create README
                await CreateReadme(fullPath, projectName, template);
                task.Increment(1);

                // Step 7: Create configuration
                await CreateConfiguration(fullPath, template);
                task.Increment(1);

                if (includeDocker)
                {
                    await CreateDockerFiles(fullPath, projectName);
                    task.Increment(1);
                }

                if (includeCI)
                {
                    await CreateCIConfiguration(fullPath);
                    task.Increment(1);
                }

                if (initGit)
                {
                    await InitializeGit(fullPath);
                }

                task.Value = task.MaxValue;
            });

        // Display summary
        DisplayProjectSummary(projectName, fullPath, template, framework);
    }

    private static async Task CreateDirectoryStructure(string basePath, string template)
    {
        Directory.CreateDirectory(basePath);
        Directory.CreateDirectory(Path.Combine(basePath, "src", Path.GetFileName(basePath)));
        Directory.CreateDirectory(Path.Combine(basePath, "tests", $"{Path.GetFileName(basePath)}.Tests"));
        Directory.CreateDirectory(Path.Combine(basePath, "docs"));

        if (template == "enterprise")
        {
            Directory.CreateDirectory(Path.Combine(basePath, "src", Path.GetFileName(basePath), "Handlers"));
            Directory.CreateDirectory(Path.Combine(basePath, "src", Path.GetFileName(basePath), "Requests"));
            Directory.CreateDirectory(Path.Combine(basePath, "src", Path.GetFileName(basePath), "Responses"));
            Directory.CreateDirectory(Path.Combine(basePath, "src", Path.GetFileName(basePath), "Validators"));
            Directory.CreateDirectory(Path.Combine(basePath, "src", Path.GetFileName(basePath), "Behaviors"));
        }

        await Task.CompletedTask;
    }

    private static async Task CreateSolution(string basePath, string projectName, string framework)
    {
        var slnContent = $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}"", ""src\{projectName}\{projectName}.csproj"", ""{{{"{"}{Guid.NewGuid()}{"}"}}}""
EndProject
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}.Tests"", ""tests\{projectName}.Tests\{projectName}.Tests.csproj"", ""{{{"{"}{Guid.NewGuid()}{"}"}}}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
";
        await File.WriteAllTextAsync(Path.Combine(basePath, $"{projectName}.sln"), slnContent.Trim());
    }

    private static async Task CreateMainProject(string basePath, string projectName, string framework, string template)
    {
        var projectPath = Path.Combine(basePath, "src", projectName);
        var csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>{framework}</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.0.0"" />
  </ItemGroup>

</Project>
";
        await File.WriteAllTextAsync(Path.Combine(projectPath, $"{projectName}.csproj"), csprojContent);

        // Create Program.cs
        var programContent = $@"using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay.Core;

var builder = Host.CreateApplicationBuilder(args);

// Register Relay
builder.Services.AddRelay();

var app = builder.Build();

// Example usage
var relay = app.Services.GetRequiredService<IRelay>();

// Your application logic here
Console.WriteLine(""🚀 {projectName} is running with Relay!"");

await app.RunAsync();
";
        await File.WriteAllTextAsync(Path.Combine(projectPath, "Program.cs"), programContent);
    }

    private static async Task CreateTestProject(string basePath, string projectName, string framework)
    {
        var testProjectPath = Path.Combine(basePath, "tests", $"{projectName}.Tests");
        var testCsprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <TargetFramework>{framework}</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.11.0"" />
    <PackageReference Include=""xunit"" Version=""2.9.0"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.8.2"" />
    <PackageReference Include=""FluentAssertions"" Version=""6.12.0"" />
    <PackageReference Include=""NSubstitute"" Version=""5.1.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\..\src\{projectName}\{projectName}.csproj"" />
  </ItemGroup>

</Project>
";
        await File.WriteAllTextAsync(Path.Combine(testProjectPath, $"{projectName}.Tests.csproj"), testCsprojContent);

        // Create sample test
        var testContent = $@"using Xunit;
using FluentAssertions;

namespace {projectName}.Tests;

public class SampleTests
{{
    [Fact]
    public void SampleTest_ShouldPass()
    {{
        // Arrange
        var value = 42;

        // Act
        var result = value * 2;

        // Assert
        result.Should().Be(84);
    }}
}}
";
        await File.WriteAllTextAsync(Path.Combine(testProjectPath, "SampleTests.cs"), testContent);
    }

    private static async Task CreateSampleCode(string basePath, string projectName, string template)
    {
        var srcPath = Path.Combine(basePath, "src", projectName);

        // Create a sample request
        var requestContent = $@"using Relay.Core;

namespace {projectName}.Requests;

/// <summary>
/// Sample request for getting user information
/// </summary>
public record GetUserQuery(int UserId) : IRequest<UserResponse>;
";

        // Create a sample response
        var responseContent = $@"namespace {projectName}.Requests;

/// <summary>
/// Response containing user information
/// </summary>
public record UserResponse(int Id, string Name, string Email);
";

        // Create a sample handler
        var handlerContent = $@"using Relay.Core;
using {projectName}.Requests;

namespace {projectName}.Handlers;

/// <summary>
/// Handler for GetUserQuery
/// </summary>
public class GetUserHandler : IRequestHandler<GetUserQuery, UserResponse>
{{
    [Handle] // Enable source generator optimizations
    public async ValueTask<UserResponse> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {{
        // TODO: Implement your business logic here
        await Task.Delay(10, cancellationToken); // Simulate async work
        
        return new UserResponse(
            request.UserId,
            ""Sample User"",
            ""user@example.com""
        );
    }}
}}
";

        if (template == "enterprise")
        {
            await File.WriteAllTextAsync(Path.Combine(srcPath, "Requests", "GetUserQuery.cs"), requestContent);
            await File.WriteAllTextAsync(Path.Combine(srcPath, "Responses", "UserResponse.cs"), responseContent);
            await File.WriteAllTextAsync(Path.Combine(srcPath, "Handlers", "GetUserHandler.cs"), handlerContent);
        }
        else
        {
            // For minimal/standard, create in root
            await File.WriteAllTextAsync(Path.Combine(srcPath, "GetUserQuery.cs"), requestContent);
            await File.WriteAllTextAsync(Path.Combine(srcPath, "UserResponse.cs"), responseContent);
            await File.WriteAllTextAsync(Path.Combine(srcPath, "GetUserHandler.cs"), handlerContent);
        }
    }

    private static async Task CreateReadme(string basePath, string projectName, string template)
    {
        var readmeContent = $@"# {projectName}

A high-performance application built with [Relay](https://github.com/genc-murat/relay) mediator framework.

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Your favorite IDE (Visual Studio, VS Code, Rider)

### Building

```bash
dotnet build
```

### Running

```bash
dotnet run --project src/{projectName}
```

### Testing

```bash
dotnet test
```

## 📖 Project Structure

```
{projectName}/
├── src/
│   └── {projectName}/           # Main application
├── tests/
│   └── {projectName}.Tests/     # Unit tests
└── docs/                        # Documentation
```

## 🎯 Features

- ⚡ High-performance request/response handling with Relay
- 🧪 Comprehensive test coverage
- 📦 Modern .NET architecture
- 🔧 Easy to extend and maintain

## 📚 Learn More

- [Relay Documentation](https://github.com/genc-murat/relay)
- [Relay Examples](https://github.com/genc-murat/relay/tree/main/docs/examples)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📝 License

This project is licensed under the MIT License.
";
        await File.WriteAllTextAsync(Path.Combine(basePath, "README.md"), readmeContent);
    }

    private static async Task CreateConfiguration(string basePath, string template)
    {
        var configContent = @"{
  ""version"": ""2.0"",
  ""relay"": {
    ""enableCaching"": false,
    ""enableValidation"": true,
    ""defaultTimeout"": ""00:00:30"",
    ""maxRetryAttempts"": 3
  }
}
";
        var cliConfigContent = @"{
  ""defaultNamespace"": ""MyApp"",
  ""templatePreference"": ""standard"",
  ""optimizationLevel"": ""standard"",
  ""includeTests"": true,
  ""backupOnOptimize"": true
}
";
        await File.WriteAllTextAsync(Path.Combine(basePath, "appsettings.json"), configContent);
        await File.WriteAllTextAsync(Path.Combine(basePath, ".relay-cli.json"), cliConfigContent);
    }

    private static async Task CreateDockerFiles(string basePath, string projectName)
    {
        var dockerfileContent = $@"FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY [""src/{projectName}/{projectName}.csproj"", ""src/{projectName}/""]
RUN dotnet restore ""src/{projectName}/{projectName}.csproj""
COPY . .
WORKDIR ""/src/src/{projectName}""
RUN dotnet build ""{projectName}.csproj"" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ""{projectName}.csproj"" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""{projectName}.dll""]
";
        var dockerComposeContent = $@"version: '3.8'
services:
  {projectName.ToLower()}:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - ""8080:8080""
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
";
        await File.WriteAllTextAsync(Path.Combine(basePath, "Dockerfile"), dockerfileContent);
        await File.WriteAllTextAsync(Path.Combine(basePath, "docker-compose.yml"), dockerComposeContent);
    }

    private static async Task CreateCIConfiguration(string basePath)
    {
        var githubActionsPath = Path.Combine(basePath, ".github", "workflows");
        Directory.CreateDirectory(githubActionsPath);

        var ciContent = @"name: CI

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal
";
        await File.WriteAllTextAsync(Path.Combine(githubActionsPath, "ci.yml"), ciContent);
    }

    private static async Task InitializeGit(string basePath)
    {
        var gitignoreContent = @"## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Bb]in/
[Oo]bj/

# Visual Studio cache/options
.vs/

# Visual Studio Code
.vscode/

# JetBrains Rider
.idea/
*.sln.iml

# NuGet Packages
*.nupkg
*.snupkg
**/packages/*

# .NET
project.lock.json
project.fragment.lock.json
artifacts/
";
        await File.WriteAllTextAsync(Path.Combine(basePath, ".gitignore"), gitignoreContent);
    }

    private static void DisplayProjectSummary(string projectName, string path, string template, string framework)
    {
        AnsiConsole.WriteLine();
        var rule = new Rule("[green]✅ Project Created Successfully![/]");
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Property[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Project Name", projectName);
        table.AddRow("Template", template);
        table.AddRow("Framework", framework);
        table.AddRow("Location", path);

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[cyan]🎯 Next Steps:[/]");
        AnsiConsole.MarkupLine($"   1. [dim]cd {projectName}[/]");
        AnsiConsole.MarkupLine("   2. [dim]dotnet build[/]");
        AnsiConsole.MarkupLine("   3. [dim]dotnet test[/]");
        AnsiConsole.MarkupLine($"   4. [dim]dotnet run --project src/{projectName}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Happy coding with Relay! 🚀[/]");
    }
}

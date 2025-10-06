namespace Relay.CLI.TemplateEngine;

/// <summary>
/// Core template generator for creating project structures and files
/// </summary>
public class TemplateGenerator
{
    private readonly string _templatesPath;
    private readonly Dictionary<string, string> _variables;

    public TemplateGenerator(string templatesPath)
    {
        _templatesPath = templatesPath;
        _variables = new Dictionary<string, string>();
    }

    public void AddVariable(string key, string value)
    {
        _variables[key] = value;
    }

    public async Task<GenerationResult> GenerateAsync(string templateId, string projectName, string outputPath, GenerationOptions options)
    {
        var result = new GenerationResult { Success = true, TemplateName = templateId };
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Validate project name first
            var validator = new TemplateValidator();
            var validationResult = validator.ValidateProjectName(projectName);
            if (!validationResult.IsValid)
            {
                result.Success = false;
                result.Message = $"Invalid project name: {string.Join(", ", validationResult.Errors)}";
                result.Errors.AddRange(validationResult.Errors);
                return result;
            }

            Console.WriteLine();
            Console.WriteLine($"🎨 Generating project '{projectName}' from template '{templateId}'...");
            Console.WriteLine();

            // Setup variables
            SetupVariables(projectName, options);

            // Create directory structure
            await CreateDirectoryStructureAsync(templateId, projectName, outputPath, options, result);

            // Generate project files
            await GenerateProjectFilesAsync(templateId, projectName, outputPath, options, result);

            // Generate common files
            await GenerateCommonFilesAsync(projectName, outputPath, options, result);

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = true;
            result.Message = $"✅ Project '{projectName}' created successfully in {result.Duration.TotalSeconds:F1}s!";

            // Display summary
            DisplayGenerationSummary(result, projectName);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = false;
            result.Message = $"❌ Error generating project: {ex.Message}";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private void SetupVariables(string projectName, GenerationOptions options)
    {
        _variables["ProjectName"] = projectName;
        _variables["ProjectGuid"] = Guid.NewGuid().ToString();
        _variables["Year"] = DateTime.Now.Year.ToString();
        _variables["Author"] = options.Author ?? Environment.UserName;
        _variables["EnableAuth"] = options.EnableAuth.ToString().ToLower();
        _variables["EnableSwagger"] = options.EnableSwagger.ToString().ToLower();
        _variables["EnableDocker"] = options.EnableDocker.ToString().ToLower();
        _variables["EnableHealthChecks"] = options.EnableHealthChecks.ToString().ToLower();
        _variables["EnableCaching"] = options.EnableCaching.ToString().ToLower();
        _variables["EnableTelemetry"] = options.EnableTelemetry.ToString().ToLower();
        _variables["DatabaseProvider"] = options.DatabaseProvider ?? "postgres";
        _variables["TargetFramework"] = options.TargetFramework ?? "net8.0";
    }

    private async Task CreateDirectoryStructureAsync(string templateId, string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        Console.WriteLine("📁 Creating directory structure...");
        var directories = GetDirectoryStructure(templateId, projectName, options);

        if (directories == null || directories.Length == 0)
        {
            throw new ArgumentException($"Invalid or unsupported template: {templateId}");
        }

        foreach (var dir in directories)
        {
            var fullPath = Path.Combine(outputPath, dir);
            Directory.CreateDirectory(fullPath);
            result.CreatedDirectories.Add(dir);
        }

        Console.WriteLine($"  ✓ Created {result.CreatedDirectories.Count} directories");
        await Task.CompletedTask;
    }

    private string[]? GetDirectoryStructure(string templateId, string projectName, GenerationOptions options)
    {
        return templateId switch
        {
            "relay-webapi" => GetWebApiDirectoryStructure(projectName),
            "relay-microservice" => GetMicroserviceDirectoryStructure(projectName),
            "relay-ddd" => GetDddDirectoryStructure(projectName),
            "relay-modular" => GetModularDirectoryStructure(projectName, options),
            "relay-graphql" => GetGraphQLDirectoryStructure(projectName),
            "relay-grpc" => GetGrpcDirectoryStructure(projectName),
            _ => null // Return null for invalid templates
        };
    }

    private string[] GetWebApiDirectoryStructure(string projectName)
    {
        return new[]
        {
            "src",
            $"src/{projectName}.Api",
            $"src/{projectName}.Api/Controllers",
            $"src/{projectName}.Api/Middleware",
            $"src/{projectName}.Application",
            $"src/{projectName}.Application/Common",
            $"src/{projectName}.Application/Features",
            $"src/{projectName}.Application/Features/Products",
            $"src/{projectName}.Application/Features/Products/Commands",
            $"src/{projectName}.Application/Features/Products/Queries",
            $"src/{projectName}.Domain",
            $"src/{projectName}.Domain/Entities",
            $"src/{projectName}.Infrastructure",
            $"src/{projectName}.Infrastructure/Persistence",
            "tests",
            $"tests/{projectName}.UnitTests",
            $"tests/{projectName}.IntegrationTests",
            "docs"
        };
    }

    private string[] GetMicroserviceDirectoryStructure(string projectName)
    {
        return new[]
        {
            "src",
            $"src/{projectName}",
            $"src/{projectName}/Application",
            $"src/{projectName}/Domain",
            $"src/{projectName}/Infrastructure",
            $"src/{projectName}/Infrastructure/Messaging",
            "tests",
            $"tests/{projectName}.Tests",
            "k8s",
            "helm",
            "docs"
        };
    }

    private string[] GetDddDirectoryStructure(string projectName)
    {
        return new[]
        {
            "src",
            $"src/{projectName}.Api",
            $"src/{projectName}.Application",
            $"src/{projectName}.Domain",
            $"src/{projectName}.Domain/Aggregates",
            $"src/{projectName}.Domain/DomainEvents",
            $"src/{projectName}.Infrastructure",
            "tests",
            "docs"
        };
    }

    private string[] GetModularDirectoryStructure(string projectName, GenerationOptions options)
    {
        var modules = options.Modules ?? new[] { "Catalog", "Orders" };
        var dirs = new List<string>
        {
            "src",
            $"src/{projectName}.Api",
            $"src/{projectName}.Modules",
            $"src/{projectName}.Shared"
        };

        foreach (var module in modules)
        {
            dirs.Add($"src/{projectName}.Modules/{module}");
        }

        dirs.AddRange(new[] { "tests", "docs" });
        return dirs.ToArray();
    }

    private string[] GetGraphQLDirectoryStructure(string projectName)
    {
        return new[]
        {
            "src",
            $"src/{projectName}",
            $"src/{projectName}/Schema",
            $"src/{projectName}/Schema/Queries",
            $"src/{projectName}/Schema/Mutations",
            "tests",
            "docs"
        };
    }

    private string[] GetGrpcDirectoryStructure(string projectName)
    {
        return new[]
        {
            "src",
            $"src/{projectName}",
            $"src/{projectName}/Protos",
            $"src/{projectName}/Services",
            "tests",
            "docs"
        };
    }

    private async Task GenerateProjectFilesAsync(string templateId, string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        Console.WriteLine("📝 Generating project files...");

        switch (templateId)
        {
            case "relay-webapi":
                await GenerateWebApiFilesAsync(projectName, outputPath, options, result);
                break;
            case "relay-microservice":
                await GenerateMicroserviceFilesAsync(projectName, outputPath, options, result);
                break;
            case "relay-ddd":
                await GenerateDddFilesAsync(projectName, outputPath, options, result);
                break;
            case "relay-modular":
                await GenerateModularFilesAsync(projectName, outputPath, options, result);
                break;
            case "relay-graphql":
                await GenerateGraphQLFilesAsync(projectName, outputPath, options, result);
                break;
            case "relay-grpc":
                await GenerateGrpcFilesAsync(projectName, outputPath, options, result);
                break;
            default:
                await GenerateBasicProjectAsync(projectName, outputPath, options, result);
                break;
        }

        Console.WriteLine($"  ✓ Generated {result.CreatedFiles.Count} files");
    }

    private async Task GenerateWebApiFilesAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        await GenerateSolutionFileAsync(projectName, outputPath, result);
        await GenerateApiProjectAsync(projectName, outputPath, options, result);
        await GenerateApplicationProjectAsync(projectName, outputPath, options, result);
        await GenerateDomainProjectAsync(projectName, outputPath, options, result);
        await GenerateInfrastructureProjectAsync(projectName, outputPath, options, result);
        await GenerateTestProjectsAsync(projectName, outputPath, options, result);
        await GenerateExampleHandlersAsync(projectName, outputPath, options, result);
    }

    private async Task GenerateBasicProjectAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        // Generate solution file
        await GenerateSolutionFileAsync(projectName, outputPath, result);

        // Generate main project file
        var projectPath = Path.Combine(outputPath, "src", projectName, $"{projectName}.csproj");
        var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Relay"" Version=""*"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(projectPath, projectContent);
        result.CreatedFiles.Add($"src/{projectName}/{projectName}.csproj");

        // Generate Program.cs
        var programPath = Path.Combine(outputPath, "src", projectName, "Program.cs");
        var programContent = @"var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Configure middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
";
        await File.WriteAllTextAsync(programPath, programContent);
        result.CreatedFiles.Add($"src/{projectName}/Program.cs");

        // Generate appsettings.json
        var appsettingsPath = Path.Combine(outputPath, "src", projectName, "appsettings.json");
        var appsettingsContent = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }
  },
  ""AllowedHosts"": ""*""
}
";
        await File.WriteAllTextAsync(appsettingsPath, appsettingsContent);
        result.CreatedFiles.Add($"src/{projectName}/appsettings.json");

        // Generate test project
        var testProjectPath = Path.Combine(outputPath, "tests", $"{projectName}.Tests", $"{projectName}.Tests.csproj");
        var testProjectContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""xunit"" Version=""2.5.0"" />
    <PackageReference Include=""xunit.runner.visualstudio"" Version=""2.5.0"" />
    <PackageReference Include=""FluentAssertions"" Version=""6.12.0"" />
    <PackageReference Include=""Microsoft.NET.Test.Sdk"" Version=""17.8.0"" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include=""..\..\src\{projectName}\{projectName}.csproj"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(testProjectPath, testProjectContent);
        result.CreatedFiles.Add($"tests/{projectName}.Tests/{projectName}.Tests.csproj");
    }

    private async Task GenerateCommonFilesAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        Console.WriteLine("📄 Generating common files...");
        await GenerateGitignoreAsync(outputPath, result);
        await GenerateReadmeAsync(projectName, outputPath, options, result);

        if (options.EnableDocker)
        {
            await GenerateDockerfilesAsync(projectName, outputPath, result);
        }
    }

    private void DisplayGenerationSummary(GenerationResult result, string projectName)
    {
        Console.WriteLine();
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine("✅ PROJECT CREATED SUCCESSFULLY!");
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine();
        Console.WriteLine($"📊 Generation Summary:");
        Console.WriteLine($"  • Directories created: {result.CreatedDirectories.Count}");
        Console.WriteLine($"  • Files generated: {result.CreatedFiles.Count}");
        Console.WriteLine($"  • Time taken: {result.Duration.TotalSeconds:F1}s");
        Console.WriteLine();
        Console.WriteLine($"📁 Project location: {projectName}/");
        Console.WriteLine();
        Console.WriteLine("🚀 Next steps:");
        Console.WriteLine($"  1. cd {projectName}");
        Console.WriteLine("  2. dotnet restore");
        Console.WriteLine($"  3. dotnet run --project src/{projectName}.Api");
        Console.WriteLine();
        Console.WriteLine("📚 Documentation:");
        Console.WriteLine($"  • README.md - Getting started guide");
        Console.WriteLine($"  • docs/ - Additional documentation");
        Console.WriteLine();
    }

    private async Task GenerateSolutionFileAsync(string projectName, string outputPath, GenerationResult result)
    {
        var solutionPath = Path.Combine(outputPath, $"{projectName}.sln");
        var vsVersion = GetVisualStudioVersion();
        var content = $@"
Microsoft Visual Studio Solution File, Format Version 12.00
{vsVersion}MinimumVisualStudioVersion = 10.0.40219.1
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
EndGlobal
";
        await File.WriteAllTextAsync(solutionPath, content.TrimStart());
        result.CreatedFiles.Add($"{projectName}.sln");
    }

    private string GetVisualStudioVersion()
    {
        try
        {
            // Try to get VS version from vswhere
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            var vswherePath = Path.Combine(programFiles, "Microsoft Visual Studio", "Installer", "vswhere.exe");

            if (File.Exists(vswherePath))
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = vswherePath,
                        Arguments = "-latest -property catalog_productDisplayVersion",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var version = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(version) && version.StartsWith("17."))
                {
                    return $"# Visual Studio Version 17\nVisualStudioVersion = {version}\n";
                }
                else if (!string.IsNullOrEmpty(version) && int.TryParse(version.Split('.')[0], out int majorVersion))
                {
                    return $"# Visual Studio Version {majorVersion}\nVisualStudioVersion = {version}\n";
                }
            }
        }
        catch
        {
            // If we can't detect, return empty string - solution will still work
        }

        return string.Empty;
    }

    private async Task GenerateApiProjectAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        var projectPath = Path.Combine(outputPath, "src", $"{projectName}.Api", $"{projectName}.Api.csproj");
        var content = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Relay"" Version=""*"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(projectPath, content);
        result.CreatedFiles.Add($"src/{projectName}.Api/{projectName}.Api.csproj");

        // Generate Program.cs
        var programPath = Path.Combine(outputPath, "src", $"{projectName}.Api", "Program.cs");
        var programContent = @"var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet(""/"", () => ""Hello World!"");
app.Run();
";
        await File.WriteAllTextAsync(programPath, programContent);
        result.CreatedFiles.Add($"src/{projectName}.Api/Program.cs");
    }

    private async Task GenerateApplicationProjectAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        var projectPath = Path.Combine(outputPath, "src", $"{projectName}.Application", $"{projectName}.Application.csproj");
        var content = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""*"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(projectPath, content);
        result.CreatedFiles.Add($"src/{projectName}.Application/{projectName}.Application.csproj");
    }

    private async Task GenerateDomainProjectAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        var projectPath = Path.Combine(outputPath, "src", $"{projectName}.Domain", $"{projectName}.Domain.csproj");
        var content = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
";
        await File.WriteAllTextAsync(projectPath, content);
        result.CreatedFiles.Add($"src/{projectName}.Domain/{projectName}.Domain.csproj");
    }

    private async Task GenerateInfrastructureProjectAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        var projectPath = Path.Combine(outputPath, "src", $"{projectName}.Infrastructure", $"{projectName}.Infrastructure.csproj");
        var content = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
";
        await File.WriteAllTextAsync(projectPath, content);
        result.CreatedFiles.Add($"src/{projectName}.Infrastructure/{projectName}.Infrastructure.csproj");
    }

    private async Task GenerateTestProjectsAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        // Unit tests
        var unitTestPath = Path.Combine(outputPath, "tests", $"{projectName}.UnitTests", $"{projectName}.UnitTests.csproj");
        var content = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""xunit"" Version=""2.5.0"" />
    <PackageReference Include=""FluentAssertions"" Version=""6.12.0"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(unitTestPath, content);
        result.CreatedFiles.Add($"tests/{projectName}.UnitTests/{projectName}.UnitTests.csproj");
    }

    private async Task GenerateExampleHandlersAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        // Example command handler
        var commandPath = Path.Combine(outputPath, "src", $"{projectName}.Application", "Features", "Products", "Commands", "CreateProductCommand.cs");
        var commandContent = @"namespace Application.Features.Products.Commands;

public record CreateProductCommand(string Name, decimal Price);
";
        await File.WriteAllTextAsync(commandPath, commandContent);
        result.CreatedFiles.Add($"src/{projectName}.Application/Features/Products/Commands/CreateProductCommand.cs");
    }

    private async Task GenerateGitignoreAsync(string outputPath, GenerationResult result)
    {
        var gitignorePath = Path.Combine(outputPath, ".gitignore");
        var content = @"bin/
obj/
.vs/
*.user
*.suo
";
        await File.WriteAllTextAsync(gitignorePath, content);
        result.CreatedFiles.Add(".gitignore");
    }

    private async Task GenerateReadmeAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        var readmePath = Path.Combine(outputPath, "README.md");
        var content = $@"# {projectName}

## Getting Started

```bash
dotnet restore
dotnet build
dotnet run --project src/{projectName}.Api
```
";
        await File.WriteAllTextAsync(readmePath, content);
        result.CreatedFiles.Add("README.md");
    }

    private async Task GenerateDockerfilesAsync(string projectName, string outputPath, GenerationResult result)
    {
        var dockerfilePath = Path.Combine(outputPath, "Dockerfile");
        var content = @"FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT [""dotnet"", ""Api.dll""]
";
        await File.WriteAllTextAsync(dockerfilePath, content);
        result.CreatedFiles.Add("Dockerfile");
    }

    private async Task GenerateMicroserviceFilesAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        await GenerateSolutionFileAsync(projectName, outputPath, result);
        await GenerateBasicProjectAsync(projectName, outputPath, options, result);
    }

    private async Task GenerateDddFilesAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        await GenerateSolutionFileAsync(projectName, outputPath, result);
        await GenerateApiProjectAsync(projectName, outputPath, options, result);
        await GenerateApplicationProjectAsync(projectName, outputPath, options, result);
        await GenerateDomainProjectAsync(projectName, outputPath, options, result);
        await GenerateInfrastructureProjectAsync(projectName, outputPath, options, result);
    }

    private async Task GenerateModularFilesAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        await GenerateSolutionFileAsync(projectName, outputPath, result);

        // Generate API project
        var apiProjectPath = Path.Combine(outputPath, "src", $"{projectName}.Api", $"{projectName}.Api.csproj");
        var apiContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Relay"" Version=""*"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(apiProjectPath, apiContent);
        result.CreatedFiles.Add($"src/{projectName}.Api/{projectName}.Api.csproj");

        // Generate shared project
        var sharedProjectPath = Path.Combine(outputPath, "src", $"{projectName}.Shared", $"{projectName}.Shared.csproj");
        var sharedContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
";
        await File.WriteAllTextAsync(sharedProjectPath, sharedContent);
        result.CreatedFiles.Add($"src/{projectName}.Shared/{projectName}.Shared.csproj");

        // Generate module projects
        var modules = options.Modules ?? new[] { "Catalog", "Orders" };
        foreach (var module in modules)
        {
            var moduleProjectPath = Path.Combine(outputPath, "src", $"{projectName}.Modules", module, $"{module}.csproj");
            var moduleContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
";
            await File.WriteAllTextAsync(moduleProjectPath, moduleContent);
            result.CreatedFiles.Add($"src/{projectName}.Modules/{module}/{module}.csproj");
        }
    }

    private async Task GenerateGraphQLFilesAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        await GenerateSolutionFileAsync(projectName, outputPath, result);

        // Generate main project with HotChocolate
        var projectPath = Path.Combine(outputPath, "src", projectName, $"{projectName}.csproj");
        var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""HotChocolate.AspNetCore"" Version=""13.9.0"" />
    <PackageReference Include=""Relay"" Version=""*"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(projectPath, projectContent);
        result.CreatedFiles.Add($"src/{projectName}/{projectName}.csproj");

        // Generate Program.cs with GraphQL setup
        var programPath = Path.Combine(outputPath, "src", projectName, "Program.cs");
        var programContent = @"var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();

var app = builder.Build();

app.MapGraphQL();

app.Run();

public class Query
{
    public string Hello() => ""Hello from GraphQL!"";
}

public class Mutation
{
    public string Echo(string message) => message;
}
";
        await File.WriteAllTextAsync(programPath, programContent);
        result.CreatedFiles.Add($"src/{projectName}/Program.cs");
    }

    private async Task GenerateGrpcFilesAsync(string projectName, string outputPath, GenerationOptions options, GenerationResult result)
    {
        await GenerateSolutionFileAsync(projectName, outputPath, result);

        // Generate main project with gRPC
        var projectPath = Path.Combine(outputPath, "src", projectName, $"{projectName}.csproj");
        var projectContent = $@"<Project Sdk=""Microsoft.NET.Sdk.Web"">
  <PropertyGroup>
    <TargetFramework>{options.TargetFramework ?? "net8.0"}</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Grpc.AspNetCore"" Version=""2.60.0"" />
    <PackageReference Include=""Relay"" Version=""*"" />
  </ItemGroup>

  <ItemGroup>
    <Protobuf Include=""Protos\greet.proto"" GrpcServices=""Server"" />
  </ItemGroup>
</Project>
";
        await File.WriteAllTextAsync(projectPath, projectContent);
        result.CreatedFiles.Add($"src/{projectName}/{projectName}.csproj");

        // Generate sample proto file
        var protoPath = Path.Combine(outputPath, "src", projectName, "Protos", "greet.proto");
        var protoContent = @"syntax = ""proto3"";

option csharp_namespace = ""GrpcService"";

package greet;

service Greeter {
  rpc SayHello (HelloRequest) returns (HelloReply);
}

message HelloRequest {
  string name = 1;
}

message HelloReply {
  string message = 1;
}
";
        await File.WriteAllTextAsync(protoPath, protoContent);
        result.CreatedFiles.Add($"src/{projectName}/Protos/greet.proto");

        // Generate Program.cs
        var programPath = Path.Combine(outputPath, "src", projectName, "Program.cs");
        var programContent = @"var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<GreeterService>();

app.Run();
";
        await File.WriteAllTextAsync(programPath, programContent);
        result.CreatedFiles.Add($"src/{projectName}/Program.cs");

        // Generate sample service
        var servicePath = Path.Combine(outputPath, "src", projectName, "Services", "GreeterService.cs");
        var serviceContent = @"using Grpc.Core;

namespace GrpcService.Services;

public class GreeterService : Greeter.GreeterBase
{
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        return Task.FromResult(new HelloReply
        {
            Message = ""Hello "" + request.Name
        });
    }
}
";
        await File.WriteAllTextAsync(servicePath, serviceContent);
        result.CreatedFiles.Add($"src/{projectName}/Services/GreeterService.cs");
    }
}

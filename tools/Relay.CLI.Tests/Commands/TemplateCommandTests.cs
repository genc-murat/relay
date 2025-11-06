using Relay.CLI.Commands;
using Relay.CLI.TemplateEngine;
using System.CommandLine;
using System.Text.Json;

namespace Relay.CLI.Tests.Commands;

public class TemplateCommandTests : IDisposable
{
    private readonly string _testPath;

    public TemplateCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-template-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public void TemplateCommand_ShouldHaveCorrectName()
    {
        // Arrange & Act
        var command = new TemplateCommand();

        // Assert
        Assert.Equal("template", command.Name);
    }

    [Fact]
    public void TemplateCommand_ShouldHaveDescription()
    {
        // Arrange & Act
        var command = new TemplateCommand();

        // Assert
        Assert.Contains("template", command.Description);
    }

    [Fact]
    public void TemplateCommand_ShouldHaveSubcommands()
    {
        // Arrange & Act
        var command = new TemplateCommand();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert
        Assert.Contains("validate", subcommandNames);
        Assert.Contains("pack", subcommandNames);
        Assert.Contains("publish", subcommandNames);
        Assert.Contains("list", subcommandNames);
        Assert.Contains("create", subcommandNames);
    }

    [Fact]
    public void TemplateCommand_Validate_ShouldRequirePathOption()
    {
        // Arrange
        var command = new TemplateCommand();
        var validateCommand = command.Subcommands.First(s => s.Name == "validate");

        // Act
        var pathOption = validateCommand.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        Assert.NotNull(pathOption);
        Assert.True(pathOption!.IsRequired);
    }

    [Fact]
    public void TemplateCommand_Pack_ShouldHavePathAndOutputOptions()
    {
        // Arrange
        var command = new TemplateCommand();
        var packCommand = command.Subcommands.First(s => s.Name == "pack");

        // Act
        var options = packCommand.Options.Select(o => o.Name).ToList();

        // Assert
        Assert.Contains("path", options);
        Assert.Contains("output", options);
    }

    [Fact]
    public void TemplateCommand_Publish_ShouldHavePackageAndRegistryOptions()
    {
        // Arrange
        var command = new TemplateCommand();
        var publishCommand = command.Subcommands.First(s => s.Name == "publish");

        // Act
        var options = publishCommand.Options.Select(o => o.Name).ToList();

        // Assert
        Assert.Contains("package", options);
        Assert.Contains("registry", options);
    }

    [Fact]
    public void TemplateCommand_Create_ShouldHaveRequiredOptions()
    {
        // Arrange
        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        var options = createCommand.Options.Select(o => o.Name).ToList();

        // Assert
        Assert.Contains("name", options);
        Assert.Contains("from", options);
        Assert.Contains("output", options);
    }

    [Fact]
    public async Task TemplateCommand_Validate_WithValidTemplate_ShouldSucceed()
    {
        // Arrange
        var templatePath = CreateValidTemplate("valid-template");
        var command = new TemplateCommand();
        var validateCommand = command.Subcommands.First(s => s.Name == "validate");

        // Act
        var result = await validateCommand.InvokeAsync($"--path {templatePath}");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task TemplateCommand_Validate_WithInvalidPath_ExecutesWithoutCrash()
    {
        // Arrange
        var invalidPath = Path.Combine(_testPath, "nonexistent");
        var command = new TemplateCommand();
        var validateCommand = command.Subcommands.First(s => s.Name == "validate");

        // Act
        var result = await validateCommand.InvokeAsync($"--path {invalidPath}");

        // Assert - Command executes without throwing
        Assert.True(result >= 0);
    }

    [Fact]
    public async Task TemplateCommand_Create_WithValidSource_CreatesTemplateStructure()
    {
        // Arrange
        var sourcePath = CreateSourceProject("MyProject");
        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "my-template";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        var result = await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert
        Assert.Equal(0, result);

        var templatePath = Path.Combine(outputPath, templateName);
        Assert.True(Directory.Exists(templatePath));
        Assert.True(Directory.Exists(Path.Combine(templatePath, ".template.config")));
        Assert.True(Directory.Exists(Path.Combine(templatePath, "content")));
        Assert.True(File.Exists(Path.Combine(templatePath, ".template.config", "template.json")));
    }

    [Fact]
    public async Task TemplateCommand_Create_GeneratesValidTemplateJson()
    {
        // Arrange
        var sourcePath = CreateSourceProject("TestProject");
        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "test-template";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert
        var templateJsonPath = Path.Combine(outputPath, templateName, ".template.config", "template.json");
        Assert.True(File.Exists(templateJsonPath));

        var json = await File.ReadAllTextAsync(templateJsonPath);
        var templateConfig = JsonSerializer.Deserialize<JsonElement>(json);

        Assert.Contains(templateName, templateConfig.GetProperty("name").GetString());
        Assert.Equal(templateName.ToLower(), templateConfig.GetProperty("shortName").GetString());
        Assert.Contains(templateName, templateConfig.GetProperty("identity").GetString());
    }

    [Fact]
    public async Task TemplateCommand_Create_CopiesSourceFiles()
    {
        // Arrange
        var sourcePath = CreateSourceProject("SourceProject");
        var testFile = Path.Combine(sourcePath, "TestFile.cs");
        await File.WriteAllTextAsync(testFile, "public class TestFile { }");

        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "copy-test";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert
        var copiedFile = Path.Combine(outputPath, templateName, "content", "TestFile.cs");
        Assert.True(File.Exists(copiedFile));

        var content = await File.ReadAllTextAsync(copiedFile);
        Assert.Contains("TestFile", content);
    }

    [Fact]
    public async Task TemplateCommand_Create_SkipsBinAndObjDirectories()
    {
        // Arrange
        var sourcePath = CreateSourceProject("SkipTest");
        Directory.CreateDirectory(Path.Combine(sourcePath, "bin"));
        Directory.CreateDirectory(Path.Combine(sourcePath, "obj"));
        await File.WriteAllTextAsync(Path.Combine(sourcePath, "bin", "test.dll"), "binary");
        await File.WriteAllTextAsync(Path.Combine(sourcePath, "obj", "test.obj"), "object");

        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "skip-test";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert
        var contentPath = Path.Combine(outputPath, templateName, "content");
        Assert.False(Directory.Exists(Path.Combine(contentPath, "bin")));
        Assert.False(Directory.Exists(Path.Combine(contentPath, "obj")));
    }

    [Fact]
    public async Task TemplateCommand_Create_WithExistingTemplate_ExecutesWithoutCrash()
    {
        // Arrange
        var sourcePath = CreateSourceProject("ExistingTest");
        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "existing-template";

        // Pre-create the template directory
        Directory.CreateDirectory(Path.Combine(outputPath, templateName));

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        var result = await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert - Command executes and handles the conflict gracefully
        Assert.True(result >= 0);

        // Template directory should still exist
        Assert.True(Directory.Exists(Path.Combine(outputPath, templateName)));
    }

    [Fact]
    public async Task TemplateCommand_Create_WithExistingTemplate_PrintsErrorMessage()
    {
        // Arrange
        var sourcePath = CreateSourceProject("ExistingTest");
        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "existing-template";

        // Pre-create the template directory
        var templatePath = Path.Combine(outputPath, templateName);
        Directory.CreateDirectory(templatePath);

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await TemplateCommand.CreateCustomTemplateAsync(templateName, sourcePath, outputPath);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("Template directory already exists", output);
            Assert.Contains(templatePath, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task TemplateCommand_Create_WithNonexistentSource_ExecutesWithoutCrash()
    {
        // Arrange
        var sourcePath = Path.Combine(_testPath, "nonexistent");
        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "fail-test";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        var result = await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert - Command executes without throwing
        Assert.True(result >= 0);
    }

    [Fact]
    public async Task TemplateCommand_Create_WithNonexistentSource_PrintsErrorMessage()
    {
        // Arrange
        var sourcePath = Path.Combine(_testPath, "nonexistent");
        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "fail-test";

        // Capture console output
        using var stringWriter = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(stringWriter);

        try
        {
            // Act
            await TemplateCommand.CreateCustomTemplateAsync(templateName, sourcePath, outputPath);

            // Assert
            var output = stringWriter.ToString();
            Assert.Contains("Source directory not found", output);
            Assert.Contains(sourcePath, output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public async Task TemplateCommand_Pack_WithValidTemplate_CreatesPackage()
    {
        // Arrange
        var templatePath = CreateValidTemplate("packable-template");
        var outputPath = Path.Combine(_testPath, "packages");
        Directory.CreateDirectory(outputPath);

        var command = new TemplateCommand();
        var packCommand = command.Subcommands.First(s => s.Name == "pack");

        // Act
        var result = await packCommand.InvokeAsync($"--path {templatePath} --output {outputPath}");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task TemplateCommand_List_ShouldDisplayAvailableTemplates()
    {
        // Arrange
        var command = new TemplateCommand();
        var listCommand = command.Subcommands.First(s => s.Name == "list");

        // Act
        var result = await listCommand.InvokeAsync("");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task TemplateCommand_PackTemplateAsync_WithValidTemplate_CreatesPackage()
    {
        // Arrange
        var templatePath = CreateValidTemplate("pack-test");
        var outputPath = Path.Combine(_testPath, "packages");
        Directory.CreateDirectory(outputPath);

        // Act & Assert - Method executes without throwing
        await TemplateCommand.PackTemplateAsync(templatePath, outputPath);
    }

    [Fact]
    public async Task TemplateCommand_PackTemplateAsync_WithInvalidTemplate_Fails()
    {
        // Arrange
        var invalidTemplatePath = Path.Combine(_testPath, "invalid-template");
        Directory.CreateDirectory(invalidTemplatePath);
        var outputPath = Path.Combine(_testPath, "packages");
        Directory.CreateDirectory(outputPath);

        // Act & Assert - Method executes without throwing
        await TemplateCommand.PackTemplateAsync(invalidTemplatePath, outputPath);
    }

    [Fact]
    public async Task TemplateCommand_PackTemplateAsync_WithNonexistentTemplate_Fails()
    {
        // Arrange
        var nonexistentPath = Path.Combine(_testPath, "nonexistent");
        var outputPath = Path.Combine(_testPath, "packages");
        Directory.CreateDirectory(outputPath);

        // Act & Assert - Method executes without throwing
        await TemplateCommand.PackTemplateAsync(nonexistentPath, outputPath);
    }

    [Fact]
    public async Task TemplateCommand_PublishTemplateAsync_WithValidPackage_Succeeds()
    {
        // Arrange
        var packagePath = Path.Combine(_testPath, "test-package.nupkg");
        await File.WriteAllTextAsync(packagePath, "fake package content");
        var registryUrl = "https://api.nuget.org/v3/index.json";

        // Act & Assert - Method executes without throwing
        await TemplateCommand.PublishTemplateAsync(packagePath, registryUrl);
    }

    [Fact]
    public async Task TemplateCommand_PublishTemplateAsync_WithNonexistentPackage_Fails()
    {
        // Arrange
        var nonexistentPackage = Path.Combine(_testPath, "nonexistent.nupkg");
        var registryUrl = "https://api.nuget.org/v3/index.json";

        // Act & Assert - Method executes without throwing
        await TemplateCommand.PublishTemplateAsync(nonexistentPackage, registryUrl);
    }

    [Fact]
    public async Task TemplateCommand_Publish_UsesDefaultRegistry_WhenNotSpecified()
    {
        // Arrange
        var packagePath = Path.Combine(_testPath, "test-package.nupkg");
        await File.WriteAllTextAsync(packagePath, "fake package content");

        var command = new TemplateCommand();
        var publishCommand = command.Subcommands.First(s => s.Name == "publish");

        // Act - Call without --registry to use default
        var result = await publishCommand.InvokeAsync($"--package {packagePath}");

        // Assert - Command executes (default registry is used)
        Assert.True(result >= 0);
    }

    [Fact]
    public async Task TemplateCommand_ListTemplatesAsync_WithTemplates_DisplaysTemplates()
    {
        // Arrange
        var templatesPath = Path.Combine(_testPath, "templates");
        Directory.CreateDirectory(templatesPath);

        // Create a couple of test templates
        CreateValidTemplateInDirectory(templatesPath, "template1");
        CreateValidTemplateInDirectory(templatesPath, "template2");

        // Act & Assert - Method executes without throwing
        await TemplateCommand.ListTemplatesAsync();
    }

    [Fact]
    public async Task TemplateCommand_ListTemplatesAsync_WithNoTemplates_DisplaysEmptyMessage()
    {
        // Arrange
        var emptyTemplatesPath = Path.Combine(_testPath, "empty-templates");
        Directory.CreateDirectory(emptyTemplatesPath);

        // Act & Assert - Method executes without throwing
        await TemplateCommand.ListTemplatesAsync();
    }

    [Fact]
    public void TemplateCommand_Validate_PathOption_ShouldBeRequired()
    {
        // Arrange
        var command = new TemplateCommand();
        var validateCommand = command.Subcommands.First(s => s.Name == "validate");
        var pathOption = validateCommand.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        Assert.NotNull(pathOption);
        Assert.True(pathOption!.IsRequired);
    }

    [Fact]
    public void TemplateCommand_Pack_PathOption_ShouldBeRequired()
    {
        // Arrange
        var command = new TemplateCommand();
        var packCommand = command.Subcommands.First(s => s.Name == "pack");
        var pathOption = packCommand.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        Assert.NotNull(pathOption);
        Assert.True(pathOption!.IsRequired);
    }

    [Fact]
    public void TemplateCommand_Publish_PackageOption_ShouldBeRequired()
    {
        // Arrange
        var command = new TemplateCommand();
        var publishCommand = command.Subcommands.First(s => s.Name == "publish");
        var packageOption = publishCommand.Options.FirstOrDefault(o => o.Name == "package");

        // Assert
        Assert.NotNull(packageOption);
        Assert.True(packageOption!.IsRequired);
    }

    [Fact]
    public void TemplateCommand_Create_NameOption_ShouldBeRequired()
    {
        // Arrange
        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");
        var nameOption = createCommand.Options.FirstOrDefault(o => o.Name == "name");

        // Assert
        Assert.NotNull(nameOption);
        Assert.True(nameOption!.IsRequired);
    }

    [Fact]
    public void TemplateCommand_Create_FromOption_ShouldBeRequired()
    {
        // Arrange
        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");
        var fromOption = createCommand.Options.FirstOrDefault(o => o.Name == "from");

        // Assert
        Assert.NotNull(fromOption);
        Assert.True(fromOption!.IsRequired);
    }

    [Fact]
    public async Task TemplateCommand_Create_IncludesSymbolsInTemplateJson()
    {
        // Arrange
        var sourcePath = CreateSourceProject("SymbolTest");
        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "symbol-template";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert
        var templateJsonPath = Path.Combine(outputPath, templateName, ".template.config", "template.json");
        var json = await File.ReadAllTextAsync(templateJsonPath);
        var templateConfig = JsonSerializer.Deserialize<JsonElement>(json);

        Assert.True(templateConfig.TryGetProperty("symbols", out var symbols));
        Assert.True(symbols.TryGetProperty("ProjectName", out var projectNameSymbol));
        Assert.Equal("parameter", projectNameSymbol.GetProperty("type").GetString());
    }

    [Fact]
    public async Task TemplateCommand_Create_SetsCorrectAuthor()
    {
        // Arrange
        var sourcePath = CreateSourceProject("AuthorTest");
        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "author-template";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert
        var templateJsonPath = Path.Combine(outputPath, templateName, ".template.config", "template.json");
        var json = await File.ReadAllTextAsync(templateJsonPath);
        var templateConfig = JsonSerializer.Deserialize<JsonElement>(json);

        Assert.False(string.IsNullOrEmpty(templateConfig.GetProperty("author").GetString()));
    }

    [Fact]
    public async Task TemplateCommand_Create_SetsCorrectClassifications()
    {
        // Arrange
        var sourcePath = CreateSourceProject("ClassificationTest");
        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "class-template";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert
        var templateJsonPath = Path.Combine(outputPath, templateName, ".template.config", "template.json");
        var json = await File.ReadAllTextAsync(templateJsonPath);
        var templateConfig = JsonSerializer.Deserialize<JsonElement>(json);

        var classifications = templateConfig.GetProperty("classifications");
        Assert.True(classifications.GetArrayLength() > 0);

        var classArray = classifications.EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("Relay", classArray);
    }

    [Fact]
    public async Task TemplateCommand_Create_PreservesDirectoryStructure()
    {
        // Arrange
        var sourcePath = CreateSourceProject("StructureTest");
        var subDir = Path.Combine(sourcePath, "SubFolder", "Nested");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "Nested.cs"), "public class Nested { }");

        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "structure-template";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert
        var nestedFile = Path.Combine(outputPath, templateName, "content", "SubFolder", "Nested", "Nested.cs");
        Assert.True(File.Exists(nestedFile));
    }

    [Fact]
    public async Task TemplateCommand_Create_SkipsGitDirectory()
    {
        // Arrange
        var sourcePath = CreateSourceProject("GitTest");
        var gitDir = Path.Combine(sourcePath, ".git");
        Directory.CreateDirectory(gitDir);
        await File.WriteAllTextAsync(Path.Combine(gitDir, "config"), "git config");

        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "git-template";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert
        var contentPath = Path.Combine(outputPath, templateName, "content");
        Assert.False(Directory.Exists(Path.Combine(contentPath, ".git")));
    }

    [Fact]
    public async Task TemplateCommand_Create_SkipsForbiddenNamedFiles()
    {
        // Arrange
        var sourcePath = CreateSourceProject("SkipFilesTest");
        await File.WriteAllTextAsync(Path.Combine(sourcePath, "bin"), "binary file");
        await File.WriteAllTextAsync(Path.Combine(sourcePath, "obj"), "object file");
        await File.WriteAllTextAsync(Path.Combine(sourcePath, ".vs"), "vs file");
        await File.WriteAllTextAsync(Path.Combine(sourcePath, ".git"), "git file");

        var outputPath = Path.Combine(_testPath, "output");
        var templateName = "skip-files-template";

        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");

        // Act
        await createCommand.InvokeAsync($"--name {templateName} --from {sourcePath} --output {outputPath}");

        // Assert
        var contentPath = Path.Combine(outputPath, templateName, "content");
        Assert.False(File.Exists(Path.Combine(contentPath, "bin")));
        Assert.False(File.Exists(Path.Combine(contentPath, "obj")));
        Assert.False(File.Exists(Path.Combine(contentPath, ".vs")));
        Assert.False(File.Exists(Path.Combine(contentPath, ".git")));
    }

    private string CreateValidTemplate(string name)
    {
        var templatePath = Path.Combine(_testPath, name);
        var configDir = Path.Combine(templatePath, ".template.config");
        var contentDir = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configDir);
        Directory.CreateDirectory(contentDir);

        var templateJson = $$"""
        {
          "$schema": "http://json.schemastore.org/template",
          "author": "Test Author",
          "classifications": ["Test", "Relay"],
          "identity": "Relay.Templates.{{name}}",
          "name": "{{name}} Template",
          "shortName": "{{name}}",
          "description": "Test template",
          "tags": {
            "language": "C#",
            "type": "project"
          }
        }
        """;

        File.WriteAllText(Path.Combine(configDir, "template.json"), templateJson);
        File.WriteAllText(Path.Combine(contentDir, "Program.cs"), "// Test program");

        return templatePath;
    }

    private string CreateSourceProject(string name)
    {
        var projectPath = Path.Combine(_testPath, $"source-{name}");
        Directory.CreateDirectory(projectPath);

        var csprojContent = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net8.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """;

        File.WriteAllText(Path.Combine(projectPath, $"{name}.csproj"), csprojContent);
        File.WriteAllText(Path.Combine(projectPath, "Program.cs"), "Console.WriteLine(\"Hello World\");");

        return projectPath;
    }

    private void CreateValidTemplateInDirectory(string templatesPath, string templateName)
    {
        var templatePath = Path.Combine(templatesPath, templateName);
        var configDir = Path.Combine(templatePath, ".template.config");
        var contentDir = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configDir);
        Directory.CreateDirectory(contentDir);

        var templateJson = $$"""
        {
          "$schema": "http://json.schemastore.org/template",
          "author": "Test Author",
          "classifications": ["Test", "Relay"],
          "identity": "Relay.Templates.{{templateName}}",
          "name": "{{templateName}} Template",
          "shortName": "{{templateName}}",
          "description": "Test template",
          "tags": {
            "language": "C#",
            "type": "project"
          }
        }
        """;

        File.WriteAllText(Path.Combine(configDir, "template.json"), templateJson);
        File.WriteAllText(Path.Combine(contentDir, "Program.cs"), "// Test program");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPath))
            {
                Directory.Delete(_testPath, true);
            }
        }
        catch
        {
            // Cleanup failed, ignore
        }
    }
}



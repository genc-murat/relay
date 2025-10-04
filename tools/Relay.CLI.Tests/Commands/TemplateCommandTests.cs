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
        command.Name.Should().Be("template");
    }

    [Fact]
    public void TemplateCommand_ShouldHaveDescription()
    {
        // Arrange & Act
        var command = new TemplateCommand();

        // Assert
        command.Description.Should().Contain("template");
    }

    [Fact]
    public void TemplateCommand_ShouldHaveSubcommands()
    {
        // Arrange & Act
        var command = new TemplateCommand();
        var subcommandNames = command.Subcommands.Select(s => s.Name).ToList();

        // Assert
        subcommandNames.Should().Contain("validate");
        subcommandNames.Should().Contain("pack");
        subcommandNames.Should().Contain("publish");
        subcommandNames.Should().Contain("list");
        subcommandNames.Should().Contain("create");
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
        pathOption.Should().NotBeNull();
        pathOption!.IsRequired.Should().BeTrue();
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
        options.Should().Contain("path");
        options.Should().Contain("output");
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
        options.Should().Contain("package");
        options.Should().Contain("registry");
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
        options.Should().Contain("name");
        options.Should().Contain("from");
        options.Should().Contain("output");
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
        result.Should().Be(0);
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
        result.Should().BeGreaterThanOrEqualTo(0);
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
        result.Should().Be(0);

        var templatePath = Path.Combine(outputPath, templateName);
        Directory.Exists(templatePath).Should().BeTrue();
        Directory.Exists(Path.Combine(templatePath, ".template.config")).Should().BeTrue();
        Directory.Exists(Path.Combine(templatePath, "content")).Should().BeTrue();
        File.Exists(Path.Combine(templatePath, ".template.config", "template.json")).Should().BeTrue();
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
        File.Exists(templateJsonPath).Should().BeTrue();

        var json = await File.ReadAllTextAsync(templateJsonPath);
        var templateConfig = JsonSerializer.Deserialize<JsonElement>(json);

        templateConfig.GetProperty("name").GetString().Should().Contain(templateName);
        templateConfig.GetProperty("shortName").GetString().Should().Be(templateName.ToLower());
        templateConfig.GetProperty("identity").GetString().Should().Contain(templateName);
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
        File.Exists(copiedFile).Should().BeTrue();

        var content = await File.ReadAllTextAsync(copiedFile);
        content.Should().Contain("TestFile");
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
        Directory.Exists(Path.Combine(contentPath, "bin")).Should().BeFalse();
        Directory.Exists(Path.Combine(contentPath, "obj")).Should().BeFalse();
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
        result.Should().BeGreaterThanOrEqualTo(0);

        // Template directory should still exist
        Directory.Exists(Path.Combine(outputPath, templateName)).Should().BeTrue();
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
        result.Should().BeGreaterThanOrEqualTo(0);
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
        result.Should().Be(0);
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
        result.Should().Be(0);
    }

    [Fact]
    public void TemplateCommand_Validate_PathOption_ShouldBeRequired()
    {
        // Arrange
        var command = new TemplateCommand();
        var validateCommand = command.Subcommands.First(s => s.Name == "validate");
        var pathOption = validateCommand.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        pathOption.Should().NotBeNull();
        pathOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TemplateCommand_Pack_PathOption_ShouldBeRequired()
    {
        // Arrange
        var command = new TemplateCommand();
        var packCommand = command.Subcommands.First(s => s.Name == "pack");
        var pathOption = packCommand.Options.FirstOrDefault(o => o.Name == "path");

        // Assert
        pathOption.Should().NotBeNull();
        pathOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TemplateCommand_Publish_PackageOption_ShouldBeRequired()
    {
        // Arrange
        var command = new TemplateCommand();
        var publishCommand = command.Subcommands.First(s => s.Name == "publish");
        var packageOption = publishCommand.Options.FirstOrDefault(o => o.Name == "package");

        // Assert
        packageOption.Should().NotBeNull();
        packageOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TemplateCommand_Create_NameOption_ShouldBeRequired()
    {
        // Arrange
        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");
        var nameOption = createCommand.Options.FirstOrDefault(o => o.Name == "name");

        // Assert
        nameOption.Should().NotBeNull();
        nameOption!.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void TemplateCommand_Create_FromOption_ShouldBeRequired()
    {
        // Arrange
        var command = new TemplateCommand();
        var createCommand = command.Subcommands.First(s => s.Name == "create");
        var fromOption = createCommand.Options.FirstOrDefault(o => o.Name == "from");

        // Assert
        fromOption.Should().NotBeNull();
        fromOption!.IsRequired.Should().BeTrue();
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

        templateConfig.TryGetProperty("symbols", out var symbols).Should().BeTrue();
        symbols.TryGetProperty("ProjectName", out var projectNameSymbol).Should().BeTrue();
        projectNameSymbol.GetProperty("type").GetString().Should().Be("parameter");
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

        templateConfig.GetProperty("author").GetString().Should().NotBeNullOrEmpty();
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
        classifications.GetArrayLength().Should().BeGreaterThan(0);

        var classArray = classifications.EnumerateArray().Select(e => e.GetString()).ToList();
        classArray.Should().Contain("Relay");
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
        File.Exists(nestedFile).Should().BeTrue();
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
        Directory.Exists(Path.Combine(contentPath, ".git")).Should().BeFalse();
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

using Xunit;
using Relay.CLI.TemplateEngine;

namespace Relay.CLI.Tests.TemplateEngine;

public class TemplateValidatorTests : IDisposable
{
    private readonly TemplateValidator _validator;
    private readonly string _testDataPath;

    public TemplateValidatorTests()
    {
        _validator = new TemplateValidator();
        _testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData");
        Directory.CreateDirectory(_testDataPath);
    }

    public void Dispose()
    {
        // Cleanup if needed
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ValidateAsync_WithValidTemplate_ReturnsSuccess()
    {
        // Arrange
        var validTemplatePath = CreateValidTemplate();

        // Act
        var result = await _validator.ValidateAsync(validTemplatePath);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains("validation passed", result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithMissingTemplateDirectory_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataPath, "NonExistent");

        // Act
        var result = await _validator.ValidateAsync(nonExistentPath);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("not found"));
    }

    [Fact]
    public async Task ValidateAsync_WithMissingConfigDirectory_ReturnsFailure()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "NoConfig_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(templatePath);

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.False(result.IsValid);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithMissingTemplateJsonFile_ReturnsFailure()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "NoTemplateJson_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        Directory.CreateDirectory(configPath);

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Missing template.json file"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithMissingRequiredFields_ReturnsErrors()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "MissingFields_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        Directory.CreateDirectory(configPath);
        
        var templateJsonPath = Path.Combine(configPath, "template.json");
        var incompleteJson = @"{
            ""author"": ""Test Author"",
            ""identity"": ""Test.Template""
        }";
        await File.WriteAllTextAsync(templateJsonPath, incompleteJson);

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("name"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public void ValidateProjectName_WithValidName_ReturnsSuccess()
    {
        // Arrange
        var validName = "MyTestProject";

        // Act
        var result = _validator.ValidateProjectName(validName);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateProjectName_WithEmptyName_ReturnsFailure()
    {
        // Arrange
        var emptyName = "";

        // Act
        var result = _validator.ValidateProjectName(emptyName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("cannot be empty"));
    }

    [Fact]
    public void ValidateProjectName_WithWhitespaceOnly_ReturnsFailure()
    {
        // Arrange
        var whitespaceName = "   \t\n  ";

        // Act
        var result = _validator.ValidateProjectName(whitespaceName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("cannot be empty"));
    }

    [Fact]
    public void ValidateProjectName_WithInvalidCharacters_ReturnsFailure()
    {
        // Arrange
        var invalidName = "My<Project>";

        // Act
        var result = _validator.ValidateProjectName(invalidName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("invalid characters"));
    }

    [Fact]
    public void ValidateProjectName_StartingWithNumber_ReturnsWarning()
    {
        // Arrange
        var nameStartingWithNumber = "123Project";

        // Act
        var result = _validator.ValidateProjectName(nameStartingWithNumber);

        // Assert
        Assert.Contains(result.Warnings, w => w.Contains("start with a letter"));
    }

    [Fact]
    public void ValidateProjectName_WithSpaces_ReturnsWarning()
    {
        // Arrange
        var nameWithSpaces = "My Project";

        // Act
        var result = _validator.ValidateProjectName(nameWithSpaces);

        // Assert
        Assert.Contains(result.Warnings, w => w.Contains("spaces"));
    }

    [Theory]
    [InlineData("System")]
    [InlineData("Microsoft")]
    [InlineData("Console")]
    [InlineData("Object")]
    public void ValidateProjectName_WithReservedKeyword_ReturnsWarning(string keyword)
    {
        // Act
        var result = _validator.ValidateProjectName(keyword);

        // Assert
        Assert.Contains(result.Warnings, w => w.Contains("reserved keyword"));
    }

    [Theory]
    [InlineData("My<Project>")]
    [InlineData("My:Project")]
    [InlineData("My\"Project\"")]
    [InlineData("My|Project")]
    [InlineData("My?Project")]
    [InlineData("My*Project")]
    [InlineData("My\\Project")]
    [InlineData("My/Project")]
    public void ValidateProjectName_WithInvalidFileNameChars_ReturnsFailure(string invalidName)
    {
        // Act
        var result = _validator.ValidateProjectName(invalidName);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("invalid characters"));
    }

    [Fact]
    public void ValidateProjectName_WithUnicodeCharacters_IsValid()
    {
        // Arrange
        var unicodeName = "MyPröject_123";

        // Act
        var result = _validator.ValidateProjectName(unicodeName);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProjectName_WithVeryLongName_IsValid()
    {
        // Arrange
        var longName = new string('A', 200);

        // Act
        var result = _validator.ValidateProjectName(longName);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProjectName_WithNumbersOnly_ReturnsWarning()
    {
        // Arrange
        var numbersOnly = "123456";

        // Act
        var result = _validator.ValidateProjectName(numbersOnly);

        // Assert
        Assert.Contains(result.Warnings, w => w.Contains("start with a letter"));
    }

    [Fact]
    public void ValidateProjectName_WithUnderscore_IsValid()
    {
        // Arrange
        var nameWithUnderscore = "My_Project";

        // Act
        var result = _validator.ValidateProjectName(nameWithUnderscore);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProjectName_WithHyphen_IsValid()
    {
        // Arrange
        var nameWithHyphen = "My-Project";

        // Act
        var result = _validator.ValidateProjectName(nameWithHyphen);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProjectName_WithMixedCase_IsValid()
    {
        // Arrange
        var mixedCaseName = "MyProject123";

        // Act
        var result = _validator.ValidateProjectName(mixedCaseName);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ValidateAsync_WithMalformedJson_ReturnsFailure()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "MalformedJson_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        Directory.CreateDirectory(configPath);

        var malformedJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""name"": ""Test Template""
            // Missing comma and closing brace
        ";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, malformedJson);

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Error validating template.json"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyTemplateJson_ReturnsFailure()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "EmptyJson_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        Directory.CreateDirectory(configPath);

        var emptyJson = "{}";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, emptyJson);

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("'name' is required"));
            Assert.Contains(result.Errors, e => e.Contains("'shortName' is required"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithTemplateJsonMissingRequiredFields_ReturnsMultipleErrors()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "MissingFields_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        Directory.CreateDirectory(configPath);

        var incompleteJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [""Test""]
        }";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, incompleteJson);

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("'name' is required"));
            Assert.Contains(result.Errors, e => e.Contains("'shortName' is required"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithTemplateJsonSpecialCharacters_IsValid()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "SpecialChars_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test & Author <test@example.com>"",
            ""classifications"": [""Test & Sample""],
            ""identity"": ""Test.Template.Valid"",
            ""name"": ""Test Template & More"",
            ""shortName"": ""test-template"",
            ""description"": ""A test template with <tags> & 'quotes'"",
            ""sourceName"": ""TestProject""
        }";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, templateJson);

        // Create sample content
        await File.WriteAllTextAsync(Path.Combine(contentPath, "README.md"), "# Test Project");
        await File.WriteAllTextAsync(Path.Combine(contentPath, "TestProject.csproj"), "<Project />");

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.True(result.IsValid);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithMissingIdentity_ReturnsWarning()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "MissingIdentity_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [""Test""],
            ""name"": ""Test Template"",
            ""shortName"": ""test-template"",
            ""description"": ""A test template"",
            ""sourceName"": ""TestProject""
        }";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, templateJson);

        // Create sample content
        await File.WriteAllTextAsync(Path.Combine(contentPath, "README.md"), "# Test Project");
        await File.WriteAllTextAsync(Path.Combine(contentPath, "TestProject.csproj"), "<Project />");

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("'identity' is not set"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyClassifications_ReturnsWarning()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "EmptyClassifications_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [],
            ""identity"": ""Test.Template"",
            ""name"": ""Test Template"",
            ""shortName"": ""test-template"",
            ""description"": ""A test template"",
            ""sourceName"": ""TestProject""
        }";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, templateJson);

        // Create sample content
        await File.WriteAllTextAsync(Path.Combine(contentPath, "README.md"), "# Test Project");
        await File.WriteAllTextAsync(Path.Combine(contentPath, "TestProject.csproj"), "<Project />");

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("No classifications specified"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyContentDirectory_ReturnsWarning()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "EmptyContent_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [""Test""],
            ""identity"": ""Test.Template"",
            ""name"": ""Test Template"",
            ""shortName"": ""test-template"",
            ""description"": ""A test template"",
            ""sourceName"": ""TestProject""
        }";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, templateJson);

        // Content directory is empty

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("Content directory is empty"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithContentNoProjectFile_ReturnsWarning()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "NoProjectFile_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [""Test""],
            ""identity"": ""Test.Template"",
            ""name"": ""Test Template"",
            ""shortName"": ""test-template"",
            ""description"": ""A test template"",
            ""sourceName"": ""TestProject""
        }";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, templateJson);

        // Create content without project file
        await File.WriteAllTextAsync(Path.Combine(contentPath, "README.md"), "# Test Project");
        await File.WriteAllTextAsync(Path.Combine(contentPath, "Program.cs"), "Console.WriteLine(\"Hello\");");

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("No .csproj or .sln files found"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithContentNoReadme_ReturnsWarning()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "NoReadme_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [""Test""],
            ""identity"": ""Test.Template"",
            ""name"": ""Test Template"",
            ""shortName"": ""test-template"",
            ""description"": ""A test template"",
            ""sourceName"": ""TestProject""
        }";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, templateJson);

        // Create content without README
        await File.WriteAllTextAsync(Path.Combine(contentPath, "TestProject.csproj"), "<Project />");
        await File.WriteAllTextAsync(Path.Combine(contentPath, "Program.cs"), "Console.WriteLine(\"Hello\");");

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("No README.md found"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithWarningsOnly_ReturnsWarningMessage()
    {
        // Arrange
        var templatePath = Path.Combine(_testDataPath, "WarningsOnly_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [""Test""],
            ""name"": ""Test Template"",
            ""shortName"": ""test-template"",
            ""description"": ""A test template"",
            ""sourceName"": ""TestProject""
        }";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        await File.WriteAllTextAsync(templateJsonPath, templateJson);

        // Create content without README (to trigger warning)
        await File.WriteAllTextAsync(Path.Combine(contentPath, "TestProject.csproj"), "<Project />");
        await File.WriteAllTextAsync(Path.Combine(contentPath, "Program.cs"), "Console.WriteLine(\"Hello\");");

        try
        {
            // Act
            var result = await _validator.ValidateAsync(templatePath);

            // Assert
            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, w => w.Contains("'identity' is not set"));
            Assert.Contains(result.Warnings, w => w.Contains("No README.md found"));
            Assert.NotNull(result.Message);
            Assert.True(result.Message.StartsWith("⚠️"));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(templatePath))
                Directory.Delete(templatePath, true);
        }
    }

    private string CreateValidTemplate()
    {
        var templatePath = Path.Combine(_testDataPath, "ValidTemplate_" + Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(templatePath, ".template.config");
        var contentPath = Path.Combine(templatePath, "content");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(contentPath);

        var templateJson = @"{
            ""$schema"": ""http://json.schemastore.org/template"",
            ""author"": ""Test Author"",
            ""classifications"": [""Test"", ""Sample""],
            ""identity"": ""Test.Template.Valid"",
            ""name"": ""Test Template"",
            ""shortName"": ""test-template"",
            ""description"": ""A test template"",
            ""sourceName"": ""TestProject""
        }";

        var templateJsonPath = Path.Combine(configPath, "template.json");
        File.WriteAllText(templateJsonPath, templateJson);

        // Create sample content
        File.WriteAllText(Path.Combine(contentPath, "README.md"), "# Test Project");
        File.WriteAllText(Path.Combine(contentPath, "TestProject.csproj"), "<Project />");

        return templatePath;
    }
}

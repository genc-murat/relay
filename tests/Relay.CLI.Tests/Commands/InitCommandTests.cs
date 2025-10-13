using System.CommandLine;
using Relay.CLI.Commands;
using Xunit;
using System.IO;
using System.Threading.Tasks;

namespace Relay.CLI.Tests.Commands;

public class InitCommandTests
{
    [Fact]
    public void Create_ReturnsCommandWithCorrectName()
    {
        // Arrange & Act
        var command = InitCommand.Create();

        // Assert
        Assert.Equal("init", command.Name);
        Assert.Equal("Initialize a new Relay project with complete scaffolding", command.Description);
    }

    [Fact]
    public void Create_CommandHasRequiredOptions()
    {
        // Arrange & Act
        var command = InitCommand.Create();

        // Assert
        Assert.Contains(command.Options, o => o.Name == "name");
        Assert.Contains(command.Options, o => o.Name == "template");
        Assert.Contains(command.Options, o => o.Name == "output");
        Assert.Contains(command.Options, o => o.Name == "framework");
        Assert.Contains(command.Options, o => o.Name == "git");
        Assert.Contains(command.Options, o => o.Name == "docker");
        Assert.Contains(command.Options, o => o.Name == "ci");
    }

    [Fact]
    public async Task ExecuteInit_WithMinimalTemplate_CreatesProjectStructure()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayInitTest");
        var projectName = "TestProject";

        try
        {
            // Act
            await InitCommand.ExecuteInit(projectName, "minimal", tempDir, "net8.0", false, false, false);

            // Assert
            var projectPath = Path.Combine(tempDir, projectName);
            Assert.True(Directory.Exists(projectPath));
            Assert.True(Directory.Exists(Path.Combine(projectPath, "src", projectName)));
            Assert.True(Directory.Exists(Path.Combine(projectPath, "tests", $"{projectName}.Tests")));
            Assert.True(File.Exists(Path.Combine(projectPath, $"{projectName}.sln")));
            Assert.True(File.Exists(Path.Combine(projectPath, "src", projectName, $"{projectName}.csproj")));
            Assert.True(File.Exists(Path.Combine(projectPath, "README.md")));
        }
        finally
        {
            if (Directory.Exists(Path.Combine(tempDir, projectName)))
            {
                Directory.Delete(Path.Combine(tempDir, projectName), true);
            }
        }
    }

    [Fact]
    public async Task ExecuteInit_WithEnterpriseTemplate_CreatesAdditionalDirectories()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayInitEnterpriseTest");
        var projectName = "TestEnterpriseProject";

        try
        {
            // Act
            await InitCommand.ExecuteInit(projectName, "enterprise", tempDir, "net8.0", false, false, false);

            // Assert
            var projectPath = Path.Combine(tempDir, projectName);
            var srcPath = Path.Combine(projectPath, "src", projectName);
            Assert.True(Directory.Exists(Path.Combine(srcPath, "Handlers")));
            Assert.True(Directory.Exists(Path.Combine(srcPath, "Requests")));
            Assert.True(Directory.Exists(Path.Combine(srcPath, "Responses")));
            Assert.True(Directory.Exists(Path.Combine(srcPath, "Validators")));
            Assert.True(Directory.Exists(Path.Combine(srcPath, "Behaviors")));
        }
        finally
        {
            if (Directory.Exists(Path.Combine(tempDir, projectName)))
            {
                Directory.Delete(Path.Combine(tempDir, projectName), true);
            }
        }
    }

    [Fact]
    public async Task ExecuteInit_WithDockerOption_CreatesDockerFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayInitDockerTest");
        var projectName = "TestDockerProject";

        try
        {
            // Act
            await InitCommand.ExecuteInit(projectName, "standard", tempDir, "net8.0", false, true, false);

            // Assert
            var projectPath = Path.Combine(tempDir, projectName);
            Assert.True(File.Exists(Path.Combine(projectPath, "Dockerfile")));
            Assert.True(File.Exists(Path.Combine(projectPath, "docker-compose.yml")));
        }
        finally
        {
            if (Directory.Exists(Path.Combine(tempDir, projectName)))
            {
                Directory.Delete(Path.Combine(tempDir, projectName), true);
            }
        }
    }

    [Fact]
    public async Task ExecuteInit_WithCIOption_CreatesCIConfiguration()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayInitCITest");
        var projectName = "TestCIProject";

        try
        {
            // Act
            await InitCommand.ExecuteInit(projectName, "standard", tempDir, "net8.0", false, false, true);

            // Assert
            var projectPath = Path.Combine(tempDir, projectName);
            var ciPath = Path.Combine(projectPath, ".github", "workflows", "ci.yml");
            Assert.True(File.Exists(ciPath));
        }
        finally
        {
            if (Directory.Exists(Path.Combine(tempDir, projectName)))
            {
                Directory.Delete(Path.Combine(tempDir, projectName), true);
            }
        }
    }

    [Fact]
    public async Task ExecuteInit_WithGitOption_CreatesGitIgnore()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayInitGitTest");
        var projectName = "TestGitProject";

        try
        {
            // Act
            await InitCommand.ExecuteInit(projectName, "standard", tempDir, "net8.0", true, false, false);

            // Assert
            var projectPath = Path.Combine(tempDir, projectName);
            Assert.True(File.Exists(Path.Combine(projectPath, ".gitignore")));
        }
        finally
        {
            if (Directory.Exists(Path.Combine(tempDir, projectName)))
            {
                Directory.Delete(Path.Combine(tempDir, projectName), true);
            }
        }
    }

    [Fact]
    public async Task ExecuteInit_CreatesValidSolutionFile()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayInitSolutionTest");
        var projectName = "TestSolutionProject";

        try
        {
            // Act
            await InitCommand.ExecuteInit(projectName, "standard", tempDir, "net8.0", false, false, false);

            // Assert
            var projectPath = Path.Combine(tempDir, projectName);
            var slnPath = Path.Combine(projectPath, $"{projectName}.sln");
            var slnContent = await File.ReadAllTextAsync(slnPath);
            Assert.Contains("Microsoft Visual Studio Solution File", slnContent);
            Assert.Contains(projectName, slnContent);
            Assert.Contains($"{projectName}.Tests", slnContent);
        }
        finally
        {
            if (Directory.Exists(Path.Combine(tempDir, projectName)))
            {
                Directory.Delete(Path.Combine(tempDir, projectName), true);
            }
        }
    }

    [Fact]
    public async Task ExecuteInit_CreatesValidProjectFiles()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayInitProjectTest");
        var projectName = "TestProjectFiles";

        try
        {
            // Act
            await InitCommand.ExecuteInit(projectName, "standard", tempDir, "net8.0", false, false, false);

            // Assert
            var projectPath = Path.Combine(tempDir, projectName);
            var csprojPath = Path.Combine(projectPath, "src", projectName, $"{projectName}.csproj");
            var csprojContent = await File.ReadAllTextAsync(csprojPath);
            Assert.Contains("net8.0", csprojContent);
            Assert.Contains("Relay.Core", csprojContent);

            var programPath = Path.Combine(projectPath, "src", projectName, "Program.cs");
            var programContent = await File.ReadAllTextAsync(programPath);
            Assert.Contains("AddRelay", programContent);
            Assert.Contains(projectName, programContent);
        }
        finally
        {
            if (Directory.Exists(Path.Combine(tempDir, projectName)))
            {
                Directory.Delete(Path.Combine(tempDir, projectName), true);
            }
        }
    }

    [Fact]
    public async Task ExecuteInit_CreatesTestProject()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayInitTestProjectTest");
        var projectName = "TestTestProject";

        try
        {
            // Act
            await InitCommand.ExecuteInit(projectName, "standard", tempDir, "net8.0", false, false, false);

            // Assert
            var testProjectPath = Path.Combine(tempDir, projectName, "tests", $"{projectName}.Tests");
            Assert.True(Directory.Exists(testProjectPath));

            var testCsprojPath = Path.Combine(testProjectPath, $"{projectName}.Tests.csproj");
            var testCsprojContent = await File.ReadAllTextAsync(testCsprojPath);
            Assert.Contains("xunit", testCsprojContent);
            Assert.Contains("FluentAssertions", testCsprojContent);

            var sampleTestPath = Path.Combine(testProjectPath, "SampleTests.cs");
            var sampleTestContent = await File.ReadAllTextAsync(sampleTestPath);
            Assert.Contains("[Fact]", sampleTestContent);
            Assert.Contains(projectName, sampleTestContent);
        }
        finally
        {
            if (Directory.Exists(Path.Combine(tempDir, projectName)))
            {
                Directory.Delete(Path.Combine(tempDir, projectName), true);
            }
        }
    }

    [Fact]
    public async Task ExecuteInit_CreatesSampleCode()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), "RelayInitSampleTest");
        var projectName = "TestSampleProject";

        try
        {
            // Act
            await InitCommand.ExecuteInit(projectName, "standard", tempDir, "net8.0", false, false, false);

            // Assert
            var srcPath = Path.Combine(tempDir, projectName, "src", projectName);
            Assert.True(File.Exists(Path.Combine(srcPath, "GetUserQuery.cs")));
            Assert.True(File.Exists(Path.Combine(srcPath, "UserResponse.cs")));
            Assert.True(File.Exists(Path.Combine(srcPath, "GetUserHandler.cs")));

            var handlerContent = await File.ReadAllTextAsync(Path.Combine(srcPath, "GetUserHandler.cs"));
            Assert.Contains("[Handle]", handlerContent);
            Assert.Contains("ValueTask", handlerContent);
            Assert.Contains(projectName, handlerContent);
        }
        finally
        {
            if (Directory.Exists(Path.Combine(tempDir, projectName)))
            {
                Directory.Delete(Path.Combine(tempDir, projectName), true);
            }
        }
    }
}
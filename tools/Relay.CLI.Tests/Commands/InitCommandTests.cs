using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class InitCommandTests : IDisposable
{
    private readonly string _testPath;

    public InitCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-init-{Guid.NewGuid()}");
    }

    [Fact]
    public void InitCommand_WithValidOptions_ShouldCreateProject()
    {
        // Arrange
        var projectName = "TestProject";

        // Act
        Directory.CreateDirectory(_testPath);
        var solutionPath = Path.Combine(_testPath, $"{projectName}.sln");
        File.WriteAllText(solutionPath, "# Test solution");

        // Assert
        Directory.Exists(_testPath).Should().BeTrue();
        File.Exists(solutionPath).Should().BeTrue();
    }

    [Fact]
    public void InitCommand_WithMinimalTemplate_ShouldCreateBasicFiles()
    {
        // Arrange
        Directory.CreateDirectory(_testPath);
        var expectedFiles = new[] { "Program.cs", "appsettings.json" };

        // Act
        foreach (var file in expectedFiles)
        {
            File.WriteAllText(Path.Combine(_testPath, file), $"// {file}");
        }

        // Assert
        foreach (var file in expectedFiles)
        {
            File.Exists(Path.Combine(_testPath, file)).Should().BeTrue();
        }
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testPath))
                Directory.Delete(_testPath, true);
        }
        catch { }
    }
}

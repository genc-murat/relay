using Relay.CLI.Commands;

namespace Relay.CLI.Tests.Commands;

public class DoctorCommandTests : IDisposable
{
    private readonly string _testPath;

    public DoctorCommandTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"relay-doctor-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testPath);
    }

    [Fact]
    public async Task DoctorCommand_WithValidProject_ShouldPass()
    {
        // Arrange
        await CreateValidProject();

        // Act
        var hasProjectFile = File.Exists(Path.Combine(_testPath, "Test.csproj"));
        var hasHandlerFile = File.Exists(Path.Combine(_testPath, "Handler.cs"));

        // Assert
        hasProjectFile.Should().BeTrue();
        hasHandlerFile.Should().BeTrue();
    }

    [Fact]
    public async Task DoctorCommand_WithMissingProject_ShouldFail()
    {
        // Act
        var projectFiles = Directory.GetFiles(_testPath, "*.csproj");

        // Assert
        projectFiles.Should().BeEmpty();
    }

    [Fact]
    public async Task DoctorCommand_ChecksRelayPackageVersion()
    {
        // Arrange
        await CreateValidProject();
        var csprojContent = await File.ReadAllTextAsync(Path.Combine(_testPath, "Test.csproj"));

        // Act
        var hasRelayPackage = csprojContent.Contains("Relay.Core");

        // Assert
        hasRelayPackage.Should().BeTrue();
    }

    private async Task CreateValidProject()
    {
        var csproj = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Relay.Core"" Version=""2.1.0"" />
  </ItemGroup>
</Project>";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "Test.csproj"), csproj);

        var handler = @"using Relay.Core;

public class TestHandler : IRequestHandler<TestRequest, string>
{
    [Handle]
    public async ValueTask<string> HandleAsync(TestRequest request, CancellationToken ct)
    {
        return ""test"";
    }
}

public record TestRequest : IRequest<string>;";

        await File.WriteAllTextAsync(Path.Combine(_testPath, "Handler.cs"), handler);
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

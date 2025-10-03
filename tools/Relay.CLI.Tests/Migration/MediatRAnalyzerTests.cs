using Relay.CLI.Migration;

namespace Relay.CLI.Tests.Migration;

public class MediatRAnalyzerTests
{
    private readonly MediatRAnalyzer _analyzer;
    private readonly string _testProjectPath;

    public MediatRAnalyzerTests()
    {
        _analyzer = new MediatRAnalyzer();
        _testProjectPath = Path.Combine(Path.GetTempPath(), $"relay-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testProjectPath);
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithNoProjectFiles_ReturnsError()
    {
        // Act
        var result = await _analyzer.AnalyzeProjectAsync(_testProjectPath);

        // Assert
        result.CanMigrate.Should().BeFalse();
        result.Issues.Should().Contain(i => i.Code == "NO_PROJECT");
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithMediatRPackage_DetectsPackageReference()
    {
        // Arrange
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.1"" />
  </ItemGroup>
</Project>";

        var projectFile = Path.Combine(_testProjectPath, "Test.csproj");
        await File.WriteAllTextAsync(projectFile, csprojContent);

        // Act
        var result = await _analyzer.AnalyzeProjectAsync(_testProjectPath);

        // Assert
        result.PackageReferences.Should().HaveCount(1);
        result.PackageReferences.First().Name.Should().Be("MediatR");
        result.PackageReferences.First().CurrentVersion.Should().Be("12.0.1");
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithHandler_DetectsHandler()
    {
        // Arrange
        await CreateTestProject();
        var handlerContent = @"
using MediatR;

public record GetUserQuery : IRequest<string>;

public class GetUserHandler : IRequestHandler<GetUserQuery, string>
{
    public async Task<string> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        return ""test"";
    }
}";

        await File.WriteAllTextAsync(Path.Combine(_testProjectPath, "Handler.cs"), handlerContent);

        // Act
        var result = await _analyzer.AnalyzeProjectAsync(_testProjectPath);

        // Assert
        result.HandlersFound.Should().Be(1);
        result.RequestsFound.Should().Be(1);
        result.CanMigrate.Should().BeTrue();
    }

    private async Task CreateTestProject()
    {
        var csprojContent = @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""MediatR"" Version=""12.0.1"" />
  </ItemGroup>
</Project>";

        await File.WriteAllTextAsync(Path.Combine(_testProjectPath, "Test.csproj"), csprojContent);
    }
}

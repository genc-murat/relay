using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class PackageReferenceTests
{
    [Fact]
    public void PackageReference_HasDefaultValues()
    {
        // Arrange & Act
        var packageRef = new PackageReference();

        // Assert
        Assert.Equal("", packageRef.Name);
        Assert.Equal("", packageRef.CurrentVersion);
        Assert.Equal("", packageRef.TargetVersion);
        Assert.Equal("", packageRef.ProjectFile);
    }

    [Fact]
    public void PackageReference_CanSetName()
    {
        // Arrange
        var packageRef = new PackageReference();

        // Act
        packageRef.Name = "MediatR";

        // Assert
        Assert.Equal("MediatR", packageRef.Name);
    }

    [Fact]
    public void PackageReference_CanSetCurrentVersion()
    {
        // Arrange
        var packageRef = new PackageReference();

        // Act
        packageRef.CurrentVersion = "12.0.0";

        // Assert
        Assert.Equal("12.0.0", packageRef.CurrentVersion);
    }

    [Fact]
    public void PackageReference_CanSetTargetVersion()
    {
        // Arrange
        var packageRef = new PackageReference();

        // Act
        packageRef.TargetVersion = "13.0.0";

        // Assert
        Assert.Equal("13.0.0", packageRef.TargetVersion);
    }

    [Fact]
    public void PackageReference_CanSetProjectFile()
    {
        // Arrange
        var packageRef = new PackageReference();

        // Act
        packageRef.ProjectFile = "/src/MyProject.csproj";

        // Assert
        Assert.Equal("/src/MyProject.csproj", packageRef.ProjectFile);
    }

    [Fact]
    public void PackageReference_SupportsObjectInitializer()
    {
        // Arrange & Act
        var packageRef = new PackageReference
        {
            Name = "MediatR",
            CurrentVersion = "12.1.1",
            TargetVersion = "13.0.0",
            ProjectFile = "/project/MyApp.csproj"
        };

        // Assert
        Assert.Equal("MediatR", packageRef.Name);
        Assert.Equal("12.1.1", packageRef.CurrentVersion);
        Assert.Equal("13.0.0", packageRef.TargetVersion);
        Assert.Equal("/project/MyApp.csproj", packageRef.ProjectFile);
    }

    [Fact]
    public void PackageReference_CanCreateMediatRReference()
    {
        // Arrange & Act
        var packageRef = new PackageReference
        {
            Name = "MediatR",
            CurrentVersion = "12.0.0",
            TargetVersion = "12.2.0",
            ProjectFile = "/src/WebApi.csproj"
        };

        // Assert
        Assert.Equal("MediatR", packageRef.Name);
        Assert.Equal("12.0.0", packageRef.CurrentVersion);
        Assert.Equal("12.2.0", packageRef.TargetVersion);
        Assert.Equal("/src/WebApi.csproj", packageRef.ProjectFile);
    }

    [Fact]
    public void PackageReference_CanCreateMediatRExtensionsReference()
    {
        // Arrange & Act
        var packageRef = new PackageReference
        {
            Name = "MediatR.Extensions.Microsoft.DependencyInjection",
            CurrentVersion = "11.1.0",
            TargetVersion = "12.0.0",
            ProjectFile = "/src/ConsoleApp.csproj"
        };

        // Assert
        Assert.Equal("MediatR.Extensions.Microsoft.DependencyInjection", packageRef.Name);
        Assert.Equal("11.1.0", packageRef.CurrentVersion);
        Assert.Equal("12.0.0", packageRef.TargetVersion);
        Assert.Equal("/src/ConsoleApp.csproj", packageRef.ProjectFile);
    }
}

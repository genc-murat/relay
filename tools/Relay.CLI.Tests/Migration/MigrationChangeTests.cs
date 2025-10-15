using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class MigrationChangeTests
{
    [Fact]
    public void MigrationChange_HasDefaultValues()
    {
        // Arrange & Act
        var change = new MigrationChange();

        // Assert
        Assert.Equal("", change.Category);
        Assert.Equal(ChangeType.Add, change.Type);
        Assert.Equal("", change.Description);
        Assert.Equal("", change.FilePath);
    }

    [Fact]
    public void MigrationChange_CanSetCategory()
    {
        // Arrange
        var change = new MigrationChange();

        // Act
        change.Category = "Using Directives";

        // Assert
        Assert.Equal("Using Directives", change.Category);
    }

    [Fact]
    public void MigrationChange_CanSetType()
    {
        // Arrange
        var change = new MigrationChange();

        // Act
        change.Type = ChangeType.Remove;

        // Assert
        Assert.Equal(ChangeType.Remove, change.Type);
    }

    [Fact]
    public void MigrationChange_CanSetDescription()
    {
        // Arrange
        var change = new MigrationChange();

        // Act
        change.Description = "Added Relay using directive";

        // Assert
        Assert.Equal("Added Relay using directive", change.Description);
    }

    [Fact]
    public void MigrationChange_CanSetFilePath()
    {
        // Arrange
        var change = new MigrationChange();

        // Act
        change.FilePath = "/src/Handler.cs";

        // Assert
        Assert.Equal("/src/Handler.cs", change.FilePath);
    }

    [Fact]
    public void MigrationChange_SupportsObjectInitializer()
    {
        // Arrange & Act
        var change = new MigrationChange
        {
            Category = "Package References",
            Type = ChangeType.Modify,
            Description = "Updated MediatR package reference",
            FilePath = "/project.csproj"
        };

        // Assert
        Assert.Equal("Package References", change.Category);
        Assert.Equal(ChangeType.Modify, change.Type);
        Assert.Equal("Updated MediatR package reference", change.Description);
        Assert.Equal("/project.csproj", change.FilePath);
    }

    [Fact]
    public void MigrationChange_CanCreateAddChange()
    {
        // Arrange & Act
        var change = new MigrationChange
        {
            Category = "Code Changes",
            Type = ChangeType.Add,
            Description = "Added Relay handler registration",
            FilePath = "/src/Program.cs"
        };

        // Assert
        Assert.Equal("Code Changes", change.Category);
        Assert.Equal(ChangeType.Add, change.Type);
        Assert.Equal("Added Relay handler registration", change.Description);
        Assert.Equal("/src/Program.cs", change.FilePath);
    }

    [Fact]
    public void MigrationChange_CanCreateRemoveChange()
    {
        // Arrange & Act
        var change = new MigrationChange
        {
            Category = "Using Directives",
            Type = ChangeType.Remove,
            Description = "Removed MediatR using directive",
            FilePath = "/src/Handler.cs"
        };

        // Assert
        Assert.Equal("Using Directives", change.Category);
        Assert.Equal(ChangeType.Remove, change.Type);
        Assert.Equal("Removed MediatR using directive", change.Description);
        Assert.Equal("/src/Handler.cs", change.FilePath);
    }

    [Fact]
    public void MigrationChange_CanCreateModifyChange()
    {
        // Arrange & Act
        var change = new MigrationChange
        {
            Category = "Handler Implementation",
            Type = ChangeType.Modify,
            Description = "Modified handler to use Relay pattern",
            FilePath = "/src/UserHandler.cs"
        };

        // Assert
        Assert.Equal("Handler Implementation", change.Category);
        Assert.Equal(ChangeType.Modify, change.Type);
        Assert.Equal("Modified handler to use Relay pattern", change.Description);
        Assert.Equal("/src/UserHandler.cs", change.FilePath);
    }
}
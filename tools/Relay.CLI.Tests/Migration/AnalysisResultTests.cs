using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class AnalysisResultTests
{
    [Fact]
    public void AnalysisResult_HasDefaultValues()
    {
        // Arrange & Act
        var result = new AnalysisResult();

        // Assert
        Assert.Equal("", result.ProjectPath);
        Assert.Equal(default, result.AnalysisDate);
        Assert.True(result.CanMigrate);
        Assert.Equal(0, result.FilesAffected);
        Assert.Equal(0, result.HandlersFound);
        Assert.Equal(0, result.RequestsFound);
        Assert.Equal(0, result.NotificationsFound);
        Assert.Equal(0, result.PipelineBehaviorsFound);
        Assert.False(result.HasCustomMediator);
        Assert.False(result.HasCustomBehaviors);
        Assert.NotNull(result.PackageReferences);
        Assert.Empty(result.PackageReferences);
        Assert.NotNull(result.Issues);
        Assert.Empty(result.Issues);
        Assert.NotNull(result.FilesWithMediatR);
        Assert.Empty(result.FilesWithMediatR);
    }

    [Fact]
    public void AnalysisResult_CanSetProjectPath()
    {
        // Arrange
        var result = new AnalysisResult
        {
            // Act
            ProjectPath = "/path/to/project"
        };

        // Assert
        Assert.Equal("/path/to/project", result.ProjectPath);
    }

    [Fact]
    public void AnalysisResult_CanSetAnalysisDate()
    {
        // Arrange
        var result = new AnalysisResult();
        var testDate = new DateTime(2023, 1, 1, 12, 0, 0);

        // Act
        result.AnalysisDate = testDate;

        // Assert
        Assert.Equal(testDate, result.AnalysisDate);
    }

    [Fact]
    public void AnalysisResult_CanSetCanMigrate()
    {
        // Arrange
        var result = new AnalysisResult
        {
            // Act
            CanMigrate = false
        };

        // Assert
        Assert.False(result.CanMigrate);
    }

    [Fact]
    public void AnalysisResult_CanSetFilesAffected()
    {
        // Arrange
        var result = new AnalysisResult
        {
            // Act
            FilesAffected = 42
        };

        // Assert
        Assert.Equal(42, result.FilesAffected);
    }

    [Fact]
    public void AnalysisResult_CanSetHandlersFound()
    {
        // Arrange
        var result = new AnalysisResult
        {
            // Act
            HandlersFound = 15
        };

        // Assert
        Assert.Equal(15, result.HandlersFound);
    }

    [Fact]
    public void AnalysisResult_CanSetRequestsFound()
    {
        // Arrange
        var result = new AnalysisResult
        {
            // Act
            RequestsFound = 10
        };

        // Assert
        Assert.Equal(10, result.RequestsFound);
    }

    [Fact]
    public void AnalysisResult_CanSetNotificationsFound()
    {
        // Arrange
        AnalysisResult result = new()
        {
            // Act
            NotificationsFound = 5
        };

        // Assert
        Assert.Equal(5, result.NotificationsFound);
    }

    [Fact]
    public void AnalysisResult_CanSetPipelineBehaviorsFound()
    {
        // Arrange
        AnalysisResult result = new()
        {
            // Act
            PipelineBehaviorsFound = 3
        };

        // Assert
        Assert.Equal(3, result.PipelineBehaviorsFound);
    }

    [Fact]
    public void AnalysisResult_CanSetHasCustomMediator()
    {
        // Arrange
        AnalysisResult result = new()
        {
            // Act
            HasCustomMediator = true
        };

        // Assert
        Assert.True(result.HasCustomMediator);
    }

    [Fact]
    public void AnalysisResult_CanSetHasCustomBehaviors()
    {
        // Arrange
        AnalysisResult result = new()
        {
            // Act
            HasCustomBehaviors = true
        };

        // Assert
        Assert.True(result.HasCustomBehaviors);
    }

    [Fact]
    public void AnalysisResult_CanAddPackageReferences()
    {
        // Arrange
        var result = new AnalysisResult();
        var packageRef = new PackageReference { Name = "MediatR", CurrentVersion = "12.0.0" };

        // Act
        result.PackageReferences.Add(packageRef);

        // Assert
        Assert.Single(result.PackageReferences);
        Assert.Equal("MediatR", result.PackageReferences[0].Name);
    }

    [Fact]
    public void AnalysisResult_CanAddIssues()
    {
        // Arrange
        var result = new AnalysisResult();
        var issue = new MigrationIssue { Severity = IssueSeverity.Warning, Message = "Test issue" };

        // Act
        result.Issues.Add(issue);

        // Assert
        Assert.Single(result.Issues);
        Assert.Equal(IssueSeverity.Warning, result.Issues[0].Severity);
        Assert.Equal("Test issue", result.Issues[0].Message);
    }

    [Fact]
    public void AnalysisResult_CanAddFilesWithMediatR()
    {
        // Arrange
        var result = new AnalysisResult();

        // Act
        result.FilesWithMediatR.Add("Handler.cs");

        // Assert
        Assert.Single(result.FilesWithMediatR);
        Assert.Equal("Handler.cs", result.FilesWithMediatR[0]);
    }

    [Fact]
    public void AnalysisResult_SupportsObjectInitializer()
    {
        // Arrange & Act
        var result = new AnalysisResult
        {
            ProjectPath = "/test/project",
            AnalysisDate = new DateTime(2023, 6, 15),
            CanMigrate = false,
            FilesAffected = 25,
            HandlersFound = 8,
            RequestsFound = 12,
            NotificationsFound = 2,
            PipelineBehaviorsFound = 1,
            HasCustomMediator = true,
            HasCustomBehaviors = false
        };

        // Assert
        Assert.Equal("/test/project", result.ProjectPath);
        Assert.Equal(new DateTime(2023, 6, 15), result.AnalysisDate);
        Assert.False(result.CanMigrate);
        Assert.Equal(25, result.FilesAffected);
        Assert.Equal(8, result.HandlersFound);
        Assert.Equal(12, result.RequestsFound);
        Assert.Equal(2, result.NotificationsFound);
        Assert.Equal(1, result.PipelineBehaviorsFound);
        Assert.True(result.HasCustomMediator);
        Assert.False(result.HasCustomBehaviors);
    }
}

using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Migration;

public class MigrationIssueTests
{
    [Fact]
    public void MigrationIssue_HasDefaultValues()
    {
        // Arrange & Act
        var issue = new MigrationIssue();

        // Assert
        Assert.Equal(IssueSeverity.Info, issue.Severity);
        Assert.Equal("", issue.Message);
        Assert.Equal("", issue.Code);
        Assert.Equal("", issue.FilePath);
        Assert.Equal(0, issue.LineNumber);
    }

    [Fact]
    public void MigrationIssue_CanSetSeverity()
    {
        // Arrange
        var issue = new MigrationIssue();

        // Act
        issue.Severity = IssueSeverity.Error;

        // Assert
        Assert.Equal(IssueSeverity.Error, issue.Severity);
    }

    [Fact]
    public void MigrationIssue_CanSetMessage()
    {
        // Arrange
        var issue = new MigrationIssue();

        // Act
        issue.Message = "Handler not found";

        // Assert
        Assert.Equal("Handler not found", issue.Message);
    }

    [Fact]
    public void MigrationIssue_CanSetCode()
    {
        // Arrange
        var issue = new MigrationIssue();

        // Act
        issue.Code = "MIG001";

        // Assert
        Assert.Equal("MIG001", issue.Code);
    }

    [Fact]
    public void MigrationIssue_CanSetFilePath()
    {
        // Arrange
        var issue = new MigrationIssue();

        // Act
        issue.FilePath = "/src/Handler.cs";

        // Assert
        Assert.Equal("/src/Handler.cs", issue.FilePath);
    }

    [Fact]
    public void MigrationIssue_CanSetLineNumber()
    {
        // Arrange
        var issue = new MigrationIssue();

        // Act
        issue.LineNumber = 25;

        // Assert
        Assert.Equal(25, issue.LineNumber);
    }

    [Fact]
    public void MigrationIssue_SupportsObjectInitializer()
    {
        // Arrange & Act
        var issue = new MigrationIssue
        {
            Severity = IssueSeverity.Warning,
            Message = "Custom mediator detected",
            Code = "MIG002",
            FilePath = "/src/Program.cs",
            LineNumber = 15
        };

        // Assert
        Assert.Equal(IssueSeverity.Warning, issue.Severity);
        Assert.Equal("Custom mediator detected", issue.Message);
        Assert.Equal("MIG002", issue.Code);
        Assert.Equal("/src/Program.cs", issue.FilePath);
        Assert.Equal(15, issue.LineNumber);
    }

    [Fact]
    public void MigrationIssue_CanCreateInfoIssue()
    {
        // Arrange & Act
        var issue = new MigrationIssue
        {
            Severity = IssueSeverity.Info,
            Message = "Analysis completed",
            Code = "INFO001",
            FilePath = "/project.json",
            LineNumber = 1
        };

        // Assert
        Assert.Equal(IssueSeverity.Info, issue.Severity);
        Assert.Equal("Analysis completed", issue.Message);
        Assert.Equal("INFO001", issue.Code);
        Assert.Equal("/project.json", issue.FilePath);
        Assert.Equal(1, issue.LineNumber);
    }

    [Fact]
    public void MigrationIssue_CanCreateWarningIssue()
    {
        // Arrange & Act
        var issue = new MigrationIssue
        {
            Severity = IssueSeverity.Warning,
            Message = "Pipeline behavior may need manual review",
            Code = "WARN001",
            FilePath = "/src/Pipeline.cs",
            LineNumber = 30
        };

        // Assert
        Assert.Equal(IssueSeverity.Warning, issue.Severity);
        Assert.Equal("Pipeline behavior may need manual review", issue.Message);
        Assert.Equal("WARN001", issue.Code);
        Assert.Equal("/src/Pipeline.cs", issue.FilePath);
        Assert.Equal(30, issue.LineNumber);
    }

    [Fact]
    public void MigrationIssue_CanCreateErrorIssue()
    {
        // Arrange & Act
        var issue = new MigrationIssue
        {
            Severity = IssueSeverity.Error,
            Message = "Unable to parse handler interface",
            Code = "ERR001",
            FilePath = "/src/InvalidHandler.cs",
            LineNumber = 10
        };

        // Assert
        Assert.Equal(IssueSeverity.Error, issue.Severity);
        Assert.Equal("Unable to parse handler interface", issue.Message);
        Assert.Equal("ERR001", issue.Code);
        Assert.Equal("/src/InvalidHandler.cs", issue.FilePath);
        Assert.Equal(10, issue.LineNumber);
    }

    [Fact]
    public void MigrationIssue_CanCreateCriticalIssue()
    {
        // Arrange & Act
        var issue = new MigrationIssue
        {
            Severity = IssueSeverity.Critical,
            Message = "Project structure incompatible",
            Code = "CRIT001",
            FilePath = "/project.csproj",
            LineNumber = 5
        };

        // Assert
        Assert.Equal(IssueSeverity.Critical, issue.Severity);
        Assert.Equal("Project structure incompatible", issue.Message);
        Assert.Equal("CRIT001", issue.Code);
        Assert.Equal("/project.csproj", issue.FilePath);
        Assert.Equal(5, issue.LineNumber);
    }
}
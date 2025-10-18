using Relay.CLI.Commands.Models.Diagnostic;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class DiagnosticCheckTests
{
    [Fact]
    public void DiagnosticCheck_ShouldHaveCategoryProperty()
    {
        // Arrange & Act
        var check = new DiagnosticCheck { Category = "Performance" };

        // Assert
        Assert.Equal("Performance", check.Category);
    }

    [Fact]
    public void DiagnosticCheck_ShouldHaveIssuesProperty()
    {
        // Arrange & Act
        var check = new DiagnosticCheck();

        // Assert
        Assert.NotNull(check.Issues);
        Assert.Empty(check.Issues);
    }

    [Fact]
    public void DiagnosticCheck_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var check = new DiagnosticCheck();

        // Assert
        Assert.Equal("", check.Category);
        Assert.NotNull(check.Issues);
        Assert.Empty(check.Issues);
    }

    [Fact]
    public void DiagnosticCheck_CanSetCategoryViaInitializer()
    {
        // Arrange & Act
        var check = new DiagnosticCheck { Category = "Security" };

        // Assert
        Assert.Equal("Security", check.Category);
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_ShouldAddIssueToList()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("Test message", DiagnosticSeverity.Warning, "TEST001", true);

        // Assert
        Assert.Equal(1, check.Issues.Count());
        var issue = check.Issues[0];
        Assert.Equal("Test message", issue.Message);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
        Assert.Equal("TEST001", issue.Code);
        Assert.True(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticCheck_AddSuccess_ShouldAddSuccessIssue()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddSuccess("Operation completed successfully");

        // Assert
        Assert.Equal(1, check.Issues.Count());
        var issue = check.Issues[0];
        Assert.Equal("Operation completed successfully", issue.Message);
        Assert.Equal(DiagnosticSeverity.Success, issue.Severity);
        Assert.Equal("SUCCESS", issue.Code);
        Assert.False(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticCheck_AddInfo_ShouldAddInfoIssue()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddInfo("This is an informational message");

        // Assert
        Assert.Equal(1, check.Issues.Count());
        var issue = check.Issues[0];
        Assert.Equal("This is an informational message", issue.Message);
        Assert.Equal(DiagnosticSeverity.Info, issue.Severity);
        Assert.Equal("INFO", issue.Code);
        Assert.False(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticCheck_CanAddMultipleIssues()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "MultiTest" };

        // Act
        check.AddIssue("Error 1", DiagnosticSeverity.Error, "ERR001");
        check.AddIssue("Warning 1", DiagnosticSeverity.Warning, "WARN001", true);
        check.AddSuccess("Success 1");
        check.AddInfo("Info 1");

        // Assert
        Assert.Equal(4, check.Issues.Count());
        Assert.Equal(1, check.Issues.Count(i => i.Severity == DiagnosticSeverity.Error));
        Assert.Equal(1, check.Issues.Count(i => i.Severity == DiagnosticSeverity.Warning));
        Assert.Equal(1, check.Issues.Count(i => i.Severity == DiagnosticSeverity.Success));
        Assert.Equal(1, check.Issues.Count(i => i.Severity == DiagnosticSeverity.Info));
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_WithAllSeverityLevels()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "SeverityTest" };

        // Act
        check.AddIssue("Success", DiagnosticSeverity.Success, "S001");
        check.AddIssue("Info", DiagnosticSeverity.Info, "I001");
        check.AddIssue("Warning", DiagnosticSeverity.Warning, "W001");
        check.AddIssue("Error", DiagnosticSeverity.Error, "E001");

        // Assert
        Assert.Equal(4, check.Issues.Count());
        Assert.Equal(new[]
        {
            DiagnosticSeverity.Success,
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error
        }, check.Issues.Select(i => i.Severity));
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_DefaultIsFixableIsFalse()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("Test message", DiagnosticSeverity.Warning, "TEST001");

        // Assert
        Assert.False(check.Issues[0].IsFixable);
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_CanSetIsFixableToTrue()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("Fixable issue", DiagnosticSeverity.Warning, "FIX001", true);

        // Assert
        Assert.True(check.Issues[0].IsFixable);
    }

    [Fact]
    public void DiagnosticCheck_Category_CanBeEmpty()
    {
        // Arrange & Act
        var check = new DiagnosticCheck { Category = "" };

        // Assert
        Assert.Equal("", check.Category);
    }

    [Fact]
    public void DiagnosticCheck_Category_CanContainSpacesAndSpecialChars()
    {
        // Arrange & Act
        var check = new DiagnosticCheck { Category = "Performance & Security Check" };

        // Assert
        Assert.Equal("Performance & Security Check", check.Category);
    }

    [Fact]
    public void DiagnosticCheck_Issues_IsReadOnlyCollection()
    {
        // Arrange
        var check = new DiagnosticCheck();

        // Act & Assert - Issues property should be read-only, but we can add via methods
        Assert.NotNull(check.Issues);
        // We can't directly assign to Issues, but we can add to it via methods
        check.AddIssue("Test", DiagnosticSeverity.Info, "TEST");
        Assert.Equal(1, check.Issues.Count());
    }

    [Fact]
    public void DiagnosticCheck_CanBeUsedInCollections()
    {
        // Arrange & Act
        var checks = new List<DiagnosticCheck>
        {
            new DiagnosticCheck { Category = "Check1" },
            new DiagnosticCheck { Category = "Check2" },
            new DiagnosticCheck { Category = "Check3" }
        };

        // Assert
        Assert.Equal(3, checks.Count());
        Assert.Equal(new[] { "Check1", "Check2", "Check3" }, checks.Select(c => c.Category));
    }

    [Fact]
    public void DiagnosticCheck_CanFilterIssuesBySeverity()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "FilterTest" };
        check.AddIssue("Error", DiagnosticSeverity.Error, "E001");
        check.AddIssue("Warning1", DiagnosticSeverity.Warning, "W001");
        check.AddIssue("Warning2", DiagnosticSeverity.Warning, "W002");
        check.AddIssue("Info", DiagnosticSeverity.Info, "I001");
        check.AddIssue("Success", DiagnosticSeverity.Success, "S001");

        // Act
        var errors = check.Issues.Where(i => i.Severity == DiagnosticSeverity.Error).ToList();
        var warnings = check.Issues.Where(i => i.Severity == DiagnosticSeverity.Warning).ToList();

        // Assert
        Assert.Equal(1, errors.Count());
        Assert.Equal(2, warnings.Count());
    }

    [Fact]
    public void DiagnosticCheck_CanFilterFixableIssues()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "FixableTest" };
        check.AddIssue("Fixable error", DiagnosticSeverity.Error, "E001", true);
        check.AddIssue("Non-fixable warning", DiagnosticSeverity.Warning, "W001", false);
        check.AddIssue("Fixable info", DiagnosticSeverity.Info, "I001", true);

        // Act
        var fixableIssues = check.Issues.Where(i => i.IsFixable).ToList();

        // Assert
        Assert.Equal(2, fixableIssues.Count());
        Assert.All(fixableIssues, i => Assert.True(i.IsFixable));
    }

    [Fact]
    public void DiagnosticCheck_CanCalculateIssueCounts()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "CountTest" };
        check.AddIssue("Error1", DiagnosticSeverity.Error, "E001");
        check.AddIssue("Error2", DiagnosticSeverity.Error, "E002");
        check.AddIssue("Warning", DiagnosticSeverity.Warning, "W001");
        check.AddIssue("Info", DiagnosticSeverity.Info, "I001");
        check.AddSuccess("Success");

        // Act
        var errorCount = check.Issues.Count(i => i.Severity == DiagnosticSeverity.Error);
        var totalCount = check.Issues.Count;

        // Assert
        Assert.Equal(2, errorCount);
        Assert.Equal(5, totalCount);
    }

    [Fact]
    public void DiagnosticCheck_WithEmptyIssues_ShouldHandleOperations()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "EmptyTest" };

        // Act & Assert
        Assert.Empty(check.Issues);
        Assert.Equal(0, check.Issues.Count);
        Assert.Empty(check.Issues.Where(i => i.Severity == DiagnosticSeverity.Error));
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_WithEmptyMessage()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("", DiagnosticSeverity.Info, "EMPTY");

        // Assert
        Assert.Equal("", check.Issues[0].Message);
        Assert.Equal("EMPTY", check.Issues[0].Code);
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_WithEmptyCode()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("Message", DiagnosticSeverity.Info, "");

        // Assert
        Assert.Equal("Message", check.Issues[0].Message);
        Assert.Equal("", check.Issues[0].Code);
    }

    [Fact]
    public void DiagnosticCheck_AddSuccess_WithSpecialCharacters()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddSuccess("Success with special chars: @#$%^&*()");

        // Assert
        Assert.Equal("Success with special chars: @#$%^&*()", check.Issues[0].Message);
        Assert.Equal(DiagnosticSeverity.Success, check.Issues[0].Severity);
    }

    [Fact]
    public void DiagnosticCheck_AddInfo_WithLongMessage()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };
        var longMessage = new string('A', 1000);

        // Act
        check.AddInfo(longMessage);

        // Assert
        Assert.Equal(longMessage, check.Issues[0].Message);
        Assert.Equal(1000, check.Issues[0].Message.Length);
    }

    [Fact]
    public void DiagnosticCheck_ShouldBeClass()
    {
        // Arrange & Act
        var check = new DiagnosticCheck();

        // Assert
        Assert.NotNull(check);
        Assert.True(check.GetType().IsClass);
    }

    [Fact]
    public void DiagnosticCheck_CanBeGroupedByCategory()
    {
        // Arrange
        var checks = new List<DiagnosticCheck>
        {
            new DiagnosticCheck { Category = "Performance" },
            new DiagnosticCheck { Category = "Security" },
            new DiagnosticCheck { Category = "Performance" },
            new DiagnosticCheck { Category = "Reliability" }
        };

        // Act
        var grouped = checks.GroupBy(c => c.Category);

        // Assert
        Assert.Equal(3, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key == "Performance").Count());
        Assert.Equal(1, grouped.First(g => g.Key == "Security").Count());
        Assert.Equal(1, grouped.First(g => g.Key == "Reliability").Count());
    }

    [Fact]
    public void DiagnosticCheck_CanAggregateIssueCounts()
    {
        // Arrange
        var checks = new List<DiagnosticCheck>
        {
            new DiagnosticCheck { Category = "Check1" },
            new DiagnosticCheck { Category = "Check2" },
            new DiagnosticCheck { Category = "Check3" }
        };

        checks[0].AddIssue("Error", DiagnosticSeverity.Error, "E001");
        checks[0].AddIssue("Warning", DiagnosticSeverity.Warning, "W001");
        checks[1].AddSuccess("Success");
        checks[2].AddInfo("Info");

        // Act
        var totalIssues = checks.Sum(c => c.Issues.Count);
        var totalErrors = checks.Sum(c => c.Issues.Count(i => i.Severity == DiagnosticSeverity.Error));

        // Assert
        Assert.Equal(4, totalIssues);
        Assert.Equal(1, totalErrors);
    }

    [Fact]
    public void DiagnosticCheck_Category_CanBeModified()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Initial" };

        // Act
        check.Category = "Modified";

        // Assert
        Assert.Equal("Modified", check.Category);
    }

    [Fact]
    public void DiagnosticCheck_WithRealisticDiagnosticData()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Database Connection" };

        // Act
        check.AddSuccess("Database connection established successfully");
        check.AddInfo("Connection pool size: 10");
        check.AddIssue("Connection timeout is set to 30 seconds", DiagnosticSeverity.Warning, "DB_TIMEOUT", true);
        check.AddIssue("Database server version is outdated", DiagnosticSeverity.Error, "DB_VERSION", false);

        // Assert
        Assert.Equal(4, check.Issues.Count());
        Assert.Equal(1, check.Issues.Count(i => i.Severity == DiagnosticSeverity.Success));
        Assert.Equal(1, check.Issues.Count(i => i.Severity == DiagnosticSeverity.Info));
        Assert.Equal(1, check.Issues.Count(i => i.Severity == DiagnosticSeverity.Warning));
        Assert.Equal(1, check.Issues.Count(i => i.Severity == DiagnosticSeverity.Error));
        Assert.Equal(1, check.Issues.Count(i => i.IsFixable));
    }
}


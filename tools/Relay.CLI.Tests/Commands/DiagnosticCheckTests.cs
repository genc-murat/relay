using Relay.CLI.Commands.Models.Diagnostic;

namespace Relay.CLI.Tests.Commands;

public class DiagnosticCheckTests
{
    [Fact]
    public void DiagnosticCheck_ShouldHaveCategoryProperty()
    {
        // Arrange & Act
        var check = new DiagnosticCheck { Category = "Performance" };

        // Assert
        check.Category.Should().Be("Performance");
    }

    [Fact]
    public void DiagnosticCheck_ShouldHaveIssuesProperty()
    {
        // Arrange & Act
        var check = new DiagnosticCheck();

        // Assert
        check.Issues.Should().NotBeNull();
        check.Issues.Should().BeEmpty();
    }

    [Fact]
    public void DiagnosticCheck_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var check = new DiagnosticCheck();

        // Assert
        check.Category.Should().Be("");
        check.Issues.Should().NotBeNull();
        check.Issues.Should().BeEmpty();
    }

    [Fact]
    public void DiagnosticCheck_CanSetCategoryViaInitializer()
    {
        // Arrange & Act
        var check = new DiagnosticCheck { Category = "Security" };

        // Assert
        check.Category.Should().Be("Security");
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_ShouldAddIssueToList()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("Test message", DiagnosticSeverity.Warning, "TEST001", true);

        // Assert
        check.Issues.Should().HaveCount(1);
        var issue = check.Issues[0];
        issue.Message.Should().Be("Test message");
        issue.Severity.Should().Be(DiagnosticSeverity.Warning);
        issue.Code.Should().Be("TEST001");
        issue.IsFixable.Should().BeTrue();
    }

    [Fact]
    public void DiagnosticCheck_AddSuccess_ShouldAddSuccessIssue()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddSuccess("Operation completed successfully");

        // Assert
        check.Issues.Should().HaveCount(1);
        var issue = check.Issues[0];
        issue.Message.Should().Be("Operation completed successfully");
        issue.Severity.Should().Be(DiagnosticSeverity.Success);
        issue.Code.Should().Be("SUCCESS");
        issue.IsFixable.Should().BeFalse();
    }

    [Fact]
    public void DiagnosticCheck_AddInfo_ShouldAddInfoIssue()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddInfo("This is an informational message");

        // Assert
        check.Issues.Should().HaveCount(1);
        var issue = check.Issues[0];
        issue.Message.Should().Be("This is an informational message");
        issue.Severity.Should().Be(DiagnosticSeverity.Info);
        issue.Code.Should().Be("INFO");
        issue.IsFixable.Should().BeFalse();
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
        check.Issues.Should().HaveCount(4);
        check.Issues.Count(i => i.Severity == DiagnosticSeverity.Error).Should().Be(1);
        check.Issues.Count(i => i.Severity == DiagnosticSeverity.Warning).Should().Be(1);
        check.Issues.Count(i => i.Severity == DiagnosticSeverity.Success).Should().Be(1);
        check.Issues.Count(i => i.Severity == DiagnosticSeverity.Info).Should().Be(1);
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
        check.Issues.Should().HaveCount(4);
        check.Issues.Select(i => i.Severity).Should().BeEquivalentTo(new[]
        {
            DiagnosticSeverity.Success,
            DiagnosticSeverity.Info,
            DiagnosticSeverity.Warning,
            DiagnosticSeverity.Error
        });
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_DefaultIsFixableIsFalse()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("Test message", DiagnosticSeverity.Warning, "TEST001");

        // Assert
        check.Issues[0].IsFixable.Should().BeFalse();
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_CanSetIsFixableToTrue()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("Fixable issue", DiagnosticSeverity.Warning, "FIX001", true);

        // Assert
        check.Issues[0].IsFixable.Should().BeTrue();
    }

    [Fact]
    public void DiagnosticCheck_Category_CanBeEmpty()
    {
        // Arrange & Act
        var check = new DiagnosticCheck { Category = "" };

        // Assert
        check.Category.Should().BeEmpty();
    }

    [Fact]
    public void DiagnosticCheck_Category_CanContainSpacesAndSpecialChars()
    {
        // Arrange & Act
        var check = new DiagnosticCheck { Category = "Performance & Security Check" };

        // Assert
        check.Category.Should().Be("Performance & Security Check");
    }

    [Fact]
    public void DiagnosticCheck_Issues_IsReadOnlyCollection()
    {
        // Arrange
        var check = new DiagnosticCheck();

        // Act & Assert - Issues property should be read-only, but we can add via methods
        check.Issues.Should().NotBeNull();
        // We can't directly assign to Issues, but we can add to it via methods
        check.AddIssue("Test", DiagnosticSeverity.Info, "TEST");
        check.Issues.Should().HaveCount(1);
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
        checks.Should().HaveCount(3);
        checks.Select(c => c.Category).Should().BeEquivalentTo(new[] { "Check1", "Check2", "Check3" });
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
        errors.Should().HaveCount(1);
        warnings.Should().HaveCount(2);
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
        fixableIssues.Should().HaveCount(2);
        fixableIssues.All(i => i.IsFixable).Should().BeTrue();
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
        errorCount.Should().Be(2);
        totalCount.Should().Be(5);
    }

    [Fact]
    public void DiagnosticCheck_WithEmptyIssues_ShouldHandleOperations()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "EmptyTest" };

        // Act & Assert
        check.Issues.Should().BeEmpty();
        check.Issues.Count.Should().Be(0);
        check.Issues.Where(i => i.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_WithEmptyMessage()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("", DiagnosticSeverity.Info, "EMPTY");

        // Assert
        check.Issues[0].Message.Should().BeEmpty();
        check.Issues[0].Code.Should().Be("EMPTY");
    }

    [Fact]
    public void DiagnosticCheck_AddIssue_WithEmptyCode()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddIssue("Message", DiagnosticSeverity.Info, "");

        // Assert
        check.Issues[0].Message.Should().Be("Message");
        check.Issues[0].Code.Should().BeEmpty();
    }

    [Fact]
    public void DiagnosticCheck_AddSuccess_WithSpecialCharacters()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Test" };

        // Act
        check.AddSuccess("Success with special chars: @#$%^&*()");

        // Assert
        check.Issues[0].Message.Should().Be("Success with special chars: @#$%^&*()");
        check.Issues[0].Severity.Should().Be(DiagnosticSeverity.Success);
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
        check.Issues[0].Message.Should().Be(longMessage);
        check.Issues[0].Message.Length.Should().Be(1000);
    }

    [Fact]
    public void DiagnosticCheck_ShouldBeClass()
    {
        // Arrange & Act
        var check = new DiagnosticCheck();

        // Assert
        check.Should().NotBeNull();
        check.GetType().IsClass.Should().BeTrue();
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
        grouped.Should().HaveCount(3);
        grouped.First(g => g.Key == "Performance").Should().HaveCount(2);
        grouped.First(g => g.Key == "Security").Should().HaveCount(1);
        grouped.First(g => g.Key == "Reliability").Should().HaveCount(1);
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
        totalIssues.Should().Be(4);
        totalErrors.Should().Be(1);
    }

    [Fact]
    public void DiagnosticCheck_Category_CanBeModified()
    {
        // Arrange
        var check = new DiagnosticCheck { Category = "Initial" };

        // Act
        check.Category = "Modified";

        // Assert
        check.Category.Should().Be("Modified");
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
        check.Issues.Should().HaveCount(4);
        check.Issues.Count(i => i.Severity == DiagnosticSeverity.Success).Should().Be(1);
        check.Issues.Count(i => i.Severity == DiagnosticSeverity.Info).Should().Be(1);
        check.Issues.Count(i => i.Severity == DiagnosticSeverity.Warning).Should().Be(1);
        check.Issues.Count(i => i.Severity == DiagnosticSeverity.Error).Should().Be(1);
        check.Issues.Count(i => i.IsFixable).Should().Be(1);
    }
}
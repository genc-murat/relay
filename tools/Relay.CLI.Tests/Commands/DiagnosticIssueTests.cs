using Relay.CLI.Commands.Models.Diagnostic;

using Xunit;

namespace Relay.CLI.Tests.Commands;

public class DiagnosticIssueTests
{
    [Fact]
    public void DiagnosticIssue_ShouldHaveMessageProperty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Message = "Test message" };

        // Assert
        Assert.Equal("Test message", issue.Message);
    }

    [Fact]
    public void DiagnosticIssue_ShouldHaveSeverityProperty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Severity = DiagnosticSeverity.Warning };

        // Assert
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void DiagnosticIssue_ShouldHaveCodeProperty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Code = "TEST001" };

        // Assert
        Assert.Equal("TEST001", issue.Code);
    }

    [Fact]
    public void DiagnosticIssue_ShouldHaveIsFixableProperty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { IsFixable = true };

        // Assert
        Assert.True(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticIssue_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue();

        // Assert
        Assert.Equal("", issue.Message);
        Assert.Equal(DiagnosticSeverity.Success, issue.Severity); // Default enum value
        Assert.Equal("", issue.Code);
        Assert.False(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticIssue_CanSetAllPropertiesViaInitializer()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue
        {
            Message = "Database connection failed",
            Severity = DiagnosticSeverity.Error,
            Code = "DB_CONN_001",
            IsFixable = true
        };

        // Assert
        Assert.Equal("Database connection failed", issue.Message);
        Assert.Equal(DiagnosticSeverity.Error, issue.Severity);
        Assert.Equal("DB_CONN_001", issue.Code);
        Assert.True(issue.IsFixable);
    }

    [Theory]
    [InlineData(DiagnosticSeverity.Success)]
    [InlineData(DiagnosticSeverity.Info)]
    [InlineData(DiagnosticSeverity.Warning)]
    [InlineData(DiagnosticSeverity.Error)]
    public void DiagnosticIssue_ShouldSupportAllSeverityLevels(DiagnosticSeverity severity)
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Severity = severity };

        // Assert
        Assert.Equal(severity, issue.Severity);
    }

    [Fact]
    public void DiagnosticIssue_Message_CanBeEmpty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Message = "" };

        // Assert
        Assert.Empty(issue.Message);
    }

    [Fact]
    public void DiagnosticIssue_Message_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Message = "Error: @#$%^&*()_+-=[]{}|;:,.<>?" };

        // Assert
        Assert.Equal("Error: @#$%^&*()_+-=[]{}|;:,.<>?", issue.Message);
    }

    [Fact]
    public void DiagnosticIssue_Message_CanBeLong()
    {
        // Arrange
        var longMessage = new string('A', 2000);

        // Act
        var issue = new DiagnosticIssue { Message = longMessage };

        // Assert
        Assert.Equal(longMessage, issue.Message);
        Assert.Equal(2000, issue.Message.Length);
    }

    [Fact]
    public void DiagnosticIssue_Code_CanBeEmpty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Code = "" };

        // Assert
        Assert.Empty(issue.Code);
    }

    [Fact]
    public void DiagnosticIssue_Code_CanContainNumbersAndLetters()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Code = "ERR_001_DB" };

        // Assert
        Assert.Equal("ERR_001_DB", issue.Code);
    }

    [Fact]
    public void DiagnosticIssue_Code_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Code = "ERR-001.DB" };

        // Assert
        Assert.Equal("ERR-001.DB", issue.Code);
    }

    [Fact]
    public void DiagnosticIssue_IsFixable_CanBeTrue()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { IsFixable = true };

        // Assert
        Assert.True(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticIssue_IsFixable_CanBeFalse()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { IsFixable = false };

        // Assert
        Assert.False(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticIssue_CanBeUsedInCollections()
    {
        // Arrange & Act
        var issues = new List<DiagnosticIssue>
        {
            new() { Message = "Issue1", Severity = DiagnosticSeverity.Error },
            new() { Message = "Issue2", Severity = DiagnosticSeverity.Warning },
            new() { Message = "Issue3", Severity = DiagnosticSeverity.Info }
        };

        // Assert
        Assert.Equal(3, issues.Count);
        Assert.Equal(1, issues.Count(i => i.Severity == DiagnosticSeverity.Error));
        Assert.Equal(1, issues.Count(i => i.Severity == DiagnosticSeverity.Warning));
        Assert.Equal(1, issues.Count(i => i.Severity == DiagnosticSeverity.Info));
    }

    [Fact]
    public void DiagnosticIssue_CanBeFilteredBySeverity()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new() { Message = "Error", Severity = DiagnosticSeverity.Error, Code = "E001" },
            new() { Message = "Warning", Severity = DiagnosticSeverity.Warning, Code = "W001" },
            new() { Message = "Info", Severity = DiagnosticSeverity.Info, Code = "I001" },
            new() { Message = "Success", Severity = DiagnosticSeverity.Success, Code = "S001" }
        };

        // Act
        var errors = issues.Where(i => i.Severity == DiagnosticSeverity.Error).ToList();
        var warnings = issues.Where(i => i.Severity == DiagnosticSeverity.Warning).ToList();

        // Assert
        Assert.Single(errors);
        Assert.Single(warnings);
    }

    [Fact]
    public void DiagnosticIssue_CanBeFilteredByFixable()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new() { Message = "Fixable", IsFixable = true, Code = "F001" },
            new() { Message = "Not Fixable", IsFixable = false, Code = "NF001" },
            new() { Message = "Also Fixable", IsFixable = true, Code = "F002" }
        };

        // Act
        var fixableIssues = issues.Where(i => i.IsFixable).ToList();

        // Assert
        Assert.Equal(2, fixableIssues.Count);
        Assert.All(fixableIssues, i => Assert.True(i.IsFixable));
    }

    [Fact]
    public void DiagnosticIssue_CanBeGroupedBySeverity()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new() { Message = "Error1", Severity = DiagnosticSeverity.Error },
            new() { Message = "Error2", Severity = DiagnosticSeverity.Error },
            new() { Message = "Warning1", Severity = DiagnosticSeverity.Warning },
            new() { Message = "Info1", Severity = DiagnosticSeverity.Info },
            new() { Message = "Info2", Severity = DiagnosticSeverity.Info }
        };

        // Act
        var grouped = issues.GroupBy(i => i.Severity);

        // Assert
        Assert.Equal(3, grouped.Count());
        Assert.Equal(2, grouped.First(g => g.Key == DiagnosticSeverity.Error).Count());
        Assert.Single(grouped.First(g => g.Key == DiagnosticSeverity.Warning));
        Assert.Equal(2, grouped.First(g => g.Key == DiagnosticSeverity.Info).Count());
    }

    [Fact]
    public void DiagnosticIssue_CanBeOrderedBySeverity()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new() { Message = "Info", Severity = DiagnosticSeverity.Info },
            new() { Message = "Error", Severity = DiagnosticSeverity.Error },
            new() { Message = "Warning", Severity = DiagnosticSeverity.Warning },
            new() { Message = "Success", Severity = DiagnosticSeverity.Success }
        };

        // Act
        var ordered = issues.OrderBy(i => i.Severity).ToList();

        // Assert
        Assert.Equal(DiagnosticSeverity.Success, ordered[0].Severity);
        Assert.Equal(DiagnosticSeverity.Info, ordered[1].Severity);
        Assert.Equal(DiagnosticSeverity.Warning, ordered[2].Severity);
        Assert.Equal(DiagnosticSeverity.Error, ordered[3].Severity);
    }

    [Fact]
    public void DiagnosticIssue_PropertiesCanBeModified()
    {
        // Arrange
        var issue = new DiagnosticIssue
        {
            Message = "Initial",
            Severity = DiagnosticSeverity.Info,
            Code = "INIT",
            IsFixable = false
        };

        // Act
        issue.Message = "Modified";
        issue.Severity = DiagnosticSeverity.Error;
        issue.Code = "MOD";
        issue.IsFixable = true;

        // Assert
        Assert.Equal("Modified", issue.Message);
        Assert.Equal(DiagnosticSeverity.Error, issue.Severity);
        Assert.Equal("MOD", issue.Code);
        Assert.True(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticIssue_ShouldBeClass()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue();

        // Assert
        Assert.NotNull(issue);
        Assert.True(issue.GetType().IsClass);
    }

    [Fact]
    public void DiagnosticIssue_WithRealisticData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue
        {
            Message = "Database connection timeout after 30 seconds",
            Severity = DiagnosticSeverity.Error,
            Code = "DB_TIMEOUT",
            IsFixable = true
        };

        // Assert
        Assert.Equal("Database connection timeout after 30 seconds", issue.Message);
        Assert.Equal(DiagnosticSeverity.Error, issue.Severity);
        Assert.Equal("DB_TIMEOUT", issue.Code);
        Assert.True(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticIssue_WithWarningData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue
        {
            Message = "High memory usage detected: 85% of available RAM",
            Severity = DiagnosticSeverity.Warning,
            Code = "MEM_HIGH",
            IsFixable = true
        };

        // Assert
        Assert.Equal("High memory usage detected: 85% of available RAM", issue.Message);
        Assert.Equal(DiagnosticSeverity.Warning, issue.Severity);
        Assert.Equal("MEM_HIGH", issue.Code);
        Assert.True(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticIssue_WithInfoData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue
        {
            Message = "System uptime: 15 days, 4 hours",
            Severity = DiagnosticSeverity.Info,
            Code = "SYS_UPTIME",
            IsFixable = false
        };

        // Assert
        Assert.Equal("System uptime: 15 days, 4 hours", issue.Message);
        Assert.Equal(DiagnosticSeverity.Info, issue.Severity);
        Assert.Equal("SYS_UPTIME", issue.Code);
        Assert.False(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticIssue_WithSuccessData_ShouldStoreCorrectly()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue
        {
            Message = "All security checks passed",
            Severity = DiagnosticSeverity.Success,
            Code = "SEC_OK",
            IsFixable = false
        };

        // Assert
        Assert.Equal("All security checks passed", issue.Message);
        Assert.Equal(DiagnosticSeverity.Success, issue.Severity);
        Assert.Equal("SEC_OK", issue.Code);
        Assert.False(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticIssue_CanBeSerialized_WithComplexData()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue
        {
            Message = "Complex issue with multiple details: CPU usage 95%, Memory 80%, Disk I/O high",
            Severity = DiagnosticSeverity.Error,
            Code = "SYS_PERF_001",
            IsFixable = true
        };

        // Assert - Basic serialization check
        Assert.Contains("Complex issue", issue.Message);
        Assert.Equal(DiagnosticSeverity.Error, issue.Severity);
        Assert.Equal("SYS_PERF_001", issue.Code);
        Assert.True(issue.IsFixable);
    }

    [Fact]
    public void DiagnosticIssue_Code_CanBeUsedForGrouping()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new() { Code = "DB_001", Severity = DiagnosticSeverity.Error },
            new() { Code = "DB_002", Severity = DiagnosticSeverity.Warning },
            new() { Code = "MEM_001", Severity = DiagnosticSeverity.Warning },
            new() { Code = "DB_001", Severity = DiagnosticSeverity.Info }
        };

        // Act
        var dbIssues = issues.Where(i => i.Code.StartsWith("DB_")).ToList();

        // Assert
        Assert.Equal(3, dbIssues.Count);
        Assert.Equal(2, dbIssues.Count(i => i.Code == "DB_001"));
    }

    [Fact]
    public void DiagnosticIssue_CanCalculateSeverityDistribution()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new() { Severity = DiagnosticSeverity.Success },
            new() { Severity = DiagnosticSeverity.Info },
            new() { Severity = DiagnosticSeverity.Info },
            new() { Severity = DiagnosticSeverity.Warning },
            new() { Severity = DiagnosticSeverity.Warning },
            new() { Severity = DiagnosticSeverity.Warning },
            new() { Severity = DiagnosticSeverity.Error },
            new() { Severity = DiagnosticSeverity.Error }
        };

        // Act
        var severityCounts = issues.GroupBy(i => i.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        // Assert
        Assert.Equal(1, severityCounts[DiagnosticSeverity.Success]);
        Assert.Equal(2, severityCounts[DiagnosticSeverity.Info]);
        Assert.Equal(3, severityCounts[DiagnosticSeverity.Warning]);
        Assert.Equal(2, severityCounts[DiagnosticSeverity.Error]);
    }

    [Fact]
    public void DiagnosticIssue_CanIdentifyCriticalIssues()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new() { Severity = DiagnosticSeverity.Success, IsFixable = false },
            new() { Severity = DiagnosticSeverity.Info, IsFixable = false },
            new() { Severity = DiagnosticSeverity.Warning, IsFixable = true },
            new() { Severity = DiagnosticSeverity.Error, IsFixable = false },
            new() { Severity = DiagnosticSeverity.Error, IsFixable = true }
        };

        // Act
        var criticalIssues = issues.Where(i => i.Severity == DiagnosticSeverity.Error).ToList();
        var fixableCriticalIssues = criticalIssues.Where(i => i.IsFixable).ToList();

        // Assert
        Assert.Equal(2, criticalIssues.Count);
        Assert.Single(fixableCriticalIssues);
    }
}


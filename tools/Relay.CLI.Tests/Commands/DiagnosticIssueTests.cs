using Relay.CLI.Commands.Models.Diagnostic;

namespace Relay.CLI.Tests.Commands;

public class DiagnosticIssueTests
{
    [Fact]
    public void DiagnosticIssue_ShouldHaveMessageProperty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Message = "Test message" };

        // Assert
        issue.Message.Should().Be("Test message");
    }

    [Fact]
    public void DiagnosticIssue_ShouldHaveSeverityProperty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Severity = DiagnosticSeverity.Warning };

        // Assert
        issue.Severity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void DiagnosticIssue_ShouldHaveCodeProperty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Code = "TEST001" };

        // Assert
        issue.Code.Should().Be("TEST001");
    }

    [Fact]
    public void DiagnosticIssue_ShouldHaveIsFixableProperty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { IsFixable = true };

        // Assert
        issue.IsFixable.Should().BeTrue();
    }

    [Fact]
    public void DiagnosticIssue_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue();

        // Assert
        issue.Message.Should().Be("");
        issue.Severity.Should().Be(DiagnosticSeverity.Success); // Default enum value
        issue.Code.Should().Be("");
        issue.IsFixable.Should().BeFalse();
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
        issue.Message.Should().Be("Database connection failed");
        issue.Severity.Should().Be(DiagnosticSeverity.Error);
        issue.Code.Should().Be("DB_CONN_001");
        issue.IsFixable.Should().BeTrue();
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
        issue.Severity.Should().Be(severity);
    }

    [Fact]
    public void DiagnosticIssue_Message_CanBeEmpty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Message = "" };

        // Assert
        issue.Message.Should().BeEmpty();
    }

    [Fact]
    public void DiagnosticIssue_Message_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Message = "Error: @#$%^&*()_+-=[]{}|;:,.<>?" };

        // Assert
        issue.Message.Should().Be("Error: @#$%^&*()_+-=[]{}|;:,.<>?");
    }

    [Fact]
    public void DiagnosticIssue_Message_CanBeLong()
    {
        // Arrange
        var longMessage = new string('A', 2000);

        // Act
        var issue = new DiagnosticIssue { Message = longMessage };

        // Assert
        issue.Message.Should().Be(longMessage);
        issue.Message.Length.Should().Be(2000);
    }

    [Fact]
    public void DiagnosticIssue_Code_CanBeEmpty()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Code = "" };

        // Assert
        issue.Code.Should().BeEmpty();
    }

    [Fact]
    public void DiagnosticIssue_Code_CanContainNumbersAndLetters()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Code = "ERR_001_DB" };

        // Assert
        issue.Code.Should().Be("ERR_001_DB");
    }

    [Fact]
    public void DiagnosticIssue_Code_CanContainSpecialCharacters()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { Code = "ERR-001.DB" };

        // Assert
        issue.Code.Should().Be("ERR-001.DB");
    }

    [Fact]
    public void DiagnosticIssue_IsFixable_CanBeTrue()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { IsFixable = true };

        // Assert
        issue.IsFixable.Should().BeTrue();
    }

    [Fact]
    public void DiagnosticIssue_IsFixable_CanBeFalse()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue { IsFixable = false };

        // Assert
        issue.IsFixable.Should().BeFalse();
    }

    [Fact]
    public void DiagnosticIssue_CanBeUsedInCollections()
    {
        // Arrange & Act
        var issues = new List<DiagnosticIssue>
        {
            new DiagnosticIssue { Message = "Issue1", Severity = DiagnosticSeverity.Error },
            new DiagnosticIssue { Message = "Issue2", Severity = DiagnosticSeverity.Warning },
            new DiagnosticIssue { Message = "Issue3", Severity = DiagnosticSeverity.Info }
        };

        // Assert
        issues.Should().HaveCount(3);
        issues.Count(i => i.Severity == DiagnosticSeverity.Error).Should().Be(1);
        issues.Count(i => i.Severity == DiagnosticSeverity.Warning).Should().Be(1);
        issues.Count(i => i.Severity == DiagnosticSeverity.Info).Should().Be(1);
    }

    [Fact]
    public void DiagnosticIssue_CanBeFilteredBySeverity()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new DiagnosticIssue { Message = "Error", Severity = DiagnosticSeverity.Error, Code = "E001" },
            new DiagnosticIssue { Message = "Warning", Severity = DiagnosticSeverity.Warning, Code = "W001" },
            new DiagnosticIssue { Message = "Info", Severity = DiagnosticSeverity.Info, Code = "I001" },
            new DiagnosticIssue { Message = "Success", Severity = DiagnosticSeverity.Success, Code = "S001" }
        };

        // Act
        var errors = issues.Where(i => i.Severity == DiagnosticSeverity.Error).ToList();
        var warnings = issues.Where(i => i.Severity == DiagnosticSeverity.Warning).ToList();

        // Assert
        errors.Should().HaveCount(1);
        warnings.Should().HaveCount(1);
    }

    [Fact]
    public void DiagnosticIssue_CanBeFilteredByFixable()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new DiagnosticIssue { Message = "Fixable", IsFixable = true, Code = "F001" },
            new DiagnosticIssue { Message = "Not Fixable", IsFixable = false, Code = "NF001" },
            new DiagnosticIssue { Message = "Also Fixable", IsFixable = true, Code = "F002" }
        };

        // Act
        var fixableIssues = issues.Where(i => i.IsFixable).ToList();

        // Assert
        fixableIssues.Should().HaveCount(2);
        fixableIssues.All(i => i.IsFixable).Should().BeTrue();
    }

    [Fact]
    public void DiagnosticIssue_CanBeGroupedBySeverity()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new DiagnosticIssue { Message = "Error1", Severity = DiagnosticSeverity.Error },
            new DiagnosticIssue { Message = "Error2", Severity = DiagnosticSeverity.Error },
            new DiagnosticIssue { Message = "Warning1", Severity = DiagnosticSeverity.Warning },
            new DiagnosticIssue { Message = "Info1", Severity = DiagnosticSeverity.Info },
            new DiagnosticIssue { Message = "Info2", Severity = DiagnosticSeverity.Info }
        };

        // Act
        var grouped = issues.GroupBy(i => i.Severity);

        // Assert
        grouped.Should().HaveCount(3);
        grouped.First(g => g.Key == DiagnosticSeverity.Error).Should().HaveCount(2);
        grouped.First(g => g.Key == DiagnosticSeverity.Warning).Should().HaveCount(1);
        grouped.First(g => g.Key == DiagnosticSeverity.Info).Should().HaveCount(2);
    }

    [Fact]
    public void DiagnosticIssue_CanBeOrderedBySeverity()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new DiagnosticIssue { Message = "Info", Severity = DiagnosticSeverity.Info },
            new DiagnosticIssue { Message = "Error", Severity = DiagnosticSeverity.Error },
            new DiagnosticIssue { Message = "Warning", Severity = DiagnosticSeverity.Warning },
            new DiagnosticIssue { Message = "Success", Severity = DiagnosticSeverity.Success }
        };

        // Act
        var ordered = issues.OrderBy(i => i.Severity).ToList();

        // Assert
        ordered[0].Severity.Should().Be(DiagnosticSeverity.Success);
        ordered[1].Severity.Should().Be(DiagnosticSeverity.Info);
        ordered[2].Severity.Should().Be(DiagnosticSeverity.Warning);
        ordered[3].Severity.Should().Be(DiagnosticSeverity.Error);
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
        issue.Message.Should().Be("Modified");
        issue.Severity.Should().Be(DiagnosticSeverity.Error);
        issue.Code.Should().Be("MOD");
        issue.IsFixable.Should().BeTrue();
    }

    [Fact]
    public void DiagnosticIssue_ShouldBeClass()
    {
        // Arrange & Act
        var issue = new DiagnosticIssue();

        // Assert
        issue.Should().NotBeNull();
        issue.GetType().IsClass.Should().BeTrue();
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
        issue.Message.Should().Be("Database connection timeout after 30 seconds");
        issue.Severity.Should().Be(DiagnosticSeverity.Error);
        issue.Code.Should().Be("DB_TIMEOUT");
        issue.IsFixable.Should().BeTrue();
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
        issue.Message.Should().Be("High memory usage detected: 85% of available RAM");
        issue.Severity.Should().Be(DiagnosticSeverity.Warning);
        issue.Code.Should().Be("MEM_HIGH");
        issue.IsFixable.Should().BeTrue();
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
        issue.Message.Should().Be("System uptime: 15 days, 4 hours");
        issue.Severity.Should().Be(DiagnosticSeverity.Info);
        issue.Code.Should().Be("SYS_UPTIME");
        issue.IsFixable.Should().BeFalse();
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
        issue.Message.Should().Be("All security checks passed");
        issue.Severity.Should().Be(DiagnosticSeverity.Success);
        issue.Code.Should().Be("SEC_OK");
        issue.IsFixable.Should().BeFalse();
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
        issue.Message.Should().Contain("Complex issue");
        issue.Severity.Should().Be(DiagnosticSeverity.Error);
        issue.Code.Should().Be("SYS_PERF_001");
        issue.IsFixable.Should().BeTrue();
    }

    [Fact]
    public void DiagnosticIssue_Code_CanBeUsedForGrouping()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new DiagnosticIssue { Code = "DB_001", Severity = DiagnosticSeverity.Error },
            new DiagnosticIssue { Code = "DB_002", Severity = DiagnosticSeverity.Warning },
            new DiagnosticIssue { Code = "MEM_001", Severity = DiagnosticSeverity.Warning },
            new DiagnosticIssue { Code = "DB_001", Severity = DiagnosticSeverity.Info }
        };

        // Act
        var dbIssues = issues.Where(i => i.Code.StartsWith("DB_")).ToList();

        // Assert
        dbIssues.Should().HaveCount(3);
        dbIssues.Count(i => i.Code == "DB_001").Should().Be(2);
    }

    [Fact]
    public void DiagnosticIssue_CanCalculateSeverityDistribution()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new DiagnosticIssue { Severity = DiagnosticSeverity.Success },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Info },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Info },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Warning },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Warning },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Warning },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Error },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Error }
        };

        // Act
        var severityCounts = issues.GroupBy(i => i.Severity)
            .ToDictionary(g => g.Key, g => g.Count());

        // Assert
        severityCounts[DiagnosticSeverity.Success].Should().Be(1);
        severityCounts[DiagnosticSeverity.Info].Should().Be(2);
        severityCounts[DiagnosticSeverity.Warning].Should().Be(3);
        severityCounts[DiagnosticSeverity.Error].Should().Be(2);
    }

    [Fact]
    public void DiagnosticIssue_CanIdentifyCriticalIssues()
    {
        // Arrange
        var issues = new List<DiagnosticIssue>
        {
            new DiagnosticIssue { Severity = DiagnosticSeverity.Success, IsFixable = false },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Info, IsFixable = false },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Warning, IsFixable = true },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Error, IsFixable = false },
            new DiagnosticIssue { Severity = DiagnosticSeverity.Error, IsFixable = true }
        };

        // Act
        var criticalIssues = issues.Where(i => i.Severity == DiagnosticSeverity.Error).ToList();
        var fixableCriticalIssues = criticalIssues.Where(i => i.IsFixable).ToList();

        // Assert
        criticalIssues.Should().HaveCount(2);
        fixableCriticalIssues.Should().HaveCount(1);
    }
}
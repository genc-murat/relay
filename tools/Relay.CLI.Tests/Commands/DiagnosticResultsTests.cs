using Relay.CLI.Commands.Models.Diagnostic;

namespace Relay.CLI.Tests.Commands;

public class DiagnosticResultsTests
{
    [Fact]
    public void DiagnosticResults_ShouldHaveChecksProperty()
    {
        // Arrange & Act
        var results = new DiagnosticResults();

        // Assert
        results.Checks.Should().NotBeNull();
        results.Checks.Should().BeEmpty();
    }

    [Fact]
    public void DiagnosticResults_DefaultValues_ShouldBeValid()
    {
        // Arrange & Act
        var results = new DiagnosticResults();

        // Assert
        results.Checks.Should().NotBeNull();
        results.Checks.Should().BeEmpty();
        results.SuccessCount.Should().Be(0);
        results.InfoCount.Should().Be(0);
        results.WarningCount.Should().Be(0);
        results.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void DiagnosticResults_AddCheck_ShouldAddCheckToList()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "TestCheck" };

        // Act
        results.AddCheck(check);

        // Assert
        results.Checks.Should().HaveCount(1);
        results.Checks[0].Should().Be(check);
    }

    [Fact]
    public void DiagnosticResults_CanAddMultipleChecks()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check1 = new DiagnosticCheck { Category = "Check1" };
        var check2 = new DiagnosticCheck { Category = "Check2" };
        var check3 = new DiagnosticCheck { Category = "Check3" };

        // Act
        results.AddCheck(check1);
        results.AddCheck(check2);
        results.AddCheck(check3);

        // Assert
        results.Checks.Should().HaveCount(3);
        results.Checks.Select(c => c.Category).Should().BeEquivalentTo(new[] { "Check1", "Check2", "Check3" });
    }

    [Fact]
    public void DiagnosticResults_SuccessCount_ShouldCountSuccessIssues()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check1 = new DiagnosticCheck { Category = "Check1" };
        var check2 = new DiagnosticCheck { Category = "Check2" };

        check1.AddSuccess("Success1");
        check1.AddSuccess("Success2");
        check2.AddSuccess("Success3");

        results.AddCheck(check1);
        results.AddCheck(check2);

        // Act & Assert
        results.SuccessCount.Should().Be(3);
    }

    [Fact]
    public void DiagnosticResults_InfoCount_ShouldCountInfoIssues()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Check" };

        check.AddInfo("Info1");
        check.AddInfo("Info2");

        results.AddCheck(check);

        // Act & Assert
        results.InfoCount.Should().Be(2);
    }

    [Fact]
    public void DiagnosticResults_WarningCount_ShouldCountWarningIssues()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check1 = new DiagnosticCheck { Category = "Check1" };
        var check2 = new DiagnosticCheck { Category = "Check2" };

        check1.AddIssue("Warning1", DiagnosticSeverity.Warning, "W001");
        check2.AddIssue("Warning2", DiagnosticSeverity.Warning, "W002");
        check2.AddIssue("Warning3", DiagnosticSeverity.Warning, "W003");

        results.AddCheck(check1);
        results.AddCheck(check2);

        // Act & Assert
        results.WarningCount.Should().Be(3);
    }

    [Fact]
    public void DiagnosticResults_ErrorCount_ShouldCountErrorIssues()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Check" };

        check.AddIssue("Error1", DiagnosticSeverity.Error, "E001");
        check.AddIssue("Error2", DiagnosticSeverity.Error, "E002");

        results.AddCheck(check);

        // Act & Assert
        results.ErrorCount.Should().Be(2);
    }

    [Fact]
    public void DiagnosticResults_Counts_ShouldAggregateAcrossAllChecks()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check1 = new DiagnosticCheck { Category = "Performance" };
        var check2 = new DiagnosticCheck { Category = "Security" };
        var check3 = new DiagnosticCheck { Category = "Reliability" };

        // Performance check
        check1.AddSuccess("Perf OK");
        check1.AddIssue("High CPU", DiagnosticSeverity.Warning, "CPU001");

        // Security check
        check2.AddSuccess("Security OK");
        check2.AddInfo("Firewall active");
        check2.AddIssue("Outdated cert", DiagnosticSeverity.Error, "CERT001");

        // Reliability check
        check3.AddInfo("Uptime good");
        check3.AddIssue("Memory leak", DiagnosticSeverity.Warning, "MEM001");
        check3.AddIssue("DB timeout", DiagnosticSeverity.Error, "DB001");

        results.AddCheck(check1);
        results.AddCheck(check2);
        results.AddCheck(check3);

        // Act & Assert
        results.SuccessCount.Should().Be(2);
        results.InfoCount.Should().Be(2);
        results.WarningCount.Should().Be(2);
        results.ErrorCount.Should().Be(2);
    }

    [Fact]
    public void DiagnosticResults_HasFixableIssues_ShouldReturnTrue_WhenFixableIssuesExist()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Check" };

        check.AddIssue("Fixable error", DiagnosticSeverity.Error, "E001", true);

        results.AddCheck(check);

        // Act & Assert
        results.HasFixableIssues().Should().BeTrue();
    }

    [Fact]
    public void DiagnosticResults_HasFixableIssues_ShouldReturnFalse_WhenNoFixableIssues()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Check" };

        check.AddIssue("Non-fixable error", DiagnosticSeverity.Error, "E001", false);
        check.AddSuccess("Success");

        results.AddCheck(check);

        // Act & Assert
        results.HasFixableIssues().Should().BeFalse();
    }

    [Fact]
    public void DiagnosticResults_HasFixableIssues_ShouldReturnFalse_WhenNoIssues()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Check" };

        results.AddCheck(check);

        // Act & Assert
        results.HasFixableIssues().Should().BeFalse();
    }

    [Fact]
    public void DiagnosticResults_GetExitCode_ShouldReturn2_WhenErrorsExist()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Check" };

        check.AddIssue("Error", DiagnosticSeverity.Error, "E001");

        results.AddCheck(check);

        // Act & Assert
        results.GetExitCode().Should().Be(2);
    }

    [Fact]
    public void DiagnosticResults_GetExitCode_ShouldReturn1_WhenWarningsExistButNoErrors()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Check" };

        check.AddIssue("Warning", DiagnosticSeverity.Warning, "W001");

        results.AddCheck(check);

        // Act & Assert
        results.GetExitCode().Should().Be(1);
    }

    [Fact]
    public void DiagnosticResults_GetExitCode_ShouldReturn0_WhenNoWarningsOrErrors()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Check" };

        check.AddSuccess("Success");
        check.AddInfo("Info");

        results.AddCheck(check);

        // Act & Assert
        results.GetExitCode().Should().Be(0);
    }

    [Fact]
    public void DiagnosticResults_GetExitCode_ShouldPrioritizeErrorsOverWarnings()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "Check" };

        check.AddIssue("Warning", DiagnosticSeverity.Warning, "W001");
        check.AddIssue("Error", DiagnosticSeverity.Error, "E001");

        results.AddCheck(check);

        // Act & Assert
        results.GetExitCode().Should().Be(2);
    }

    [Fact]
    public void DiagnosticResults_Checks_IsReadOnlyCollection()
    {
        // Arrange
        var results = new DiagnosticResults();

        // Act & Assert - Checks property should be read-only, but we can add via AddCheck method
        results.Checks.Should().NotBeNull();
        // We can't directly assign to Checks, but we can add to it via AddCheck
        results.AddCheck(new DiagnosticCheck { Category = "Test" });
        results.Checks.Should().HaveCount(1);
    }

    [Fact]
    public void DiagnosticResults_CanBeUsedInCollections()
    {
        // Arrange & Act
        var resultsList = new List<DiagnosticResults>
        {
            new DiagnosticResults(),
            new DiagnosticResults(),
            new DiagnosticResults()
        };

        // Assert
        resultsList.Should().HaveCount(3);
        resultsList.All(r => r.Checks.Count == 0).Should().BeTrue();
    }

    [Fact]
    public void DiagnosticResults_CanFilterChecksByCategory()
    {
        // Arrange
        var results = new DiagnosticResults();
        var perfCheck = new DiagnosticCheck { Category = "Performance" };
        var secCheck = new DiagnosticCheck { Category = "Security" };
        var relCheck = new DiagnosticCheck { Category = "Reliability" };

        results.AddCheck(perfCheck);
        results.AddCheck(secCheck);
        results.AddCheck(relCheck);

        // Act
        var perfChecks = results.Checks.Where(c => c.Category == "Performance").ToList();

        // Assert
        perfChecks.Should().HaveCount(1);
        perfChecks[0].Category.Should().Be("Performance");
    }

    [Fact]
    public void DiagnosticResults_CanCalculateTotalIssueCount()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check1 = new DiagnosticCheck { Category = "Check1" };
        var check2 = new DiagnosticCheck { Category = "Check2" };

        check1.AddSuccess("S1");
        check1.AddInfo("I1");
        check2.AddIssue("W1", DiagnosticSeverity.Warning, "W001");
        check2.AddIssue("E1", DiagnosticSeverity.Error, "E001");

        results.AddCheck(check1);
        results.AddCheck(check2);

        // Act
        var totalIssues = results.SuccessCount + results.InfoCount + results.WarningCount + results.ErrorCount;

        // Assert
        totalIssues.Should().Be(4);
    }

    [Fact]
    public void DiagnosticResults_WithEmptyChecks_ShouldHandleOperations()
    {
        // Arrange
        var results = new DiagnosticResults();

        // Act & Assert
        results.Checks.Should().BeEmpty();
        results.SuccessCount.Should().Be(0);
        results.InfoCount.Should().Be(0);
        results.WarningCount.Should().Be(0);
        results.ErrorCount.Should().Be(0);
        results.HasFixableIssues().Should().BeFalse();
        results.GetExitCode().Should().Be(0);
    }

    [Fact]
    public void DiagnosticResults_ShouldBeClass()
    {
        // Arrange & Act
        var results = new DiagnosticResults();

        // Assert
        results.Should().NotBeNull();
        results.GetType().IsClass.Should().BeTrue();
    }

    [Fact]
    public void DiagnosticResults_WithComplexDiagnosticData()
    {
        // Arrange
        var results = new DiagnosticResults();

        // Performance check
        var perfCheck = new DiagnosticCheck { Category = "Performance" };
        perfCheck.AddSuccess("CPU usage within normal range");
        perfCheck.AddIssue("Memory usage high: 85%", DiagnosticSeverity.Warning, "MEM_HIGH", true);
        perfCheck.AddInfo("Average response time: 150ms");

        // Security check
        var secCheck = new DiagnosticCheck { Category = "Security" };
        secCheck.AddSuccess("All security patches applied");
        secCheck.AddIssue("SSL certificate expires in 7 days", DiagnosticSeverity.Warning, "CERT_EXP", true);
        secCheck.AddIssue("Firewall rule outdated", DiagnosticSeverity.Error, "FW_RULE", true);

        // Database check
        var dbCheck = new DiagnosticCheck { Category = "Database" };
        dbCheck.AddSuccess("Database connection healthy");
        dbCheck.AddInfo("Active connections: 15/100");
        dbCheck.AddIssue("Slow query detected", DiagnosticSeverity.Warning, "SLOW_QUERY", false);

        results.AddCheck(perfCheck);
        results.AddCheck(secCheck);
        results.AddCheck(dbCheck);

        // Act & Assert
        results.SuccessCount.Should().Be(3);
        results.InfoCount.Should().Be(2);
        results.WarningCount.Should().Be(3);
        results.ErrorCount.Should().Be(1);
        results.HasFixableIssues().Should().BeTrue();
        results.GetExitCode().Should().Be(2); // Has errors
    }

    [Fact]
    public void DiagnosticResults_CanAggregateIssueCountsByCategory()
    {
        // Arrange
        var results = new DiagnosticResults();

        var check1 = new DiagnosticCheck { Category = "CategoryA" };
        check1.AddIssue("Error", DiagnosticSeverity.Error, "E001");
        check1.AddIssue("Warning", DiagnosticSeverity.Warning, "W001");

        var check2 = new DiagnosticCheck { Category = "CategoryB" };
        check2.AddIssue("Error", DiagnosticSeverity.Error, "E002");

        results.AddCheck(check1);
        results.AddCheck(check2);

        // Act
        var categoryAChecks = results.Checks.Where(c => c.Category == "CategoryA").ToList();
        var categoryAErrors = categoryAChecks.Sum(c => c.Issues.Count(i => i.Severity == DiagnosticSeverity.Error));

        // Assert
        categoryAErrors.Should().Be(1);
        results.ErrorCount.Should().Be(2); // Total across all categories
    }

    [Fact]
    public void DiagnosticResults_GetExitCode_ShouldHandleEdgeCases()
    {
        // Test with only successes
        var successResults = new DiagnosticResults();
        var successCheck = new DiagnosticCheck { Category = "Success" };
        successCheck.AddSuccess("All good");
        successResults.AddCheck(successCheck);
        successResults.GetExitCode().Should().Be(0);

        // Test with only infos
        var infoResults = new DiagnosticResults();
        var infoCheck = new DiagnosticCheck { Category = "Info" };
        infoCheck.AddInfo("Just info");
        infoResults.AddCheck(infoCheck);
        infoResults.GetExitCode().Should().Be(0);

        // Test with mixed warnings and errors
        var mixedResults = new DiagnosticResults();
        var mixedCheck = new DiagnosticCheck { Category = "Mixed" };
        mixedCheck.AddIssue("Warning", DiagnosticSeverity.Warning, "W001");
        mixedCheck.AddIssue("Error", DiagnosticSeverity.Error, "E001");
        mixedResults.AddCheck(mixedCheck);
        mixedResults.GetExitCode().Should().Be(2);
    }

    [Fact]
    public void DiagnosticResults_HasFixableIssues_ShouldWorkAcrossMultipleChecks()
    {
        // Arrange
        var results = new DiagnosticResults();

        var check1 = new DiagnosticCheck { Category = "Check1" };
        check1.AddIssue("Non-fixable error", DiagnosticSeverity.Error, "E001", false);

        var check2 = new DiagnosticCheck { Category = "Check2" };
        check2.AddIssue("Fixable warning", DiagnosticSeverity.Warning, "W001", true);

        results.AddCheck(check1);
        results.AddCheck(check2);

        // Act & Assert
        results.HasFixableIssues().Should().BeTrue();
    }

    [Fact]
    public void DiagnosticResults_Counts_ShouldUpdateWhenChecksAreAdded()
    {
        // Arrange
        var results = new DiagnosticResults();

        // Initially empty
        results.SuccessCount.Should().Be(0);
        results.ErrorCount.Should().Be(0);

        // Add first check
        var check1 = new DiagnosticCheck { Category = "Check1" };
        check1.AddSuccess("Success");
        results.AddCheck(check1);

        results.SuccessCount.Should().Be(1);
        results.ErrorCount.Should().Be(0);

        // Add second check
        var check2 = new DiagnosticCheck { Category = "Check2" };
        check2.AddIssue("Error", DiagnosticSeverity.Error, "E001");
        results.AddCheck(check2);

        results.SuccessCount.Should().Be(1);
        results.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void DiagnosticResults_CanBeUsedForReporting()
    {
        // Arrange
        var results = new DiagnosticResults();
        var check = new DiagnosticCheck { Category = "System Health" };

        check.AddSuccess("CPU: OK");
        check.AddSuccess("Memory: OK");
        check.AddInfo("Disk space: 75% used");
        check.AddIssue("Network latency high", DiagnosticSeverity.Warning, "NET_LAT", true);
        check.AddIssue("Service unresponsive", DiagnosticSeverity.Error, "SVC_DOWN", false);

        results.AddCheck(check);

        // Act - Simulate report generation
        var report = new
        {
            TotalChecks = results.Checks.Count,
            Successes = results.SuccessCount,
            Infos = results.InfoCount,
            Warnings = results.WarningCount,
            Errors = results.ErrorCount,
            HasFixable = results.HasFixableIssues(),
            ExitCode = results.GetExitCode()
        };

        // Assert
        report.TotalChecks.Should().Be(1);
        report.Successes.Should().Be(2);
        report.Infos.Should().Be(1);
        report.Warnings.Should().Be(1);
        report.Errors.Should().Be(1);
        report.HasFixable.Should().BeTrue();
        report.ExitCode.Should().Be(2);
    }
}
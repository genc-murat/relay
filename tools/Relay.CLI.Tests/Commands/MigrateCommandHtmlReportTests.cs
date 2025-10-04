using System.Reflection;
using Relay.CLI.Migration;
using Xunit;

namespace Relay.CLI.Tests.Commands;

public class MigrateCommandHtmlReportTests
{
    private static string InvokeGenerateHtmlReport(MigrationResult result)
    {
        var migrateCommandType = typeof(Relay.CLI.Commands.MigrateCommand);
        var method = migrateCommandType.GetMethod("GenerateHtmlReport",
            BindingFlags.NonPublic | BindingFlags.Static);

        if (method == null)
            throw new InvalidOperationException("GenerateHtmlReport method not found");

        return (string)method.Invoke(null, new object[] { result })!;
    }

    [Fact]
    public void GenerateHtmlReport_ShouldGenerateValidHtml()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(5.5),
            FilesModified = 10,
            LinesChanged = 100,
            HandlersMigrated = 15
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<html lang=\"en\">", html);
        Assert.Contains("</html>", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldIncludeTitle()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(3.2),
            FilesModified = 5,
            LinesChanged = 50,
            HandlersMigrated = 8
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("<title>Migration Report</title>", html);
        Assert.Contains("<h1>🔄 Migration Report</h1>", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldDisplaySuccessStatus()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(2.0),
            FilesModified = 3,
            LinesChanged = 30,
            HandlersMigrated = 5
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("✅", html);
        Assert.Contains("Success", html);
        Assert.Contains("#4CAF50", html); // Green color for success
    }

    [Fact]
    public void GenerateHtmlReport_ShouldDisplayPartialStatus()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Partial,
            Duration = TimeSpan.FromSeconds(4.0),
            FilesModified = 7,
            LinesChanged = 70,
            HandlersMigrated = 10
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("⚠️", html);
        Assert.Contains("Partial", html);
        Assert.Contains("#FFC107", html); // Yellow color for partial
    }

    [Fact]
    public void GenerateHtmlReport_ShouldDisplayFailedStatus()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Failed,
            Duration = TimeSpan.FromSeconds(1.5),
            FilesModified = 0,
            LinesChanged = 0,
            HandlersMigrated = 0
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("❌", html);
        Assert.Contains("Failed", html);
        Assert.Contains("#F44336", html); // Red color for failed
    }

    [Fact]
    public void GenerateHtmlReport_ShouldDisplaySummaryMetrics()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(7.89),
            FilesModified = 25,
            LinesChanged = 350,
            HandlersMigrated = 42
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("📊 Summary", html);
        Assert.Contains("<td>Files Modified</td><td>25</td>", html);
        Assert.Contains("<td>Lines Changed</td><td>350</td>", html);
        Assert.Contains("<td>Handlers Migrated</td><td>42</td>", html);
        // Duration is formatted with :F2 - may use comma or dot depending on culture
        Assert.Matches(@"Duration:\s*7[.,]89s", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldIncludeChangesSection_WhenChangesExist()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(3.0),
            FilesModified = 5,
            LinesChanged = 50,
            HandlersMigrated = 8,
            Changes = new List<MigrationChange>
            {
                new() { Category = "Using Statements", Type = ChangeType.Modify, Description = "Updated MediatR to Relay.Core" },
                new() { Category = "Handler Methods", Type = ChangeType.Add, Description = "Added [Handle] attribute" },
                new() { Category = "Return Types", Type = ChangeType.Modify, Description = "Changed Task to ValueTask" }
            }
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("📝 Changes Applied", html);
        Assert.Contains("Using Statements", html);
        Assert.Contains("Handler Methods", html);
        Assert.Contains("Return Types", html);
        Assert.Contains("Updated MediatR to Relay.Core", html);
        Assert.Contains("Added [Handle] attribute", html);
        Assert.Contains("Changed Task to ValueTask", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldNotIncludeChangesSection_WhenNoChanges()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 0,
            LinesChanged = 0,
            HandlersMigrated = 0,
            Changes = new List<MigrationChange>()
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.DoesNotContain("📝 Changes Applied", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldStyleChangesByType()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(2.0),
            FilesModified = 3,
            LinesChanged = 30,
            HandlersMigrated = 5,
            Changes = new List<MigrationChange>
            {
                new() { Category = "Test", Type = ChangeType.Add, Description = "Added feature" },
                new() { Category = "Test", Type = ChangeType.Remove, Description = "Removed old code" },
                new() { Category = "Test", Type = ChangeType.Modify, Description = "Modified handler" }
            }
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("change-add", html);
        Assert.Contains("change-remove", html);
        Assert.Contains("change-modify", html);
        Assert.Contains("icon-add", html);
        Assert.Contains("icon-remove", html);
        Assert.Contains("icon-modify", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldIncludeManualStepsSection_WhenManualStepsExist()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Partial,
            Duration = TimeSpan.FromSeconds(5.0),
            FilesModified = 10,
            LinesChanged = 100,
            HandlersMigrated = 15,
            ManualSteps = new List<string>
            {
                "Update NuGet packages to Relay.Core",
                "Review and test all migrated handlers",
                "Update project documentation"
            }
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("⚠️ Manual Steps Required", html);
        Assert.Contains("Update NuGet packages to Relay.Core", html);
        Assert.Contains("Review and test all migrated handlers", html);
        Assert.Contains("Update project documentation", html);
        Assert.Contains("<ol>", html);
        Assert.Contains("<li>", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldNotIncludeManualStepsSection_WhenNoManualSteps()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(2.0),
            FilesModified = 5,
            LinesChanged = 50,
            HandlersMigrated = 8,
            ManualSteps = new List<string>()
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.DoesNotContain("⚠️ Manual Steps Required", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldIncludeBackupSection_WhenBackupCreated()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(3.0),
            FilesModified = 8,
            LinesChanged = 80,
            HandlersMigrated = 12,
            CreatedBackup = true,
            BackupPath = @"C:\backup\migration-2024-01-15"
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("💾 Backup Information", html);
        Assert.Contains("<code>C:\\backup\\migration-2024-01-15</code>", html);
        Assert.Contains("Rollback Command:", html);
        Assert.Contains("relay migrate rollback --backup", html);
        Assert.Contains("<pre><code>", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldNotIncludeBackupSection_WhenNoBackupCreated()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(2.0),
            FilesModified = 5,
            LinesChanged = 50,
            HandlersMigrated = 8,
            CreatedBackup = false,
            BackupPath = null
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.DoesNotContain("💾 Backup Information", html);
        Assert.DoesNotContain("Rollback Command:", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldEscapeHtmlInDescriptions()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(2.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1,
            Changes = new List<MigrationChange>
            {
                new() { Category = "Test", Type = ChangeType.Modify, Description = "Updated <Handler> with <Attribute>" }
            }
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("&lt;Handler&gt;", html);
        Assert.Contains("&lt;Attribute&gt;", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldIncludeResponsiveMetaTag()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">", html);
        Assert.Contains("<meta charset=\"UTF-8\">", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldIncludeCSS()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("<style>", html);
        Assert.Contains("</style>", html);
        Assert.Contains("font-family:", html);
        Assert.Contains(".container", html);
        Assert.Contains(".header", html);
        Assert.Contains(".section", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldIncludeFooter()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("Generated by", html);
        Assert.Contains("Relay CLI Migration Tool", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldGroupChangesByCategory()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(3.0),
            FilesModified = 5,
            LinesChanged = 50,
            HandlersMigrated = 8,
            Changes = new List<MigrationChange>
            {
                new() { Category = "Using Statements", Type = ChangeType.Modify, Description = "Change 1" },
                new() { Category = "Using Statements", Type = ChangeType.Modify, Description = "Change 2" },
                new() { Category = "Handlers", Type = ChangeType.Add, Description = "Change 3" },
                new() { Category = "Handlers", Type = ChangeType.Modify, Description = "Change 4" }
            }
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("<h3>Using Statements</h3>", html);
        Assert.Contains("<h3>Handlers</h3>", html);
        Assert.Contains("category-group", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldFormatDurationCorrectly()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(123.456),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        // Duration is formatted to 2 decimal places - may use comma or dot depending on culture
        Assert.Matches(@"Duration:\s*123[.,]46s", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldIncludeTimestamp()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("Generated:", html);
        Assert.Matches(@"\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}", html); // yyyy-MM-dd HH:mm:ss pattern
    }

    [Theory]
    [InlineData(ChangeType.Add, "➕")]
    [InlineData(ChangeType.Remove, "➖")]
    [InlineData(ChangeType.Modify, "✏️")]
    public void GenerateHtmlReport_ShouldUseCorrectIconForChangeType(ChangeType changeType, string expectedIcon)
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1,
            Changes = new List<MigrationChange>
            {
                new() { Category = "Test", Type = changeType, Description = "Test change" }
            }
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains(expectedIcon, html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldHandleEmptyBackupPath()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1,
            CreatedBackup = true,
            BackupPath = ""
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.DoesNotContain("💾 Backup Information", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldHandleSpecialCharactersInManualSteps()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1,
            ManualSteps = new List<string>
            {
                "Review <code> blocks & 'special' characters"
            }
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("&lt;code&gt;", html);
        Assert.Contains("&amp;", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldIncludeTableForMetrics()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Contains("<table>", html);
        Assert.Contains("<thead>", html);
        Assert.Contains("<tbody>", html);
        Assert.Contains("<th>Metric</th>", html);
        Assert.Contains("<th>Value</th>", html);
    }

    [Fact]
    public void GenerateHtmlReport_ShouldBeWellFormedXml()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1
        };

        // Act
        var html = InvokeGenerateHtmlReport(result);

        // Assert
        Assert.Equal(html.Count(c => c == '<'), html.Count(c => c == '>'));
    }
}

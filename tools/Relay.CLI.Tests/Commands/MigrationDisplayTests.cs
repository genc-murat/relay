using Relay.CLI.Commands;
using Relay.CLI.Migration;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;
using TestConsole = Spectre.Console.Testing.TestConsole;

namespace Relay.CLI.Tests.Commands;

public class MigrationDisplayTests
{
    [Fact]
    public void DisplayAnalysisResults_WithNoPackagesNoIssuesCanMigrate_ShouldDisplayCorrectly()
    {
        // Arrange
        var analysis = new AnalysisResult
        {
            ProjectPath = "C:\\TestProject\\Test.csproj",
            FilesAffected = 10,
            HandlersFound = 5,
            RequestsFound = 3,
            NotificationsFound = 2,
            PipelineBehaviorsFound = 1,
            PackageReferences = new(),
            Issues = new(),
            CanMigrate = true
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayAnalysisResults(analysis);

            // Assert
            var output = console.Output;
            Assert.Contains("Test.csproj", output);
            Assert.Contains("Files Affected: 10", output);
            Assert.Contains("Handlers Found: 5", output);
            Assert.Contains("Requests Found: 3", output);
            Assert.Contains("Notifications Found: 2", output);
            Assert.Contains("Pipeline Behaviors: 1", output);
            Assert.Contains("✅ Migration can proceed", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayAnalysisResults_WithPackages_ShouldDisplayPackageUpdates()
    {
        // Arrange
        var analysis = new AnalysisResult
        {
            ProjectPath = "C:\\TestProject\\Test.csproj",
            FilesAffected = 1,
            HandlersFound = 1,
            RequestsFound = 1,
            NotificationsFound = 1,
            PipelineBehaviorsFound = 1,
            PackageReferences =
            [
                new() { Name = "MediatR", CurrentVersion = "12.0.0", TargetVersion = "Relay.Core" },
                new() { Name = "MediatR.Extensions.Microsoft.DependencyInjection", CurrentVersion = "11.1.0", TargetVersion = "Relay.Core" }
            ],
            Issues = new(),
            CanMigrate = true
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayAnalysisResults(analysis);

            // Assert
            var output = console.Output;
            Assert.Contains("📦 Packages to Update:", output);
            Assert.Contains("MediatR", output);
            Assert.Contains("12.0.0", output);
            Assert.Contains("Relay.Core", output);
            Assert.Contains("MediatR.Extensions.Microsoft.DependencyInjection", output);
            Assert.Contains("11.1.0", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayAnalysisResults_WithIssues_ShouldDisplayIssuesWithIcons()
    {
        // Arrange
        var analysis = new AnalysisResult
        {
            ProjectPath = "C:\\TestProject\\Test.csproj",
            FilesAffected = 1,
            HandlersFound = 1,
            RequestsFound = 1,
            NotificationsFound = 1,
            PipelineBehaviorsFound = 1,
            PackageReferences = new(),
            Issues =
            [
                new() { Severity = IssueSeverity.Error, Message = "Critical error message" },
                new() { Severity = IssueSeverity.Warning, Message = "Warning message" },
                new() { Severity = IssueSeverity.Info, Message = "Info message" },
                new() { Severity = IssueSeverity.Critical, Message = "Critical message" }
            ],
            CanMigrate = false
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayAnalysisResults(analysis);

            // Assert
            var output = console.Output;
            Assert.True(output.Length > 0, "Output should not be empty");
            Assert.Contains("Issues Found", output);
            Assert.Contains("Critical error message", output);
            Assert.Contains("Warning message", output);
            Assert.Contains("Info message", output);
            Assert.Contains("Critical message", output);
            Assert.Contains("Migration blocked", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayAnalysisResults_WithManyIssues_ShouldTruncateAndShowMore()
    {
        // Arrange
        var issues = new List<MigrationIssue>();
        for (int i = 0; i < 8; i++)
        {
            issues.Add(new MigrationIssue { Severity = IssueSeverity.Warning, Message = $"Issue {i}" });
        }
        var analysis = new AnalysisResult
        {
            ProjectPath = "C:\\TestProject\\Test.csproj",
            FilesAffected = 1,
            HandlersFound = 1,
            RequestsFound = 1,
            NotificationsFound = 1,
            PipelineBehaviorsFound = 1,
            PackageReferences = new(),
            Issues = issues,
            CanMigrate = true
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayAnalysisResults(analysis);

            // Assert
            var output = console.Output;
            Assert.Contains("⚠️  Issues Found: 8", output);
            Assert.Contains("... and 3 more", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayAnalysisResults_WithCannotMigrate_ShouldShowBlockedMessage()
    {
        // Arrange
        var analysis = new AnalysisResult
        {
            ProjectPath = "C:\\TestProject\\Test.csproj",
            FilesAffected = 1,
            HandlersFound = 1,
            RequestsFound = 1,
            NotificationsFound = 1,
            PipelineBehaviorsFound = 1,
            PackageReferences = new(),
            Issues = new(),
            CanMigrate = false
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayAnalysisResults(analysis);

            // Assert
            var output = console.Output;
            Assert.Contains("❌ Migration blocked - fix critical issues first", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithSuccessStatus_ShouldDisplayCorrectly()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(5.5),
            FilesModified = 10,
            LinesChanged = 150,
            HandlersMigrated = 8,
            CreatedBackup = false,
            Changes = new(),
            ManualSteps = new()
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, false);

            // Assert
            var output = console.Output;
            Assert.Contains("✅ Success", output);
            Assert.Contains("5.50s", output);
            Assert.Contains("Files Modified", output);
            Assert.Contains("10", output);
            Assert.Contains("Lines Changed", output);
            Assert.Contains("150", output);
            Assert.Contains("Handlers Migrated", output);
            Assert.Contains("8", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithPartialStatus_ShouldDisplayCorrectly()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Partial,
            Duration = TimeSpan.FromSeconds(3.2),
            FilesModified = 5,
            LinesChanged = 75,
            HandlersMigrated = 4,
            CreatedBackup = true,
            BackupPath = "C:\\backup\\path",
            Changes = new(),
            ManualSteps = new()
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, false);

            // Assert
            var output = console.Output;
            Assert.Contains("⚠️  Partial Success", output);
            Assert.Contains("Backup Path", output);
            Assert.Contains("C:\\backup\\path", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithFailedStatus_ShouldDisplayCorrectly()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Failed,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 0,
            LinesChanged = 0,
            HandlersMigrated = 0,
            CreatedBackup = false,
            Changes = new(),
            ManualSteps = new()
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, false);

            // Assert
            var output = console.Output;
            Assert.Contains("❌ Failed", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithChanges_ShouldDisplayTree()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1,
            CreatedBackup = false,
            Changes =
            [
                new() { Category = "Using Directives", Type = ChangeType.Remove, Description = "Removed MediatR using" },
                new() { Category = "Using Directives", Type = ChangeType.Add, Description = "Added Relay.Core using" },
                new() { Category = "Package References", Type = ChangeType.Modify, Description = "Updated MediatR to Relay.Core" }
            ],
            ManualSteps = new()
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, false);

            // Assert
            var output = console.Output;
            Assert.Contains("Changes Applied", output);
            Assert.Contains("Using Directives", output);
            Assert.Contains("Package References", output);
            Assert.Contains("Removed MediatR using", output);
            Assert.Contains("Added Relay.Core using", output);
            Assert.Contains("Updated MediatR to Relay.Core", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithManyChanges_ShouldTruncatePerCategory()
    {
        // Arrange
        var changes = new List<MigrationChange>();
        for (int i = 0; i < 12; i++)
        {
            changes.Add(new MigrationChange
            {
                Category = "Using Directives",
                Type = ChangeType.Add,
                Description = $"Change {i}"
            });
        }
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1,
            CreatedBackup = false,
            Changes = changes,
            ManualSteps = new()
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, false);

            // Assert
            var output = console.Output;
            Assert.Contains("... and 2 more", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithManualSteps_ShouldDisplayList()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1,
            CreatedBackup = false,
            Changes = new(),
            ManualSteps = ["Step 1: Update configuration", "Step 2: Test the application"]
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, false);

            // Assert
            var output = console.Output;
            Assert.Contains("⚠️  Manual Steps Required:", output);
            Assert.Contains("Step 1: Update configuration", output);
            Assert.Contains("Step 2: Test the application", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithDryRun_ShouldShowPreview()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1,
            CreatedBackup = false,
            Changes = [new() { Category = "Test", Type = ChangeType.Add, Description = "Test change" }],
            ManualSteps = new()
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, true);

            // Assert
            var output = console.Output;
            Assert.Contains("🔍 Dry Run Results", output);
            Assert.Contains("(Preview)", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithBackupAndNotDryRun_ShouldShowRollbackInfo()
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
            BackupPath = "C:\\backup\\test",
            Changes = new(),
            ManualSteps = new()
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, false);

            // Assert
            var output = console.Output;
            Assert.Contains("💡 To rollback: relay migrate rollback --backup C:\\backup\\test", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithoutBackup_ShouldNotShowRollbackInfo()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            Duration = TimeSpan.FromSeconds(1.0),
            FilesModified = 1,
            LinesChanged = 10,
            HandlersMigrated = 1,
            CreatedBackup = false,
            Changes = new(),
            ManualSteps = new()
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, false);

            // Assert
            var output = console.Output;
            Assert.DoesNotContain("To rollback", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithNullBackupPath_ShouldNotShowRollbackInfo()
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
            BackupPath = null,
            Changes = new(),
            ManualSteps = new()
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, false);

            // Assert
            var output = console.Output;
            Assert.DoesNotContain("To rollback", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }

    [Fact]
    public void DisplayMigrationResults_WithDryRun_ShouldNotShowRollbackInfo()
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
            BackupPath = "C:\\backup\\test",
            Changes = new(),
            ManualSteps = new()
        };
        var console = new TestConsole();
        var originalConsole = AnsiConsole.Console;
        AnsiConsole.Console = console;

        try
        {
            // Act
            MigrationDisplay.DisplayMigrationResults(result, true);

            // Assert
            var output = console.Output;
            Assert.DoesNotContain("To rollback", output);
        }
        finally
        {
            AnsiConsole.Console = originalConsole;
        }
    }
}
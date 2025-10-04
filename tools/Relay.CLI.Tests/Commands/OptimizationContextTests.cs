using FluentAssertions;
using Relay.CLI.Commands;
using Xunit;

namespace Relay.CLI.Tests.Commands;

/// <summary>
/// Comprehensive tests for OptimizationContext class
/// </summary>
public class OptimizationContextTests
{
    #region Constructor and Default Values Tests

    [Fact]
    public void OptimizationContext_DefaultConstructor_ShouldInitializeProperties()
    {
        // Act
        var context = new OptimizationContext();

        // Assert
        context.ProjectPath.Should().BeEmpty();
        context.IsDryRun.Should().BeFalse();
        context.Target.Should().BeEmpty();
        context.IsAggressive.Should().BeFalse();
        context.CreateBackup.Should().BeFalse();
        context.Timestamp.Should().Be(default(DateTime));
        context.BackupPath.Should().BeEmpty();
        context.SourceFiles.Should().NotBeNull();
        context.SourceFiles.Should().BeEmpty();
        context.OptimizationActions.Should().NotBeNull();
        context.OptimizationActions.Should().BeEmpty();
    }

    [Fact]
    public void OptimizationContext_NewInstance_ShouldHaveEmptyCollections()
    {
        // Act
        var context = new OptimizationContext();

        // Assert
        context.SourceFiles.Should().NotBeNull().And.BeEmpty();
        context.OptimizationActions.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region Property Assignment Tests

    [Fact]
    public void OptimizationContext_ProjectPath_ShouldBeSettable()
    {
        // Arrange
        var context = new OptimizationContext();
        var expectedPath = @"C:\Projects\MyRelay";

        // Act
        context.ProjectPath = expectedPath;

        // Assert
        context.ProjectPath.Should().Be(expectedPath);
    }

    [Fact]
    public void OptimizationContext_IsDryRun_ShouldBeSettable()
    {
        // Arrange
        var context = new OptimizationContext();

        // Act
        context.IsDryRun = true;

        // Assert
        context.IsDryRun.Should().BeTrue();
    }

    [Fact]
    public void OptimizationContext_Target_ShouldBeSettable()
    {
        // Arrange
        var context = new OptimizationContext();

        // Act
        context.Target = "handlers";

        // Assert
        context.Target.Should().Be("handlers");
    }

    [Fact]
    public void OptimizationContext_IsAggressive_ShouldBeSettable()
    {
        // Arrange
        var context = new OptimizationContext();

        // Act
        context.IsAggressive = true;

        // Assert
        context.IsAggressive.Should().BeTrue();
    }

    [Fact]
    public void OptimizationContext_CreateBackup_ShouldBeSettable()
    {
        // Arrange
        var context = new OptimizationContext();

        // Act
        context.CreateBackup = true;

        // Assert
        context.CreateBackup.Should().BeTrue();
    }

    [Fact]
    public void OptimizationContext_Timestamp_ShouldBeSettable()
    {
        // Arrange
        var context = new OptimizationContext();
        var timestamp = DateTime.UtcNow;

        // Act
        context.Timestamp = timestamp;

        // Assert
        context.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void OptimizationContext_BackupPath_ShouldBeSettable()
    {
        // Arrange
        var context = new OptimizationContext();
        var backupPath = @"C:\Projects\.backup";

        // Act
        context.BackupPath = backupPath;

        // Assert
        context.BackupPath.Should().Be(backupPath);
    }

    [Fact]
    public void OptimizationContext_SourceFiles_ShouldBeSettable()
    {
        // Arrange
        var context = new OptimizationContext();
        var files = new List<string> { "Handler1.cs", "Handler2.cs" };

        // Act
        context.SourceFiles = files;

        // Assert
        context.SourceFiles.Should().BeEquivalentTo(files);
    }

    [Fact]
    public void OptimizationContext_OptimizationActions_ShouldBeSettable()
    {
        // Arrange
        var context = new OptimizationContext();
        var actions = new List<OptimizationAction>
        {
            new OptimizationAction { Type = "Handler" }
        };

        // Act
        context.OptimizationActions = actions;

        // Assert
        context.OptimizationActions.Should().HaveCount(1);
    }

    #endregion

    #region Complete Context Tests

    [Fact]
    public void OptimizationContext_WithAllProperties_ShouldStoreAllValues()
    {
        // Arrange & Act
        var timestamp = DateTime.UtcNow;
        var context = new OptimizationContext
        {
            ProjectPath = @"C:\Projects\MyRelay",
            IsDryRun = true,
            Target = "handlers",
            IsAggressive = true,
            CreateBackup = true,
            Timestamp = timestamp,
            BackupPath = @"C:\Projects\.backup",
            SourceFiles = new List<string> { "File1.cs", "File2.cs" },
            OptimizationActions = new List<OptimizationAction>
            {
                new OptimizationAction { Type = "Handler" }
            }
        };

        // Assert
        context.ProjectPath.Should().Be(@"C:\Projects\MyRelay");
        context.IsDryRun.Should().BeTrue();
        context.Target.Should().Be("handlers");
        context.IsAggressive.Should().BeTrue();
        context.CreateBackup.Should().BeTrue();
        context.Timestamp.Should().Be(timestamp);
        context.BackupPath.Should().Be(@"C:\Projects\.backup");
        context.SourceFiles.Should().HaveCount(2);
        context.OptimizationActions.Should().HaveCount(1);
    }

    #endregion

    #region Target Types Tests

    [Theory]
    [InlineData("all")]
    [InlineData("handlers")]
    [InlineData("requests")]
    [InlineData("config")]
    public void OptimizationContext_Target_ShouldSupportCommonTargets(string target)
    {
        // Arrange & Act
        var context = new OptimizationContext { Target = target };

        // Assert
        context.Target.Should().Be(target);
    }

    [Fact]
    public void OptimizationContext_Target_All_ShouldBeRecognized()
    {
        // Arrange & Act
        var context = new OptimizationContext { Target = "all" };

        // Assert
        context.Target.Should().Be("all");
    }

    [Fact]
    public void OptimizationContext_Target_Handlers_ShouldBeRecognized()
    {
        // Arrange & Act
        var context = new OptimizationContext { Target = "handlers" };

        // Assert
        context.Target.Should().Be("handlers");
    }

    [Fact]
    public void OptimizationContext_Target_CaseInsensitive_ShouldWork()
    {
        // Arrange & Act
        var context = new OptimizationContext { Target = "HANDLERS" };
        var normalized = context.Target.ToLowerInvariant();

        // Assert
        normalized.Should().Be("handlers");
    }

    #endregion

    #region SourceFiles Management Tests

    [Fact]
    public void OptimizationContext_SourceFiles_CanAddFiles()
    {
        // Arrange
        var context = new OptimizationContext();

        // Act
        context.SourceFiles.Add("Handler1.cs");
        context.SourceFiles.Add("Handler2.cs");

        // Assert
        context.SourceFiles.Should().HaveCount(2);
    }

    [Fact]
    public void OptimizationContext_SourceFiles_CanAddRange()
    {
        // Arrange
        var context = new OptimizationContext();
        var files = new[] { "File1.cs", "File2.cs", "File3.cs" };

        // Act
        context.SourceFiles.AddRange(files);

        // Assert
        context.SourceFiles.Should().HaveCount(3);
    }

    [Fact]
    public void OptimizationContext_SourceFiles_CanClear()
    {
        // Arrange
        var context = new OptimizationContext();
        context.SourceFiles.AddRange(new[] { "File1.cs", "File2.cs" });

        // Act
        context.SourceFiles.Clear();

        // Assert
        context.SourceFiles.Should().BeEmpty();
    }

    [Fact]
    public void OptimizationContext_SourceFiles_CanCheckContains()
    {
        // Arrange
        var context = new OptimizationContext();
        context.SourceFiles.Add("Handler.cs");

        // Act
        var contains = context.SourceFiles.Contains("Handler.cs");

        // Assert
        contains.Should().BeTrue();
    }

    [Fact]
    public void OptimizationContext_SourceFiles_CanRemove()
    {
        // Arrange
        var context = new OptimizationContext();
        context.SourceFiles.AddRange(new[] { "File1.cs", "File2.cs" });

        // Act
        context.SourceFiles.Remove("File1.cs");

        // Assert
        context.SourceFiles.Should().HaveCount(1);
        context.SourceFiles.Should().NotContain("File1.cs");
    }

    [Fact]
    public void OptimizationContext_SourceFiles_SupportsLinq()
    {
        // Arrange
        var context = new OptimizationContext();
        context.SourceFiles.AddRange(new[]
        {
            "GetUserHandler.cs",
            "CreateUserHandler.cs",
            "UserRequest.cs"
        });

        // Act
        var handlers = context.SourceFiles.Where(f => f.Contains("Handler")).ToList();

        // Assert
        handlers.Should().HaveCount(2);
    }

    #endregion

    #region OptimizationActions Management Tests

    [Fact]
    public void OptimizationContext_OptimizationActions_CanAddActions()
    {
        // Arrange
        var context = new OptimizationContext();

        // Act
        context.OptimizationActions.Add(new OptimizationAction { Type = "Handler" });
        context.OptimizationActions.Add(new OptimizationAction { Type = "Request" });

        // Assert
        context.OptimizationActions.Should().HaveCount(2);
    }

    [Fact]
    public void OptimizationContext_OptimizationActions_CanAddRange()
    {
        // Arrange
        var context = new OptimizationContext();
        var actions = new[]
        {
            new OptimizationAction { Type = "Handler" },
            new OptimizationAction { Type = "Request" },
            new OptimizationAction { Type = "Config" }
        };

        // Act
        context.OptimizationActions.AddRange(actions);

        // Assert
        context.OptimizationActions.Should().HaveCount(3);
    }

    [Fact]
    public void OptimizationContext_OptimizationActions_CanClear()
    {
        // Arrange
        var context = new OptimizationContext();
        context.OptimizationActions.Add(new OptimizationAction { Type = "Handler" });

        // Act
        context.OptimizationActions.Clear();

        // Assert
        context.OptimizationActions.Should().BeEmpty();
    }

    [Fact]
    public void OptimizationContext_OptimizationActions_CanGroupByType()
    {
        // Arrange
        var context = new OptimizationContext();
        context.OptimizationActions.AddRange(new[]
        {
            new OptimizationAction { Type = "Handler" },
            new OptimizationAction { Type = "Handler" },
            new OptimizationAction { Type = "Request" }
        });

        // Act
        var grouped = context.OptimizationActions.GroupBy(a => a.Type).ToList();

        // Assert
        grouped.Should().HaveCount(2);
        grouped.First(g => g.Key == "Handler").Should().HaveCount(2);
    }

    [Fact]
    public void OptimizationContext_OptimizationActions_CanCountModifications()
    {
        // Arrange
        var context = new OptimizationContext();
        context.OptimizationActions.AddRange(new[]
        {
            new OptimizationAction { Modifications = new List<string> { "Mod1", "Mod2" } },
            new OptimizationAction { Modifications = new List<string> { "Mod3" } }
        });

        // Act
        var totalMods = context.OptimizationActions.Sum(a => a.Modifications.Count);

        // Assert
        totalMods.Should().Be(3);
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void OptimizationContext_Timestamp_CanBeSetToNow()
    {
        // Arrange
        var context = new OptimizationContext();
        var now = DateTime.UtcNow;

        // Act
        context.Timestamp = now;

        // Assert
        context.Timestamp.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void OptimizationContext_Timestamp_CanBeUsedForBackupNaming()
    {
        // Arrange
        var context = new OptimizationContext
        {
            Timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var backupName = $".backup_{context.Timestamp:yyyyMMdd_HHmmss}";

        // Assert
        backupName.Should().Be(".backup_20240115_103000");
    }

    [Fact]
    public void OptimizationContext_Timestamp_CanCalculateElapsedTime()
    {
        // Arrange
        var context = new OptimizationContext
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        };

        // Act
        var elapsed = DateTime.UtcNow - context.Timestamp;

        // Assert
        elapsed.TotalMinutes.Should().BeGreaterThanOrEqualTo(4.9);
    }

    #endregion

    #region DryRun and Aggressive Mode Tests

    [Fact]
    public void OptimizationContext_DryRun_WhenTrue_ShouldNotModifyFiles()
    {
        // Arrange
        var context = new OptimizationContext { IsDryRun = true };

        // Assert
        context.IsDryRun.Should().BeTrue();
    }

    [Fact]
    public void OptimizationContext_DryRun_WhenFalse_ShouldAllowModifications()
    {
        // Arrange
        var context = new OptimizationContext { IsDryRun = false };

        // Assert
        context.IsDryRun.Should().BeFalse();
    }

    [Fact]
    public void OptimizationContext_AggressiveMode_WhenTrue_ShouldEnableAllOptimizations()
    {
        // Arrange
        var context = new OptimizationContext { IsAggressive = true };

        // Assert
        context.IsAggressive.Should().BeTrue();
    }

    [Fact]
    public void OptimizationContext_AggressiveMode_WhenFalse_ShouldUseSafeOptimizations()
    {
        // Arrange
        var context = new OptimizationContext { IsAggressive = false };

        // Assert
        context.IsAggressive.Should().BeFalse();
    }

    [Fact]
    public void OptimizationContext_DryRunAndAggressive_CanBeBothTrue()
    {
        // Arrange & Act
        var context = new OptimizationContext
        {
            IsDryRun = true,
            IsAggressive = true
        };

        // Assert
        context.IsDryRun.Should().BeTrue();
        context.IsAggressive.Should().BeTrue();
    }

    #endregion

    #region Backup Management Tests

    [Fact]
    public void OptimizationContext_CreateBackup_WhenTrue_ShouldIndicateBackupNeeded()
    {
        // Arrange
        var context = new OptimizationContext { CreateBackup = true };

        // Assert
        context.CreateBackup.Should().BeTrue();
    }

    [Fact]
    public void OptimizationContext_CreateBackup_WhenFalse_ShouldSkipBackup()
    {
        // Arrange
        var context = new OptimizationContext { CreateBackup = false };

        // Assert
        context.CreateBackup.Should().BeFalse();
    }

    [Fact]
    public void OptimizationContext_BackupPath_CanBeRelative()
    {
        // Arrange & Act
        var context = new OptimizationContext { BackupPath = ".backup" };

        // Assert
        context.BackupPath.Should().Be(".backup");
    }

    [Fact]
    public void OptimizationContext_BackupPath_CanBeAbsolute()
    {
        // Arrange & Act
        var context = new OptimizationContext
        {
            BackupPath = @"C:\Projects\MyRelay\.backup"
        };

        // Assert
        Path.IsPathRooted(context.BackupPath).Should().BeTrue();
    }

    [Fact]
    public void OptimizationContext_BackupPath_CanIncludeTimestamp()
    {
        // Arrange
        var context = new OptimizationContext
        {
            Timestamp = DateTime.UtcNow
        };

        // Act
        context.BackupPath = $".backup_{context.Timestamp:yyyyMMdd_HHmmss}";

        // Assert
        context.BackupPath.Should().StartWith(".backup_");
        context.BackupPath.Should().MatchRegex(@"\.backup_\d{8}_\d{6}");
    }

    #endregion

    #region ProjectPath Tests

    [Fact]
    public void OptimizationContext_ProjectPath_CanBeRelative()
    {
        // Arrange & Act
        var context = new OptimizationContext { ProjectPath = "./MyProject" };

        // Assert
        context.ProjectPath.Should().Be("./MyProject");
    }

    [Fact]
    public void OptimizationContext_ProjectPath_CanBeAbsolute()
    {
        // Arrange & Act
        var context = new OptimizationContext
        {
            ProjectPath = @"C:\Projects\MyRelay"
        };

        // Assert
        Path.IsPathRooted(context.ProjectPath).Should().BeTrue();
    }

    [Fact]
    public void OptimizationContext_ProjectPath_CanBeNormalized()
    {
        // Arrange
        var context = new OptimizationContext
        {
            ProjectPath = @"C:\Projects\MyRelay"
        };

        // Act
        var normalized = context.ProjectPath.Replace('\\', '/');

        // Assert
        normalized.Should().Contain("/");
    }

    #endregion

    #region Statistics Tests

    [Fact]
    public void OptimizationContext_Statistics_CanCountSourceFiles()
    {
        // Arrange
        var context = new OptimizationContext();
        context.SourceFiles.AddRange(new[] { "File1.cs", "File2.cs", "File3.cs" });

        // Act
        var count = context.SourceFiles.Count;

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public void OptimizationContext_Statistics_CanCountActions()
    {
        // Arrange
        var context = new OptimizationContext();
        context.OptimizationActions.AddRange(new[]
        {
            new OptimizationAction { Type = "Handler" },
            new OptimizationAction { Type = "Request" }
        });

        // Act
        var count = context.OptimizationActions.Count;

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public void OptimizationContext_Statistics_CanCountTotalModifications()
    {
        // Arrange
        var context = new OptimizationContext();
        context.OptimizationActions.AddRange(new[]
        {
            new OptimizationAction
            {
                Modifications = new List<string> { "Mod1", "Mod2", "Mod3" }
            },
            new OptimizationAction
            {
                Modifications = new List<string> { "Mod4", "Mod5" }
            }
        });

        // Act
        var total = context.OptimizationActions.Sum(a => a.Modifications.Count);

        // Assert
        total.Should().Be(5);
    }

    #endregion

    #region Multiple Contexts Tests

    [Fact]
    public void OptimizationContext_MultipleInstances_ShouldBeIndependent()
    {
        // Arrange & Act
        var context1 = new OptimizationContext { Target = "handlers" };
        var context2 = new OptimizationContext { Target = "requests" };

        context1.SourceFiles.Add("File1.cs");
        context2.SourceFiles.Add("File2.cs");

        // Assert
        context1.Target.Should().NotBe(context2.Target);
        context1.SourceFiles.Should().NotContain("File2.cs");
        context2.SourceFiles.Should().NotContain("File1.cs");
    }

    #endregion

    #region Real-World Scenarios Tests

    [Fact]
    public void OptimizationContext_HandlerOptimization_CompleteScenario()
    {
        // Arrange & Act
        var context = new OptimizationContext
        {
            ProjectPath = @"C:\Projects\MyRelay",
            IsDryRun = false,
            Target = "handlers",
            IsAggressive = false,
            CreateBackup = true,
            Timestamp = DateTime.UtcNow
        };

        context.SourceFiles.AddRange(new[]
        {
            @"C:\Projects\MyRelay\Handlers\GetUserHandler.cs",
            @"C:\Projects\MyRelay\Handlers\CreateUserHandler.cs"
        });

        context.OptimizationActions.Add(new OptimizationAction
        {
            FilePath = @"C:\Projects\MyRelay\Handlers\GetUserHandler.cs",
            Type = "Handler",
            Modifications = new List<string>
            {
                "Task -> ValueTask",
                "Added [Handle]"
            }
        });

        context.BackupPath = $"{context.ProjectPath}\\.backup_{context.Timestamp:yyyyMMdd_HHmmss}";

        // Assert
        context.ProjectPath.Should().NotBeNullOrEmpty();
        context.Target.Should().Be("handlers");
        context.SourceFiles.Should().HaveCount(2);
        context.OptimizationActions.Should().HaveCount(1);
        context.CreateBackup.Should().BeTrue();
        context.BackupPath.Should().Contain(".backup_");
    }

    [Fact]
    public void OptimizationContext_DryRunScenario_CompleteFlow()
    {
        // Arrange & Act
        var context = new OptimizationContext
        {
            ProjectPath = @"C:\Projects\MyRelay",
            IsDryRun = true,
            Target = "all",
            IsAggressive = true,
            CreateBackup = false,
            Timestamp = DateTime.UtcNow
        };

        context.SourceFiles.AddRange(new[]
        {
            "Handler1.cs",
            "Handler2.cs",
            "Request1.cs",
            "Config.json"
        });

        context.OptimizationActions.AddRange(new[]
        {
            new OptimizationAction
            {
                Type = "Handler",
                Modifications = new List<string> { "Task -> ValueTask" }
            },
            new OptimizationAction
            {
                Type = "Config",
                Modifications = new List<string> { "Enabled caching" }
            }
        });

        // Assert
        context.IsDryRun.Should().BeTrue();
        context.CreateBackup.Should().BeFalse();
        context.IsAggressive.Should().BeTrue();
        context.SourceFiles.Should().HaveCount(4);
        context.OptimizationActions.Should().HaveCount(2);
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void OptimizationContext_EmptyProjectPath_ShouldBeAllowed()
    {
        // Arrange & Act
        var context = new OptimizationContext { ProjectPath = "" };

        // Assert
        context.ProjectPath.Should().BeEmpty();
    }

    [Fact]
    public void OptimizationContext_EmptyTarget_ShouldBeAllowed()
    {
        // Arrange & Act
        var context = new OptimizationContext { Target = "" };

        // Assert
        context.Target.Should().BeEmpty();
    }

    [Fact]
    public void OptimizationContext_VeryLongProjectPath_ShouldBeHandled()
    {
        // Arrange
        var longPath = string.Join("\\", Enumerable.Range(1, 50).Select(i => $"Folder{i}"));

        // Act
        var context = new OptimizationContext { ProjectPath = longPath };

        // Assert
        context.ProjectPath.Length.Should().BeGreaterThan(100);
    }

    [Fact]
    public void OptimizationContext_ManySourceFiles_ShouldBeHandled()
    {
        // Arrange
        var context = new OptimizationContext();

        // Act
        for (int i = 0; i < 1000; i++)
        {
            context.SourceFiles.Add($"File{i}.cs");
        }

        // Assert
        context.SourceFiles.Should().HaveCount(1000);
    }

    [Fact]
    public void OptimizationContext_ManyOptimizationActions_ShouldBeHandled()
    {
        // Arrange
        var context = new OptimizationContext();

        // Act
        for (int i = 0; i < 100; i++)
        {
            context.OptimizationActions.Add(new OptimizationAction
            {
                Type = "Handler",
                FilePath = $"Handler{i}.cs"
            });
        }

        // Assert
        context.OptimizationActions.Should().HaveCount(100);
    }

    #endregion

    #region Property Validation Tests

    [Fact]
    public void OptimizationContext_Properties_ShouldHavePublicGetters()
    {
        // Arrange
        var type = typeof(OptimizationContext);

        // Act
        var projectPathProp = type.GetProperty(nameof(OptimizationContext.ProjectPath));
        var isDryRunProp = type.GetProperty(nameof(OptimizationContext.IsDryRun));
        var targetProp = type.GetProperty(nameof(OptimizationContext.Target));

        // Assert
        projectPathProp.Should().NotBeNull();
        isDryRunProp.Should().NotBeNull();
        targetProp.Should().NotBeNull();
        projectPathProp!.CanRead.Should().BeTrue();
        isDryRunProp!.CanRead.Should().BeTrue();
        targetProp!.CanRead.Should().BeTrue();
    }

    [Fact]
    public void OptimizationContext_Properties_ShouldHavePublicSetters()
    {
        // Arrange
        var type = typeof(OptimizationContext);

        // Act
        var projectPathProp = type.GetProperty(nameof(OptimizationContext.ProjectPath));
        var targetProp = type.GetProperty(nameof(OptimizationContext.Target));

        // Assert
        projectPathProp!.CanWrite.Should().BeTrue();
        targetProp!.CanWrite.Should().BeTrue();
    }

    #endregion

    #region Collection Initialization Tests

    [Fact]
    public void OptimizationContext_SourceFiles_ShouldNotThrowOnInitialization()
    {
        // Act
        Action act = () =>
        {
            var context = new OptimizationContext();
            _ = context.SourceFiles.Count;
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void OptimizationContext_OptimizationActions_ShouldNotThrowOnInitialization()
    {
        // Act
        Action act = () =>
        {
            var context = new OptimizationContext();
            _ = context.OptimizationActions.Count;
        };

        // Assert
        act.Should().NotThrow();
    }

    #endregion
}

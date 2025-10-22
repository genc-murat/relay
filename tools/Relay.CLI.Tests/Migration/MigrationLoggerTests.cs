using Microsoft.Extensions.Logging;
using Relay.CLI.Migration;

namespace Relay.CLI.Tests.Migration;

#pragma warning disable CS8600, CS8625
public class MigrationLoggerTests
{
    private readonly Mock<ILogger<MigrationLogger>> _loggerMock;
    private readonly MigrationLogger _migrationLogger;

    public MigrationLoggerTests()
    {
        _loggerMock = new Mock<ILogger<MigrationLogger>>();
        _migrationLogger = new MigrationLogger(_loggerMock.Object);
    }

    [Fact]
    public void LogMigrationStarted_ShouldLogInformationWithCorrectMessage()
    {
        // Arrange
        var options = new MigrationOptions
        {
            SourceFramework = "MediatR",
            TargetFramework = "Relay",
            ProjectPath = "/path/to/project"
        };

        // Act
        _migrationLogger.LogMigrationStarted(options);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Migration started: MediatR -> Relay at /path/to/project")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void LogFileTransformed_ShouldLogDebugWithCorrectMessage()
    {
        // Arrange
        var filePath = "TestHandler.cs";
        var changes = 5;

        // Act
        _migrationLogger.LogFileTransformed(filePath, changes);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Transformed file: TestHandler.cs with 5 changes")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void LogMigrationCompleted_ShouldLogInformationWithCorrectMessage()
    {
        // Arrange
        var result = new MigrationResult
        {
            Status = MigrationStatus.Success,
            FilesModified = 10,
            Duration = TimeSpan.FromMinutes(2)
        };

        // Act
        _migrationLogger.LogMigrationCompleted(result);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Migration completed: Status=Success, Files=10")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void LogSyntaxError_ShouldLogWarningWithException()
    {
        // Arrange
        var exception = new SyntaxException("Invalid syntax", "Test.cs", 42);
        var file = "Test.cs";

        // Act
        _migrationLogger.LogSyntaxError(exception, file);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Syntax error in file: Test.cs at line 42")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void LogSyntaxError_WithNullLineNumber_ShouldLogNegativeOne()
    {
        // Arrange
        var exception = new SyntaxException("Invalid syntax", "Test.cs", null);
        var file = "Test.cs";

        // Act
        _migrationLogger.LogSyntaxError(exception, file);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Syntax error in file: Test.cs at line -1")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void LogTransformationError_ShouldLogErrorWithException()
    {
        // Arrange
        var exception = new Exception("Transformation failed");
        var file = "Handler.cs";

        // Act
        _migrationLogger.LogTransformationError(exception, file);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Unexpected error processing file: Handler.cs")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void LogMigrationFailed_ShouldLogErrorWithException()
    {
        // Arrange
        var exception = new MigrationException("Migration failed", "Handler.cs", MigrationStage.TransformingCode, new Exception());

        // Act
        _migrationLogger.LogMigrationFailed(exception);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Migration failed at stage TransformingCode for file Handler.cs")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void LogMigrationFailed_WithNullFilePath_ShouldLogUnknown()
    {
        // Arrange
        var exception = new MigrationException("Migration failed", MigrationStage.TransformingCode, (Exception)null);

        // Act
        _migrationLogger.LogMigrationFailed(exception);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Migration failed at stage TransformingCode for file unknown")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void LogPackageTransformed_ShouldLogDebugWithCorrectMessage()
    {
        // Arrange
        var packageName = "MediatR";
        var action = "updated";
        var projectFile = "Test.csproj";

        // Act
        _migrationLogger.LogPackageTransformed(packageName, action, projectFile);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Package transformation: updated MediatR in Test.csproj")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void LogBackupCreated_ShouldLogInformationWithCorrectMessage()
    {
        // Arrange
        var backupPath = "/backup/project.zip";

        // Act
        _migrationLogger.LogBackupCreated(backupPath);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Backup created at: /backup/project.zip")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void LogAnalysisCompleted_ShouldLogInformationWithCorrectMessage()
    {
        // Arrange
        var handlersFound = 15;
        var issuesFound = 3;
        var canMigrate = true;

        // Act
        _migrationLogger.LogAnalysisCompleted(handlersFound, issuesFound, canMigrate);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Analysis completed: Handlers=15, Issues=3, CanMigrate=True")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }

    [Fact]
    public void Constructor_ShouldAcceptLogger()
    {
        // Arrange & Act
        var logger = new Mock<ILogger<MigrationLogger>>();
        var migrationLogger = new MigrationLogger(logger.Object);

        // Assert
        Assert.NotNull(migrationLogger);
    }
}
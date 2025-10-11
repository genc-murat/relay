using Microsoft.Extensions.Logging;

namespace Relay.CLI.Migration;

/// <summary>
/// Provides structured logging for migration operations
/// </summary>
public class MigrationLogger
{
    private readonly ILogger<MigrationLogger> _logger;

    public MigrationLogger(ILogger<MigrationLogger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs the start of a migration operation
    /// </summary>
    public void LogMigrationStarted(MigrationOptions options)
    {
        _logger.LogInformation(
            "Migration started: {Framework} -> {Target} at {Path}",
            options.SourceFramework,
            options.TargetFramework,
            options.ProjectPath
        );
    }

    /// <summary>
    /// Logs a file transformation
    /// </summary>
    public void LogFileTransformed(string filePath, int changes)
    {
        _logger.LogDebug(
            "Transformed file: {FilePath} with {Changes} changes",
            filePath,
            changes
        );
    }

    /// <summary>
    /// Logs migration completion
    /// </summary>
    public void LogMigrationCompleted(MigrationResult result)
    {
        _logger.LogInformation(
            "Migration completed: Status={Status}, Files={Files}, Duration={Duration}",
            result.Status,
            result.FilesModified,
            result.Duration
        );
    }

    /// <summary>
    /// Logs a syntax error during file transformation
    /// </summary>
    public void LogSyntaxError(SyntaxException ex, string file)
    {
        _logger.LogWarning(
            ex,
            "Syntax error in file: {File} at line {Line}",
            file,
            ex.LineNumber ?? -1
        );
    }

    /// <summary>
    /// Logs an unexpected error during file transformation
    /// </summary>
    public void LogTransformationError(Exception ex, string file)
    {
        _logger.LogError(
            ex,
            "Unexpected error processing file: {File}",
            file
        );
    }

    /// <summary>
    /// Logs a critical migration failure
    /// </summary>
    public void LogMigrationFailed(MigrationException ex)
    {
        _logger.LogError(
            ex,
            "Migration failed at stage {Stage} for file {File}",
            ex.Stage,
            ex.FilePath ?? "unknown"
        );
    }

    /// <summary>
    /// Logs package transformation
    /// </summary>
    public void LogPackageTransformed(string packageName, string action, string projectFile)
    {
        _logger.LogDebug(
            "Package transformation: {Action} {Package} in {ProjectFile}",
            action,
            packageName,
            projectFile
        );
    }

    /// <summary>
    /// Logs backup creation
    /// </summary>
    public void LogBackupCreated(string backupPath)
    {
        _logger.LogInformation(
            "Backup created at: {BackupPath}",
            backupPath
        );
    }

    /// <summary>
    /// Logs analysis results
    /// </summary>
    public void LogAnalysisCompleted(int handlersFound, int issuesFound, bool canMigrate)
    {
        _logger.LogInformation(
            "Analysis completed: Handlers={Handlers}, Issues={Issues}, CanMigrate={CanMigrate}",
            handlersFound,
            issuesFound,
            canMigrate
        );
    }
}

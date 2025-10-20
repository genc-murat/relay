namespace Relay.CLI.Migration;

/// <summary>
/// Exception thrown when migration operations fail
/// </summary>
public class MigrationException : Exception
{
    /// <summary>
    /// Gets the file path where the error occurred
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// Gets the migration stage where the error occurred
    /// </summary>
    public MigrationStage? Stage { get; }

    public MigrationException(string message) : base(message)
    {
    }

    public MigrationException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }

    public MigrationException(string message, string filePath) 
        : base(message)
    {
        FilePath = filePath;
    }

    public MigrationException(string message, string filePath, Exception innerException) 
        : base(message, innerException)
    {
        FilePath = filePath;
    }

    public MigrationException(string message, MigrationStage stage, Exception innerException) 
        : base(message, innerException)
    {
        Stage = stage;
    }

    public MigrationException(string message, string filePath, MigrationStage stage, Exception innerException) 
        : base(message, innerException)
    {
        FilePath = filePath;
        Stage = stage;
    }
}

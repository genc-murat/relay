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

/// <summary>
/// Exception thrown when syntax errors occur during code transformation
/// </summary>
public class SyntaxException : MigrationException
{
    /// <summary>
    /// Gets the line number where the syntax error occurred
    /// </summary>
    public int? LineNumber { get; }

    public SyntaxException(string message) : base(message)
    {
    }

    public SyntaxException(string message, string filePath, int lineNumber) 
        : base(message, filePath)
    {
        LineNumber = lineNumber;
    }

    public SyntaxException(string message, string filePath, Exception innerException) 
        : base(message, filePath, innerException)
    {
    }
}

namespace Relay.CLI.Migration;

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

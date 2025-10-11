namespace Relay.CLI.Migration;

/// <summary>
/// Individual change applied during migration
/// </summary>
public class MigrationChange
{
    /// <summary>
    /// Category of the change (e.g., "Using Directives", "Package References")
    /// </summary>
    public string Category { get; set; } = "";

    /// <summary>
    /// Type of change (Add, Remove, Modify)
    /// </summary>
    public ChangeType Type { get; set; }

    /// <summary>
    /// Human-readable description of the change
    /// </summary>
    public string Description { get; set; } = "";

    /// <summary>
    /// File path where the change was made
    /// </summary>
    public string FilePath { get; set; } = "";
}

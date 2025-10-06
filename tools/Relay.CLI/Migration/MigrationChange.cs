namespace Relay.CLI.Migration;

/// <summary>
/// Individual change applied during migration
/// </summary>
public class MigrationChange
{
    public string Category { get; set; } = "";
    public ChangeType Type { get; set; }
    public string Description { get; set; } = "";
    public string FilePath { get; set; } = "";
}

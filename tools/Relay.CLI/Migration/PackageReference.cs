namespace Relay.CLI.Migration;

/// <summary>
/// Package reference information
/// </summary>
public class PackageReference
{
    public string Name { get; set; } = "";
    public string CurrentVersion { get; set; } = "";
    public string ProjectFile { get; set; } = "";
}

namespace Relay.CLI.Commands;

public class OptimizationContext
{
    public string ProjectPath { get; set; } = "";
    public bool IsDryRun { get; set; }
    public string Target { get; set; } = "";
    public bool IsAggressive { get; set; }
    public bool CreateBackup { get; set; }
    public DateTime Timestamp { get; set; }
    public string BackupPath { get; set; } = "";
    public List<string> SourceFiles { get; set; } = new();
    public List<OptimizationAction> OptimizationActions { get; set; } = new();
}

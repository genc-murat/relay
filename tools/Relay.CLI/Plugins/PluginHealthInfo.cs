namespace Relay.CLI.Plugins;

/// <summary>
/// Information about plugin health
/// </summary>
public class PluginHealthInfo
{
    public string PluginName { get; }
    public PluginHealthStatus Status { get; set; } = PluginHealthStatus.Unknown;
    public int SuccessCount { get; private set; } = 0;
    public int FailureCount { get; set; } = 0; // Changed to public set to allow modification
    public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
    public DateTime LastSuccessTime { get; set; } = DateTime.MinValue;
    public DateTime LastFailureTime { get; set; } = DateTime.MinValue;
    public string? LastErrorMessage { get; private set; }

    public PluginHealthInfo(string pluginName)
    {
        PluginName = pluginName;
    }

    public void RecordSuccess()
    {
        SuccessCount++;
        LastActivityTime = DateTime.UtcNow;
        LastSuccessTime = DateTime.UtcNow;
        Status = PluginHealthStatus.Healthy;
    }

    public void RecordFailure(string? errorMessage)
    {
        FailureCount++;
        LastActivityTime = DateTime.UtcNow;
        LastFailureTime = DateTime.UtcNow;
        LastErrorMessage = errorMessage;
        
        if (Status != PluginHealthStatus.Disabled)
        {
            Status = PluginHealthStatus.Unhealthy;
        }
    }

    public void Reset()
    {
        SuccessCount = 0;
        FailureCount = 0;
        LastActivityTime = DateTime.UtcNow;
        LastSuccessTime = DateTime.MinValue;
        LastFailureTime = DateTime.MinValue;
        LastErrorMessage = null;
        Status = PluginHealthStatus.Healthy;
    }
}

namespace Relay.CLI.Plugins;

/// <summary>
/// Simple plugin logger implementation
/// </summary>
public class PluginLogger : IPluginLogger
{
    private readonly string _pluginName;

    public PluginLogger(string pluginName)
    {
        _pluginName = pluginName;
    }

    public void LogTrace(string message) => Log("TRACE", message);
    public void LogDebug(string message) => Log("DEBUG", message);
    public void LogInformation(string message) => Log("INFO", message);
    public void LogWarning(string message) => Log("WARN", message);
    public void LogError(string message, Exception? exception = null)
    {
        Log("ERROR", message);
        if (exception != null)
            Console.WriteLine($"  Exception: {exception.Message}");
    }
    public void LogCritical(string message, Exception? exception = null)
    {
        Log("CRITICAL", message);
        if (exception != null)
            Console.WriteLine($"  Exception: {exception.Message}");
    }

    private void Log(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        Console.WriteLine($"[{timestamp}] [{level}] [{_pluginName}] {message}");
    }
}

namespace Relay.CLI.Plugins;

/// <summary>
/// Logger interface for plugins
/// </summary>
public interface IPluginLogger
{
    void LogTrace(string message);
    void LogDebug(string message);
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message, Exception? exception = null);
    void LogCritical(string message, Exception? exception = null);
}

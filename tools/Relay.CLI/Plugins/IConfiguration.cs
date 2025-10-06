namespace Relay.CLI.Plugins;

/// <summary>
/// Configuration interface for plugins
/// </summary>
public interface IConfiguration
{
    string? this[string key] { get; set; }
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value);
    Task<bool> ContainsKeyAsync(string key);
    Task RemoveAsync(string key);
}

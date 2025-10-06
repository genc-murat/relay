namespace Relay.CLI.Plugins;

/// <summary>
/// Simple configuration implementation
/// </summary>
public class PluginConfiguration : IConfiguration
{
    private readonly Dictionary<string, string> _settings = new();

    public string? this[string key]
    {
        get => _settings.TryGetValue(key, out var value) ? value : null;
        set
        {
            if (value != null)
                _settings[key] = value;
            else
                _settings.Remove(key);
        }
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (!_settings.TryGetValue(key, out var value))
            return Task.FromResult<T?>(default);

        try
        {
            var converted = (T?)Convert.ChangeType(value, typeof(T));
            return Task.FromResult(converted);
        }
        catch
        {
            return Task.FromResult<T?>(default);
        }
    }

    public Task SetAsync<T>(string key, T value)
    {
        if (value != null)
            _settings[key] = value.ToString() ?? "";
        return Task.CompletedTask;
    }

    public Task<bool> ContainsKeyAsync(string key) => Task.FromResult(_settings.ContainsKey(key));

    public Task RemoveAsync(string key)
    {
        _settings.Remove(key);
        return Task.CompletedTask;
    }
}

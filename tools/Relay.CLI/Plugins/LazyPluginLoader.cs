using System.Collections.Concurrent;

namespace Relay.CLI.Plugins;

/// <summary>
/// Implements lazy loading for plugins to improve startup time and resource usage
/// </summary>
public class LazyPluginLoader
{
    private readonly PluginManager _pluginManager;
    private readonly IPluginLogger _logger;
    private readonly ConcurrentDictionary<string, Lazy<Task<IRelayPlugin?>>> _lazyPlugins;
    private readonly TimeSpan _cacheTimeout;
    
    public LazyPluginLoader(PluginManager pluginManager, IPluginLogger logger, TimeSpan? cacheTimeout = null)
    {
        _pluginManager = pluginManager;
        _logger = logger;
        _lazyPlugins = new ConcurrentDictionary<string, Lazy<Task<IRelayPlugin?>>>();
        _cacheTimeout = cacheTimeout ?? TimeSpan.FromMinutes(30); // Default 30 minute cache
    }

    /// <summary>
    /// Gets a plugin instance, loading it lazily if not already loaded
    /// </summary>
    /// <param name="pluginName">Name of the plugin to get</param>
    /// <param name="context">Plugin context</param>
    /// <returns>The plugin instance or null if not found/loaded</returns>
    public async Task<IRelayPlugin?> GetPluginAsync(string pluginName, IPluginContext context)
    {
        try
        {
            var lazyPlugin = _lazyPlugins.GetOrAdd(pluginName, name => 
                new Lazy<Task<IRelayPlugin?>>(() => _pluginManager.LoadPluginAsync(name, context)));
            
            var plugin = await lazyPlugin.Value;
            return plugin;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading plugin {pluginName} lazily: {ex.Message}", ex);
            return null;
        }
    }

    /// <summary>
    /// Preloads a plugin without waiting for it to be used
    /// </summary>
    /// <param name="pluginName">Name of the plugin to preload</param>
    /// <param name="context">Plugin context</param>
    public void PreloadPlugin(string pluginName, IPluginContext context)
    {
        _ = _lazyPlugins.GetOrAdd(pluginName, name => 
            new Lazy<Task<IRelayPlugin?>>(() => _pluginManager.LoadPluginAsync(name, context)));
    }

    /// <summary>
    /// Clears the lazy loading cache
    /// </summary>
    public void ClearCache()
    {
        _lazyPlugins.Clear();
    }

    /// <summary>
    /// Removes a specific plugin from the lazy loading cache
    /// </summary>
    /// <param name="pluginName">Name of the plugin to remove</param>
    public bool RemoveFromCache(string pluginName)
    {
        return _lazyPlugins.TryRemove(pluginName, out _);
    }

    /// <summary>
    /// Gets information about currently cached plugins
    /// </summary>
    /// <returns>Dictionary with plugin names and their cached state</returns>
    public Dictionary<string, bool> GetCacheInfo()
    {
        var info = new Dictionary<string, bool>();
        foreach (var kvp in _lazyPlugins)
        {
            info[kvp.Key] = kvp.Value.IsValueCreated;
        }
        return info;
    }

    /// <summary>
    /// Executes a plugin with lazy loading
    /// </summary>
    /// <param name="pluginName">Name of the plugin to execute</param>
    /// <param name="args">Arguments to pass to the plugin</param>
    /// <param name="context">Plugin context</param>
    /// <param name="timeout">Execution timeout</param>
    /// <returns>Exit code from plugin execution</returns>
    public async Task<int> ExecutePluginAsync(string pluginName, string[] args, IPluginContext context, TimeSpan? timeout = null)
    {
        var plugin = await GetPluginAsync(pluginName, context);
        if (plugin == null)
        {
            _logger.LogError($"Could not load plugin {pluginName} for execution");
            return -1;
        }

        return await _pluginManager.ExecutePluginAsync(pluginName, args, context, timeout);
    }
}
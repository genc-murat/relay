using System.Collections.Concurrent;

namespace Relay.CLI.Plugins;

/// <summary>
/// Monitors plugin health and handles failures
/// </summary>
public class PluginHealthMonitor : IDisposable
{
    private readonly IPluginLogger _logger;
    private readonly ConcurrentDictionary<string, PluginHealthInfo> _pluginHealth;
    private readonly Timer? _healthCheckTimer;
    private readonly TimeSpan _healthCheckInterval = TimeSpan.FromMinutes(1);
    private readonly Dictionary<string, int> _restartCount;
    private readonly Dictionary<string, DateTime> _restartCooldown;
    private readonly TimeSpan _maxRestartAttempts = TimeSpan.FromMinutes(5);
    private bool _disposed = false;

    public PluginHealthMonitor(IPluginLogger logger)
    {
        _logger = logger;
        _pluginHealth = new ConcurrentDictionary<string, PluginHealthInfo>();
        _restartCount = new Dictionary<string, int>();
        _restartCooldown = new Dictionary<string, DateTime>();
        
        // Start health check timer
        _healthCheckTimer = new Timer(CheckHealth, null, _healthCheckInterval, _healthCheckInterval);
    }

    /// <summary>
    /// Records a successful plugin operation
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    public void RecordSuccess(string pluginName)
    {
        if (_disposed) return;

        var healthInfo = _pluginHealth.GetOrAdd(pluginName, name => new PluginHealthInfo(name));
        healthInfo.RecordSuccess();
        
        _logger.LogDebug($"Recorded success for plugin: {pluginName}");
    }

    /// <summary>
    /// Records a failed plugin operation
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    /// <param name="error">Error that occurred</param>
    public void RecordFailure(string pluginName, Exception? error = null)
    {
        if (_disposed) return;

        var healthInfo = _pluginHealth.GetOrAdd(pluginName, name => new PluginHealthInfo(name));
        healthInfo.RecordFailure(error?.Message ?? "Unknown error");
        
        _logger.LogWarning($"Recorded failure for plugin: {pluginName}");
        
        // Check if plugin should be disabled
        // Disable if there are more than 3 failures and the first failure happened less than 5 minutes ago
        if (healthInfo.FailureCount > 3)
        {
            _logger.LogError($"Plugin {pluginName} has failed multiple times and will be temporarily disabled");
            healthInfo.Status = PluginHealthStatus.Disabled;
            
            // Track restart attempts to prevent infinite restart loops
            if (!_restartCount.ContainsKey(pluginName))
            {
                _restartCount[pluginName] = 0;
            }
            _restartCount[pluginName]++;
            
            // Check if we should stop trying to restart
            if (_restartCount[pluginName] > 5) // Max 5 restart attempts
            {
                _logger.LogError($"Plugin {pluginName} has exceeded maximum restart attempts and will remain disabled");
                return;
            }
        }
    }

    /// <summary>
    /// Attempts to restart a failed plugin
    /// </summary>
    /// <param name="pluginName">Name of the plugin to restart</param>
    /// <returns>True if restart was initiated, false otherwise</returns>
    public bool AttemptRestart(string pluginName)
    {
        if (_disposed) return false;

        if (_pluginHealth.TryGetValue(pluginName, out var healthInfo))
        {
            // Check if we're in a restart cooldown period
            if (_restartCooldown.ContainsKey(pluginName) && 
                DateTime.UtcNow - _restartCooldown[pluginName] < TimeSpan.FromMinutes(1))
            {
                _logger.LogWarning($"Plugin {pluginName} is still in restart cooldown period");
                return false;
            }

            // Reset the plugin's health status to allow restart
            if (healthInfo.Status == PluginHealthStatus.Disabled)
            {
                healthInfo.Status = PluginHealthStatus.Unknown;
                healthInfo.LastFailureTime = DateTime.MinValue; // Reset failure time
                
                // Update restart cooldown to prevent rapid restarts
                _restartCooldown[pluginName] = DateTime.UtcNow;
                
                _logger.LogInformation($"Initiating restart for plugin: {pluginName}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resets restart count for a plugin after successful operation
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    public void ResetRestartCount(string pluginName)
    {
        if (_restartCount.ContainsKey(pluginName))
        {
            _restartCount[pluginName] = 0;
        }
        
        if (_restartCooldown.ContainsKey(pluginName))
        {
            _restartCooldown.Remove(pluginName);
        }
    }

    /// <summary>
    /// Checks if a plugin is healthy and can be executed
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    /// <returns>True if healthy, false otherwise</returns>
    public bool IsHealthy(string pluginName)
    {
        if (_disposed) return false;

        if (_pluginHealth.TryGetValue(pluginName, out var healthInfo))
        {
            // Check if plugin is disabled
            if (healthInfo.Status == PluginHealthStatus.Disabled)
            {
                // Check if it's time to re-enable (after cooldown period)
                if (DateTime.UtcNow - healthInfo.LastFailureTime > TimeSpan.FromMinutes(10))
                {
                    healthInfo.Status = PluginHealthStatus.Healthy;
                    healthInfo.FailureCount = 0;
                    _logger.LogInformation($"Re-enabling plugin: {pluginName}");
                    return true;
                }
                
                return false;
            }
            
            // Check for timeout since last activity
            if (DateTime.UtcNow - healthInfo.LastActivityTime > TimeSpan.FromMinutes(30))
            {
                healthInfo.Status = PluginHealthStatus.Unknown;
            }
            
            return healthInfo.Status == PluginHealthStatus.Healthy;
        }
        
        // If we don't have health info for this plugin, assume it's healthy
        return true;
    }

    /// <summary>
    /// Gets health information for a specific plugin
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    /// <returns>Health information</returns>
    public PluginHealthInfo? GetHealthInfo(string pluginName)
    {
        if (_disposed) return null;

        _pluginHealth.TryGetValue(pluginName, out var healthInfo);
        return healthInfo;
    }

    /// <summary>
    /// Gets health information for all plugins
    /// </summary>
    /// <returns>Collection of health information</returns>
    public IEnumerable<PluginHealthInfo> GetAllHealthInfo()
    {
        if (_disposed) return new List<PluginHealthInfo>();

        return _pluginHealth.Values.ToList();
    }

    /// <summary>
    /// Resets health information for a plugin
    /// </summary>
    /// <param name="pluginName">Name of the plugin</param>
    public void ResetHealth(string pluginName)
    {
        if (_disposed) return;

        if (_pluginHealth.TryGetValue(pluginName, out var healthInfo))
        {
            healthInfo.Reset();
            _logger.LogDebug($"Reset health info for plugin: {pluginName}");
        }
    }

    /// <summary>
    /// Performs periodic health checks
    /// </summary>
    /// <param name="state">Timer state</param>
    private void CheckHealth(object? state)
    {
        if (_disposed) return;

        try
        {
            var now = DateTime.UtcNow;
            var pluginsToCheck = _pluginHealth.Values.ToList();

            foreach (var healthInfo in pluginsToCheck)
            {
                // Update status based on time since last activity
                var timeSinceActivity = now - healthInfo.LastActivityTime;

                if (timeSinceActivity > TimeSpan.FromMinutes(60))
                {
                    healthInfo.Status = PluginHealthStatus.Unknown;
                }
                else if (timeSinceActivity > TimeSpan.FromMinutes(30))
                {
                    healthInfo.Status = PluginHealthStatus.Degraded;
                }
                else
                {
                    healthInfo.Status = PluginHealthStatus.Healthy;
                }

                // Log health status if it has changed significantly
                if (healthInfo.Status == PluginHealthStatus.Degraded || healthInfo.Status == PluginHealthStatus.Unknown)
                {
                    _logger.LogInformation($"Plugin {healthInfo.PluginName} status is {healthInfo.Status}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during health check: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _healthCheckTimer?.Dispose();
            _disposed = true;
        }
    }
}

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

/// <summary>
/// Plugin health status
/// </summary>
public enum PluginHealthStatus
{
    Unknown,
    Healthy,
    Degraded,
    Unhealthy,
    Disabled
}
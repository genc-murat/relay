using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI;

/// <summary>
/// Provides system load metrics for AI optimization decisions.
/// </summary>
public sealed class SystemLoadMetricsProvider : ISystemLoadMetricsProvider, IDisposable
{
    private readonly ILogger<SystemLoadMetricsProvider> _logger;
    private readonly SystemLoadMetricsOptions _options;
    private readonly MetricsCollector _collector;
    private readonly Timer? _cacheTimer;
    private SystemLoadMetrics? _cachedMetrics;
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly object _cacheLock = new object();

    public SystemLoadMetricsProvider(
        ILogger<SystemLoadMetricsProvider> logger,
        SystemLoadMetricsOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? new SystemLoadMetricsOptions();
        _collector = new MetricsCollector(_logger, _options);

        if (_options.EnableCaching)
        {
            _cacheTimer = new Timer(
                RefreshCacheCallback,
                null,
                _options.CacheRefreshInterval,
                _options.CacheRefreshInterval);
        }
    }

    public async ValueTask<SystemLoadMetrics> GetCurrentLoadAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_options.EnableCaching)
        {
            lock (_cacheLock)
            {
                if (_cachedMetrics != null &&
                    (DateTime.UtcNow - _lastCacheUpdate) < _options.CacheTtl)
                {
                    _logger.LogTrace("Returning cached system load metrics");
                    return _cachedMetrics;
                }
            }
        }

        // Collect fresh metrics
        var metrics = await _collector.CollectMetricsAsync(cancellationToken);

        // Update cache
        if (_options.EnableCaching)
        {
            lock (_cacheLock)
            {
                _cachedMetrics = metrics;
                _lastCacheUpdate = DateTime.UtcNow;
            }
        }

        return metrics;
    }

    private async void RefreshCacheCallback(object? state)
    {
        try
        {
            await GetCurrentLoadAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh cached system load metrics");
        }
    }

    public void Dispose()
    {
        _cacheTimer?.Dispose();
        _collector?.Dispose();
    }
}
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Relay.MessageBroker.Saga.Services;

/// <summary>
/// Background service that monitors and handles saga timeouts.
/// Automatically compensates sagas that have exceeded their timeout duration.
/// </summary>
public class SagaTimeoutService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SagaTimeoutService> _logger;
    private readonly TimeSpan _checkInterval;
    private readonly TimeSpan _defaultTimeout;

    /// <summary>
    /// Initializes a new instance of the <see cref="SagaTimeoutService"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for creating scopes.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="checkInterval">Interval between timeout checks (default: 30 seconds).</param>
    /// <param name="defaultTimeout">Default timeout for sagas without explicit timeout (default: 5 minutes).</param>
    public SagaTimeoutService(
        IServiceProvider serviceProvider,
        ILogger<SagaTimeoutService> logger,
        TimeSpan? checkInterval = null,
        TimeSpan? defaultTimeout = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkInterval = checkInterval ?? TimeSpan.FromSeconds(30);
        _defaultTimeout = defaultTimeout ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Saga Timeout Service started. Check interval: {CheckInterval}, Default timeout: {DefaultTimeout}",
            _checkInterval,
            _defaultTimeout);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndHandleTimeoutsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking saga timeouts");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown during delay
                break;
            }
        }

        _logger.LogInformation("Saga Timeout Service stopped");
    }

    /// <summary>
    /// Checks for timed-out sagas and triggers compensation.
    /// </summary>
    private async Task<SagaTimeoutCheckResult> CheckAndHandleTimeoutsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var timeoutHandlers = scope.ServiceProvider.GetServices<ISagaTimeoutHandler>();

        var timedOutCount = 0;
        var checkedCount = 0;

        foreach (var handler in timeoutHandlers)
        {
            try
            {
                var result = await handler.CheckAndHandleTimeoutsAsync(_defaultTimeout, cancellationToken);
                checkedCount += result.CheckedCount;
                timedOutCount += result.TimedOutCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in timeout handler {HandlerType}", handler.GetType().Name);
            }
        }

        if (timedOutCount > 0)
        {
            _logger.LogWarning(
                "Found {TimedOutCount} timed-out sagas out of {CheckedCount} checked sagas",
                timedOutCount,
                checkedCount);
        }
        else if (checkedCount > 0)
        {
            _logger.LogDebug("Checked {CheckedCount} sagas, no timeouts found", checkedCount);
        }

        return new SagaTimeoutCheckResult
        {
            CheckedCount = checkedCount,
            TimedOutCount = timedOutCount
        };
    }
}

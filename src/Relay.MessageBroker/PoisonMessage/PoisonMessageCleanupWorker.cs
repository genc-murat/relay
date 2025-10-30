using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Relay.MessageBroker.PoisonMessage;

/// <summary>
/// Background service that periodically cleans up expired poison messages.
/// </summary>
public sealed class PoisonMessageCleanupWorker : BackgroundService
{
    private readonly IPoisonMessageHandler _handler;
    private readonly PoisonMessageOptions _options;
    private readonly ILogger<PoisonMessageCleanupWorker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoisonMessageCleanupWorker"/> class.
    /// </summary>
    /// <param name="handler">The poison message handler.</param>
    /// <param name="options">The poison message options.</param>
    /// <param name="logger">The logger.</param>
    public PoisonMessageCleanupWorker(
        IPoisonMessageHandler handler,
        IOptions<PoisonMessageOptions> options,
        ILogger<PoisonMessageCleanupWorker> logger)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Poison message cleanup worker is disabled");
            return;
        }

        _logger.LogInformation(
            "Poison message cleanup worker started. Cleanup interval: {CleanupInterval}, Retention period: {RetentionPeriod}",
            _options.CleanupInterval,
            _options.RetentionPeriod);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.CleanupInterval, stoppingToken);

                _logger.LogDebug("Starting poison message cleanup");

                var removedCount = await _handler.CleanupExpiredAsync(stoppingToken);

                _logger.LogDebug("Poison message cleanup completed. Removed {Count} messages", removedCount);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Poison message cleanup worker is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during poison message cleanup");
            }
        }

        _logger.LogInformation("Poison message cleanup worker stopped");
    }
}

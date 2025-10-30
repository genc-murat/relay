using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Relay.MessageBroker.Inbox;

/// <summary>
/// Background service that periodically cleans up expired inbox entries.
/// </summary>
public sealed class InboxCleanupWorker : BackgroundService
{
    private readonly IInboxStore _store;
    private readonly InboxOptions _options;
    private readonly ILogger<InboxCleanupWorker> _logger;
    private long _totalEntriesRemoved;
    private long _totalCleanupOperations;
    private long _totalCleanupDurationMs;

    /// <summary>
    /// Initializes a new instance of the <see cref="InboxCleanupWorker"/> class.
    /// </summary>
    /// <param name="store">The inbox store.</param>
    /// <param name="options">The inbox options.</param>
    /// <param name="logger">The logger.</param>
    public InboxCleanupWorker(
        IInboxStore store,
        IOptions<InboxOptions> options,
        ILogger<InboxCleanupWorker> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Validate options
        _options.Validate();
    }

    /// <summary>
    /// Gets the total number of entries removed across all cleanup operations.
    /// </summary>
    public long TotalEntriesRemoved => _totalEntriesRemoved;

    /// <summary>
    /// Gets the total number of cleanup operations performed.
    /// </summary>
    public long TotalCleanupOperations => _totalCleanupOperations;

    /// <summary>
    /// Gets the average cleanup duration in milliseconds.
    /// </summary>
    public double AverageCleanupDurationMs =>
        _totalCleanupOperations > 0
            ? (double)_totalCleanupDurationMs / _totalCleanupOperations
            : 0;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Inbox cleanup worker started with cleanup interval: {CleanupInterval}, retention period: {RetentionPeriod}",
            _options.CleanupInterval,
            _options.RetentionPeriod);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error performing inbox cleanup");
            }

            try
            {
                await Task.Delay(_options.CleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                break;
            }
        }

        _logger.LogInformation(
            "Inbox cleanup worker stopped. Total operations: {TotalOperations}, Total entries removed: {TotalRemoved}, Average duration: {AvgDuration}ms",
            _totalCleanupOperations,
            _totalEntriesRemoved,
            AverageCleanupDurationMs);
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var removedCount = await _store.CleanupExpiredAsync(_options.RetentionPeriod, cancellationToken);

            stopwatch.Stop();

            // Update metrics
            Interlocked.Increment(ref _totalCleanupOperations);
            Interlocked.Add(ref _totalEntriesRemoved, removedCount);
            Interlocked.Add(ref _totalCleanupDurationMs, stopwatch.ElapsedMilliseconds);

            if (removedCount > 0)
            {
                _logger.LogInformation(
                    "Inbox cleanup completed: removed {RemovedCount} expired entries in {Duration}ms",
                    removedCount,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogDebug(
                    "Inbox cleanup completed: no expired entries found (duration: {Duration}ms)",
                    stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "Failed to perform inbox cleanup after {Duration}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

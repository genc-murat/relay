using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Relay.MessageBroker;

/// <summary>
/// Hosted service for managing message broker lifecycle.
/// </summary>
public sealed class MessageBrokerHostedService : IHostedService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<MessageBrokerHostedService> _logger;

    public MessageBrokerHostedService(
        IMessageBroker messageBroker,
        ILogger<MessageBrokerHostedService> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting message broker...");

        try
        {
            await _messageBroker.StartAsync(cancellationToken);
            _logger.LogInformation("Message broker started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start message broker");
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping message broker...");

        try
        {
            await _messageBroker.StopAsync(cancellationToken);
            _logger.LogInformation("Message broker stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop message broker");
        }
    }
}

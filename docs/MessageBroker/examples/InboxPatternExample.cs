using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay.MessageBroker;
using Relay.MessageBroker.Inbox;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.MessageBroker.Examples;

/// <summary>
/// Example demonstrating the Inbox Pattern with PostgreSQL for idempotent message processing.
/// The Inbox Pattern ensures that messages are processed exactly once, even if delivered multiple times.
/// </summary>
public class InboxPatternExample
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure PostgreSQL for Inbox
                var connectionString = context.Configuration.GetConnectionString("PostgresConnection");
                services.AddDbContext<InboxDbContext>(options =>
                    options.UseNpgsql(connectionString));

                // Add Inbox Pattern
                services.AddInboxPattern(options =>
                {
                    options.Enabled = true;
                    options.RetentionPeriod = TimeSpan.FromDays(7);
                    options.CleanupInterval = TimeSpan.FromHours(1);
                    options.ConsumerName = "OrderProcessor";
                });

                // Register Inbox store
                services.AddScoped<IInboxStore, SqlInboxStore>();

                // Register Inbox cleanup worker
                services.AddHostedService<InboxCleanupWorker>();

                // Configure RabbitMQ
                services.AddRabbitMQ(options =>
                {
                    options.HostName = "localhost";
                    options.Port = 5672;
                    options.UserName = "guest";
                    options.Password = "guest";
                });

                // Add application services
                services.AddScoped<OrderProcessor>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseNpgsql(connectionString));

                // Add message broker hosted service
                services.AddMessageBrokerHostedService();
            })
            .Build();

        // Run migrations
        using (var scope = host.Services.CreateScope())
        {
            var inboxContext = scope.ServiceProvider.GetRequiredService<InboxDbContext>();
            await inboxContext.Database.MigrateAsync();

            var appContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await appContext.Database.MigrateAsync();
        }

        // Start the host
        await host.StartAsync();

        // Subscribe to messages with inbox pattern
        using (var scope = host.Services.CreateScope())
        {
            var messageBroker = scope.ServiceProvider.GetRequiredService<IMessageBroker>();
            var orderProcessor = scope.ServiceProvider.GetRequiredService<OrderProcessor>();

            await messageBroker.SubscribeAsync<OrderCreatedEvent>(
                orderProcessor.ProcessOrderCreatedAsync);

            Console.WriteLine("Subscribed to OrderCreatedEvent with Inbox pattern");
            Console.WriteLine("Duplicate messages will be automatically filtered");
        }

        // Keep running
        await host.WaitForShutdownAsync();
    }
}

/// <summary>
/// Order processor with idempotent message handling
/// </summary>
public class OrderProcessor
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IInboxStore _inboxStore;
    private readonly ILogger<OrderProcessor> _logger;

    public OrderProcessor(
        ApplicationDbContext dbContext,
        IInboxStore inboxStore,
        ILogger<OrderProcessor> logger)
    {
        _dbContext = dbContext;
        _inboxStore = inboxStore;
        _logger = logger;
    }

    /// <summary>
    /// Processes order created event with idempotency guarantee.
    /// The Inbox pattern ensures this method is called only once per unique message ID.
    /// </summary>
    public async Task ProcessOrderCreatedAsync(
        OrderCreatedEvent message,
        MessageContext context,
        CancellationToken cancellationToken)
    {
        var messageId = context.MessageId ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Processing OrderCreatedEvent: OrderId={OrderId} MessageId={MessageId}",
            message.OrderId,
            messageId);

        // Check if message was already processed (inbox check)
        var alreadyProcessed = await _inboxStore.ExistsAsync(messageId, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Message {MessageId} already processed. Skipping.",
                messageId);

            // Acknowledge the message (it was already processed)
            if (context.Acknowledge != null)
            {
                await context.Acknowledge();
            }

            return;
        }

        // Start transaction for processing
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Process the order
            var order = new Order
            {
                Id = message.OrderId,
                CustomerId = message.CustomerId,
                TotalAmount = message.TotalAmount,
                CreatedAt = message.CreatedAt,
                ProcessedAt = DateTime.UtcNow
            };

            await _dbContext.Orders.AddAsync(order, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} processed successfully", message.OrderId);

            // Store message ID in inbox (same transaction)
            var inboxMessage = new InboxMessage
            {
                MessageId = messageId,
                MessageType = typeof(OrderCreatedEvent).FullName!,
                ProcessedAt = DateTimeOffset.UtcNow,
                ConsumerName = "OrderProcessor"
            };

            await _inboxStore.StoreAsync(inboxMessage, cancellationToken);

            _logger.LogInformation("Message {MessageId} stored in inbox", messageId);

            // Commit transaction
            await transaction.CommitAsync(cancellationToken);

            // Acknowledge the message
            if (context.Acknowledge != null)
            {
                await context.Acknowledge();
            }

            _logger.LogInformation(
                "Transaction committed and message acknowledged for order {OrderId}",
                message.OrderId);
        }
        catch (Exception ex)
        {
            // Rollback transaction on error
            await transaction.RollbackAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to process order {OrderId}. Transaction rolled back.",
                message.OrderId);

            // Reject the message for retry
            if (context.Reject != null)
            {
                await context.Reject(requeue: true);
            }

            throw;
        }
    }

    /// <summary>
    /// Example of processing with custom idempotency key
    /// </summary>
    public async Task ProcessPaymentAsync(
        PaymentProcessedEvent message,
        MessageContext context,
        CancellationToken cancellationToken)
    {
        // Use custom idempotency key (e.g., payment transaction ID)
        var idempotencyKey = $"payment-{message.PaymentId}";

        _logger.LogInformation(
            "Processing PaymentProcessedEvent: PaymentId={PaymentId} IdempotencyKey={Key}",
            message.PaymentId,
            idempotencyKey);

        // Check inbox with custom key
        var alreadyProcessed = await _inboxStore.ExistsAsync(idempotencyKey, cancellationToken);

        if (alreadyProcessed)
        {
            _logger.LogInformation(
                "Payment {PaymentId} already processed. Skipping.",
                message.PaymentId);

            if (context.Acknowledge != null)
            {
                await context.Acknowledge();
            }

            return;
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Process payment
            var payment = new Payment
            {
                Id = message.PaymentId,
                OrderId = message.OrderId,
                Amount = message.Amount,
                ProcessedAt = DateTime.UtcNow
            };

            await _dbContext.Payments.AddAsync(payment, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Store in inbox with custom key
            var inboxMessage = new InboxMessage
            {
                MessageId = idempotencyKey,
                MessageType = typeof(PaymentProcessedEvent).FullName!,
                ProcessedAt = DateTimeOffset.UtcNow,
                ConsumerName = "OrderProcessor"
            };

            await _inboxStore.StoreAsync(inboxMessage, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            if (context.Acknowledge != null)
            {
                await context.Acknowledge();
            }

            _logger.LogInformation("Payment {PaymentId} processed successfully", message.PaymentId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to process payment {PaymentId}", message.PaymentId);

            if (context.Reject != null)
            {
                await context.Reject(requeue: true);
            }

            throw;
        }
    }
}

/// <summary>
/// Payment entity
/// </summary>
public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// Payment processed event
/// </summary>
public class PaymentProcessedEvent
{
    public int PaymentId { get; set; }
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// Example of monitoring inbox status
/// </summary>
public class InboxMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InboxMonitoringService> _logger;

    public InboxMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<InboxMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<InboxDbContext>();

                // Check inbox size
                var totalCount = await dbContext.InboxMessages.CountAsync(stoppingToken);
                _logger.LogInformation("Total inbox messages: {Count}", totalCount);

                // Check old messages
                var oldThreshold = DateTimeOffset.UtcNow.AddDays(-7);
                var oldCount = await dbContext.InboxMessages
                    .Where(m => m.ProcessedAt < oldThreshold)
                    .CountAsync(stoppingToken);

                if (oldCount > 0)
                {
                    _logger.LogInformation(
                        "Old inbox messages (>7 days): {Count}. Cleanup will remove these.",
                        oldCount);
                }

                // Check for duplicate processing attempts
                var recentDuplicates = await dbContext.InboxMessages
                    .Where(m => m.ProcessedAt > DateTimeOffset.UtcNow.AddHours(-1))
                    .GroupBy(m => m.MessageType)
                    .Select(g => new { MessageType = g.Key, Count = g.Count() })
                    .Where(x => x.Count > 100)
                    .ToListAsync(stoppingToken);

                foreach (var duplicate in recentDuplicates)
                {
                    _logger.LogWarning(
                        "High duplicate rate for {MessageType}: {Count} in last hour",
                        duplicate.MessageType,
                        duplicate.Count);
                }

                // Alert if inbox is growing too large
                if (totalCount > 1000000) // 1 million
                {
                    _logger.LogCritical(
                        "Inbox size is very large: {Count}. Consider reducing retention period!",
                        totalCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring inbox");
            }

            // Check every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

/// <summary>
/// Example of manual inbox cleanup
/// </summary>
public class ManualInboxCleanupService
{
    private readonly IInboxStore _inboxStore;
    private readonly ILogger<ManualInboxCleanupService> _logger;

    public ManualInboxCleanupService(
        IInboxStore inboxStore,
        ILogger<ManualInboxCleanupService> logger)
    {
        _inboxStore = inboxStore;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger inbox cleanup
    /// </summary>
    public async Task CleanupAsync(TimeSpan retentionPeriod)
    {
        _logger.LogInformation(
            "Starting manual inbox cleanup with retention period: {Period}",
            retentionPeriod);

        var sw = Stopwatch.StartNew();

        try
        {
            await _inboxStore.CleanupExpiredAsync(retentionPeriod, CancellationToken.None);

            sw.Stop();

            _logger.LogInformation(
                "Inbox cleanup completed in {Duration}ms",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inbox cleanup failed");
            throw;
        }
    }

    /// <summary>
    /// Get inbox statistics
    /// </summary>
    public async Task<InboxStatistics> GetStatisticsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InboxDbContext>();

        var totalCount = await dbContext.InboxMessages.CountAsync();
        var last24Hours = await dbContext.InboxMessages
            .Where(m => m.ProcessedAt > DateTimeOffset.UtcNow.AddHours(-24))
            .CountAsync();
        var last7Days = await dbContext.InboxMessages
            .Where(m => m.ProcessedAt > DateTimeOffset.UtcNow.AddDays(-7))
            .CountAsync();

        var messageTypes = await dbContext.InboxMessages
            .GroupBy(m => m.MessageType)
            .Select(g => new { MessageType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        return new InboxStatistics
        {
            TotalMessages = totalCount,
            Last24Hours = last24Hours,
            Last7Days = last7Days,
            TopMessageTypes = messageTypes.ToDictionary(x => x.MessageType, x => x.Count)
        };
    }
}

/// <summary>
/// Inbox statistics
/// </summary>
public class InboxStatistics
{
    public int TotalMessages { get; set; }
    public int Last24Hours { get; set; }
    public int Last7Days { get; set; }
    public Dictionary<string, int> TopMessageTypes { get; set; } = new();
}

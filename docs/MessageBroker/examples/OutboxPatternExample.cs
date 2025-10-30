using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Relay.MessageBroker;
using Relay.MessageBroker.Outbox;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.MessageBroker.Examples;

/// <summary>
/// Example demonstrating the Outbox Pattern with SQL Server for reliable message publishing.
/// The Outbox Pattern ensures transactional consistency between database operations and message publishing.
/// </summary>
public class OutboxPatternExample
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure SQL Server for Outbox
                var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
                services.AddDbContext<OutboxDbContext>(options =>
                    options.UseSqlServer(connectionString));

                // Add Outbox Pattern
                services.AddOutboxPattern(options =>
                {
                    options.Enabled = true;
                    options.PollingInterval = TimeSpan.FromSeconds(5);
                    options.BatchSize = 100;
                    options.MaxRetryAttempts = 3;
                    options.RetryDelay = TimeSpan.FromSeconds(2);
                    options.UseExponentialBackoff = true;
                });

                // Register Outbox store
                services.AddScoped<IOutboxStore, SqlOutboxStore>();

                // Register Outbox worker
                services.AddHostedService<OutboxWorker>();

                // Configure RabbitMQ
                services.AddRabbitMQ(options =>
                {
                    options.HostName = "localhost";
                    options.Port = 5672;
                    options.UserName = "guest";
                    options.Password = "guest";
                });

                // Add application services
                services.AddScoped<OrderService>();
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(connectionString));
            })
            .Build();

        // Run migrations
        using (var scope = host.Services.CreateScope())
        {
            var outboxContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
            await outboxContext.Database.MigrateAsync();

            var appContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await appContext.Database.MigrateAsync();
        }

        // Start the host
        await host.StartAsync();

        // Example usage
        using (var scope = host.Services.CreateScope())
        {
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            // Create order with transactional messaging
            var order = new Order
            {
                Id = 123,
                CustomerId = 456,
                TotalAmount = 99.99m,
                CreatedAt = DateTime.UtcNow
            };

            await orderService.CreateOrderAsync(order);
            Console.WriteLine($"Order {order.Id} created successfully");
            Console.WriteLine("Message will be published via Outbox pattern");
        }

        // Wait for outbox to process
        await Task.Delay(TimeSpan.FromSeconds(10));

        await host.StopAsync();
    }
}

/// <summary>
/// Order entity
/// </summary>
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Order created event
/// </summary>
public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Application database context
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; } = null!;
}

/// <summary>
/// Order service demonstrating transactional messaging with Outbox pattern
/// </summary>
public class OrderService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IOutboxStore _outboxStore;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ApplicationDbContext dbContext,
        IOutboxStore outboxStore,
        ILogger<OrderService> logger)
    {
        _dbContext = dbContext;
        _outboxStore = outboxStore;
        _logger = logger;
    }

    /// <summary>
    /// Creates an order with transactional consistency.
    /// Both the order and the outbox message are saved in the same transaction.
    /// </summary>
    public async Task CreateOrderAsync(Order order)
    {
        // Start database transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Save order to database
            await _dbContext.Orders.AddAsync(order);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} saved to database", order.Id);

            // Create event
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = order.Id,
                CustomerId = order.CustomerId,
                TotalAmount = order.TotalAmount,
                CreatedAt = order.CreatedAt
            };

            // Store message in outbox (same transaction)
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = typeof(OrderCreatedEvent).FullName!,
                Payload = JsonSerializer.SerializeToUtf8Bytes(orderEvent),
                CreatedAt = DateTimeOffset.UtcNow,
                Status = OutboxMessageStatus.Pending,
                RetryCount = 0
            };

            await _outboxStore.StoreAsync(outboxMessage, CancellationToken.None);

            _logger.LogInformation("Message stored in outbox: {MessageId}", outboxMessage.Id);

            // Commit transaction
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Transaction committed successfully for order {OrderId}",
                order.Id);
        }
        catch (Exception ex)
        {
            // Rollback transaction on error
            await transaction.RollbackAsync();

            _logger.LogError(
                ex,
                "Failed to create order {OrderId}. Transaction rolled back.",
                order.Id);

            throw;
        }
    }

    /// <summary>
    /// Example of handling order cancellation with compensation
    /// </summary>
    public async Task CancelOrderAsync(int orderId)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            // Find and update order
            var order = await _dbContext.Orders.FindAsync(orderId);
            if (order == null)
            {
                throw new InvalidOperationException($"Order {orderId} not found");
            }

            _dbContext.Orders.Remove(order);
            await _dbContext.SaveChangesAsync();

            // Store cancellation event in outbox
            var cancellationEvent = new OrderCancelledEvent
            {
                OrderId = orderId,
                CancelledAt = DateTime.UtcNow
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = typeof(OrderCancelledEvent).FullName!,
                Payload = JsonSerializer.SerializeToUtf8Bytes(cancellationEvent),
                CreatedAt = DateTimeOffset.UtcNow,
                Status = OutboxMessageStatus.Pending,
                RetryCount = 0
            };

            await _outboxStore.StoreAsync(outboxMessage, CancellationToken.None);

            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to cancel order {OrderId}", orderId);
            throw;
        }
    }
}

/// <summary>
/// Order cancelled event
/// </summary>
public class OrderCancelledEvent
{
    public int OrderId { get; set; }
    public DateTime CancelledAt { get; set; }
}

/// <summary>
/// Example of monitoring outbox status
/// </summary>
public class OutboxMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxMonitoringService> _logger;

    public OutboxMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<OutboxMonitoringService> logger)
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
                var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();

                // Check pending messages
                var pendingMessages = await outboxStore.GetPendingAsync(1000, stoppingToken);
                var pendingCount = await pendingMessages.CountAsync(stoppingToken);

                if (pendingCount > 0)
                {
                    _logger.LogInformation("Pending outbox messages: {Count}", pendingCount);
                }

                // Check failed messages
                var failedMessages = await outboxStore.GetFailedAsync(100, stoppingToken);
                var failedCount = await failedMessages.CountAsync(stoppingToken);

                if (failedCount > 0)
                {
                    _logger.LogWarning("Failed outbox messages: {Count}", failedCount);

                    // Log details of failed messages
                    await foreach (var message in failedMessages)
                    {
                        _logger.LogError(
                            "Failed message: {MessageId} Type={MessageType} Error={Error} Retries={RetryCount}",
                            message.Id,
                            message.MessageType,
                            message.LastError,
                            message.RetryCount);
                    }
                }

                // Alert if too many failed messages
                if (failedCount > 10)
                {
                    _logger.LogCritical(
                        "High number of failed outbox messages: {Count}. Investigation required!",
                        failedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring outbox");
            }

            // Check every minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

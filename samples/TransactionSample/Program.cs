using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.Core;
using Relay.Core.Pipeline;
using Relay.Core.Transactions;
using System.Transactions;

Console.WriteLine("=== Relay Transaction & Unit of Work Sample ===\n");

// Setup DI container with EF Core and Relay
var services = new ServiceCollection();

// Add logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Add EF Core DbContext
services.AddDbContext<OrderDbContext>(options =>
    options.UseInMemoryDatabase("OrdersDb"));

// Register DbContext as IUnitOfWork
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<OrderDbContext>());

// Add Relay with transaction support
services.AddRelay(typeof(Program).Assembly);

// Add transaction behaviors
services.AddRelayTransactions(
    scopeOption: TransactionScopeOption.Required,
    isolationLevel: IsolationLevel.ReadCommitted,
    timeout: TimeSpan.FromMinutes(1));

services.AddRelayUnitOfWork(saveOnlyForTransactionalRequests: false);

var provider = services.BuildServiceProvider();

// Example 1: Create order with automatic transaction and SaveChanges
Console.WriteLine("--- Example 1: Create Order (Transactional) ---");
try
{
    var relay = provider.GetRequiredService<IRelay>();
    var order = await relay.SendAsync(new CreateOrderCommand(
        CustomerId: 1,
        Items: new[] { "Product A", "Product B" },
        TotalAmount: 199.99m));

    Console.WriteLine($"✓ Order created: ID={order.Id}, Total=${order.TotalAmount}, Status={order.Status}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Failed: {ex.Message}");
}

// Example 2: Update order (with automatic transaction)
Console.WriteLine("\n--- Example 2: Update Order Status ---");
try
{
    var relay = provider.GetRequiredService<IRelay>();
    var result = await relay.SendAsync(new UpdateOrderStatusCommand(
        OrderId: 1,
        NewStatus: "Shipped"));

    Console.WriteLine($"✓ Order updated: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Failed: {ex.Message}");
}

// Example 3: Intentional failure - transaction rollback
Console.WriteLine("\n--- Example 3: Failed Order (Transaction Rollback) ---");
try
{
    var relay = provider.GetRequiredService<IRelay>();
    await relay.SendAsync(new CreateOrderCommand(
        CustomerId: 999, // Non-existent customer
        Items: new[] { "Product C" },
        TotalAmount: -50)); // Invalid amount
}
catch (Exception ex)
{
    Console.WriteLine($"✓ Transaction rolled back as expected: {ex.Message}");
}

// Example 4: Query without transaction
Console.WriteLine("\n--- Example 4: Get Order (No Transaction) ---");
try
{
    var relay = provider.GetRequiredService<IRelay>();
    var order = await relay.SendAsync(new GetOrderQuery(OrderId: 1));

    if (order != null)
    {
        Console.WriteLine($"✓ Order retrieved: ID={order.Id}, Status={order.Status}, Items={order.Items.Length}");
    }
    else
    {
        Console.WriteLine("✗ Order not found");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"✗ Failed: {ex.Message}");
}

Console.WriteLine("\n=== Sample Complete ===");

#region Domain Models

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string[] Items { get; set; } = Array.Empty<string>();
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; }
}

#endregion

#region EF Core DbContext

public class OrderDbContext : DbContext, IUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();

    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerId).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.Status).HasMaxLength(50);
        });
    }

    // IUnitOfWork is satisfied by DbContext.SaveChangesAsync
}

#endregion

#region Commands (Transactional)

// Transactional command - wrapped in transaction + auto SaveChanges
public record CreateOrderCommand(int CustomerId, string[] Items, decimal TotalAmount)
    : IRequest<Order>, ITransactionalRequest<Order>;

public record UpdateOrderStatusCommand(int OrderId, string NewStatus)
    : IRequest<string>, ITransactionalRequest<string>;

#endregion

#region Queries (Non-Transactional)

public record GetOrderQuery(int OrderId) : IRequest<Order?>;

#endregion

#region Handlers

public class OrderHandlers
{
    private readonly OrderDbContext _dbContext;
    private readonly ILogger<OrderHandlers> _logger;

    public OrderHandlers(OrderDbContext dbContext, ILogger<OrderHandlers> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [Handle]
    public async ValueTask<Order> CreateOrder(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", command.CustomerId);

        // Validation
        if (command.TotalAmount <= 0)
        {
            throw new InvalidOperationException("Total amount must be greater than zero");
        }

        if (command.Items.Length == 0)
        {
            throw new InvalidOperationException("Order must contain at least one item");
        }

        // Create order entity
        var order = new Order
        {
            CustomerId = command.CustomerId,
            Items = command.Items,
            TotalAmount = command.TotalAmount,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Orders.Add(order);

        // Note: SaveChangesAsync is called automatically by UnitOfWorkBehavior
        // Transaction is managed automatically by TransactionBehavior

        _logger.LogInformation("Order created in transaction");

        return order;
    }

    [Handle]
    public async ValueTask<string> UpdateOrderStatus(
        UpdateOrderStatusCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating order {OrderId} to status {Status}",
            command.OrderId, command.NewStatus);

        var order = await _dbContext.Orders.FindAsync(new object[] { command.OrderId }, cancellationToken);

        if (order == null)
        {
            throw new InvalidOperationException($"Order {command.OrderId} not found");
        }

        order.Status = command.NewStatus;

        // Auto SaveChanges + Transaction

        _logger.LogInformation("Order status updated");

        return $"Order {command.OrderId} updated to {command.NewStatus}";
    }

    [Handle]
    public async ValueTask<Order?> GetOrder(
        GetOrderQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving order {OrderId}", query.OrderId);

        // This is a query - no transaction needed
        return await _dbContext.Orders.FindAsync(new object[] { query.OrderId }, cancellationToken);
    }
}

#endregion

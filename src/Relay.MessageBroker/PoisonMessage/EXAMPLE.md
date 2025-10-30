# Poison Message Handling Examples

This document provides practical examples of using the Poison Message Handling feature.

## Example 1: Basic Configuration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker;
using Relay.MessageBroker.PoisonMessage;

var services = new ServiceCollection();

// Configure message broker with poison message handling
services.AddMessageBroker(options =>
{
    options.ConnectionString = "amqp://localhost";
    options.PoisonMessage = new PoisonMessageOptions
    {
        Enabled = true,
        FailureThreshold = 3,
        RetentionPeriod = TimeSpan.FromDays(7)
    };
});

// Add poison message handling services
services.AddPoisonMessageHandling(options =>
{
    options.Enabled = true;
    options.FailureThreshold = 3;
    options.RetentionPeriod = TimeSpan.FromDays(7);
    options.CleanupInterval = TimeSpan.FromHours(1);
});

var serviceProvider = services.BuildServiceProvider();
```

## Example 2: Message Processing with Automatic Poison Detection

```csharp
public class OrderService
{
    private readonly IMessageBroker _messageBroker;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IMessageBroker messageBroker, ILogger<OrderService> logger)
    {
        _messageBroker = messageBroker;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Subscribe to order events
        await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
            async (message, context, ct) =>
            {
                try
                {
                    // Process the order
                    await ProcessOrder(message);
                    
                    _logger.LogInformation(
                        "Order processed successfully. OrderId: {OrderId}",
                        message.OrderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to process order. OrderId: {OrderId}",
                        message.OrderId);
                    
                    // Exception will be tracked by poison message handler
                    // After 3 failures, message will be moved to poison queue
                    throw;
                }
            },
            cancellationToken: cancellationToken);
    }

    private async Task ProcessOrder(OrderCreatedEvent order)
    {
        // Simulate processing that might fail
        if (order.TotalAmount < 0)
        {
            throw new InvalidOperationException("Order amount cannot be negative");
        }

        // Process order...
        await Task.Delay(100);
    }
}

public record OrderCreatedEvent(string OrderId, decimal TotalAmount);
```

## Example 3: Monitoring Poison Messages

```csharp
using Microsoft.AspNetCore.Mvc;
using Relay.MessageBroker.PoisonMessage;

[ApiController]
[Route("api/[controller]")]
public class PoisonMessagesController : ControllerBase
{
    private readonly IPoisonMessageHandler _poisonMessageHandler;
    private readonly ILogger<PoisonMessagesController> _logger;

    public PoisonMessagesController(
        IPoisonMessageHandler poisonMessageHandler,
        ILogger<PoisonMessagesController> logger)
    {
        _poisonMessageHandler = poisonMessageHandler;
        _logger = logger;
    }

    /// <summary>
    /// Gets all poison messages.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PoisonMessageDto>>> GetAll()
    {
        var messages = await _poisonMessageHandler.GetPoisonMessagesAsync();
        
        var dtos = messages.Select(m => new PoisonMessageDto
        {
            Id = m.Id,
            MessageType = m.MessageType,
            FailureCount = m.FailureCount,
            FirstFailureAt = m.FirstFailureAt,
            LastFailureAt = m.LastFailureAt,
            Errors = m.Errors,
            OriginalMessageId = m.OriginalMessageId
        });

        return Ok(dtos);
    }

    /// <summary>
    /// Gets a specific poison message by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PoisonMessageDetailDto>> GetById(Guid id)
    {
        var messages = await _poisonMessageHandler.GetPoisonMessagesAsync();
        var message = messages.FirstOrDefault(m => m.Id == id);

        if (message == null)
        {
            return NotFound();
        }

        var dto = new PoisonMessageDetailDto
        {
            Id = message.Id,
            MessageType = message.MessageType,
            FailureCount = message.FailureCount,
            FirstFailureAt = message.FirstFailureAt,
            LastFailureAt = message.LastFailureAt,
            Errors = message.Errors,
            OriginalMessageId = message.OriginalMessageId,
            CorrelationId = message.CorrelationId,
            Headers = message.Headers,
            PayloadSize = message.Payload.Length
        };

        return Ok(dto);
    }

    /// <summary>
    /// Reprocesses a poison message.
    /// </summary>
    [HttpPost("{id}/reprocess")]
    public async Task<IActionResult> Reprocess(Guid id)
    {
        try
        {
            await _poisonMessageHandler.ReprocessAsync(id);
            
            _logger.LogInformation(
                "Poison message reprocessing initiated. MessageId: {MessageId}",
                id);

            return Ok(new { message = "Message reprocessing initiated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to reprocess poison message. MessageId: {MessageId}",
                id);
            
            return StatusCode(500, new { error = "Failed to reprocess message" });
        }
    }

    /// <summary>
    /// Gets poison message statistics.
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<PoisonMessageStatistics>> GetStatistics()
    {
        var messages = await _poisonMessageHandler.GetPoisonMessagesAsync();
        var messageList = messages.ToList();

        var stats = new PoisonMessageStatistics
        {
            TotalCount = messageList.Count,
            MessageTypeBreakdown = messageList
                .GroupBy(m => m.MessageType)
                .ToDictionary(g => g.Key, g => g.Count()),
            OldestMessage = messageList.MinBy(m => m.FirstFailureAt)?.FirstFailureAt,
            NewestMessage = messageList.MaxBy(m => m.LastFailureAt)?.LastFailureAt,
            AverageFailureCount = messageList.Any() 
                ? messageList.Average(m => m.FailureCount) 
                : 0
        };

        return Ok(stats);
    }
}

public record PoisonMessageDto
{
    public Guid Id { get; init; }
    public string MessageType { get; init; } = string.Empty;
    public int FailureCount { get; init; }
    public DateTimeOffset FirstFailureAt { get; init; }
    public DateTimeOffset LastFailureAt { get; init; }
    public List<string> Errors { get; init; } = new();
    public string? OriginalMessageId { get; init; }
}

public record PoisonMessageDetailDto : PoisonMessageDto
{
    public string? CorrelationId { get; init; }
    public Dictionary<string, object>? Headers { get; init; }
    public int PayloadSize { get; init; }
}

public record PoisonMessageStatistics
{
    public int TotalCount { get; init; }
    public Dictionary<string, int> MessageTypeBreakdown { get; init; } = new();
    public DateTimeOffset? OldestMessage { get; init; }
    public DateTimeOffset? NewestMessage { get; init; }
    public double AverageFailureCount { get; init; }
}
```

## Example 4: Custom Poison Message Store with Entity Framework

```csharp
using Microsoft.EntityFrameworkCore;
using Relay.MessageBroker.PoisonMessage;

public class ApplicationDbContext : DbContext
{
    public DbSet<PoisonMessage> PoisonMessages { get; set; } = null!;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PoisonMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MessageType).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.FailureCount).IsRequired();
            entity.Property(e => e.FirstFailureAt).IsRequired();
            entity.Property(e => e.LastFailureAt).IsRequired();
            entity.Property(e => e.OriginalMessageId).HasMaxLength(500);
            entity.Property(e => e.CorrelationId).HasMaxLength(500);
            
            entity.HasIndex(e => e.MessageType);
            entity.HasIndex(e => e.LastFailureAt);
            entity.HasIndex(e => e.OriginalMessageId);
        });
    }
}

public class EfPoisonMessageStore : IPoisonMessageStore
{
    private readonly ApplicationDbContext _dbContext;

    public EfPoisonMessageStore(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask StoreAsync(PoisonMessage message, CancellationToken cancellationToken = default)
    {
        _dbContext.PoisonMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<PoisonMessage?> GetByIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PoisonMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
    }

    public async ValueTask<IEnumerable<PoisonMessage>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PoisonMessages
            .OrderByDescending(m => m.LastFailureAt)
            .ToListAsync(cancellationToken);
    }

    public async ValueTask RemoveAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.PoisonMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);
        
        if (message != null)
        {
            _dbContext.PoisonMessages.Remove(message);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async ValueTask<int> CleanupExpiredAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        var cutoffTime = DateTimeOffset.UtcNow - retentionPeriod;
        
        var expiredMessages = await _dbContext.PoisonMessages
            .Where(m => m.LastFailureAt < cutoffTime)
            .ToListAsync(cancellationToken);

        _dbContext.PoisonMessages.RemoveRange(expiredMessages);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return expiredMessages.Count;
    }

    public async ValueTask UpdateAsync(PoisonMessage message, CancellationToken cancellationToken = default)
    {
        _dbContext.PoisonMessages.Update(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

// Registration
services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

services.AddPoisonMessageHandling<EfPoisonMessageStore>(options =>
{
    options.Enabled = true;
    options.FailureThreshold = 5;
    options.RetentionPeriod = TimeSpan.FromDays(30);
});
```

## Example 5: Alerting on Poison Messages

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker.PoisonMessage;

public class PoisonMessageAlertService : BackgroundService
{
    private readonly IPoisonMessageHandler _poisonMessageHandler;
    private readonly ILogger<PoisonMessageAlertService> _logger;
    private readonly IEmailService _emailService;
    private int _lastKnownCount = 0;

    public PoisonMessageAlertService(
        IPoisonMessageHandler poisonMessageHandler,
        ILogger<PoisonMessageAlertService> logger,
        IEmailService emailService)
    {
        _poisonMessageHandler = poisonMessageHandler;
        _logger = logger;
        _emailService = emailService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                var messages = await _poisonMessageHandler.GetPoisonMessagesAsync(stoppingToken);
                var currentCount = messages.Count();

                if (currentCount > _lastKnownCount)
                {
                    var newMessages = currentCount - _lastKnownCount;
                    
                    _logger.LogWarning(
                        "New poison messages detected. Count: {NewCount}, Total: {TotalCount}",
                        newMessages,
                        currentCount);

                    // Send alert
                    await _emailService.SendAlertAsync(
                        "Poison Messages Alert",
                        $"Detected {newMessages} new poison messages. Total: {currentCount}",
                        stoppingToken);
                }

                _lastKnownCount = currentCount;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking poison messages");
            }
        }
    }
}

public interface IEmailService
{
    Task SendAlertAsync(string subject, string body, CancellationToken cancellationToken);
}
```

## Example 6: Testing Poison Message Handling

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Relay.MessageBroker.PoisonMessage;
using Xunit;

public class PoisonMessageHandlingTests
{
    [Fact]
    public async Task Message_Should_Move_To_Poison_Queue_After_Threshold_Exceeded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPoisonMessageHandling(options =>
        {
            options.Enabled = true;
            options.FailureThreshold = 3;
        });

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<IPoisonMessageHandler>();

        var messageId = "test-message-123";
        var messageType = "TestMessage";
        var payload = new byte[] { 1, 2, 3 };
        var context = new MessageContext { MessageId = messageId };

        // Act - Track failures
        var isPoisonAfterFirst = await handler.TrackFailureAsync(
            messageId, messageType, payload, "Error 1", context, default);
        
        var isPoisonAfterSecond = await handler.TrackFailureAsync(
            messageId, messageType, payload, "Error 2", context, default);
        
        var isPoisonAfterThird = await handler.TrackFailureAsync(
            messageId, messageType, payload, "Error 3", context, default);

        // Assert
        Assert.False(isPoisonAfterFirst);
        Assert.False(isPoisonAfterSecond);
        Assert.True(isPoisonAfterThird);

        var poisonMessages = await handler.GetPoisonMessagesAsync();
        Assert.Single(poisonMessages);
        
        var poisonMessage = poisonMessages.First();
        Assert.Equal(messageType, poisonMessage.MessageType);
        Assert.Equal(3, poisonMessage.FailureCount);
        Assert.Equal(3, poisonMessage.Errors.Count);
    }

    [Fact]
    public async Task Reprocess_Should_Remove_Message_From_Poison_Queue()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPoisonMessageHandling(options =>
        {
            options.Enabled = true;
            options.FailureThreshold = 2;
        });

        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetRequiredService<IPoisonMessageHandler>();

        var messageId = "test-message-456";
        var messageType = "TestMessage";
        var payload = new byte[] { 1, 2, 3 };
        var context = new MessageContext { MessageId = messageId };

        // Track failures to create poison message
        await handler.TrackFailureAsync(messageId, messageType, payload, "Error 1", context, default);
        await handler.TrackFailureAsync(messageId, messageType, payload, "Error 2", context, default);

        var poisonMessages = await handler.GetPoisonMessagesAsync();
        var poisonMessageId = poisonMessages.First().Id;

        // Act
        await handler.ReprocessAsync(poisonMessageId);

        // Assert
        var remainingMessages = await handler.GetPoisonMessagesAsync();
        Assert.Empty(remainingMessages);
    }
}
```

## Example 7: Integration with Monitoring Dashboard

```csharp
using Microsoft.AspNetCore.SignalR;
using Relay.MessageBroker.PoisonMessage;

public class PoisonMessageHub : Hub
{
    private readonly IPoisonMessageHandler _poisonMessageHandler;

    public PoisonMessageHub(IPoisonMessageHandler poisonMessageHandler)
    {
        _poisonMessageHandler = poisonMessageHandler;
    }

    public async Task<object> GetPoisonMessageStats()
    {
        var messages = await _poisonMessageHandler.GetPoisonMessagesAsync();
        var messageList = messages.ToList();

        return new
        {
            TotalCount = messageList.Count,
            ByType = messageList.GroupBy(m => m.MessageType)
                .Select(g => new { Type = g.Key, Count = g.Count() }),
            Recent = messageList
                .OrderByDescending(m => m.LastFailureAt)
                .Take(10)
                .Select(m => new
                {
                    m.Id,
                    m.MessageType,
                    m.FailureCount,
                    m.LastFailureAt
                })
        };
    }
}

// Background service to push updates
public class PoisonMessageNotificationService : BackgroundService
{
    private readonly IPoisonMessageHandler _poisonMessageHandler;
    private readonly IHubContext<PoisonMessageHub> _hubContext;
    private int _lastCount = 0;

    public PoisonMessageNotificationService(
        IPoisonMessageHandler poisonMessageHandler,
        IHubContext<PoisonMessageHub> hubContext)
    {
        _poisonMessageHandler = poisonMessageHandler;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            var messages = await _poisonMessageHandler.GetPoisonMessagesAsync(stoppingToken);
            var currentCount = messages.Count();

            if (currentCount != _lastCount)
            {
                await _hubContext.Clients.All.SendAsync(
                    "PoisonMessageCountChanged",
                    currentCount,
                    stoppingToken);

                _lastCount = currentCount;
            }
        }
    }
}
```

These examples demonstrate various aspects of poison message handling, from basic configuration to advanced monitoring and alerting scenarios.

using Microsoft.EntityFrameworkCore;

namespace Relay.MessageBroker.Outbox;

/// <summary>
/// SQL-based implementation of the outbox store using Entity Framework Core.
/// </summary>
public sealed class SqlOutboxStore : IOutboxStore
{
    private readonly IDbContextFactory<OutboxDbContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlOutboxStore"/> class.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    public SqlOutboxStore(IDbContextFactory<OutboxDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <inheritdoc />
    public async ValueTask<OutboxMessage> StoreAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (message.Id == Guid.Empty)
        {
            message.Id = Guid.NewGuid();
        }

        message.CreatedAt = DateTimeOffset.UtcNow;
        message.Status = OutboxMessageStatus.Pending;

        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync(cancellationToken);

        return message;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var pending = await context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return pending;
    }

    /// <inheritdoc />
    public async ValueTask MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var message = await context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.Status = OutboxMessageStatus.Published;
            message.PublishedAt = DateTimeOffset.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async ValueTask MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var message = await context.OutboxMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message != null)
        {
            message.RetryCount++;
            message.LastError = error;
            message.Status = OutboxMessageStatus.Failed;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<OutboxMessage>> GetFailedAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var failed = await context.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Failed)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return failed;
    }
}

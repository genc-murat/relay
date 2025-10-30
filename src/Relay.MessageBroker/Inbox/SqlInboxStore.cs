using Microsoft.EntityFrameworkCore;

namespace Relay.MessageBroker.Inbox;

/// <summary>
/// SQL-based implementation of the inbox store using Entity Framework Core.
/// </summary>
public sealed class SqlInboxStore : IInboxStore
{
    private readonly IDbContextFactory<InboxDbContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlInboxStore"/> class.
    /// </summary>
    /// <param name="contextFactory">The database context factory.</param>
    public SqlInboxStore(IDbContextFactory<InboxDbContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <inheritdoc />
    public async ValueTask<bool> ExistsAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.InboxMessages
            .AnyAsync(m => m.MessageId == messageId, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask StoreAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(message.MessageId);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        message.ProcessedAt = DateTimeOffset.UtcNow;

        // Use AddOrUpdate pattern to handle potential race conditions
        var existing = await context.InboxMessages
            .FirstOrDefaultAsync(m => m.MessageId == message.MessageId, cancellationToken);

        if (existing == null)
        {
            context.InboxMessages.Add(message);
            await context.SaveChangesAsync(cancellationToken);
        }
        // If it already exists, we don't need to do anything (idempotency)
    }

    /// <inheritdoc />
    public async ValueTask<int> CleanupExpiredAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var cutoffTime = DateTimeOffset.UtcNow - retentionPeriod;

        var expiredMessages = await context.InboxMessages
            .Where(m => m.ProcessedAt < cutoffTime)
            .ToListAsync(cancellationToken);

        if (expiredMessages.Count > 0)
        {
            context.InboxMessages.RemoveRange(expiredMessages);
            await context.SaveChangesAsync(cancellationToken);
        }

        return expiredMessages.Count;
    }
}

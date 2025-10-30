using Microsoft.EntityFrameworkCore;

namespace Relay.MessageBroker.Inbox;

/// <summary>
/// Database context for the Inbox pattern.
/// </summary>
public class InboxDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InboxDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public InboxDbContext(DbContextOptions<InboxDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the inbox messages.
    /// </summary>
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InboxMessage>(entity =>
        {
            entity.ToTable("InboxMessages");

            entity.HasKey(e => e.MessageId);

            entity.Property(e => e.MessageId)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.MessageType)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ProcessedAt)
                .IsRequired();

            entity.Property(e => e.ConsumerName)
                .HasMaxLength(500);

            entity.HasIndex(e => e.ProcessedAt);
            entity.HasIndex(e => e.MessageType);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Relay.MessageBroker.Outbox;

/// <summary>
/// Database context for the Outbox pattern.
/// </summary>
public class OutboxDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public OutboxDbContext(DbContextOptions<OutboxDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the outbox messages.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.MessageType)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Payload)
                .IsRequired();

            entity.Property(e => e.Headers)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null));

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.PublishedAt);

            entity.Property(e => e.Status)
                .IsRequired();

            entity.Property(e => e.RetryCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.LastError)
                .HasMaxLength(2000);

            entity.Property(e => e.RoutingKey)
                .HasMaxLength(500);

            entity.Property(e => e.Exchange)
                .HasMaxLength(500);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}

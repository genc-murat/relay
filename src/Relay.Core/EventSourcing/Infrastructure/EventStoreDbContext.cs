using Microsoft.EntityFrameworkCore;

namespace Relay.Core.EventSourcing.Infrastructure;

/// <summary>
/// DbContext for event sourcing.
/// </summary>
public class EventStoreDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public EventStoreDbContext(DbContextOptions<EventStoreDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Events DbSet.
    /// </summary>
    public DbSet<EventEntity> Events { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EventEntity>(entity =>
        {
            entity.ToTable("Events");

            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.AggregateId)
                .HasDatabaseName("IX_Events_AggregateId");

            entity.HasIndex(e => new { e.AggregateId, e.AggregateVersion })
                .IsUnique()
                .HasDatabaseName("IX_Events_AggregateId_Version");

            entity.Property(e => e.Id)
                .IsRequired();

            entity.Property(e => e.AggregateId)
                .IsRequired();

            entity.Property(e => e.AggregateVersion)
                .IsRequired();

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.EventData)
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .IsRequired();
        });
    }
}

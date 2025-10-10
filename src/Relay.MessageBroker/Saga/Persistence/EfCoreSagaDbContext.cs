using Microsoft.EntityFrameworkCore;

namespace Relay.MessageBroker.Saga.Persistence;

/// <summary>
/// Entity Framework Core database context for saga persistence.
/// Supports SQL Server, PostgreSQL, SQLite, and other EF Core providers.
/// </summary>
public class EfCoreSagaDbContext : DbContext, ISagaDbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfCoreSagaDbContext"/> class.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    public EfCoreSagaDbContext(DbContextOptions<EfCoreSagaDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Sagas DbSet.
    /// </summary>
    public DbSet<SagaEntityBase> SagaEntities { get; set; } = null!;

    /// <inheritdoc/>
    IQueryable<SagaEntityBase> ISagaDbContext.Sagas => SagaEntities;

    /// <inheritdoc/>
    void ISagaDbContext.Add(SagaEntityBase entity)
    {
        SagaEntities.Add(entity);
    }

    /// <inheritdoc/>
    void ISagaDbContext.Update(SagaEntityBase entity)
    {
        SagaEntities.Update(entity);
    }

    /// <inheritdoc/>
    void ISagaDbContext.Remove(SagaEntityBase entity)
    {
        SagaEntities.Remove(entity);
    }

    /// <summary>
    /// Configures the database schema for sagas.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SagaEntityBase>(entity =>
        {
            // Table configuration
            entity.ToTable("Sagas");

            // Primary key
            entity.HasKey(e => e.SagaId);

            // Indexes for performance
            entity.HasIndex(e => e.CorrelationId)
                .HasDatabaseName("IX_Sagas_CorrelationId");

            entity.HasIndex(e => e.State)
                .HasDatabaseName("IX_Sagas_State");

            entity.HasIndex(e => e.SagaType)
                .HasDatabaseName("IX_Sagas_SagaType");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_Sagas_CreatedAt");

            entity.HasIndex(e => e.UpdatedAt)
                .HasDatabaseName("IX_Sagas_UpdatedAt");

            // Composite index for active saga queries
            entity.HasIndex(e => new { e.State, e.CreatedAt })
                .HasDatabaseName("IX_Sagas_State_CreatedAt");

            // Property configurations
            entity.Property(e => e.SagaId)
                .IsRequired()
                .ValueGeneratedNever();

            entity.Property(e => e.CorrelationId)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.State)
                .IsRequired()
                .HasConversion<string>() // Store as string for readability
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            entity.Property(e => e.CurrentStep)
                .IsRequired();

            entity.Property(e => e.MetadataJson)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(e => e.DataJson)
                .IsRequired()
                .HasColumnType("text");

            entity.Property(e => e.SagaType)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.ErrorStackTrace)
                .HasColumnType("text");

            // Optimistic concurrency via Version field
            entity.Property(e => e.Version)
                .IsRequired()
                .IsConcurrencyToken();
        });
    }
}

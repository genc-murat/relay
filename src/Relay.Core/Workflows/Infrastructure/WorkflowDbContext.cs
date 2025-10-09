using Microsoft.EntityFrameworkCore;

namespace Relay.Core.Workflows.Infrastructure;

/// <summary>
/// DbContext for workflow engine persistence.
/// </summary>
public class WorkflowDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowDbContext"/> class.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the workflow executions DbSet.
    /// </summary>
    public DbSet<WorkflowExecutionEntity> WorkflowExecutions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the workflow definitions DbSet.
    /// </summary>
    public DbSet<WorkflowDefinitionEntity> WorkflowDefinitions { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure WorkflowExecutionEntity
        modelBuilder.Entity<WorkflowExecutionEntity>(entity =>
        {
            entity.ToTable("WorkflowExecutions");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.WorkflowDefinitionId)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.StartedAt)
                .IsRequired();

            entity.Property(e => e.CurrentStepIndex)
                .IsRequired();

            entity.Property(e => e.Version)
                .IsRequired()
                .IsConcurrencyToken();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.WorkflowDefinitionId)
                .HasDatabaseName("IX_WorkflowExecutions_DefinitionId");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_WorkflowExecutions_Status");

            entity.HasIndex(e => e.StartedAt)
                .HasDatabaseName("IX_WorkflowExecutions_StartedAt");
        });

        // Configure WorkflowDefinitionEntity
        modelBuilder.Entity<WorkflowDefinitionEntity>(entity =>
        {
            entity.ToTable("WorkflowDefinitions");

            entity.HasKey(e => new { e.Id, e.Version });

            entity.Property(e => e.Id)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            entity.Property(e => e.Version)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired();

            entity.Property(e => e.StepsData)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.Name)
                .HasDatabaseName("IX_WorkflowDefinitions_Name");

            entity.HasIndex(e => new { e.Id, e.IsActive })
                .HasDatabaseName("IX_WorkflowDefinitions_Id_IsActive");
        });
    }
}

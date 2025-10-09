using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Workflows.Infrastructure;

/// <summary>
/// EF Core implementation of IWorkflowDefinitionStore with versioning support.
/// </summary>
public class EfCoreWorkflowDefinitionStore : IWorkflowDefinitionStore
{
    private readonly WorkflowDbContext _context;
    private readonly JsonSerializerOptions _jsonOptions;

    public EfCoreWorkflowDefinitionStore(WorkflowDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async ValueTask<WorkflowDefinition?> GetDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var entity = await _context.WorkflowDefinitions
            .AsNoTracking()
            .Where(d => d.Id == definitionId && d.IsActive)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return entity != null ? MapToDefinition(entity) : null;
    }

    public async ValueTask SaveDefinitionAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));

        var latestVersion = await _context.WorkflowDefinitions
            .Where(d => d.Id == definition.Id)
            .OrderByDescending(d => d.Version)
            .Select(d => (int?)d.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var entity = new WorkflowDefinitionEntity
        {
            Id = definition.Id,
            Name = definition.Name,
            Description = definition.Description,
            Version = (latestVersion ?? 0) + 1,
            IsActive = true,
            StepsData = JsonSerializer.Serialize(definition.Steps, _jsonOptions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Deactivate previous versions
        var previousVersions = await _context.WorkflowDefinitions
            .Where(d => d.Id == definition.Id && d.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var prev in previousVersions)
        {
            prev.IsActive = false;
            prev.UpdatedAt = DateTime.UtcNow;
        }

        await _context.WorkflowDefinitions.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<IEnumerable<WorkflowDefinition>> GetAllDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.WorkflowDefinitions
            .AsNoTracking()
            .Where(d => d.IsActive)
            .GroupBy(d => d.Id)
            .Select(g => g.OrderByDescending(d => d.Version).First())
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDefinition);
    }

    public async ValueTask<bool> DeleteDefinitionAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var definitions = await _context.WorkflowDefinitions
            .Where(d => d.Id == definitionId)
            .ToListAsync(cancellationToken);

        if (!definitions.Any()) return false;

        _context.WorkflowDefinitions.RemoveRange(definitions);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private WorkflowDefinition MapToDefinition(WorkflowDefinitionEntity entity)
    {
        var steps = JsonSerializer.Deserialize<List<WorkflowStep>>(entity.StepsData, _jsonOptions) ?? new List<WorkflowStep>();

        return new WorkflowDefinition
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Steps = steps
        };
    }
}

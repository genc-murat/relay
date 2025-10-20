using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Relay.MessageBroker.Saga.Persistence;

/// <summary>
/// Database-backed saga persistence implementation.
/// </summary>
/// <typeparam name="TSagaData">The type of saga data.</typeparam>
public sealed class DatabaseSagaPersistence<TSagaData> : ISagaPersistence<TSagaData> 
    where TSagaData : ISagaData, new()
{
    private readonly ISagaDbContext _dbContext;
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseSagaPersistence{TSagaData}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    public DatabaseSagaPersistence(ISagaDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <inheritdoc/>
    public async ValueTask SaveAsync(TSagaData data, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Sagas
            .FirstOrDefaultAsync(s => s.SagaId == data.SagaId, cancellationToken);

        if (entity == null)
        {
            // Create new
            entity = new SagaEntityBase
            {
                SagaId = data.SagaId,
                CorrelationId = data.CorrelationId,
                State = data.State,
                CreatedAt = data.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow,
                CurrentStep = data.CurrentStep,
                MetadataJson = JsonSerializer.Serialize(data.Metadata, _jsonOptions),
                DataJson = JsonSerializer.Serialize(data, _jsonOptions),
                SagaType = typeof(TSagaData).FullName ?? typeof(TSagaData).Name,
                Version = 1
            };

            _dbContext.Add(entity);
        }
        else
        {
            // Update existing
            entity.CorrelationId = data.CorrelationId;
            entity.State = data.State;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.CurrentStep = data.CurrentStep;
            entity.MetadataJson = JsonSerializer.Serialize(data.Metadata, _jsonOptions);
            entity.DataJson = JsonSerializer.Serialize(data, _jsonOptions);
            entity.Version++;

            _dbContext.Update(entity);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask<TSagaData?> GetByIdAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Sagas
            .FirstOrDefaultAsync(s => s.SagaId == sagaId, cancellationToken);

        return entity != null ? DeserializeSagaData(entity) : default;
    }

    /// <inheritdoc/>
    public async ValueTask<TSagaData?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Sagas
            .FirstOrDefaultAsync(s => s.CorrelationId == correlationId, cancellationToken);

        return entity != null ? DeserializeSagaData(entity) : default;
    }

    /// <inheritdoc/>
    public async ValueTask DeleteAsync(Guid sagaId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Sagas
            .FirstOrDefaultAsync(s => s.SagaId == sagaId, cancellationToken);

        if (entity != null)
        {
            _dbContext.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TSagaData> GetActiveSagasAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Sagas
            .Where(s => s.State == SagaState.Running || s.State == SagaState.Compensating)
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            var data = DeserializeSagaData(entity);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TSagaData> GetByStateAsync(SagaState state, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Sagas
            .Where(s => s.State == state)
            .ToListAsync(cancellationToken);

        foreach (var entity in entities)
        {
            var data = DeserializeSagaData(entity);
            if (data != null)
            {
                yield return data;
            }
        }
    }

    private TSagaData? DeserializeSagaData(SagaEntityBase entity)
    {
        try
        {
            var data = JsonSerializer.Deserialize<TSagaData>(entity.DataJson, _jsonOptions);
            if (data != null)
            {
                // Ensure core properties are synced
                data.SagaId = entity.SagaId;
                data.CorrelationId = entity.CorrelationId;
                data.State = entity.State;
                data.CreatedAt = entity.CreatedAt;
                data.UpdatedAt = entity.UpdatedAt;
                data.CurrentStep = entity.CurrentStep;

                // Deserialize metadata
                if (!string.IsNullOrWhiteSpace(entity.MetadataJson))
                {
                    data.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.MetadataJson, _jsonOptions) 
                        ?? new Dictionary<string, object>();
                }
            }
            return data;
        }
        catch
        {
            return default;
        }
    }
}

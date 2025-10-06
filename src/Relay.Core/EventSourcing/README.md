# Event Sourcing with EF Core and PostgreSQL

Bu klasör, Relay.Core için EF Core tabanlı Event Sourcing implementasyonunu içerir.

## Bileşenler

### 1. EventEntity
Event'lerin veritabanında saklanması için kullanılan entity sınıfı.

### 2. EventStoreDbContext
EF Core DbContext sınıfı. PostgreSQL veritabanı ile etkileşim için kullanılır.

### 3. EfCoreEventStore
`IEventStore` arayüzünün EF Core tabanlı implementasyonu. Event'leri PostgreSQL veritabanına kaydeder ve okur.

### 4. EventSourcingExtensions
Dependency Injection için extension metodları içerir.

### 5. EventStoreDbContextFactory
EF Core migration araçları için design-time factory.

## Kullanım

### 1. PostgreSQL Bağlantı Ayarları

appsettings.json dosyanıza connection string ekleyin:

```json
{
  "ConnectionStrings": {
    "EventStore": "Host=localhost;Database=relay_events;Username=postgres;Password=postgres"
  }
}
```

### 2. Dependency Injection Yapılandırması

Program.cs veya Startup.cs dosyanızda:

```csharp
using Relay.Core.EventSourcing;

// Connection string ile
builder.Services.AddEfCoreEventStore(
    builder.Configuration.GetConnectionString("EventStore"));

// Veya custom options ile
builder.Services.AddEfCoreEventStore(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("EventStore"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
            npgsqlOptions.CommandTimeout(30);
        }));
```

### 3. Veritabanı Migration

Uygulama başlangıcında veritabanını oluşturmak için:

```csharp
// Program.cs içinde
var app = builder.Build();

// Veritabanını oluştur ve migration'ları uygula
await app.Services.EnsureEventStoreDatabaseAsync();

app.Run();
```

### 4. Event Store Kullanımı

```csharp
public class MyService
{
    private readonly IEventStore _eventStore;

    public MyService(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<Event> events)
    {
        // Event'leri kaydet
        await _eventStore.SaveEventsAsync(aggregateId, events, expectedVersion: -1);
    }

    public async Task<List<Event>> GetEventsAsync(Guid aggregateId)
    {
        // Event'leri oku
        var events = new List<Event>();
        await foreach (var @event in _eventStore.GetEventsAsync(aggregateId))
        {
            events.Add(@event);
        }
        return events;
    }
}
```

## Migration Komutları

### Yeni Migration Oluşturma
```bash
cd src/Relay.Core
dotnet ef migrations add MigrationName --context EventStoreDbContext
```

### Migration Uygulama
```bash
dotnet ef database update --context EventStoreDbContext
```

### Migration Geri Alma
```bash
dotnet ef migrations remove --context EventStoreDbContext
```

### SQL Script Oluşturma
```bash
dotnet ef migrations script --context EventStoreDbContext -o migration.sql
```

## Veritabanı Şeması

### Events Tablosu

| Kolon | Tip | Açıklama |
|-------|-----|----------|
| Id | UUID | Event'in benzersiz kimliği |
| AggregateId | UUID | Aggregate'in kimliği |
| AggregateVersion | Integer | Event'in aggregate versiyonu |
| EventType | String(500) | Event'in tip adı (FullName) |
| EventData | Text | Serialize edilmiş event verisi (JSON) |
| Timestamp | Timestamp | Event'in oluşturulma zamanı |

### İndeksler

- **IX_Events_AggregateId**: AggregateId üzerinde hızlı arama için
- **IX_Events_AggregateId_Version**: Unique constraint ve concurrency kontrolü için

## Özellikler

- ✅ PostgreSQL desteği
- ✅ Optimistic Concurrency Control
- ✅ JSON serialization
- ✅ Async/await desteği
- ✅ IAsyncEnumerable ile streaming
- ✅ Migration desteği
- ✅ Connection pooling
- ✅ Indexed queries

## Notlar

- Event'ler immutable olmalıdır
- EventType, event sınıfının tam adını içerir (Type.FullName)
- Concurrency conflict durumunda `InvalidOperationException` fırlatılır
- JSON serialization için System.Text.Json kullanılır
- PostgreSQL specific özellikleri için Npgsql provider kullanılır

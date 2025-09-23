# Minimal API Example

Bu örnek, Relay framework'ü kullanarak modern .NET Minimal API yaklaşımıyla oluşturulmuş basit bir CRUD API'dir.

## Özellikler

- ✅ **Minimal API** - Controller'sız, modern endpoint mapping
- ✅ **Relay Framework** - Yüksek performanslı mediator pattern
- ✅ **CRUD Operasyonları** - Create, Read, Update, Delete
- ✅ **Request/Response Handlers** - `[Handle]` attribute ile
- ✅ **Event Notifications** - `[Notification]` attribute ile
- ✅ **Dependency Injection** - .NET'in built-in DI sistemi
- ✅ **In-Memory Repository** - Test için basit veri depolama
- ✅ **OpenAPI/Swagger** - Otomatik API dokümantasyonu
- ✅ **Endpoint Grouping** - Organize edilmiş endpoint'ler

## Proje Yapısı

```
minimal-api/
└── src/
    └── MinimalApi/
        ├── Endpoints/
        │   └── UserEndpoints.cs
        ├── Models/
        │   ├── User.cs
        │   └── UserRequests.cs
        ├── Services/
        │   ├── UserService.cs
        │   ├── UserNotificationHandlers.cs
        │   ├── IUserRepository.cs
        │   └── InMemoryUserRepository.cs
        ├── Program.cs
        └── MinimalApi.csproj
```

## API Endpoint'leri

| Method | Endpoint | Açıklama |
|--------|----------|----------|
| GET | `/api/users` | Tüm kullanıcıları listele (sayfalama ile) |
| GET | `/api/users/{id}` | ID'ye göre kullanıcı getir |
| POST | `/api/users` | Yeni kullanıcı oluştur |
| PUT | `/api/users/{id}` | Kullanıcıyı güncelle |
| DELETE | `/api/users/{id}` | Kullanıcıyı sil |

## Çalıştırma

```bash
cd src/MinimalApi
dotnet run
```

Uygulama `http://localhost:5000` adresinde çalışacak.

## Swagger UI

API dokümantasyonu için: `http://localhost:5000/swagger`

## Test Örnekleri

### Kullanıcıları Listele
```bash
curl http://localhost:5000/api/users
```

### Kullanıcı Getir
```bash
curl http://localhost:5000/api/users/1
```

### Yeni Kullanıcı Oluştur
```bash
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Ali Veli","email":"ali@example.com"}'
```

### Kullanıcı Güncelle
```bash
curl -X PUT http://localhost:5000/api/users/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"Ali Yeni","email":"ali.yeni@example.com"}'
```

### Kullanıcı Sil
```bash
curl -X DELETE http://localhost:5000/api/users/1
```

## Minimal API vs Controller Karşılaştırması

### Minimal API Avantajları:
- 🚀 **Daha Az Kod** - Boilerplate kod minimum
- ⚡ **Hızlı Geliştirme** - Endpoint'ler daha hızlı yazılır
- 🎯 **Functional Yaklaşım** - Lambda expressions ile
- 📦 **Lightweight** - Controller overhead'i yok

### Minimal API'de Öne Çıkan Özellikler:

#### 1. Endpoint Grouping
```csharp
var group = routes.MapGroup("/api/users")
    .WithTags("Users")
    .WithOpenApi();
```

#### 2. Inline Handler Functions
```csharp
group.MapGet("/{id:int}", async (int id, IRelay relay, CancellationToken cancellationToken) =>
{
    var user = await relay.SendAsync(new GetUserQuery(id), cancellationToken);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});
```

#### 3. OpenAPI Metadata
```csharp
.WithName("GetUser")
.WithSummary("Get user by ID")
.Produces<User>()
.Produces(404)
```

## Relay Pattern Kullanımı

Minimal API'de Relay framework'ü doğrudan endpoint handler'larında kullanılır:

```csharp
// Request gönderme
var user = await relay.SendAsync(new GetUserQuery(id), cancellationToken);

// Command çalıştırma
await relay.SendAsync(new DeleteUserCommand(id), cancellationToken);

// Notification yayınlama (handler içinde)
await _relay.PublishAsync(new UserCreatedNotification(createdUser), cancellationToken);
```

Bu yaklaşım, geleneksel Controller pattern'e göre daha az kod ve daha yüksek performans sağlar.
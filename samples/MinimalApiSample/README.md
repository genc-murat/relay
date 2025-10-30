# Relay.MessageBroker Minimal API Sample

Bu örnek, Relay.MessageBroker'ın ASP.NET Core Minimal API ile nasıl kullanılacağını gösterir.

## Özellikler

- ✅ Minimal API ile basit ve temiz kod
- ✅ RabbitMQ entegrasyonu
- ✅ Development profili ile hızlı başlangıç
- ✅ Health checks
- ✅ Swagger/OpenAPI dokümantasyonu
- ✅ Connection pooling
- ✅ Metrics desteği

## Gereksinimler

- .NET 8.0 SDK
- RabbitMQ (Docker ile çalıştırılabilir)

## RabbitMQ'yu Başlatma

Docker ile RabbitMQ'yu başlatın:

```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3.13-management-alpine
```

RabbitMQ Management UI: http://localhost:15672 (guest/guest)

## Uygulamayı Çalıştırma

```bash
cd samples/MinimalApiSample
dotnet run
```

Uygulama şu adreste çalışacak: https://localhost:7000 (veya http://localhost:5000)

## API Endpoints

### Swagger UI

Swagger dokümantasyonuna erişin: https://localhost:7000/swagger

### Health Check

```bash
curl http://localhost:5000/health
```

### Mesaj Gönderme

```bash
curl -X POST http://localhost:5000/api/messages/publish \
  -H "Content-Type: application/json" \
  -d '{
    "exchange": "test-exchange",
    "routingKey": "test.key",
    "message": "Hello from Minimal API!"
  }'
```

### Mesaj Dinleme

```bash
curl -X POST http://localhost:5000/api/messages/subscribe \
  -H "Content-Type: application/json" \
  -d '{
    "exchange": "test-exchange",
    "routingKey": "test.key"
  }'
```

### Broker Bilgisi

```bash
curl http://localhost:5000/api/messages/info
```

## Yapılandırma

`appsettings.json` dosyasında RabbitMQ ayarlarını değiştirebilirsiniz:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

## Profiller

Bu örnek **Development** profilini kullanır. Farklı profiller için:

### Production Profili

```csharp
builder.Services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.Production,
    options => { /* ... */ });
```

Production profili şunları içerir:
- Outbox pattern
- Inbox pattern
- Connection pooling (5-50 bağlantı)
- Message deduplication
- Health checks
- Metrics
- Distributed tracing
- Bulkhead pattern
- Poison message handling
- Backpressure management

### High Throughput Profili

```csharp
builder.Services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.HighThroughput,
    options => { /* ... */ });
```

Yüksek performans için optimize edilmiş:
- Connection pooling (10-100 bağlantı)
- Batch processing (1000 mesaj, 50ms flush)
- Message deduplication
- Backpressure management

## Özel Yapılandırma

Fluent API ile özel yapılandırma:

```csharp
builder.Services.AddMessageBrokerWithPatterns(options =>
{
    options.BrokerType = MessageBrokerType.RabbitMQ;
    options.RabbitMQ = new RabbitMQOptions
    {
        HostName = "localhost",
        Port = 5672
    };
})
.WithOutbox()
.WithInbox()
.WithConnectionPool()
.WithBatching()
.WithDeduplication()
.WithHealthChecks()
.WithMetrics()
.Build();
```

## Test Senaryosu

1. Uygulamayı başlatın
2. RabbitMQ'nun çalıştığından emin olun
3. Swagger UI'ı açın: https://localhost:7000/swagger
4. "Subscribe" endpoint'ini kullanarak bir exchange'e abone olun
5. "Publish" endpoint'ini kullanarak mesaj gönderin
6. Uygulama loglarında mesajın alındığını görün

## Daha Fazla Bilgi

- [Fluent Configuration Guide](../../docs/MessageBroker/FLUENT_CONFIGURATION.md)
- [Getting Started Guide](../../docs/MessageBroker/GETTING_STARTED.md)
- [Best Practices](../../docs/MessageBroker/BEST_PRACTICES.md)
- [Complete Examples](../../docs/MessageBroker/examples/)

## Sorun Giderme

### RabbitMQ'ya bağlanamıyor

```bash
# RabbitMQ'nun çalıştığını kontrol edin
docker ps | grep rabbitmq

# RabbitMQ loglarını kontrol edin
docker logs rabbitmq
```

### Port zaten kullanımda

`launchSettings.json` dosyasında portları değiştirin veya:

```bash
dotnet run --urls "http://localhost:5001"
```

## Lisans

MIT License - Detaylar için [LICENSE](../../LICENSE) dosyasına bakın.

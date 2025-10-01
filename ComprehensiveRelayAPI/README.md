# 🚀 Comprehensive Relay API - Tüm Özellikleri Gösteren Demo Projesi

Bu proje, **Relay Framework**'ün tüm özelliklerini kapsamlı bir şekilde gösteren enterprise-level bir Minimal API projesidir.

## 📋 Proje Özellikleri

### ✅ Kullanılan Relay Özellikleri

1. **🔄 Request/Response Handling**
   - `IRequest<T>` ve `IRequest` arayüzleri
   - `IRequestHandler<TRequest, TResponse>` implementasyonları
   - Async ValueTask tabanlı performant işlemler

2. **📡 Streaming Support**
   - `IStreamRequest<T>` ile async enumerable desteği
   - `IStreamHandler<TRequest, TResponse>` implementasyonları
   - Real-time veri akışı (User Activity Stream)

3. **📢 Notification System**
   - `INotification` arayüzü ile event publishing
   - `INotificationHandler<TNotification>` ile event handling
   - Parallel ve Sequential notification processing
   - Multiple handlers per notification type

4. **🔧 Pipeline Behaviors**
   - **Validation Pipeline**: FluentValidation entegrasyonu
   - **Logging Pipeline**: Comprehensive request/response logging
   - **Caching Pipeline**: Memory caching with intelligent key generation
   - **Exception Handling Pipeline**: Global exception handling
   - **Performance Monitoring Pipeline**: Request timing and metrics

5. **📊 Enterprise Features**
   - **Health Checks**: Relay health monitoring
   - **OpenTelemetry**: Distributed tracing support
   - **Serilog Integration**: Structured logging
   - **Memory Caching**: Response caching with TTL
   - **CORS Support**: Cross-origin resource sharing

### 🏗️ Proje Yapısı

```
ComprehensiveRelayAPI/
├── Models/                    # Domain modelleri (User, Product, Order)
├── Requests/                  # Request/Response DTOs ve Validators
├── Handlers/                  # Request/Notification Handlers
├── Services/                  # Business Services (DataService)
├── Pipeline/                  # Pipeline Behaviors
├── Configuration/             # Relay configuration ve extensions
└── Program.cs                 # Minimal API endpoints ve configuration
```

### 🎯 API Endpoints

#### 👥 User Management
- `GET /api/users/{id}` - Kullanıcı detayı (cached)
- `GET /api/users` - Kullanıcı listesi (paginated)
- `POST /api/users` - Yeni kullanıcı oluşturma (with notifications)
- `PUT /api/users/{id}` - Kullanıcı güncelleme
- `DELETE /api/users/{id}` - Kullanıcı silme

#### 🛍️ Product Management
- `GET /api/products/{id}` - Ürün detayı (cached)
- `GET /api/products` - Ürün listesi (filtered)
- `POST /api/products` - Yeni ürün oluşturma

#### 📦 Order Management
- `GET /api/orders/{id}` - Sipariş detayı
- `POST /api/orders` - Yeni sipariş oluşturma (with notifications)

#### 🔧 System Endpoints
- `GET /` - API bilgisi ve Swagger UI
- `GET /health` - System health check
- `GET /api/performance-test` - Performance benchmarking

### 📊 Notification Flows

#### User Created Event
```
CreateUserCommand → UserCreatedNotification
    ├── 📧 Email Service (Welcome email)
    ├── 📊 Analytics Service (User metrics)
    └── 📝 Audit Service (Audit log)
```

#### Order Created Event
```
CreateOrderCommand → OrderCreatedNotification
    ├── 📦 Inventory Service (Stock update)
    ├── 💳 Payment Service (Payment processing)
    └── 📧 Email Service (Order confirmation)
```

#### Order Status Changed Event
```
UpdateOrderStatusCommand → OrderStatusChangedNotification
    ├── 📱 Customer Service (Notification)
    ├── 🚚 Logistics Service (Shipping update)
    └── 📊 Analytics Service (Status metrics)
```

### 🛠️ Kullanılan Teknolojiler

- **Framework**: ASP.NET Core 8.0 Minimal APIs
- **Mediator**: Relay Framework (Ultra-high performance)
- **Validation**: FluentValidation
- **Logging**: Serilog with file and console sinks
- **Caching**: Microsoft.Extensions.Caching.Memory
- **Tracing**: OpenTelemetry
- **Documentation**: Swagger/OpenAPI
- **Health Checks**: Microsoft.Extensions.Diagnostics.HealthChecks

### 🚀 Çalıştırma

```bash
# Proje dizinine git
cd ComprehensiveRelayAPI

# Bağımlılıkları yükle
dotnet restore

# Projeyi build et
dotnet build

# Projeyi çalıştır
dotnet run

# API'ye erişim
# Swagger UI: https://localhost:7156 veya http://localhost:5268
# Health Check: http://localhost:5268/health
```

### 📋 Test Scenarios

#### 1. User Management Test
```bash
# Yeni kullanıcı oluştur
curl -X POST "http://localhost:5268/api/users" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Murat Genç",
    "email": "murat@example.com",
    "phoneNumber": "+905551234567",
    "roles": ["Admin", "User"]
  }'

# Kullanıcı listesini getir
curl "http://localhost:5268/api/users?pageSize=5"

# Specific kullanıcı getir (cached)
curl "http://localhost:5268/api/users/1"
```

#### 2. Product Management Test
```bash
# Yeni ürün oluştur
curl -X POST "http://localhost:5268/api/products" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Gaming Laptop",
    "description": "High-performance gaming laptop",
    "price": 25000,
    "stock": 10,
    "category": "Electronics"
  }'

# Ürün listesini filtreli getir
curl "http://localhost:5268/api/products?category=Electronics&minPrice=1000"
```

#### 3. Order Management Test
```bash
# Yeni sipariş oluştur
curl -X POST "http://localhost:5268/api/orders" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "items": [
      {"productId": 1, "quantity": 2},
      {"productId": 2, "quantity": 1}
    ],
    "notes": "Urgent delivery requested"
  }'
```

#### 4. Performance Test
```bash
# Performance benchmark çalıştır
curl "http://localhost:5268/api/performance-test?iterations=1000"
```

### 📈 Beklenen Sonuçlar

1. **Notifications**: Her user/order oluşturulduğunda paralel notification işlemleri
2. **Caching**: User ve Product queries'ler cache'lenir
3. **Validation**: Tüm input'lar FluentValidation ile validate edilir
4. **Logging**: Her request detaylı olarak loglanır
5. **Performance**: Relay'in high-performance karakteristikleri gözlenir
6. **Health**: System health durumu izlenir

### 🎯 Bu Proje Ne Gösterir?

✅ **Complete Relay Integration**: Tüm Relay feature'ları tek projede
✅ **Enterprise Patterns**: Production-ready patterns ve practices
✅ **Performance Focus**: High-performance async patterns
✅ **Comprehensive Logging**: Structured logging ve monitoring
✅ **Error Handling**: Global exception handling
✅ **Validation**: Input validation ve error responses
✅ **Caching Strategy**: Intelligent caching implementation
✅ **Event-Driven Architecture**: Notification-based decoupling
✅ **API Documentation**: OpenAPI/Swagger integration
✅ **Health Monitoring**: Application health checks

Bu proje, Relay Framework'ün enterprise-level uygulamalarda nasıl kullanılabileceğinin kapsamlı bir örneğidir.
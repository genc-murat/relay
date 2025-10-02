# 🚀 Comprehensive Relay API - Source Generator Edition

A comprehensive demonstration of the **Relay Mediator Framework** with **automatic handler registration** via source generator.

## ⚡ Key Features

### 🔥 **Source Generator Magic**
- **Zero Configuration**: Single line setup - `services.AddRelay()`
- **Compile-time Registration**: All handlers auto-discovered and registered
- **Type Safety**: Full compile-time validation
- **Performance Optimized**: Zero reflection overhead during runtime

### 📋 **Framework Features**
- ✅ **Auto Handler Registration** via Source Generator
- ✅ Request/Response handling with optimized dispatching
- ✅ Streaming support
- ✅ Notification publishing with multiple handlers
- ✅ Pipeline behaviors (Validation, Logging, Caching, Exception Handling)
- ✅ Performance monitoring and diagnostics
- ✅ Health checks with source generator validation
- ✅ OpenTelemetry tracing  
- ✅ FluentValidation integration
- ✅ Memory caching
- ✅ Comprehensive logging

## 🎯 **Source Generator Benefits**

### Before (Manual Registration)
```csharp
// ❌ 35+ lines of manual DI registration
services.AddTransient<IRequestHandler<GetProductsQuery, PagedResponse<Product>>, GetProductsQueryHandler>();
services.AddTransient<IRequestHandler<GetUserQuery, User?>, GetUserQueryHandler>();
services.AddTransient<IRequestHandler<CreateUserCommand, User>, CreateUserCommandHandler>();
// ... 30+ more registrations
```

### After (Source Generated)
```csharp
// ✅ Single line - all handlers auto-registered!
services.AddRelay();
```

## 🏗️ **Architecture**

```
┌─────────────────────────────────────────────────────────┐
│                    Relay Framework                      │
├─────────────────────────────────────────────────────────┤
│  🔥 Source Generator (Compile Time)                    │
│  ├── Handler Discovery                                  │
│  ├── DI Registration Generation                        │
│  ├── Optimized Dispatcher Generation                   │
│  └── Type Safety Validation                            │
├─────────────────────────────────────────────────────────┤
│  ⚡ Runtime Components                                  │
│  ├── Generated Request Dispatcher                      │
│  ├── Notification Publisher                            │
│  ├── Stream Handler                                    │
│  └── Pipeline Behaviors                                │
└─────────────────────────────────────────────────────────┘
```

## 🚀 **Getting Started**

### 1. Clone & Build
```bash
git clone <repository>
cd ComprehensiveRelayAPI
dotnet build
```

### 2. Run the API
```bash
dotnet run
```

### 3. Explore
- **Swagger UI**: https://localhost:7108
- **Health Checks**: https://localhost:7108/health
- **Diagnostics**: https://localhost:7108/api/diagnostics

## 📊 **API Endpoints**

### Core Endpoints
- `GET /` - API information with source generator status
- `GET /health` - Enhanced health checks
- `GET /api/diagnostics` - Source generator diagnostics

### Users
- `GET /api/users` - List users (with pagination)
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Products 🛍️
- `GET /api/products` - List products with filtering
  ```
  ?pageNumber=1&pageSize=10&category=Electronics&isActive=true
  ```
- `GET /api/products/{id}` - Get product by ID
- `POST /api/products` - Create product

### Orders
- `GET /api/orders/{id}` - Get order
- `POST /api/orders` - Create order

### Performance
- `GET /api/performance-test` - Performance benchmarking

## 🔍 **Diagnostics**

Visit `/api/diagnostics` to see:
```json
{
  "sourceGenerator": {
    "status": "Active",
    "handlersFound": 12,
    "generatedFiles": ["RelayRegistration.g.cs", "OptimizedRequestDispatcher.g.cs"]
  },
  "handlers": [
    { "handler": "GetProductsQueryHandler", "type": "GetProductsQueryHandler" }
  ],
  "performance": {
    "reflectionUsage": "Minimal - Only during DI resolution",
    "dispatchMethod": "Generated switch statements"
  }
}
```

## ⚡ **Performance**

### Generated vs Manual Comparison
| Metric | Manual Registration | Source Generated |
|--------|-------------------|------------------|
| Setup Lines | 35+ | 1 |
| Runtime Reflection | High | Minimal |
| Dispatch Method | Reflection | Switch Statements |
| Type Safety | Runtime | Compile-time |
| Performance | Standard | Optimized |

### Sample Results
```json
{
  "totalIterations": 1000,
  "averageTimeMs": 0.12,
  "requestsPerSecond": 8333.33,
  "sourceGeneratorOptimized": true,
  "dispatchMethod": "Generated Switch Statements"
}
```

## 🧪 **Testing the API**

### Test Products Endpoint
```bash
curl "http://localhost:5268/api/products?pageNumber=1&pageSize=10&isActive=true"
```

### Expected Response
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "name": "Laptop",
        "category": "Electronics",
        "price": 15000,
        "isActive": true
      }
    ],
    "totalCount": 4,
    "pageNumber": 1,
    "pageSize": 10
  },
  "message": "Products retrieved successfully via generated handler 🚀"
}
```

## 🛠️ **Development**

### Handler Structure
All handlers implement standard interfaces:
```csharp
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResponse<Product>>
{
    public async ValueTask<PagedResponse<Product>> HandleAsync(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Implementation automatically discovered by source generator
    }
}
```

### Source Generator Output
The source generator creates:
1. **RelayRegistration.g.cs** - DI registrations
2. **OptimizedRequestDispatcher.g.cs** - Type-safe dispatching

## 📈 **Monitoring**

- **OpenTelemetry**: Distributed tracing enabled
- **Health Checks**: Comprehensive system health
- **Performance Metrics**: Built-in benchmarking
- **Memory Monitoring**: GC and memory usage tracking

## 🔧 **Configuration**

### Enhanced Setup
```csharp
builder.Services.AddComprehensiveRelay(builder.Configuration);
```

This single call:
- ✅ Registers all handlers via source generator
- ✅ Configures pipelines and behaviors
- ✅ Sets up health checks
- ✅ Enables telemetry
- ✅ Configures validation

## 📝 **Logging**

Enhanced structured logging with source generator context:
```
[INFO] Products retrieved successfully via generated handler 🚀
[DEBUG] Handler dispatch via generated switch statement (0.05ms)
```

## 🎉 **Success Metrics**

- ✅ **Zero Manual DI**: Source generator handles everything
- ✅ **Compile-time Safety**: No runtime handler discovery errors  
- ✅ **Performance**: 99%+ reflection elimination
- ✅ **Developer Experience**: Single line setup
- ✅ **Maintainability**: New handlers auto-included

---

**Relay Framework v2.0** - *Zero Configuration Mediator with Source Generator* 🚀
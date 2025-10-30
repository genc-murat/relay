# Sample Applications

This directory contains complete sample applications demonstrating Relay.MessageBroker enhancements in real-world scenarios.

## Available Samples

### 1. Minimal API Sample
**Location:** `samples/MinimalApiSample/`

A minimal ASP.NET Core Web API demonstrating basic Relay.MessageBroker usage:
- Simple publish/subscribe endpoints
- RabbitMQ integration
- Development profile configuration
- Health checks
- Swagger documentation

**Quick Start:**
```bash
cd samples/MinimalApiSample
docker-compose up -d  # Start RabbitMQ
dotnet run
```

**Features Demonstrated:**
- ✅ Fluent Configuration API
- ✅ Development Profile
- ✅ Connection Pooling
- ✅ Health Checks
- ✅ Metrics
- ✅ Minimal API endpoints

**Perfect for:** Learning the basics, quick prototyping, getting started

---

### 2. E-Commerce Order Processing (Microservices)
**Location:** `samples/ECommerce/`

A complete e-commerce order processing system demonstrating:
- Order Service (Outbox pattern)
- Payment Service (Inbox pattern, encryption)
- Inventory Service (rate limiting, bulkhead)
- Shipping Service (backpressure management)
- Notification Service (batch processing)

**Architecture:**
```
┌─────────────┐     ┌──────────────┐     ┌───────────────┐
│   Order     │────>│   Payment    │────>│   Inventory   │
│   Service   │     │   Service    │     │   Service     │
└─────────────┘     └──────────────┘     └───────────────┘
       │                    │                     │
       └────────────────────┴─────────────────────┘
                            │
                    ┌───────▼────────┐
                    │   RabbitMQ     │
                    └────────────────┘
                            │
                    ┌───────▼────────┐
                    │   Shipping &   │
                    │  Notification  │
                    └────────────────┘
```

**Features Demonstrated:**
- ✅ Outbox Pattern (Order Service)
- ✅ Inbox Pattern (Payment, Inventory Services)
- ✅ Message Encryption (Payment Service)
- ✅ Authentication & Authorization
- ✅ Rate Limiting (per-tenant)
- ✅ Distributed Tracing (Jaeger)
- ✅ Health Checks
- ✅ Prometheus Metrics
- ✅ Connection Pooling
- ✅ Batch Processing (Notification Service)
- ✅ Bulkhead Pattern (Inventory Service)
- ✅ Poison Message Handling
- ✅ Backpressure Management (Shipping Service)

**Run:**
```bash
cd samples/ECommerce
docker-compose up -d
dotnet run --project OrderService
dotnet run --project PaymentService
dotnet run --project InventoryService
dotnet run --project ShippingService
dotnet run --project NotificationService
```

**Test:**
```bash
# Create an order
curl -X POST http://localhost:5001/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": 123, "items": [{"productId": 1, "quantity": 2}]}'

# Check order status
curl http://localhost:5001/orders/1

# View metrics
curl http://localhost:5001/metrics

# View traces in Jaeger
open http://localhost:16686
```

### 2. Order Processing Saga
**Location:** `samples/OrderSaga/`

Demonstrates the Saga pattern for distributed transactions:
- Order creation
- Payment processing
- Inventory reservation
- Shipping arrangement
- Compensation on failure

**Saga Flow:**
```
Create Order → Process Payment → Reserve Inventory → Arrange Shipping
     ↓              ↓                  ↓                   ↓
  Success        Success            Success             Success
                                                           ↓
                                                      Complete

If any step fails:
     ↓              ↓                  ↓                   ↓
  Rollback    Refund Payment    Release Inventory    Cancel Shipping
```

**Features Demonstrated:**
- ✅ Saga Pattern
- ✅ Compensation Logic
- ✅ Outbox Pattern (for saga state)
- ✅ Distributed Tracing
- ✅ Event Sourcing

**Run:**
```bash
cd samples/OrderSaga
docker-compose up -d
dotnet run --project SagaOrchestrator
```

### 3. Monitoring Dashboard (Grafana)
**Location:** `samples/MonitoringDashboard/`

Pre-configured Grafana dashboards for monitoring MessageBroker:
- Message throughput
- Latency percentiles (p50, p95, p99)
- Error rates
- Connection pool metrics
- Batch processing statistics
- Circuit breaker state
- Rate limiting metrics

**Dashboards:**
1. **Overview Dashboard** - High-level system health
2. **Performance Dashboard** - Throughput and latency metrics
3. **Reliability Dashboard** - Error rates, retries, circuit breaker
4. **Security Dashboard** - Authentication, rate limiting
5. **Resource Dashboard** - Connection pool, memory, CPU

**Setup:**
```bash
cd samples/MonitoringDashboard
docker-compose up -d

# Import dashboards
./import-dashboards.sh

# Open Grafana
open http://localhost:3000
```

### 4. Load Testing (K6)
**Location:** `samples/LoadTesting/`

K6 load testing scripts for performance validation:
- Throughput testing
- Latency testing
- Stress testing
- Spike testing
- Soak testing

**Tests:**
```javascript
// throughput-test.js - Test message throughput
// latency-test.js - Test message latency
// stress-test.js - Test system under stress
// spike-test.js - Test sudden traffic spikes
// soak-test.js - Test long-running stability
```

**Run:**
```bash
cd samples/LoadTesting

# Throughput test
k6 run throughput-test.js

# Latency test
k6 run latency-test.js

# Stress test
k6 run stress-test.js

# Generate HTML report
k6 run --out json=results.json throughput-test.js
k6-reporter results.json
```

## Sample Application Structure

### E-Commerce Sample Structure

```
samples/ECommerce/
├── docker-compose.yml
├── README.md
├── OrderService/
│   ├── OrderService.csproj
│   ├── Program.cs
│   ├── Controllers/
│   │   └── OrdersController.cs
│   ├── Services/
│   │   └── OrderService.cs
│   ├── Models/
│   │   ├── Order.cs
│   │   └── OrderCreatedEvent.cs
│   └── appsettings.json
├── PaymentService/
│   ├── PaymentService.csproj
│   ├── Program.cs
│   ├── Services/
│   │   └── PaymentProcessor.cs
│   ├── Models/
│   │   └── PaymentProcessedEvent.cs
│   └── appsettings.json
├── InventoryService/
│   ├── InventoryService.csproj
│   ├── Program.cs
│   ├── Services/
│   │   └── InventoryManager.cs
│   └── appsettings.json
├── ShippingService/
│   ├── ShippingService.csproj
│   ├── Program.cs
│   ├── Services/
│   │   └── ShippingCoordinator.cs
│   └── appsettings.json
└── NotificationService/
    ├── NotificationService.csproj
    ├── Program.cs
    ├── Services/
    │   └── NotificationSender.cs
    └── appsettings.json
```

### Docker Compose Configuration

```yaml
# docker-compose.yml
version: '3.8'

services:
  # Message Broker
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest

  # Databases
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports:
      - "1433:1433"
    environment:
      ACCEPT_EULA: Y
      SA_PASSWORD: YourStrong@Passw0rd

  postgres:
    image: postgres:15
    ports:
      - "5432:5432"
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: ecommerce

  # Observability
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "6831:6831/udp"
      - "16686:16686"
    environment:
      COLLECTOR_ZIPKIN_HOST_PORT: :9411

  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      GF_SECURITY_ADMIN_PASSWORD: admin
      GF_INSTALL_PLUGINS: grafana-piechart-panel
    volumes:
      - ./grafana/dashboards:/etc/grafana/provisioning/dashboards
      - ./grafana/datasources:/etc/grafana/provisioning/datasources

  # Services
  order-service:
    build: ./OrderService
    ports:
      - "5001:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Orders;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
      - RabbitMQ__HostName=rabbitmq
    depends_on:
      - rabbitmq
      - sqlserver
      - jaeger

  payment-service:
    build: ./PaymentService
    ports:
      - "5002:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=Payments;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True
      - RabbitMQ__HostName=rabbitmq
    depends_on:
      - rabbitmq
      - sqlserver
      - jaeger

  inventory-service:
    build: ./InventoryService
    ports:
      - "5003:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=postgres;Database=inventory;Username=postgres;Password=postgres
      - RabbitMQ__HostName=rabbitmq
    depends_on:
      - rabbitmq
      - postgres
      - jaeger

  shipping-service:
    build: ./ShippingService
    ports:
      - "5004:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - RabbitMQ__HostName=rabbitmq
    depends_on:
      - rabbitmq
      - jaeger

  notification-service:
    build: ./NotificationService
    ports:
      - "5005:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - RabbitMQ__HostName=rabbitmq
    depends_on:
      - rabbitmq
      - jaeger
```

## Running the Samples

### Prerequisites

1. Install required software:
   ```bash
   # .NET SDK
   dotnet --version  # Should be 8.0 or later

   # Docker
   docker --version
   docker-compose --version

   # K6 (for load testing)
   brew install k6  # macOS
   # or download from https://k6.io/
   ```

2. Clone the repository:
   ```bash
   git clone https://github.com/your-org/relay.git
   cd relay/samples
   ```

### Quick Start

1. Start infrastructure:
   ```bash
   cd ECommerce
   docker-compose up -d
   ```

2. Wait for services to be ready:
   ```bash
   # Check RabbitMQ
   curl http://localhost:15672

   # Check Jaeger
   curl http://localhost:16686

   # Check Prometheus
   curl http://localhost:9090

   # Check Grafana
   curl http://localhost:3000
   ```

3. Run migrations:
   ```bash
   cd OrderService
   dotnet ef database update

   cd ../PaymentService
   dotnet ef database update

   cd ../InventoryService
   dotnet ef database update
   ```

4. Start services:
   ```bash
   # Terminal 1
   cd OrderService
   dotnet run

   # Terminal 2
   cd PaymentService
   dotnet run

   # Terminal 3
   cd InventoryService
   dotnet run

   # Terminal 4
   cd ShippingService
   dotnet run

   # Terminal 5
   cd NotificationService
   dotnet run
   ```

5. Test the system:
   ```bash
   # Create an order
   curl -X POST http://localhost:5001/api/orders \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer your-jwt-token" \
     -d '{
       "customerId": 123,
       "items": [
         {"productId": 1, "quantity": 2, "price": 29.99},
         {"productId": 2, "quantity": 1, "price": 49.99}
       ]
     }'

   # Check order status
   curl http://localhost:5001/api/orders/1

   # View all orders
   curl http://localhost:5001/api/orders
   ```

6. Monitor the system:
   ```bash
   # View metrics
   curl http://localhost:5001/metrics
   curl http://localhost:5002/metrics
   curl http://localhost:5003/metrics

   # View health
   curl http://localhost:5001/health
   curl http://localhost:5002/health
   curl http://localhost:5003/health

   # View traces in Jaeger
   open http://localhost:16686

   # View dashboards in Grafana
   open http://localhost:3000
   ```

## Testing Scenarios

### Scenario 1: Happy Path
Test successful order processing through all services.

```bash
# Create order
ORDER_ID=$(curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": 123, "items": [{"productId": 1, "quantity": 1}]}' \
  | jq -r '.orderId')

# Wait for processing
sleep 5

# Verify order completed
curl http://localhost:5001/api/orders/$ORDER_ID | jq '.status'
# Expected: "Completed"
```

### Scenario 2: Payment Failure
Test compensation when payment fails.

```bash
# Create order with invalid payment
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": 123, "items": [{"productId": 1, "quantity": 1}], "paymentMethod": "invalid"}'

# Verify order cancelled
curl http://localhost:5001/api/orders/$ORDER_ID | jq '.status'
# Expected: "Cancelled"
```

### Scenario 3: Inventory Shortage
Test handling when inventory is insufficient.

```bash
# Create order with large quantity
curl -X POST http://localhost:5001/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": 123, "items": [{"productId": 1, "quantity": 10000}]}'

# Verify order failed
curl http://localhost:5001/api/orders/$ORDER_ID | jq '.status'
# Expected: "Failed"
```

### Scenario 4: Rate Limiting
Test rate limiting with multiple requests.

```bash
# Send many requests quickly
for i in {1..100}; do
  curl -X POST http://localhost:5001/api/orders \
    -H "Content-Type: application/json" \
    -H "X-Tenant-Id: tenant-basic" \
    -d '{"customerId": 123, "items": [{"productId": 1, "quantity": 1}]}' &
done

# Some requests should be rate limited
# Check logs for "Rate limit exceeded" messages
```

### Scenario 5: Load Testing
Test system under load.

```bash
cd LoadTesting

# Run throughput test
k6 run throughput-test.js

# Expected output:
# checks.........................: 100.00% ✓ 10000 ✗ 0
# data_received..................: 1.2 MB  120 kB/s
# data_sent......................: 1.0 MB  100 kB/s
# http_req_duration..............: avg=50ms min=10ms med=45ms max=200ms p(90)=80ms p(95)=100ms
# http_reqs......................: 10000   1000/s
```

## Monitoring and Observability

### Grafana Dashboards

1. **Overview Dashboard**
   - System health status
   - Message throughput
   - Error rate
   - Active connections

2. **Performance Dashboard**
   - Message latency (p50, p95, p99)
   - Throughput by service
   - Batch processing metrics
   - Connection pool utilization

3. **Reliability Dashboard**
   - Circuit breaker state
   - Retry attempts
   - Poison message count
   - Outbox/Inbox metrics

4. **Security Dashboard**
   - Authentication success/failure rate
   - Rate limiting metrics
   - Encryption overhead
   - Authorization denials

### Prometheus Queries

```promql
# Message throughput
rate(relay_messages_published_total[5m])

# Message latency (95th percentile)
histogram_quantile(0.95, rate(relay_message_publish_duration_bucket[5m]))

# Error rate
rate(relay_messages_failed_total[5m]) / rate(relay_messages_published_total[5m])

# Connection pool utilization
relay_connection_pool_active / relay_connection_pool_max

# Circuit breaker state
relay_circuit_breaker_state

# Rate limit rejections
rate(relay_rate_limit_rejected_total[5m])
```

### Jaeger Traces

View distributed traces to debug issues:

1. Open Jaeger UI: http://localhost:16686
2. Select service: "OrderService"
3. Find traces with errors
4. Analyze span details
5. Identify bottlenecks

## Troubleshooting

### Services Not Starting

```bash
# Check Docker containers
docker-compose ps

# Check logs
docker-compose logs rabbitmq
docker-compose logs sqlserver
docker-compose logs postgres

# Restart services
docker-compose restart
```

### Messages Not Being Processed

```bash
# Check RabbitMQ queues
curl -u guest:guest http://localhost:15672/api/queues

# Check service logs
docker-compose logs order-service
docker-compose logs payment-service

# Check health endpoints
curl http://localhost:5001/health
curl http://localhost:5002/health
```

### Performance Issues

```bash
# Check metrics
curl http://localhost:5001/metrics | grep relay_

# Check connection pool
curl http://localhost:5001/metrics | grep connection_pool

# Check batch processing
curl http://localhost:5001/metrics | grep batch

# View Grafana dashboards
open http://localhost:3000
```

## Additional Resources

- [Getting Started Guide](../GETTING_STARTED.md)
- [Configuration Guide](../CONFIGURATION.md)
- [Best Practices](../BEST_PRACTICES.md)
- [Troubleshooting Guide](../TROUBLESHOOTING.md)
- [Code Examples](../examples/)

## Contributing

Want to add a new sample application? Please see [CONTRIBUTING.md](../../../CONTRIBUTING.md).

## License

MIT License - see [LICENSE](../../../LICENSE) for details.

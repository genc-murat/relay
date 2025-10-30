# Docker Deployment Guide

This guide covers deploying Relay.MessageBroker applications using Docker and Docker Compose.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Docker Compose Setup](#docker-compose-setup)
- [Application Dockerfile](#application-dockerfile)
- [Environment Configuration](#environment-configuration)
- [Running the Stack](#running-the-stack)
- [Monitoring](#monitoring)
- [Troubleshooting](#troubleshooting)

## Prerequisites

- Docker 24.0+ installed
- Docker Compose 2.20+ installed
- Basic understanding of Docker concepts

## Docker Compose Setup

### Complete Stack with RabbitMQ

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  # RabbitMQ Message Broker
  rabbitmq:
    image: rabbitmq:3.13-management-alpine
    container_name: relay-rabbitmq
    ports:
      - "5672:5672"   # AMQP port
      - "15672:15672" # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASSWORD:-admin123}
      RABBITMQ_DEFAULT_VHOST: /
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - relay-network

  # PostgreSQL for Outbox/Inbox
  postgres:
    image: postgres:16-alpine
    container_name: relay-postgres
    ports:
      - "5432:5432"
    environment:
      POSTGRES_DB: relay
      POSTGRES_USER: relay
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-relay123}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init-db.sql:/docker-entrypoint-initdb.d/init-db.sql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U relay"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - relay-network

  # Redis for caching/deduplication
  redis:
    image: redis:7-alpine
    container_name: relay-redis
    ports:
      - "6379:6379"
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - relay-network

  # Jaeger for distributed tracing
  jaeger:
    image: jaegertracing/all-in-one:1.54
    container_name: relay-jaeger
    ports:
      - "6831:6831/udp" # Jaeger agent
      - "16686:16686"   # Jaeger UI
      - "4317:4317"     # OTLP gRPC
      - "4318:4318"     # OTLP HTTP
    environment:
      COLLECTOR_OTLP_ENABLED: "true"
    networks:
      - relay-network

  # Prometheus for metrics
  prometheus:
    image: prom/prometheus:v2.49.1
    container_name: relay-prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/usr/share/prometheus/console_libraries'
      - '--web.console.templates=/usr/share/prometheus/consoles'
    networks:
      - relay-network

  # Grafana for visualization
  grafana:
    image: grafana/grafana:10.3.1
    container_name: relay-grafana
    ports:
      - "3000:3000"
    environment:
      GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_PASSWORD:-admin}
      GF_INSTALL_PLUGINS: grafana-piechart-panel
    volumes:
      - grafana_data:/var/lib/grafana
      - ./grafana/dashboards:/etc/grafana/provisioning/dashboards
      - ./grafana/datasources:/etc/grafana/provisioning/datasources
    depends_on:
      - prometheus
    networks:
      - relay-network

  # Your Application
  app:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: relay-app
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      
      # RabbitMQ Configuration
      RabbitMQ__HostName: rabbitmq
      RabbitMQ__Port: 5672
      RabbitMQ__UserName: admin
      RabbitMQ__Password: ${RABBITMQ_PASSWORD:-admin123}
      
      # Database Configuration
      ConnectionStrings__Outbox: "Host=postgres;Database=relay;Username=relay;Password=${POSTGRES_PASSWORD:-relay123}"
      ConnectionStrings__Inbox: "Host=postgres;Database=relay;Username=relay;Password=${POSTGRES_PASSWORD:-relay123}"
      
      # Redis Configuration
      Redis__ConnectionString: "redis:6379"
      
      # Tracing Configuration
      Tracing__ServiceName: relay-app
      Tracing__OtlpEndpoint: http://jaeger:4317
      
      # Metrics Configuration
      Metrics__PrometheusPort: 9090
    depends_on:
      rabbitmq:
        condition: service_healthy
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - relay-network

volumes:
  rabbitmq_data:
  postgres_data:
  redis_data:
  prometheus_data:
  grafana_data:

networks:
  relay-network:
    driver: bridge
```

### Prometheus Configuration

Create `prometheus.yml`:

```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'relay-app'
    static_configs:
      - targets: ['app:8080']
    metrics_path: '/metrics'
```

### Database Initialization

Create `init-db.sql`:

```sql
-- Create Outbox table
CREATE TABLE IF NOT EXISTS outbox_messages (
    id UUID PRIMARY KEY,
    message_type VARCHAR(255) NOT NULL,
    payload BYTEA NOT NULL,
    headers JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    published_at TIMESTAMP WITH TIME ZONE,
    status VARCHAR(50) NOT NULL,
    retry_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT
);

CREATE INDEX idx_outbox_status ON outbox_messages(status);
CREATE INDEX idx_outbox_created_at ON outbox_messages(created_at);

-- Create Inbox table
CREATE TABLE IF NOT EXISTS inbox_messages (
    message_id VARCHAR(255) PRIMARY KEY,
    message_type VARCHAR(255) NOT NULL,
    processed_at TIMESTAMP WITH TIME ZONE NOT NULL,
    consumer_name VARCHAR(255)
);

CREATE INDEX idx_inbox_processed_at ON inbox_messages(processed_at);

-- Create Poison Messages table
CREATE TABLE IF NOT EXISTS poison_messages (
    id UUID PRIMARY KEY,
    message_type VARCHAR(255) NOT NULL,
    payload BYTEA NOT NULL,
    failure_count INTEGER NOT NULL,
    errors TEXT[],
    first_failure_at TIMESTAMP WITH TIME ZONE NOT NULL,
    last_failure_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX idx_poison_last_failure ON poison_messages(last_failure_at);
```

## Application Dockerfile

Create `Dockerfile`:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["YourApp/YourApp.csproj", "YourApp/"]
RUN dotnet restore "YourApp/YourApp.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/YourApp"
RUN dotnet build "YourApp.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "YourApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Create non-root user
RUN useradd -m -u 1000 appuser && chown -R appuser:appuser /app
USER appuser

# Expose ports
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "YourApp.dll"]
```

## Environment Configuration

### Development Environment

Create `.env.development`:

```env
# RabbitMQ
RABBITMQ_PASSWORD=admin123

# PostgreSQL
POSTGRES_PASSWORD=relay123

# Grafana
GRAFANA_PASSWORD=admin

# Application
ASPNETCORE_ENVIRONMENT=Development
LOG_LEVEL=Debug
```

### Production Environment

Create `.env.production`:

```env
# RabbitMQ
RABBITMQ_PASSWORD=<strong-password>

# PostgreSQL
POSTGRES_PASSWORD=<strong-password>

# Grafana
GRAFANA_PASSWORD=<strong-password>

# Application
ASPNETCORE_ENVIRONMENT=Production
LOG_LEVEL=Information

# Security
ENCRYPTION_KEY=<base64-encoded-key>
JWT_SECRET=<strong-secret>
```

## Running the Stack

### Start All Services

```bash
# Development
docker-compose --env-file .env.development up -d

# Production
docker-compose --env-file .env.production up -d
```

### Start Specific Services

```bash
# Start only RabbitMQ and PostgreSQL
docker-compose up -d rabbitmq postgres

# Start application
docker-compose up -d app
```

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f app

# Last 100 lines
docker-compose logs --tail=100 app
```

### Stop Services

```bash
# Stop all
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

## Monitoring

### Access Monitoring Tools

- **RabbitMQ Management:** http://localhost:15672 (admin/admin123)
- **Jaeger UI:** http://localhost:16686
- **Prometheus:** http://localhost:9090
- **Grafana:** http://localhost:3000 (admin/admin)
- **Application Health:** http://localhost:8080/health

### Health Checks

```bash
# Check all services
docker-compose ps

# Check application health
curl http://localhost:8080/health

# Check RabbitMQ
curl -u admin:admin123 http://localhost:15672/api/health/checks/alarms
```

## Scaling

### Scale Application Instances

```bash
# Scale to 3 instances
docker-compose up -d --scale app=3

# Use load balancer
docker-compose -f docker-compose.yml -f docker-compose.lb.yml up -d
```

### Load Balancer Configuration

Create `docker-compose.lb.yml`:

```yaml
version: '3.8'

services:
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
    depends_on:
      - app
    networks:
      - relay-network
```

Create `nginx.conf`:

```nginx
events {
    worker_connections 1024;
}

http {
    upstream app_servers {
        least_conn;
        server app:8080 max_fails=3 fail_timeout=30s;
    }

    server {
        listen 80;
        
        location / {
            proxy_pass http://app_servers;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        }
        
        location /health {
            access_log off;
            proxy_pass http://app_servers/health;
        }
    }
}
```

## Troubleshooting

### Container Won't Start

```bash
# Check logs
docker-compose logs app

# Check container status
docker-compose ps

# Inspect container
docker inspect relay-app
```

### Connection Issues

```bash
# Test RabbitMQ connection
docker-compose exec app curl -v rabbitmq:5672

# Test PostgreSQL connection
docker-compose exec app pg_isready -h postgres -U relay

# Check network
docker network inspect relay_relay-network
```

### Performance Issues

```bash
# Check resource usage
docker stats

# Check container limits
docker inspect relay-app | grep -A 10 Resources
```

### Database Issues

```bash
# Connect to PostgreSQL
docker-compose exec postgres psql -U relay -d relay

# Check tables
docker-compose exec postgres psql -U relay -d relay -c "\dt"

# View outbox messages
docker-compose exec postgres psql -U relay -d relay -c "SELECT * FROM outbox_messages LIMIT 10;"
```

## Best Practices

1. **Use .env files** for environment-specific configuration
2. **Enable health checks** for all services
3. **Use volumes** for persistent data
4. **Set resource limits** in production
5. **Use secrets** for sensitive data (Docker Swarm/Kubernetes)
6. **Enable logging drivers** for centralized logging
7. **Regular backups** of volumes
8. **Monitor resource usage** with docker stats
9. **Use multi-stage builds** to minimize image size
10. **Run as non-root user** in containers

## Security Considerations

1. **Change default passwords** in production
2. **Use Docker secrets** for sensitive data
3. **Limit container capabilities**
4. **Use read-only root filesystem** where possible
5. **Scan images** for vulnerabilities
6. **Keep images updated**
7. **Use private registries** for production images
8. **Enable TLS** for external connections
9. **Implement network policies**
10. **Regular security audits**

## Production Checklist

- [ ] All default passwords changed
- [ ] Environment variables configured
- [ ] Health checks enabled
- [ ] Resource limits set
- [ ] Logging configured
- [ ] Monitoring enabled
- [ ] Backups configured
- [ ] Security scanning enabled
- [ ] TLS certificates configured
- [ ] Documentation updated

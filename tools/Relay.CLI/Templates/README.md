# Relay Project Templates

This directory contains official project templates for Relay Framework. Each template provides a complete, production-ready starting point for different types of applications.

## 📁 Available Templates

### 1. **relay-webapi** - Clean Architecture Web API
Production-ready REST API following Clean Architecture principles.

**Features:**
- Clean Architecture (Domain, Application, Infrastructure, API layers)
- CQRS with Relay
- Entity Framework Core
- Authentication & Authorization (JWT)
- Swagger/OpenAPI
- Health checks
- Docker support
- CI/CD pipeline (GitHub Actions)

**Best for:** Enterprise REST APIs, Backend services

---

### 2. **relay-microservice** - Event-Driven Microservice
Microservice template with message broker integration.

**Features:**
- Event-driven architecture
- Message broker integration (RabbitMQ/Kafka)
- Service discovery
- Distributed tracing (OpenTelemetry)
- Circuit breaker & Retry patterns
- Kubernetes manifests
- Helm charts

**Best for:** Microservices architecture, Event-driven systems

---

### 3. **relay-ddd** - Domain-Driven Design
DDD tactical patterns with Relay.

**Features:**
- Bounded contexts
- Aggregates, Entities, Value Objects
- Domain events
- Repository pattern
- Specification pattern
- Unit of Work

**Best for:** Complex business domains, Enterprise applications

---

### 4. **relay-cqrs-es** - CQRS + Event Sourcing
Complete CQRS with Event Sourcing implementation.

**Features:**
- Full Event Sourcing
- Event Store
- Projections
- Snapshots
- Saga pattern
- Time-travel debugging

**Best for:** Systems requiring full audit trail, Financial applications

---

### 5. **relay-modular** - Modular Monolith
Modular monolith with vertical slices.

**Features:**
- Vertical slice architecture
- Feature folders
- Module isolation
- Shared kernel
- Easy migration to microservices

**Best for:** Starting with monolith, future microservices migration

---

### 6. **relay-graphql** - GraphQL API
GraphQL API with Hot Chocolate.

**Features:**
- GraphQL schema-first
- Queries, Mutations, Subscriptions
- DataLoader
- Filtering, Sorting, Pagination
- Real-time updates

**Best for:** Flexible APIs, Mobile/SPA backends

---

### 7. **relay-grpc** - gRPC Service
High-performance gRPC service.

**Features:**
- Protocol Buffers
- Streaming support
- Service discovery
- Load balancing
- TLS/SSL

**Best for:** Microservice communication, High-performance APIs

---

### 8. **relay-serverless** - Serverless Functions
AWS Lambda / Azure Functions template.

**Features:**
- Function-as-a-Service
- API Gateway integration
- Cold start optimization
- Cost-optimized
- Deployment scripts

**Best for:** Serverless applications, Cost-sensitive workloads

---

### 9. **relay-blazor** - Blazor Application
Full-stack Blazor app with Relay.

**Features:**
- Blazor Server/WebAssembly
- Real-time updates (SignalR)
- Authentication
- State management
- PWA support

**Best for:** Full-stack .NET applications, Internal tools

---

### 10. **relay-maui** - MAUI Mobile App
Cross-platform mobile app.

**Features:**
- iOS, Android, Windows, macOS
- MVVM pattern
- Offline-first
- Local database (SQLite)
- Push notifications

**Best for:** Mobile applications, Cross-platform apps

---

## 🚀 Usage

### List all templates
```bash
relay new --list
```

### Create project from template
```bash
relay new relay-webapi --name MyApi
```

### With features
```bash
relay new relay-webapi --name MyApi \
  --features "auth,swagger,docker,tests,healthchecks"
```

### With custom configuration
```bash
relay new relay-microservice --name OrderService \
  --broker rabbitmq \
  --database postgres \
  --auth jwt
```

## 🛠️ Creating Custom Templates

### 1. Create template structure
```bash
relay template create --name my-template --from ./MyProject
```

### 2. Define template.json
```json
{
  "name": "my-template",
  "version": "1.0.0",
  "description": "My custom template",
  "author": "Your Name",
  "tags": ["custom", "enterprise"],
  "parameters": [
    {
      "name": "ProjectName",
      "type": "string",
      "required": true
    }
  ],
  "features": [
    {
      "name": "docker",
      "description": "Add Docker support",
      "default": true
    }
  ]
}
```

### 3. Publish template
```bash
relay template publish --name my-template
```

## 📚 Template Structure

```
templates/
├── relay-webapi/
│   ├── template.json           # Template metadata
│   ├── .template.config/       # Template configuration
│   ├── content/                # Template files
│   │   ├── src/
│   │   ├── tests/
│   │   ├── docs/
│   │   └── README.md
│   └── scripts/                # Setup scripts
├── relay-microservice/
└── ...
```

## 🔧 Template Development

### Testing template locally
```bash
relay template test --path ./my-template
```

### Validating template
```bash
relay template validate --path ./my-template
```

### Package template
```bash
relay template pack --path ./my-template --output my-template.nupkg
```

## 📖 Documentation

For detailed documentation on each template, see:
- [Template Development Guide](../docs/template-development.md)
- [Template Best Practices](../docs/template-best-practices.md)
- [Template Contribution Guide](../docs/template-contribution.md)

## 🤝 Contributing

Want to contribute a template? See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines.

## 📄 License

All templates are licensed under MIT License.

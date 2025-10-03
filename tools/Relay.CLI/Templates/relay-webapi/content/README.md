# {{ProjectName}} - Relay Clean Architecture Web API

A production-ready REST API built with Relay mediator framework following Clean Architecture principles.

## 🏗️ Architecture

This project follows Clean Architecture with the following layers:

```
{{ProjectName}}/
├── src/
│   ├── {{ProjectName}}.Api/              # Presentation Layer (Controllers, Endpoints)
│   ├── {{ProjectName}}.Application/      # Application Layer (Handlers, Commands, Queries)
│   ├── {{ProjectName}}.Domain/           # Domain Layer (Entities, Value Objects, Domain Events)
│   └── {{ProjectName}}.Infrastructure/   # Infrastructure Layer (EF Core, External APIs, etc.)
├── tests/
│   ├── {{ProjectName}}.UnitTests/
│   ├── {{ProjectName}}.IntegrationTests/
│   └── {{ProjectName}}.ArchitectureTests/
└── docs/
    ├── api/                              # API documentation
    └── architecture/                     # Architecture diagrams
```

## 🚀 Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or later
- [Docker](https://www.docker.com/get-started) (optional, for containerization)
- [PostgreSQL](https://www.postgresql.org/download/) (or your chosen database)

### Running the Application

#### Using .NET CLI

```bash
# Restore dependencies
dotnet restore

# Run the API
dotnet run --project src/{{ProjectName}}.Api

# The API will be available at:
# - HTTP: http://localhost:5000
# - HTTPS: https://localhost:5001
```

#### Using Docker

```bash
# Build and run with Docker Compose
docker-compose up

# The API will be available at:
# - HTTP: http://localhost:8080
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/{{ProjectName}}.UnitTests
dotnet test tests/{{ProjectName}}.IntegrationTests

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## 📚 Features

### ✅ Included Features

- **Clean Architecture** - Clear separation of concerns with dependency inversion
- **CQRS with Relay** - Command Query Responsibility Segregation using Relay mediator
- **Entity Framework Core** - {{DatabaseProvider}} database provider
{{#if EnableAuth}}
- **Authentication & Authorization** - JWT-based authentication with role-based authorization
{{/if}}
{{#if EnableSwagger}}
- **Swagger/OpenAPI** - Interactive API documentation
{{/if}}
{{#if EnableDocker}}
- **Docker Support** - Dockerfile and docker-compose for containerization
{{/if}}
{{#if EnableHealthChecks}}
- **Health Checks** - Built-in health check endpoints
{{/if}}
{{#if EnableCaching}}
- **Redis Caching** - Distributed caching with Redis
{{/if}}
{{#if EnableTelemetry}}
- **OpenTelemetry** - Distributed tracing and metrics
{{/if}}
- **Validation** - FluentValidation for request validation
- **Exception Handling** - Global exception handling middleware
- **Logging** - Structured logging with Serilog
- **CI/CD** - GitHub Actions workflow
- **Testing** - Unit, Integration, and Architecture tests

## 🔧 Configuration

Configuration is managed through `appsettings.json` and environment variables.

### Database Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database={{ProjectName}};Username=postgres;Password=your_password"
  }
}
```

### JWT Configuration (if enabled)

```json
{
  "Jwt": {
    "Issuer": "{{ProjectName}}",
    "Audience": "{{ProjectName}}Api",
    "SecretKey": "your-256-bit-secret-key-here",
    "ExpirationMinutes": 60
  }
}
```

## 📖 API Documentation

{{#if EnableSwagger}}
After starting the application, navigate to:
- **Swagger UI**: https://localhost:5001/swagger
- **OpenAPI JSON**: https://localhost:5001/swagger/v1/swagger.json
{{else}}
API documentation is available in the `docs/api` folder.
{{/if}}

## 🧪 Testing Strategy

### Unit Tests
- Test individual components in isolation
- Mock dependencies using Moq
- Fast execution, no external dependencies

### Integration Tests
- Test API endpoints end-to-end
- Use in-memory database or test containers
- Verify correct integration between components

### Architecture Tests
- Enforce architectural boundaries
- Validate layer dependencies
- Ensure naming conventions

## 🏛️ Architecture Patterns

### CQRS (Command Query Responsibility Segregation)

Commands (Write Operations):
```csharp
public record CreateProductCommand(string Name, decimal Price) : IRequest<ProductDto>;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    [Handle]
    public async ValueTask<ProductDto> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        // Business logic here
    }
}
```

Queries (Read Operations):
```csharp
public record GetProductQuery(int Id) : IRequest<ProductDto>;

public class GetProductHandler : IRequestHandler<GetProductQuery, ProductDto>
{
    [Handle]
    public async ValueTask<ProductDto> HandleAsync(
        GetProductQuery query,
        CancellationToken cancellationToken)
    {
        // Query logic here
    }
}
```

### Domain Events

```csharp
public record ProductCreatedEvent(int ProductId, string Name) : INotification;

public class ProductCreatedNotificationHandler : INotificationHandler<ProductCreatedEvent>
{
    [Notification]
    public async ValueTask HandleAsync(
        ProductCreatedEvent notification,
        CancellationToken cancellationToken)
    {
        // Handle domain event
    }
}
```

## 🚢 Deployment

### Docker Deployment

```bash
# Build image
docker build -t {{ProjectName}}:latest .

# Run container
docker run -p 8080:80 {{ProjectName}}:latest
```

### Kubernetes Deployment

```bash
# Apply Kubernetes manifests
kubectl apply -f k8s/

# Check deployment status
kubectl get pods
kubectl get services
```

## 📊 Monitoring

{{#if EnableHealthChecks}}
### Health Checks

- **Liveness**: `/health/live` - Indicates if the application is running
- **Readiness**: `/health/ready` - Indicates if the application is ready to serve traffic
- **Detailed**: `/health` - Detailed health information
{{/if}}

{{#if EnableTelemetry}}
### OpenTelemetry

The application exports metrics and traces to configured exporters:
- **Metrics**: Application performance metrics
- **Traces**: Distributed tracing across services
- **Logs**: Structured logging with correlation IDs
{{/if}}

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 Code Style

This project follows:
- C# coding conventions
- Clean Code principles
- SOLID principles
- DRY (Don't Repeat Yourself)

Run code analysis:
```bash
dotnet format
dotnet build /p:TreatWarningsAsErrors=true
```

## 🔐 Security

- Secrets should never be committed to the repository
- Use User Secrets for local development
- Use environment variables or Azure Key Vault for production
- Keep dependencies up to date

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- Built with [Relay Framework](https://github.com/genc-murat/relay)
- Inspired by Clean Architecture principles
- Following CQRS best practices

## 📞 Support

For questions and support:
- Create an issue in the repository
- Check the documentation in the `docs` folder
- Review the example implementations

---

**{{ProjectName}}** - Built with ❤️ using Relay Framework

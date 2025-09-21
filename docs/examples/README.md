# Relay Examples

This directory contains comprehensive examples demonstrating various Relay features and usage patterns.

## Basic Examples

### [Simple CRUD API](simple-crud-api/)
A basic web API demonstrating:
- Request/response handlers
- Command handlers
- Basic pipeline behaviors
- Dependency injection setup

### [Console Application](console-app/)
A console application showing:
- Handler registration
- Request processing
- Error handling
- Configuration

## Advanced Examples

### [E-commerce Platform](ecommerce-platform/)
A comprehensive e-commerce example featuring:
- Complex domain models
- Multiple bounded contexts
- Event-driven architecture with notifications
- Advanced pipeline behaviors (validation, caching, logging)
- Performance optimizations

### [Real-time Chat Application](realtime-chat/)
Demonstrates:
- Streaming responses with `IAsyncEnumerable`
- Real-time notifications
- SignalR integration
- Backpressure handling

### [Microservices Architecture](microservices/)
Shows how to use Relay in a distributed system:
- Service-to-service communication
- Distributed tracing
- Circuit breaker patterns
- Message queue integration

## Performance Examples

### [High-Throughput API](high-throughput-api/)
Optimized for maximum performance:
- ValueTask usage patterns
- Object pooling
- Memory optimization techniques
- Benchmarking setup

### [Streaming Data Processing](streaming-data/)
Large-scale data processing:
- Efficient streaming patterns
- Memory-conscious processing
- Cancellation handling
- Progress reporting

## Integration Examples

### [ASP.NET Core Integration](aspnet-core/)
Complete web application integration:
- Controller integration
- Middleware setup
- Authentication/authorization
- OpenAPI generation

### [Background Services](background-services/)
Long-running background processing:
- Hosted services integration
- Scheduled processing
- Queue-based processing
- Health checks

### [Testing Examples](testing/)
Comprehensive testing strategies:
- Unit testing handlers
- Integration testing
- Performance testing
- Mocking strategies

## Specialized Examples

### [Multi-tenant Application](multi-tenant/)
Tenant-aware request processing:
- Tenant isolation
- Per-tenant configuration
- Tenant-specific handlers
- Data partitioning

### [Event Sourcing](event-sourcing/)
Event-driven architecture:
- Event store integration
- Projection handlers
- Saga patterns
- Eventual consistency

### [CQRS Implementation](cqrs/)
Command Query Responsibility Segregation:
- Separate read/write models
- Command validation
- Query optimization
- Event publishing

## Getting Started

Each example includes:
- Complete source code
- README with setup instructions
- Docker configuration (where applicable)
- Performance benchmarks
- Test suites

### Prerequisites

- .NET 8.0 or later
- Visual Studio 2022 or VS Code
- Docker (for some examples)

### Running Examples

1. Clone the repository
2. Navigate to the example directory
3. Follow the README instructions for that example
4. Run `dotnet run` or use your IDE

### Example Structure

```
example-name/
├── src/
│   ├── ExampleApp/
│   ├── ExampleApp.Domain/
│   └── ExampleApp.Infrastructure/
├── tests/
│   ├── ExampleApp.Tests/
│   └── ExampleApp.IntegrationTests/
├── docker-compose.yml
├── README.md
└── benchmarks/
```

## Contributing Examples

We welcome contributions of new examples! Please:

1. Follow the existing structure
2. Include comprehensive documentation
3. Add unit and integration tests
4. Include performance benchmarks where relevant
5. Ensure examples are production-ready

See [CONTRIBUTING.md](../CONTRIBUTING.md) for detailed guidelines.

## Example Categories

### By Complexity
- **Beginner**: Simple CRUD, Console App
- **Intermediate**: E-commerce, Chat App
- **Advanced**: Microservices, Event Sourcing

### By Domain
- **Web APIs**: ASP.NET Core, High-throughput
- **Real-time**: Chat, Streaming
- **Enterprise**: Multi-tenant, CQRS
- **Infrastructure**: Background Services, Testing

### By Feature
- **Handlers**: All examples demonstrate handler patterns
- **Pipelines**: E-commerce, ASP.NET Core, Multi-tenant
- **Streaming**: Chat, Streaming Data, Real-time
- **Performance**: High-throughput, Streaming Data
- **Testing**: Testing Examples, all others include tests

## Learning Path

Recommended order for learning Relay:

1. **Start Here**: [Simple CRUD API](simple-crud-api/)
2. **Add Complexity**: [E-commerce Platform](ecommerce-platform/)
3. **Real-time Features**: [Real-time Chat](realtime-chat/)
4. **Performance**: [High-Throughput API](high-throughput-api/)
5. **Production**: [ASP.NET Core Integration](aspnet-core/)
6. **Advanced Patterns**: [CQRS](cqrs/) or [Event Sourcing](event-sourcing/)

Each example builds on concepts from previous ones while introducing new features and patterns.
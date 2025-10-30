# NuGet Package Structure

Relay.MessageBroker is distributed as a set of NuGet packages to provide flexibility and minimize dependencies.

## Package Overview

### Core Package

#### Relay.MessageBroker
**Description:** Core message broker functionality with all patterns and features included.

**Dependencies:**
- Microsoft.Extensions.DependencyInjection.Abstractions
- Microsoft.Extensions.Hosting.Abstractions
- Microsoft.Extensions.Logging.Abstractions
- Microsoft.Extensions.Options
- Polly (for resilience)
- OpenTelemetry packages (for observability)
- Scrutor (for service decoration)
- Entity Framework Core (for persistence)

**Includes:**
- Base message broker abstractions
- All broker implementations (RabbitMQ, Kafka, Azure Service Bus, AWS SQS/SNS, NATS, Redis Streams)
- Outbox and Inbox patterns
- Connection pooling
- Batch processing
- Message deduplication
- Health checks
- Metrics and telemetry
- Distributed tracing
- Message encryption
- Authentication and authorization
- Rate limiting
- Bulkhead pattern
- Poison message handling
- Backpressure management

**Installation:**
```bash
dotnet add package Relay.MessageBroker
```

**Usage:**
```csharp
using Relay.MessageBroker;

services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.Production,
    options =>
    {
        options.BrokerType = MessageBrokerType.RabbitMQ;
        // Configure broker options
    });
```

## Optional Packages (Future Consideration)

For organizations that want to minimize dependencies, the following package structure could be considered:

### Relay.MessageBroker.Core
Core abstractions and base functionality without specific broker implementations.

### Relay.MessageBroker.Patterns
Outbox and Inbox patterns with persistence implementations.

**Would include:**
- Outbox pattern (in-memory and SQL)
- Inbox pattern (in-memory and SQL)
- Entity Framework Core contexts
- Background workers

### Relay.MessageBroker.Security
Security features including encryption and authentication.

**Would include:**
- AES message encryption
- Azure Key Vault integration
- JWT authentication
- Azure AD integration
- OAuth2 integration
- Role-based authorization

### Relay.MessageBroker.Testing
Test utilities and in-memory implementations for testing.

**Would include:**
- In-memory message broker
- In-memory outbox/inbox stores
- Test helpers
- Fake implementations

## Package Versioning

All packages follow [Semantic Versioning 2.0.0](https://semver.org/):

- **Major version:** Breaking changes
- **Minor version:** New features (backward compatible)
- **Patch version:** Bug fixes (backward compatible)

### Version History

#### 2.0.0 (Current)
- Complete rewrite with fluent configuration API
- All patterns and features integrated
- Production-ready with comprehensive testing
- Full observability support
- Enterprise security features

#### 1.x
- Legacy version with basic broker functionality
- Limited pattern support

## Package Metadata

### Common Metadata (All Packages)

```xml
<PropertyGroup>
  <Authors>Relay Team</Authors>
  <Company>Your Organization</Company>
  <Product>Relay Framework</Product>
  <Copyright>Copyright © 2025 Your Organization</Copyright>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/your-org/relay</PackageProjectUrl>
  <RepositoryUrl>https://github.com/your-org/relay</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageTags>messaging;message-broker;rabbitmq;kafka;azure-service-bus;patterns;microservices</PackageTags>
  <PackageIcon>icon.png</PackageIcon>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageReleaseNotes>See CHANGELOG.md for details</PackageReleaseNotes>
</PropertyGroup>
```

### Relay.MessageBroker Specific

```xml
<PropertyGroup>
  <PackageId>Relay.MessageBroker</PackageId>
  <Description>
    Enterprise-grade message broker abstraction with support for multiple brokers (RabbitMQ, Kafka, Azure Service Bus, AWS SQS/SNS, NATS, Redis Streams).
    Includes reliability patterns (Outbox, Inbox), performance optimizations (connection pooling, batching, deduplication),
    comprehensive observability (health checks, metrics, distributed tracing), security features (encryption, authentication, authorization),
    and resilience patterns (rate limiting, bulkhead, poison message handling, backpressure management).
  </Description>
  <PackageTags>messaging;message-broker;rabbitmq;kafka;azure-service-bus;aws-sqs;nats;redis;outbox;inbox;patterns;microservices;distributed-systems;observability;security;resilience</PackageTags>
</PropertyGroup>
```

## Building Packages

### Local Build

```bash
# Build all packages
dotnet pack src/Relay.MessageBroker/Relay.MessageBroker.csproj -c Release -o ./packoutput

# Build with specific version
dotnet pack src/Relay.MessageBroker/Relay.MessageBroker.csproj -c Release -o ./packoutput /p:Version=2.0.0
```

### CI/CD Build

```yaml
# GitHub Actions example
- name: Pack NuGet packages
  run: |
    dotnet pack src/Relay.MessageBroker/Relay.MessageBroker.csproj \
      -c Release \
      -o ./packoutput \
      /p:Version=${{ github.ref_name }} \
      /p:PackageReleaseNotes="See https://github.com/your-org/relay/releases/tag/${{ github.ref_name }}"
```

## Publishing Packages

### To NuGet.org

```bash
# Publish to NuGet.org
dotnet nuget push ./packoutput/Relay.MessageBroker.2.0.0.nupkg \
  --api-key $NUGET_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### To Private Feed

```bash
# Publish to Azure Artifacts
dotnet nuget push ./packoutput/Relay.MessageBroker.2.0.0.nupkg \
  --api-key $AZURE_ARTIFACTS_PAT \
  --source https://pkgs.dev.azure.com/your-org/_packaging/relay/nuget/v3/index.json
```

### To GitHub Packages

```bash
# Publish to GitHub Packages
dotnet nuget push ./packoutput/Relay.MessageBroker.2.0.0.nupkg \
  --api-key $GITHUB_TOKEN \
  --source https://nuget.pkg.github.com/your-org/index.json
```

## Package Dependencies

### Runtime Dependencies

All packages target .NET 8.0 and have the following common dependencies:

- Microsoft.Extensions.* (8.0.0+)
- Polly (8.0.0+)
- OpenTelemetry.* (1.10.0+)

### Broker-Specific Dependencies

- **RabbitMQ:** RabbitMQ.Client (6.8.0+)
- **Kafka:** Confluent.Kafka (2.5.0+)
- **Azure Service Bus:** Azure.Messaging.ServiceBus (7.18.0+)
- **AWS SQS/SNS:** AWSSDK.SQS, AWSSDK.SimpleNotificationService (3.7.0+)
- **NATS:** NATS.Client.Core (2.4.0+)
- **Redis:** StackExchange.Redis (2.8.0+)

## Package Size Optimization

### Current Package Sizes (Approximate)

- **Relay.MessageBroker:** ~2.5 MB (includes all features and broker implementations)

### Reducing Package Size

If package size is a concern, consider:

1. Using the modular package structure (future)
2. Trimming unused broker implementations (requires custom build)
3. Using NuGet package references instead of assembly references

## Package Documentation

Each package includes:

- **README.md:** Quick start guide and basic usage
- **CHANGELOG.md:** Version history and release notes
- **LICENSE:** MIT license
- **icon.png:** Package icon for NuGet gallery

### README.md Template

```markdown
# Relay.MessageBroker

Enterprise-grade message broker abstraction for .NET with support for multiple brokers and comprehensive patterns.

## Features

- Multiple broker support (RabbitMQ, Kafka, Azure Service Bus, AWS SQS/SNS, NATS, Redis Streams)
- Reliability patterns (Outbox, Inbox)
- Performance optimizations (Connection pooling, Batching, Deduplication)
- Observability (Health checks, Metrics, Distributed tracing)
- Security (Encryption, Authentication, Authorization)
- Resilience (Rate limiting, Bulkhead, Poison message handling, Backpressure)

## Installation

```bash
dotnet add package Relay.MessageBroker
```

## Quick Start

```csharp
services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.Production,
    options =>
    {
        options.BrokerType = MessageBrokerType.RabbitMQ;
        options.RabbitMQ = new RabbitMQOptions
        {
            HostName = "localhost",
            Port = 5672
        };
    });
```

## Documentation

Full documentation available at: https://docs.relay.dev/messagebroker

## License

MIT License - see LICENSE file for details
```

## Support and Maintenance

### Support Policy

- **Current version (2.x):** Full support with new features and bug fixes
- **Previous version (1.x):** Security fixes only for 6 months after 2.0 release
- **Older versions:** No support

### Release Cadence

- **Major releases:** Annually (breaking changes)
- **Minor releases:** Quarterly (new features)
- **Patch releases:** As needed (bug fixes)

### Security Updates

Security vulnerabilities are addressed immediately with patch releases.

Report security issues to: security@relay.dev

## Package Quality

### Quality Metrics

- **Code coverage:** 85%+
- **Static analysis:** Clean (no warnings)
- **Performance tests:** Passing
- **Integration tests:** Passing
- **Documentation:** Complete

### Package Signing

All packages are signed with a strong name key for authenticity.

## Migration Guide

See [MIGRATION.md](./MIGRATION.md) for guidance on upgrading between versions.

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for information on contributing to Relay.MessageBroker.

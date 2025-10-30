# Relay.MessageBroker Documentation Index

Welcome to the comprehensive documentation for Relay.MessageBroker enhancements. This index will help you find the information you need.

## Quick Links

- 🚀 [Getting Started](./GETTING_STARTED.md) - Start here if you're new
- 🎨 [Fluent Configuration API](./FLUENT_CONFIGURATION.md) - Modern fluent API for easy setup
- ⚙️ [Configuration Guide](./CONFIGURATION.md) - Detailed configuration options
- ✅ [Best Practices](./BEST_PRACTICES.md) - Production deployment guidelines
- 🔧 [Troubleshooting](./TROUBLESHOOTING.md) - Common issues and solutions
- 📦 [Migration Guide](./MIGRATION.md) - Migrating from existing implementations
- 💻 [Code Examples](./examples/) - Working code examples
- 🎯 [Sample Applications](./samples/) - Complete sample applications

## Documentation Structure

### Core Documentation

#### [Getting Started Guide](./GETTING_STARTED.md)
Your first stop for understanding and using Relay.MessageBroker enhancements.

**Contents:**
- Prerequisites and installation
- Quick start examples
- Pattern guides for all features
- Next steps

**Best for:** New users, quick reference

#### [Fluent Configuration API](./FLUENT_CONFIGURATION.md)
Modern fluent API for configuring message broker with all patterns and features.

**Contents:**
- Basic usage
- Configuration profiles (Development, Production, High Throughput, High Reliability)
- Individual feature configuration
- Complete examples
- Validation
- Best practices
- Migration from legacy configuration

**Best for:** Quick setup, clean configuration code, production deployments

#### [Configuration Guide](./CONFIGURATION.md)
Comprehensive reference for all configuration options.

**Contents:**
- Configuration for each pattern
- Environment-specific profiles
- Environment variables
- Configuration validation

**Best for:** Setting up production systems, fine-tuning

#### [Best Practices](./BEST_PRACTICES.md)
Guidelines for production deployments.

**Contents:**
- General best practices
- Reliability patterns
- Performance optimization
- Security guidelines
- Observability setup
- Resilience patterns
- Deployment strategies
- Monitoring and alerting
- Capacity planning
- Disaster recovery

**Best for:** Production deployments, architecture decisions

#### [Troubleshooting Guide](./TROUBLESHOOTING.md)
Solutions for common issues.

**Contents:**
- General issues
- Pattern-specific issues
- Performance issues
- Security issues
- Diagnostic tools

**Best for:** Debugging, problem-solving

#### [Migration Guide](./MIGRATION.md)
Step-by-step guide for migrating to enhanced MessageBroker.

**Contents:**
- Migration strategies
- Pre-migration checklist
- Phase-by-phase migration
- Feature-specific migration
- Testing migration
- Rollback plans

**Best for:** Upgrading existing systems

### Code Examples

#### [Examples Directory](./examples/)
Working code examples for all patterns.

**Available Examples:**
- Outbox Pattern with SQL Server
- Inbox Pattern with PostgreSQL
- Connection Pooling
- Batch Processing with compression
- Message Encryption with Azure Key Vault
- Rate Limiting with per-tenant limits
- Distributed Tracing with Jaeger
- Comprehensive example with all features

**Best for:** Learning by example, copy-paste starting points

### Sample Applications

#### [Samples Directory](./samples/)
Complete, production-like sample applications.

**Available Samples:**
- E-Commerce Order Processing (Microservices)
- Order Processing Saga
- Monitoring Dashboard (Grafana)
- Load Testing (K6)

**Best for:** Understanding real-world usage, reference architectures

## Feature Documentation

### Reliability Patterns

#### Outbox Pattern
- [Getting Started](./GETTING_STARTED.md#outbox-pattern)
- [Configuration](./CONFIGURATION.md#outbox-pattern-configuration)
- [Best Practices](./BEST_PRACTICES.md#outbox-pattern)
- [Troubleshooting](./TROUBLESHOOTING.md#outbox-pattern-issues)
- [Example](./examples/OutboxPatternExample.cs)
- [Implementation](../src/Relay.MessageBroker/Outbox/README.md)

#### Inbox Pattern
- [Getting Started](./GETTING_STARTED.md#inbox-pattern)
- [Configuration](./CONFIGURATION.md#inbox-pattern-configuration)
- [Best Practices](./BEST_PRACTICES.md#inbox-pattern)
- [Troubleshooting](./TROUBLESHOOTING.md#inbox-pattern-issues)
- [Example](./examples/InboxPatternExample.cs)
- [Implementation](../src/Relay.MessageBroker/Inbox/README.md)

### Performance Optimizations

#### Connection Pooling
- [Getting Started](./GETTING_STARTED.md#connection-pooling)
- [Configuration](./CONFIGURATION.md#connection-pool-configuration)
- [Best Practices](./BEST_PRACTICES.md#connection-pooling)
- [Troubleshooting](./TROUBLESHOOTING.md#connection-pool-issues)
- [Example](../src/Relay.MessageBroker/ConnectionPool/EXAMPLE.md)

#### Batch Processing
- [Getting Started](./GETTING_STARTED.md#batch-processing)
- [Configuration](./CONFIGURATION.md#batch-processing-configuration)
- [Best Practices](./BEST_PRACTICES.md#batch-processing)
- [Troubleshooting](./TROUBLESHOOTING.md#batch-processing-issues)
- [Example](../src/Relay.MessageBroker/Batch/EXAMPLE.md)

#### Message Deduplication
- [Getting Started](./GETTING_STARTED.md#message-deduplication)
- [Configuration](./CONFIGURATION.md#deduplication-configuration)
- [Best Practices](./BEST_PRACTICES.md#deduplication)
- [Troubleshooting](./TROUBLESHOOTING.md#deduplication-issues)
- [Example](../src/Relay.MessageBroker/Deduplication/EXAMPLE.md)

### Observability

#### Health Checks
- [Getting Started](./GETTING_STARTED.md#health-checks)
- [Configuration](./CONFIGURATION.md#health-checks-configuration)
- [Best Practices](./BEST_PRACTICES.md#health-checks)
- [Troubleshooting](./TROUBLESHOOTING.md#health-check-issues)
- [Example](../src/Relay.MessageBroker/HealthChecks/EXAMPLE.md)

#### Metrics and Telemetry
- [Getting Started](./GETTING_STARTED.md#metrics-and-telemetry)
- [Configuration](./CONFIGURATION.md#metrics-configuration)
- [Best Practices](./BEST_PRACTICES.md#metrics)
- [Troubleshooting](./TROUBLESHOOTING.md#metrics-and-tracing-issues)
- [Example](../src/Relay.MessageBroker/Metrics/EXAMPLE.md)

#### Distributed Tracing
- [Getting Started](./GETTING_STARTED.md#distributed-tracing)
- [Configuration](./CONFIGURATION.md#distributed-tracing-configuration)
- [Best Practices](./BEST_PRACTICES.md#distributed-tracing)
- [Troubleshooting](./TROUBLESHOOTING.md#metrics-and-tracing-issues)
- [Example](../src/Relay.MessageBroker/DistributedTracing/EXAMPLE.md)

### Security

#### Message Encryption
- [Getting Started](./GETTING_STARTED.md#message-encryption)
- [Configuration](./CONFIGURATION.md#encryption-configuration)
- [Best Practices](./BEST_PRACTICES.md#message-encryption)
- [Troubleshooting](./TROUBLESHOOTING.md#encryption-errors)
- [Example](../src/Relay.MessageBroker/Security/EXAMPLE.md)

#### Authentication and Authorization
- [Getting Started](./GETTING_STARTED.md#authentication-and-authorization)
- [Configuration](./CONFIGURATION.md#authentication-configuration)
- [Best Practices](./BEST_PRACTICES.md#authentication-and-authorization)
- [Troubleshooting](./TROUBLESHOOTING.md#authentication-failures)
- [Example](../src/Relay.MessageBroker/Security/AUTHENTICATION_EXAMPLE.md)

#### Rate Limiting
- [Getting Started](./GETTING_STARTED.md#rate-limiting)
- [Configuration](./CONFIGURATION.md#rate-limiting-configuration)
- [Best Practices](./BEST_PRACTICES.md#rate-limiting)
- [Troubleshooting](./TROUBLESHOOTING.md#rate-limiting-issues)
- [Example](../src/Relay.MessageBroker/RateLimit/EXAMPLE.md)

### Resilience

#### Bulkhead Pattern
- [Getting Started](./GETTING_STARTED.md#bulkhead-pattern)
- [Configuration](./CONFIGURATION.md#bulkhead-configuration)
- [Best Practices](./BEST_PRACTICES.md#bulkhead-pattern)
- [Troubleshooting](./TROUBLESHOOTING.md#resilience-issues)
- [Example](../src/Relay.MessageBroker/Bulkhead/EXAMPLE.md)

#### Poison Message Handling
- [Getting Started](./GETTING_STARTED.md#poison-message-handling)
- [Configuration](./CONFIGURATION.md#poison-message-configuration)
- [Best Practices](./BEST_PRACTICES.md#poison-message-handling)
- [Troubleshooting](./TROUBLESHOOTING.md#poison-messages-accumulating)
- [Example](../src/Relay.MessageBroker/PoisonMessage/EXAMPLE.md)

#### Backpressure Management
- [Getting Started](./GETTING_STARTED.md#backpressure-management)
- [Configuration](./CONFIGURATION.md#backpressure-configuration)
- [Best Practices](./BEST_PRACTICES.md#backpressure-management)
- [Troubleshooting](./TROUBLESHOOTING.md#resilience-issues)
- [Example](../src/Relay.MessageBroker/Backpressure/EXAMPLE.md)

## Learning Paths

### Path 1: Quick Start (30 minutes)
1. Read [Getting Started](./GETTING_STARTED.md) - Quick Start section
2. Run [Comprehensive Example](./examples/ComprehensiveExample.cs)
3. Explore [Sample Application](./samples/)

### Path 2: Production Deployment (2-3 hours)
1. Read [Getting Started](./GETTING_STARTED.md) - All sections
2. Read [Configuration Guide](./CONFIGURATION.md) - Relevant sections
3. Read [Best Practices](./BEST_PRACTICES.md) - All sections
4. Review [Migration Guide](./MIGRATION.md) - If migrating
5. Set up monitoring with [Sample Dashboard](./samples/README.md#monitoring-dashboard-grafana)

### Path 3: Deep Dive (1 day)
1. Read all core documentation
2. Study all code examples
3. Run all sample applications
4. Perform load testing
5. Set up complete monitoring stack

### Path 4: Specific Feature (1 hour)
1. Read feature section in [Getting Started](./GETTING_STARTED.md)
2. Review configuration in [Configuration Guide](./CONFIGURATION.md)
3. Check best practices in [Best Practices](./BEST_PRACTICES.md)
4. Run relevant example
5. Review troubleshooting section if needed

## Common Scenarios

### Scenario: Reliable Messaging
**Goal:** Ensure messages are never lost

**Documentation:**
1. [Outbox Pattern](./GETTING_STARTED.md#outbox-pattern)
2. [Inbox Pattern](./GETTING_STARTED.md#inbox-pattern)
3. [Best Practices - Reliability](./BEST_PRACTICES.md#reliability-patterns)

**Examples:**
- [Outbox Example](./examples/OutboxPatternExample.cs)
- [Inbox Example](./examples/InboxPatternExample.cs)

### Scenario: High Performance
**Goal:** Maximize throughput and minimize latency

**Documentation:**
1. [Connection Pooling](./GETTING_STARTED.md#connection-pooling)
2. [Batch Processing](./GETTING_STARTED.md#batch-processing)
3. [Best Practices - Performance](./BEST_PRACTICES.md#performance-optimization)

**Examples:**
- [Comprehensive Example](./examples/ComprehensiveExample.cs)

### Scenario: Secure Messaging
**Goal:** Protect sensitive data

**Documentation:**
1. [Message Encryption](./GETTING_STARTED.md#message-encryption)
2. [Authentication](./GETTING_STARTED.md#authentication-and-authorization)
3. [Best Practices - Security](./BEST_PRACTICES.md#security)

**Examples:**
- [Comprehensive Example](./examples/ComprehensiveExample.cs)

### Scenario: Observable System
**Goal:** Monitor and debug effectively

**Documentation:**
1. [Health Checks](./GETTING_STARTED.md#health-checks)
2. [Metrics](./GETTING_STARTED.md#metrics-and-telemetry)
3. [Distributed Tracing](./GETTING_STARTED.md#distributed-tracing)
4. [Best Practices - Observability](./BEST_PRACTICES.md#observability)

**Examples:**
- [Monitoring Dashboard](./samples/README.md#monitoring-dashboard-grafana)

### Scenario: Resilient System
**Goal:** Handle failures gracefully

**Documentation:**
1. [Bulkhead Pattern](./GETTING_STARTED.md#bulkhead-pattern)
2. [Poison Message Handling](./GETTING_STARTED.md#poison-message-handling)
3. [Backpressure](./GETTING_STARTED.md#backpressure-management)
4. [Best Practices - Resilience](./BEST_PRACTICES.md#resilience)

**Examples:**
- [E-Commerce Sample](./samples/README.md#e-commerce-order-processing-microservices)

## API Reference

For detailed API documentation, see:
- [IMessageBroker Interface](../src/Relay.MessageBroker/IMessageBroker.cs)
- [Pattern Interfaces](../src/Relay.MessageBroker/)
- XML documentation in source code

## Support and Community

### Getting Help
- 📖 [Documentation](https://docs.relay.dev)
- 🐛 [GitHub Issues](https://github.com/your-org/relay/issues)
- 💬 [Discussions](https://github.com/your-org/relay/discussions)
- 📧 [Email Support](mailto:support@relay.dev)

### Contributing
- [Contributing Guide](../../CONTRIBUTING.md)
- [Code of Conduct](../../CODE_OF_CONDUCT.md)
- [Development Setup](../../DEVELOPMENT.md)

### Resources
- [Blog](https://blog.relay.dev)
- [YouTube Channel](https://youtube.com/@relay)
- [Twitter](https://twitter.com/relayframework)

## Version Information

This documentation is for Relay.MessageBroker v2.0.0 and later.

For older versions:
- [v1.x Documentation](./v1/)

## License

MIT License - see [LICENSE](../../LICENSE) for details.

---

**Last Updated:** 2025-01-XX  
**Documentation Version:** 2.0.0

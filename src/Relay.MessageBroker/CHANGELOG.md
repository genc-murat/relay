# Changelog

All notable changes to Relay.MessageBroker will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2025-01-XX

### Added

#### Fluent Configuration API
- New fluent configuration API for easy setup with `AddMessageBrokerWithPatterns()`
- Configuration profiles: Development, Production, HighThroughput, HighReliability
- Automatic validation of all configuration options
- Builder pattern for composing features

#### Reliability Patterns
- **Outbox Pattern**: Ensures reliable message publishing with persistent storage
  - In-memory and SQL implementations
  - Configurable polling interval and batch size
  - Automatic retry with exponential backoff
  - Background worker for processing pending messages
- **Inbox Pattern**: Ensures idempotent message processing
  - In-memory and SQL implementations
  - Configurable retention period
  - Automatic cleanup of expired entries
  - Background worker for cleanup

#### Performance Optimizations
- **Connection Pooling**: Reusable connection management
  - Configurable min/max pool sizes
  - Connection validation and health checks
  - Automatic connection lifecycle management
  - Idle connection timeout
- **Batch Processing**: High-throughput message batching
  - Configurable batch size (1-10,000 messages)
  - Time-based and size-based flushing
  - Optional compression (30%+ reduction for JSON)
  - Partial retry for failed messages
- **Message Deduplication**: Automatic duplicate detection
  - Content-based hashing (SHA256)
  - Configurable time window (1 min - 24 hours)
  - LRU cache with max size enforcement
  - Multiple strategies (ContentHash, MessageId, Custom)

#### Observability
- **Health Checks**: Comprehensive health monitoring
  - Broker connectivity checks
  - Circuit breaker state monitoring
  - Connection pool metrics
  - ASP.NET Core integration
  - Custom health check response writer
- **Metrics and Telemetry**: OpenTelemetry integration
  - Message throughput counters
  - Latency histograms (P50, P95, P99)
  - Error rate counters
  - Connection pool metrics
  - Queue depth gauges
  - Prometheus exporter
- **Distributed Tracing**: End-to-end request tracing
  - W3C Trace Context standard support
  - Automatic trace context injection/extraction
  - OpenTelemetry exporters (OTLP, Jaeger, Zipkin)
  - Configurable sampling rates
  - Rich span attributes

#### Security
- **Message Encryption**: AES-256-GCM encryption
  - Environment variable key provider
  - Azure Key Vault integration
  - Key rotation support with grace period
  - Encryption metadata in headers
- **Authentication and Authorization**: JWT-based security
  - JWT token validation
  - Role-based authorization
  - Azure AD integration
  - OAuth2 integration
  - Custom identity provider support
  - Token caching for performance
  - Security event logging
- **Rate Limiting**: Request rate control
  - Token bucket strategy
  - Sliding window strategy
  - Per-tenant rate limiting
  - Configurable limits and strategies
  - Rate limit metrics

#### Resilience
- **Bulkhead Pattern**: Resource isolation
  - Configurable concurrent operation limits
  - Request queuing with max queue size
  - Separate bulkheads for publish/subscribe
  - Bulkhead metrics (active, queued, rejected)
- **Poison Message Handling**: Automatic failure management
  - Configurable failure threshold
  - Poison message queue
  - Reprocess API
  - Automatic cleanup
  - Full diagnostic logging
- **Backpressure Management**: Consumer protection
  - Latency-based detection
  - Queue depth monitoring
  - Automatic throttling (50% reduction)
  - Automatic recovery
  - Backpressure events

#### Testing
- Comprehensive unit tests (85%+ coverage)
- Integration tests with test containers
- Performance benchmarks
- Chaos engineering tests
- Test utilities and helpers

#### Documentation
- Complete getting started guide
- Fluent configuration API guide
- Detailed configuration reference
- Best practices guide
- Troubleshooting guide
- Migration guide
- Code examples for all features
- Sample applications

### Changed
- **Breaking:** Minimum .NET version is now .NET 8.0
- **Breaking:** Configuration API redesigned with fluent builder pattern
- **Breaking:** Some option class names and namespaces changed for consistency
- Improved error messages and validation
- Enhanced logging throughout
- Better exception handling and error recovery
- Optimized performance for high-throughput scenarios

### Deprecated
- Legacy configuration methods (still supported but marked obsolete)
- Individual `Add*Pattern()` methods (use fluent API instead)

### Removed
- **Breaking:** .NET 6.0 and .NET 7.0 support
- **Breaking:** Legacy serialization options
- **Breaking:** Obsolete APIs from v1.x

### Fixed
- Connection leak in RabbitMQ implementation
- Race condition in circuit breaker state transitions
- Memory leak in deduplication cache
- Incorrect retry delay calculation
- Thread safety issues in connection pool
- Message ordering issues in batch processing

### Security
- Fixed potential information disclosure in error messages
- Improved encryption key handling
- Enhanced authentication token validation
- Added rate limiting to prevent DoS attacks

## [1.x] - Previous Versions

See [v1.x CHANGELOG](./CHANGELOG-v1.md) for older versions.

## Upgrade Guide

### From 1.x to 2.0

#### Configuration Changes

**Before (1.x):**
```csharp
services.AddMessageBroker(options => { /* ... */ });
services.AddOutboxPattern();
services.DecorateMessageBrokerWithOutbox();
```

**After (2.0):**
```csharp
services.AddMessageBrokerWithPatterns(options => { /* ... */ })
    .WithOutbox()
    .Build();
```

#### Breaking Changes

1. **Minimum .NET Version**: Upgrade to .NET 8.0
2. **Configuration API**: Use new fluent API
3. **Namespace Changes**: Update using statements
4. **Option Classes**: Some renamed for consistency

#### Migration Steps

1. Update project to .NET 8.0
2. Update NuGet package to 2.0.0
3. Replace configuration code with fluent API
4. Update namespace imports
5. Test thoroughly
6. Deploy incrementally

See [MIGRATION.md](../../docs/MessageBroker/MIGRATION.md) for detailed migration guide.

## Support

- **Documentation**: https://docs.relay.dev/messagebroker
- **Issues**: https://github.com/your-org/relay/issues
- **Discussions**: https://github.com/your-org/relay/discussions
- **Email**: support@relay.dev

## License

MIT License - see [LICENSE](../../LICENSE) for details.

# Configuration

<cite>
**Referenced Files in This Document**   
- [MessageBrokerOptions.cs](file://src/Relay.MessageBroker/Configuration/MessageBrokerOptions.cs)
- [MessageBrokerProfile.cs](file://src/Relay.MessageBroker/Configuration/MessageBrokerProfile.cs)
- [RelayOptions.cs](file://src/Relay.Core/Configuration/Options/Core/RelayOptions.cs)
- [PerformanceOptions.cs](file://src/Relay.Core/Configuration/Options/Performance/PerformanceOptions.cs)
- [PipelineOptions.cs](file://src/Relay.Core/Configuration/Options/Core/PipelineOptions.cs)
- [ValidationOptions.cs](file://src/Relay.Core/Configuration/Options/Core/ValidationOptions.cs)
- [RelayConfigurationExtensions.cs](file://src/Relay.Core/Configuration/Core/RelayConfigurationExtensions.cs)
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md)
- [FLUENT_CONFIGURATION.md](file://docs/MessageBroker/FLUENT_CONFIGURATION.md)
</cite>

## Table of Contents
1. [Fluent API Configuration](#fluent-api-configuration)
2. [Configuration File Approach](#configuration-file-approach)
3. [Attribute-Based Configuration](#attribute-based-configuration)
4. [Code-Based Configuration via Extension Methods](#code-based-configuration-via-extension-methods)
5. [Configuring Pipeline Behaviors](#configuring-pipeline-behaviors)
6. [Configuring Message Brokers](#configuring-message-brokers)
7. [Performance Settings Configuration](#performance-settings-configuration)
8. [Configuration Hierarchy and Precedence](#configuration-hierarchy-and-precedence)
9. [Best Practices for Configuration Management](#best-practices-for-configuration-management)
10. [Configuration Validation and Error Handling](#configuration-validation-and-error-handling)
11. [Complex Configuration Scenarios](#complex-configuration-scenarios)

## Fluent API Configuration

The Relay framework provides a fluent API configuration approach that enables code-based setup with full IntelliSense support. This approach allows developers to configure the message broker and its various patterns in a clean, readable, and type-safe manner. The fluent API is designed to guide developers through the configuration process, providing method chaining that makes it easy to discover available options and their relationships.

The fluent API centers around the `AddMessageBrokerWithPatterns` and `AddMessageBrokerWithProfile` extension methods, which return a builder pattern that enables sequential configuration of various components. This approach eliminates the need for manual service registration and decorator ordering, as the fluent API handles these concerns automatically.

Configuration profiles are available for common scenarios such as development, production, high throughput, and high reliability. These profiles provide pre-configured settings optimized for specific use cases, allowing developers to start with a sensible baseline and customize as needed. For example, the development profile enables minimal features with in-memory stores, while the production profile enables all reliability and observability features.

The fluent API also supports incremental configuration of individual features such as outbox pattern, inbox pattern, connection pooling, batch processing, deduplication, health checks, metrics, distributed tracing, encryption, authentication, rate limiting, bulkhead pattern, poison message handling, and backpressure management. Each feature can be configured with specific options through lambda expressions that provide IntelliSense support for available properties.

**Section sources**
- [FLUENT_CONFIGURATION.md](file://docs/MessageBroker/FLUENT_CONFIGURATION.md#L1-L554)
- [MessageBrokerOptions.cs](file://src/Relay.MessageBroker/Configuration/MessageBrokerOptions.cs#L1-L50)

## Configuration File Approach

The Relay framework supports configuration through JSON files and other formats for environment-specific settings. This approach allows different configurations to be used across development, testing, staging, and production environments without changing code. The framework integrates with the standard .NET configuration system, enabling configuration values to be loaded from appsettings.json files, environment variables, command-line arguments, and other configuration providers.

For message broker configuration, settings can be defined in appsettings.json with a structure that mirrors the configuration options classes. For example, message broker settings can be organized under a "MessageBroker" section with subsections for specific broker types like RabbitMQ, Kafka, Azure Service Bus, AWS SQS/SNS, NATS, and Redis Streams. Each subsection contains the relevant connection settings and options for that broker.

Environment-specific configuration files such as appsettings.Development.json, appsettings.Production.json, and appsettings.Staging.json allow different settings to be applied based on the current environment. This enables developers to use different connection strings, enable or disable specific features, and adjust performance settings based on the deployment context.

The configuration system also supports hierarchical merging of settings, where values from multiple configuration sources are combined. This allows base settings to be defined in appsettings.json while environment-specific overrides are provided through environment variables or other configuration providers. This approach is particularly useful for sensitive information like connection strings and API keys, which can be provided through environment variables rather than being stored in configuration files.

**Section sources**
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md#L1-L800)
- [MessageBrokerOptions.cs](file://src/Relay.MessageBroker/Configuration/MessageBrokerOptions.cs#L1-L50)

## Attribute-Based Configuration

The Relay framework supports attribute-based configuration that allows inline specification of behavior directly on classes, methods, and properties. This approach enables developers to configure specific aspects of the framework's behavior at the point of use, making the configuration more discoverable and maintainable.

Attributes are available for various aspects of the framework, including authorization, caching, contract validation, monitoring, notification dispatch, pipeline behavior, and transaction management. For example, the `AuthorizeAttribute` can be applied to handlers to specify authorization requirements, while the `CacheAttribute` can be used to enable caching for specific operations.

The `HandleAttribute` is used to mark classes as message handlers, while the `NotificationAttribute` identifies classes as notification handlers. These attributes eliminate the need for explicit registration in configuration, as the framework automatically discovers and registers types decorated with these attributes.

Pipeline behavior can be configured using the `PipelineAttribute`, which allows developers to specify the order and scope of pipeline behaviors for specific handlers. This enables fine-grained control over the execution pipeline, allowing different handlers to have different pipeline configurations based on their requirements.

Attribute-based configuration also supports versioning through the `HandlerVersionAttribute`, which allows multiple versions of a handler to coexist and be selected based on version information in the request. This facilitates gradual migration and backward compatibility when evolving APIs.

**Section sources**
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md#L1-L800)
- [FLUENT_CONFIGURATION.md](file://docs/MessageBroker/FLUENT_CONFIGURATION.md#L1-L554)

## Code-Based Configuration via Extension Methods

The Relay framework provides numerous extension methods for code-based configuration of various components. These extension methods follow the IServiceCollection pattern, allowing configuration to be added to the dependency injection container in a consistent and discoverable manner.

For message broker configuration, extension methods such as `AddOutboxPattern`, `AddInboxPattern`, `AddConnectionPooling`, `AddBatchProcessing`, `AddDeduplication`, `AddMessageBrokerHealthChecks`, `AddMessageBrokerMetrics`, `AddDistributedTracing`, `AddMessageEncryption`, `AddMessageBrokerSecurity`, `AddRateLimiting`, `AddBulkhead`, `AddPoisonMessageHandling`, and `AddBackpressure` enable specific features to be configured with strongly-typed options.

Each extension method accepts a lambda expression that provides access to the corresponding options class, enabling IntelliSense support for available configuration properties. For example, `AddOutboxPattern` provides access to `OutboxOptions`, while `AddDistributedTracing` provides access to `DistributedTracingOptions`.

The extension methods handle the registration of required services and decorators, ensuring that components are properly wired together. They also perform validation of configuration options, throwing exceptions with descriptive messages if invalid values are provided. This early validation helps catch configuration errors during application startup rather than at runtime.

Additional extension methods are available for configuring specific message brokers, such as `AddRabbitMQ`, `AddKafka`, `AddAzureServiceBus`, `AddAwsSqsSns`, `AddNats`, and `AddRedisStreams`. These methods configure the broker-specific settings and register the appropriate implementations.

**Section sources**
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md#L1-L800)
- [FLUENT_CONFIGURATION.md](file://docs/MessageBroker/FLUENT_CONFIGURATION.md#L1-L554)
- [RelayConfigurationExtensions.cs](file://src/Relay.Core/Configuration/Core/RelayConfigurationExtensions.cs#L1-L50)

## Configuring Pipeline Behaviors

Pipeline behaviors in the Relay framework can be configured through both code-based and attribute-based approaches. The framework provides a flexible pipeline system that allows cross-cutting concerns such as logging, validation, authorization, caching, and error handling to be applied consistently across handlers.

The `PipelineOptions` class contains configuration settings for pipeline behaviors, including the default order, scope, caching behavior, and execution timeout. These options can be configured through the fluent API or extension methods, allowing global defaults to be established while still permitting per-handler overrides through attributes.

Multiple pipeline behaviors can be registered in a specific order, with each behavior having a defined position in the execution pipeline. The framework supports both global pipeline behaviors that apply to all handlers and scoped behaviors that apply only to specific handlers or handler types.

The pipeline system is extensible, allowing developers to create custom pipeline behaviors by implementing the `IPipelineBehavior` interface. These custom behaviors can then be registered through extension methods or configuration, enabling the framework to be adapted to specific application requirements.

Pipeline behaviors can be configured to run conditionally based on message type, handler type, or other criteria. This allows different processing pipelines to be established for different types of messages or handlers, optimizing performance and resource usage.

**Section sources**
- [PipelineOptions.cs](file://src/Relay.Core/Configuration/Options/Core/PipelineOptions.cs#L1-L30)
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md#L1-L800)

## Configuring Message Brokers

The Relay framework supports configuration of multiple message brokers through a unified configuration system. The `MessageBrokerOptions` class serves as the central configuration point, containing properties for different broker types including RabbitMQ, Kafka, Azure Service Bus, AWS SQS/SNS, NATS, and Redis Streams.

Each broker type has its own options class that contains broker-specific settings. For example, `RabbitMQOptions` contains properties for host name, port, user name, password, virtual host, and other RabbitMQ-specific settings. Similarly, `KafkaOptions` contains properties for bootstrap servers, consumer group ID, and other Kafka-specific settings.

The framework uses the `MessageBrokerType` enum to specify which broker implementation to use, with options for RabbitMQ, Kafka, AzureServiceBus, AwsSqsSns, Nats, and RedisStreams. This enum is used in conjunction with the configuration options to determine which broker-specific settings to apply.

Configuration can be provided through multiple sources, with environment-specific settings taking precedence over default values. This allows different brokers to be used in different environments, such as using an in-memory broker for development and testing while using a production-grade broker like RabbitMQ or Kafka in production.

The framework also supports advanced broker configuration such as connection pooling, message compression, encryption, authentication, and network resilience features like circuit breakers and retry policies. These features can be configured independently of the broker type, providing a consistent configuration experience across different brokers.

**Section sources**
- [MessageBrokerOptions.cs](file://src/Relay.MessageBroker/Configuration/MessageBrokerOptions.cs#L1-L50)
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md#L1-L800)

## Performance Settings Configuration

Performance settings in the Relay framework can be configured through the `PerformanceOptions` class and related configuration options. These settings allow developers to optimize the framework's behavior for specific performance requirements and workloads.

Key performance settings include connection pooling configuration, batch processing options, message compression settings, and resource utilization limits. Connection pooling can be configured with minimum and maximum pool sizes, connection timeout, validation interval, and idle timeout settings to balance resource usage and performance.

Batch processing can be configured with maximum batch size, flush interval, compression settings, and partial retry behavior. These settings allow messages to be processed in batches, reducing network overhead and improving throughput for high-volume scenarios.

Message compression can be enabled with selection of compression algorithm (GZip, Brotli, Deflate) and compression level. This reduces message size and network bandwidth usage, particularly for large messages or high-throughput scenarios.

The framework also provides settings for bulkhead pattern (limiting concurrent operations), rate limiting (controlling request rates), and backpressure management (responding to system load). These settings help prevent resource exhaustion and maintain system stability under heavy load.

Performance profiles can be used to apply pre-configured performance settings for common scenarios such as high throughput processing or high reliability systems. These profiles provide optimized defaults that can be further customized as needed.

**Section sources**
- [PerformanceOptions.cs](file://src/Relay.Core/Configuration/Options/Performance/PerformanceOptions.cs#L1-L50)
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md#L1-L800)

## Configuration Hierarchy and Precedence

The Relay framework follows a well-defined hierarchy and precedence for configuration methods, ensuring predictable behavior when multiple configuration sources are used. The configuration system follows the principle of "convention over configuration" with sensible defaults that can be overridden as needed.

The precedence order from highest to lowest is: code-based configuration via extension methods, fluent API configuration, attribute-based configuration, and configuration file settings. This means that settings applied through code take precedence over those in configuration files, allowing environment-specific overrides to be applied programmatically.

Within code-based configuration, more specific settings take precedence over general ones. For example, per-handler configuration through attributes takes precedence over global configuration through extension methods. This allows default behavior to be established at the application level while permitting exceptions for specific handlers.

Configuration profiles provide a middle ground between defaults and explicit configuration, offering pre-configured settings for common scenarios. These profiles can be used as a starting point and then customized through additional configuration.

The framework also supports conditional configuration based on environment, allowing different settings to be applied in development, testing, staging, and production environments. This is typically achieved through environment-specific configuration files or conditional logic in code.

When multiple configuration sources provide values for the same setting, the highest precedence source wins. If a setting is not specified in a higher precedence source, the framework falls back to lower precedence sources until a value is found or the default is used.

**Section sources**
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md#L1-L800)
- [FLUENT_CONFIGURATION.md](file://docs/MessageBroker/FLUENT_CONFIGURATION.md#L1-L554)

## Best Practices for Configuration Management

Effective configuration management in the Relay framework involves following several best practices to ensure reliability, security, and maintainability. These practices help organizations manage configuration across different environments and team members.

Use configuration profiles as a starting point for common scenarios, then customize as needed. This reduces configuration complexity and ensures consistency across similar applications. Profiles like Development, Production, HighThroughput, and HighReliability provide optimized defaults for specific use cases.

Enable features incrementally, particularly in development environments. Start with core functionality and add advanced features like outbox, inbox, deduplication, and distributed tracing as needed. This simplifies troubleshooting and reduces resource usage during development.

Validate configuration early by calling validation methods during application startup. This catches configuration errors before the application becomes available, preventing runtime failures due to misconfiguration.

Monitor configuration in production by enabling health checks and metrics. This provides visibility into the message broker's status and performance, enabling proactive issue detection and resolution.

Secure sensitive configuration data by using environment variables or secure configuration stores for secrets like connection strings, API keys, and encryption keys. Avoid storing sensitive information in configuration files that may be committed to source control.

Plan for failure by enabling reliability features like outbox, inbox, and poison message handling in production environments. These features help ensure message delivery and processing even in the face of transient failures.

Optimize configuration for the specific workload. Use high throughput profiles for event processing systems and high reliability profiles for mission-critical applications. Adjust performance settings based on actual usage patterns and requirements.

Document configuration decisions and rationale, particularly for non-default settings. This helps ensure consistency across team members and provides context for future maintenance.

**Section sources**
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md#L1-L800)
- [FLUENT_CONFIGURATION.md](file://docs/MessageBroker/FLUENT_CONFIGURATION.md#L1-L554)

## Configuration Validation and Error Handling

The Relay framework includes comprehensive validation for configuration options to prevent runtime errors due to invalid settings. Validation occurs at multiple levels, including property-level validation, cross-property validation, and system-level validation.

Property-level validation ensures that individual configuration properties meet their requirements, such as minimum and maximum values, valid ranges, and acceptable formats. For example, polling intervals have minimum values (100ms for outbox polling), batch sizes have valid ranges (1-10000), and retry counts have limits (0-10).

Cross-property validation ensures that related properties are configured consistently. For example, if exponential backoff is enabled, the retry delay must be positive. Similarly, if connection validation is enabled, the validation interval must be positive.

System-level validation checks the overall configuration for consistency and compatibility. This includes verifying that required services are available, that dependencies are properly configured, and that there are no conflicting settings.

Validation is performed when configuration is applied, typically during application startup. If validation fails, the framework throws descriptive exceptions that identify the specific configuration issue. This early failure prevents the application from starting with invalid configuration.

The fluent API includes built-in validation that is triggered when the `Build()` method is called. This ensures that all configuration is valid before the message broker is constructed. If validation fails, an `InvalidOperationException` is thrown with details about the validation failure.

Error handling for invalid configurations should include logging the error, providing clear error messages to administrators, and preventing the application from starting until the configuration is corrected. This prevents silent failures and ensures that configuration issues are addressed promptly.

**Section sources**
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md#L1-L800)
- [FLUENT_CONFIGURATION.md](file://docs/MessageBroker/FLUENT_CONFIGURATION.md#L1-L554)

## Complex Configuration Scenarios

The Relay framework supports complex configuration scenarios involving multiple components and advanced patterns. These scenarios demonstrate how different configuration approaches can be combined to address sophisticated requirements.

For a microservice with full observability, configure the message broker with outbox and inbox patterns, connection pooling, health checks, metrics, and distributed tracing. Set up distributed tracing with OTLP exporter to send telemetry data to a collector, and configure metrics to expose Prometheus endpoints for monitoring.

For high-volume event processing, use the high throughput profile with Kafka as the message broker. Configure batch processing with large batch sizes and short flush intervals, enable message compression with Brotli algorithm at maximum compression level, and set up deduplication with a short time window and large cache size. Configure backpressure management to respond to system load and prevent resource exhaustion.

For a secure multi-tenant system, use Azure Service Bus as the message broker with encryption configured to use Azure Key Vault for key management. Enable authentication with JWT tokens and role-based authorization, and configure rate limiting with per-tenant limits. Set up separate message queues or topics for each tenant to ensure data isolation.

For a mission-critical system, use the high reliability profile with RabbitMQ as the message broker. Enable all resilience patterns including outbox with aggressive polling, inbox with long retention, connection pooling with validation, message deduplication, health checks, metrics, distributed tracing, rate limiting, bulkhead pattern, poison message handling, and backpressure management. Configure circuit breakers and retry policies to handle transient failures.

These complex scenarios demonstrate how the Relay framework's configuration system can be used to create robust, scalable, and maintainable message processing systems. By combining different configuration approaches and features, developers can address a wide range of requirements and constraints.

**Section sources**
- [CONFIGURATION.md](file://docs/MessageBroker/CONFIGURATION.md#L1-L800)
- [FLUENT_CONFIGURATION.md](file://docs/MessageBroker/FLUENT_CONFIGURATION.md#L1-L554)
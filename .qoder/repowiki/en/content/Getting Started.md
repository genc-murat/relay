# Getting Started

<cite>
**Referenced Files in This Document**   
- [README.md](file://README.md)
- [docs/MessageBroker/GETTING_STARTED.md](file://docs/MessageBroker/GETTING_STARTED.md)
- [docs/MessageBroker/NUGET_PACKAGES.md](file://docs/MessageBroker/NUGET_PACKAGES.md)
- [src/Relay/RelayServiceCollectionExtensions.cs](file://src/Relay/RelayServiceCollectionExtensions.cs)
- [src/Relay/Core/Configuration/MessageQueueOptions.cs](file://src/Relay/Core/Configuration/MessageQueueOptions.cs)
- [samples/MinimalApiSample/Program.cs](file://samples/MinimalApiSample/Program.cs)
- [samples/Relay.ControllerApiSample/Program.cs](file://samples/Relay.ControllerApiSample/Program.cs)
- [samples/MinimalApiSample/appsettings.json](file://samples/MinimalApiSample/appsettings.json)
- [samples/MinimalApiSample/Relay.MessageBroker.MinimalApiSample.csproj](file://samples/MinimalApiSample/Relay.MessageBroker.MinimalApiSample.csproj)
</cite>

## Table of Contents
1. [Installation](#installation)
2. [Setup and Service Registration](#setup-and-service-registration)
3. [Basic Usage Examples](#basic-usage-examples)
4. [Configuration Options](#configuration-options)
5. [Step-by-Step Beginner Guide](#step-by-step-beginner-guide)
6. [Common Pitfalls and Solutions](#common-pitfalls-and-solutions)
7. [Troubleshooting Initialization Issues](#troubleshooting-initialization-issues)
8. [Performance Considerations](#performance-considerations)

## Installation

The Relay framework is distributed through NuGet packages, with different packages serving various purposes. The core package provides the fundamental mediator functionality, while additional packages offer extended features.

### Core Package Installation

To get started with the Relay framework, install the core package:

```bash
dotnet add package Relay.Core
```

This package includes the essential components for request/response patterns, notification publishing, and handler execution.

### Message Broker Integration

For applications requiring message broker integration, install the Relay.MessageBroker package:

```bash
dotnet add package Relay.MessageBroker
```

This package provides comprehensive support for multiple message brokers including RabbitMQ, Kafka, Azure Service Bus, AWS SQS/SNS, NATS, and Redis Streams. It also includes advanced patterns like Outbox, Inbox, connection pooling, batch processing, and message deduplication.

### CLI Tool Installation

The Relay CLI provides development tools for scaffolding, migration, and optimization:

```bash
# Install globally
dotnet tool install -g Relay.CLI

# Update to latest version
dotnet tool update -g Relay.CLI
```

The CLI tool includes features such as:
- Automated MediatR to Relay migration
- Project health checks and diagnostics
- Project scaffolding with templates
- Performance analysis and optimization
- Code validation and best practices enforcement

**Section sources**
- [README.md](file://README.md#L57-L71)
- [docs/MessageBroker/NUGET_PACKAGES.md](file://docs/MessageBroker/NUGET_PACKAGES.md#L39-L42)

## Setup and Service Registration

The Relay framework integrates with Microsoft.Extensions.DependencyInjection through extension methods that simplify the registration process.

### Basic Service Registration

Register the Relay framework services in your application's service collection:

```csharp
services.AddRelay();
```

This method automatically registers all discovered handlers and core services through source generation at compile time, eliminating runtime reflection overhead.

### Feature-Specific Registration

For applications requiring specific features, use the appropriate extension methods:

```csharp
// Add validation, pre/post processors, and exception handlers
services.AddRelayWithFeatures();

// Add advanced features including AI optimization and performance monitoring
services.AddRelayWithAdvancedFeatures();

// Configure for specific scenarios (Web API, High Performance, Event-Driven, etc.)
services.AddRelayForScenario(RelayScenario.WebApi);
```

### Message Broker Registration

When using message broker integration, register the broker with specific configuration:

```csharp
// Configure RabbitMQ message broker
builder.Services.AddRabbitMQ(options =>
{
    options.HostName = "localhost";
    options.Port = 5672;
    options.UserName = "guest";
    options.Password = "guest";
});

// Add hosted service for automatic start/stop
builder.Services.AddMessageBrokerHostedService();
```

The framework supports multiple broker types through specific registration methods including `AddKafka`, `AddAzureServiceBus`, `AddAwsSqsSns`, `AddNats`, and `AddRedisStreams`.

**Section sources**
- [src/Relay/RelayServiceCollectionExtensions.cs](file://src/Relay/RelayServiceCollectionExtensions.cs#L38-L127)
- [samples/MinimalApiSample/Program.cs](file://samples/MinimalApiSample/Program.cs#L10-L27)

## Basic Usage Examples

### Sending Requests

The Relay framework follows the request/response pattern for command and query handling:

```csharp
// Define a request
public record GetUserQuery(int UserId) : IRequest<User>;

// Define a handler
public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    [Handle]
    public async ValueTask<User> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        return await _userRepository.GetByIdAsync(request.UserId);
    }
}

// Send the request
User user = await _relay.SendAsync(new GetUserQuery(123));
```

### Publishing Notifications

The framework supports event-driven architecture through notification publishing:

```csharp
// Define a notification
public record UserCreated(int UserId, string Email) : INotification;

// Multiple handlers can handle the same notification
public class SendWelcomeEmailHandler : INotificationHandler<UserCreated>
{
    [Notification(Priority = 1)]
    public async ValueTask HandleAsync(UserCreated notification, CancellationToken cancellationToken)
    {
        await _emailService.SendWelcomeEmailAsync(notification.Email);
    }
}

// Publish notification
await _relay.PublishAsync(new UserCreated(123, "user@example.com"));
```

### Creating Handlers

Handlers are created by implementing the appropriate interface and using the `[Handle]` attribute:

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserCommand, User>
{
    [Handle]
    public async ValueTask<User> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Business logic here
        var user = new User(request.Name, request.Email);
        await _userRepository.AddAsync(user);
        return user;
    }
}
```

**Section sources**
- [README.md](file://README.md#L87-L216)
- [src/Relay/RelayServiceCollectionExtensions.cs](file://src/Relay/RelayServiceCollectionExtensions.cs#L231-L236)

## Configuration Options

The Relay framework provides flexible configuration options through both code-based configuration and JSON configuration approaches.

### Code-Based Configuration

Configure the framework directly in code using lambda expressions:

```csharp
// Global configuration
services.ConfigureRelay(options =>
{
    options.EnableTelemetry = true;
    options.MaxConcurrentNotificationHandlers = 10;
});

// Handler-specific configuration
services.ConfigureHandler("GetUserHandler.HandleAsync", options =>
{
    options.EnableCaching = true;
    options.DefaultTimeout = TimeSpan.FromSeconds(30);
    options.EnableRetry = true;
    options.MaxRetryAttempts = 3;
});
```

### JSON Configuration

Configuration can also be provided through JSON files such as appsettings.json:

```json
{
  "Relay": {
    "EnableTelemetry": true,
    "MaxConcurrentNotificationHandlers": 10,
    "Performance": {
      "EnableSIMDOptimizations": true,
      "EnableHandlerCache": true
    }
  }
}
```

Then register the configuration:

```csharp
services.ConfigureRelay(Configuration.GetSection("Relay"));
```

### Performance Profiles

The framework offers pre-configured performance profiles:

```csharp
// Low Memory profile (best for containers/serverless)
services.AddRelay().WithPerformanceProfile(PerformanceProfile.LowMemory);

// Balanced profile (recommended for most applications)
services.AddRelay().WithPerformanceProfile(PerformanceProfile.Balanced);

// High Throughput profile (best for high-traffic APIs)
services.AddRelay().WithPerformanceProfile(PerformanceProfile.HighThroughput);

// Ultra Low Latency profile (best for trading, gaming, real-time systems)
services.AddRelay().WithPerformanceProfile(PerformanceProfile.UltraLowLatency);
```

**Section sources**
- [README.md](file://README.md#L544-L562)
- [samples/MinimalApiSample/appsettings.json](file://samples/MinimalApiSample/appsettings.json#L1-L17)
- [src/Relay/RelayServiceCollectionExtensions.cs](file://src/Relay/RelayServiceCollectionExtensions.cs#L136-L163)

## Step-by-Step Beginner Guide

### Minimal Example

Start with a minimal example to understand the basic workflow:

1. **Define your request and handler:**
```csharp
public record GetUserQuery(int UserId) : IRequest<User>;

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    [Handle]
    public async ValueTask<User> HandleAsync(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Return mock data for simplicity
        return new User { Id = request.UserId, Name = "John Doe" };
    }
}
```

2. **Register services in Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRelay();
var app = builder.Build();
```

3. **Use the mediator in your application:**
```csharp
app.MapGet("/user/{id}", async (int id, IRelay relay) =>
{
    return await relay.SendAsync(new GetUserQuery(id));
});
```

### Intermediate Example with Message Broker

Build upon the minimal example by adding message broker integration:

1. **Install the Relay.MessageBroker package:**
```bash
dotnet add package Relay.MessageBroker
```

2. **Configure the message broker:**
```csharp
builder.Services.AddRabbitMQ(options =>
{
    options.HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
    options.Port = int.Parse(builder.Configuration["RabbitMQ:Port"] ?? "5672");
    options.UserName = builder.Configuration["RabbitMQ:UserName"] ?? "guest";
    options.Password = builder.Configuration["RabbitMQ:Password"] ?? "guest";
});

builder.Services.AddMessageBrokerHostedService();
```

3. **Publish messages:**
```csharp
await _messageBroker.PublishAsync(new OrderCreatedEvent
{
    OrderId = order.Id,
    Amount = order.Amount,
    CreatedAt = DateTime.UtcNow
});
```

4. **Subscribe to messages:**
```csharp
await _messageBroker.SubscribeAsync<OrderCreatedEvent>(
    async (message, context, ct) =>
    {
        await ProcessOrderAsync(message);
        await context.Acknowledge!();
    });
```

**Section sources**
- [samples/MinimalApiSample/Program.cs](file://samples/MinimalApiSample/Program.cs#L1-L125)
- [docs/MessageBroker/GETTING_STARTED.md](file://docs/MessageBroker/GETTING_STARTED.md#L58-L131)

## Common Pitfalls and Solutions

### Missing Source Generator

**Problem:** The `AddRelay()` method is not available or handlers are not being discovered.

**Solution:** Ensure the Relay.SourceGenerator package is properly referenced in your project. Check that your .csproj file includes:

```xml
<ProjectReference Include="..\..\src\Relay.SourceGenerator\Relay.SourceGenerator.csproj" 
                  OutputItemType="Analyzer" 
                  ReferenceOutputAssembly="false" />
```

### Configuration Section Not Found

**Problem:** Configuration values are not being applied from appsettings.json.

**Solution:** Verify that the configuration section name matches exactly. By default, the framework looks for a "Relay" section:

```json
{
  "Relay": {
    "EnableTelemetry": true
  }
}
```

If using a different section name, specify it explicitly:

```csharp
services.ConfigureRelay(Configuration.GetSection("CustomRelaySection"));
```

### Message Broker Connection Issues

**Problem:** Unable to connect to the message broker.

**Solution:** Verify connection settings and ensure the broker is running. Use configuration from appsettings.json:

```json
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672",
    "UserName": "guest",
    "Password": "guest"
  }
}
```

And access these values in code:

```csharp
options.HostName = builder.Configuration["RabbitMQ:HostName"] ?? "localhost";
```

**Section sources**
- [src/Relay/Relay.csproj](file://src/Relay/Relay.csproj#L94)
- [samples/MinimalApiSample/appsettings.json](file://samples/MinimalApiSample/appsettings.json#L10-L15)
- [samples/MinimalApiSample/Program.cs](file://samples/MinimalApiSample/Program.cs#L18-L22)

## Troubleshooting Initialization Issues

### Service Registration Order

**Issue:** Services are not registered in the correct order.

**Resolution:** Ensure that core services are registered before feature-specific services:

```csharp
// Correct order
services.AddRelayCore();
services.AddRelayValidation();
services.AddRelayPrePostProcessors();
```

### Missing Hosted Service

**Issue:** Message broker does not start automatically.

**Resolution:** Add the message broker hosted service:

```csharp
builder.Services.AddMessageBrokerHostedService();
```

This ensures the message broker starts and stops with the application lifecycle.

### Configuration Binding Problems

**Issue:** Configuration values are not properly bound to options classes.

**Resolution:** Verify that configuration keys match the options properties exactly:

```csharp
// Options class
public class RabbitMQOptions
{
    public string HostName { get; set; }
    public int Port { get; set; }
}

// Configuration (keys must match property names exactly)
{
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": "5672"
  }
}
```

**Section sources**
- [src/Relay/RelayServiceCollectionExtensions.cs](file://src/Relay/RelayServiceCollectionExtensions.cs#L218-L240)
- [samples/MinimalApiSample/Program.cs](file://samples/MinimalApiSample/Program.cs#L27)

## Performance Considerations

### Optimal Configuration from Start

Configure performance settings from the beginning to avoid rework:

```csharp
services.AddRelay()
    .WithPerformanceProfile(PerformanceProfile.Balanced)
    .ConfigurePerformance(perf =>
    {
        perf.EnableSIMDOptimizations = true;
        perf.EnableHandlerCache = true;
        perf.HandlerCacheMaxSize = 10000;
        perf.EnableMemoryPrefetch = true;
        perf.UsePreAllocatedExceptions = true;
        perf.EnableZeroAllocationPaths = true;
    });
```

### Connection Pooling

Enable connection pooling for better resource utilization:

```csharp
builder.Services.AddConnectionPooling<IConnection>(options =>
{
    options.MinPoolSize = 5;
    options.MaxPoolSize = 50;
    options.ConnectionTimeout = TimeSpan.FromSeconds(5);
});
```

### Batch Processing

Use batch processing for high-volume scenarios:

```csharp
builder.Services.AddBatchProcessing(options =>
{
    options.Enabled = true;
    options.MaxBatchSize = 100;
    options.FlushInterval = TimeSpan.FromMilliseconds(100);
});
```

These performance optimizations should be implemented from the start to ensure optimal application performance and scalability.

**Section sources**
- [README.md](file://README.md#L114-L152)
- [docs/MessageBroker/GETTING_STARTED.md](file://docs/MessageBroker/GETTING_STARTED.md#L224-L233)
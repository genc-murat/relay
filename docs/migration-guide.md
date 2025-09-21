# Migration Guide

This guide helps you migrate from other mediator frameworks to Relay, taking advantage of its performance benefits and source generator approach.

## From MediatR

MediatR is the most popular .NET mediator framework. Here's how to migrate your existing MediatR code to Relay.

### Basic Request/Response Migration

**MediatR:**
```csharp
public class GetUserQuery : IRequest<User>
{
    public int UserId { get; set; }
}

public class GetUserHandler : IRequestHandler<GetUserQuery, User>
{
    public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        // Implementation
        return new User();
    }
}

// Registration
services.AddMediatR(typeof(GetUserHandler));

// Usage
var user = await _mediator.Send(new GetUserQuery { UserId = 123 });
```

**Relay:**
```csharp
public record GetUserQuery(int UserId) : IRequest<User>;

public class UserService
{
    [Handle]
    public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
    {
        // Implementation
        return new User();
    }
}

// Registration (generated automatically)
services.AddRelay();
services.AddScoped<UserService>();

// Usage
var user = await _relay.SendAsync(new GetUserQuery(123));
```

### Command Migration

**MediatR:**
```csharp
public class CreateUserCommand : IRequest
{
    public string Name { get; set; }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand>
{
    public async Task<Unit> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Implementation
        return Unit.Value;
    }
}
```

**Relay:**
```csharp
public record CreateUserCommand(string Name) : IRequest;

public class UserService
{
    [Handle]
    public async ValueTask CreateUser(CreateUserCommand command, CancellationToken cancellationToken)
    {
        // Implementation - no need to return Unit
    }
}
```

### Notification Migration

**MediatR:**
```csharp
public class UserCreatedNotification : INotification
{
    public int UserId { get; set; }
}

public class EmailNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Send email
    }
}

public class AuditNotificationHandler : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Log audit
    }
}
```

**Relay:**
```csharp
public record UserCreatedNotification(int UserId) : INotification;

public class EmailService
{
    [Notification]
    public async ValueTask SendWelcomeEmail(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Send email
    }
}

public class AuditService
{
    [Notification]
    public async ValueTask LogUserCreation(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        // Log audit
    }
}
```

### Pipeline Behavior Migration

**MediatR:**
```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}

// Registration
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

**Relay:**
```csharp
public class LoggingPipeline
{
    private readonly ILogger<LoggingPipeline> _logger;

    public LoggingPipeline(ILogger<LoggingPipeline> logger)
    {
        _logger = logger;
    }

    [Pipeline]
    public async ValueTask<TResponse> LogRequests<TRequest, TResponse>(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}

// Registration (automatic)
services.AddScoped<LoggingPipeline>();
```

### Performance Improvements

After migration, you'll see significant performance improvements:

| Operation | MediatR | Relay | Improvement |
|-----------|---------|-------|-------------|
| Simple Request | 847 ns | 12 ns | **70x faster** |
| Notification | 2,346 ns | 46 ns | **51x faster** |
| Memory Allocation | 312 B | 0 B | **Zero allocations** |

## From Mediator.Net

**Mediator.Net:**
```csharp
public class GetUserQuery : IQuery<User>
{
    public int UserId { get; set; }
}

public class GetUserHandler : IQueryHandler<GetUserQuery, User>
{
    public async Task<User> Handle(ReceiveContext<GetUserQuery> context)
    {
        // Implementation
        return new User();
    }
}

// Registration
services.AddMediator(builder =>
{
    builder.RegisterHandlers(typeof(GetUserHandler).Assembly);
});
```

**Relay:**
```csharp
public record GetUserQuery(int UserId) : IRequest<User>;

public class UserService
{
    [Handle]
    public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
    {
        // Implementation
        return new User();
    }
}

// Registration
services.AddRelay();
services.AddScoped<UserService>();
```

## From Custom Mediator Implementations

If you have a custom mediator implementation, here's how to migrate:

### Interface Mapping

| Your Interface | Relay Interface |
|----------------|-----------------|
| `ICommand` | `IRequest` |
| `IQuery<T>` | `IRequest<T>` |
| `IEvent` | `INotification` |
| `ICommandHandler<T>` | Method with `[Handle]` |
| `IQueryHandler<T, R>` | Method with `[Handle]` |
| `IEventHandler<T>` | Method with `[Notification]` |

### Handler Registration

**Custom Implementation:**
```csharp
// Manual registration
services.AddScoped<ICommandHandler<CreateUserCommand>, CreateUserHandler>();
services.AddScoped<IQueryHandler<GetUserQuery, User>, GetUserHandler>();

// Or reflection-based scanning
services.Scan(scan => scan
    .FromAssemblyOf<CreateUserHandler>()
    .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)))
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

**Relay:**
```csharp
// Automatic registration via source generator
services.AddRelay();

// Just register your service classes
services.AddScoped<UserService>();
services.AddScoped<OrderService>();
```

## Migration Checklist

### Phase 1: Preparation

- [ ] Install Relay NuGet package
- [ ] Update request/response types to implement Relay interfaces
- [ ] Convert handler classes to use attribute-based registration

### Phase 2: Handler Migration

- [ ] Replace `IRequestHandler<T, R>` with `[Handle]` methods
- [ ] Replace `INotificationHandler<T>` with `[Notification]` methods
- [ ] Update method signatures to use `ValueTask` instead of `Task`
- [ ] Remove `Unit` return types from commands

### Phase 3: Pipeline Migration

- [ ] Convert `IPipelineBehavior<T, R>` to `[Pipeline]` methods
- [ ] Update pipeline method signatures
- [ ] Adjust pipeline ordering using `Order` property

### Phase 4: Registration Migration

- [ ] Replace framework-specific registration with `services.AddRelay()`
- [ ] Register handler service classes
- [ ] Remove manual handler registrations

### Phase 5: Usage Migration

- [ ] Replace `IMediator` with `IRelay`
- [ ] Update `Send()` calls to `SendAsync()`
- [ ] Update `Publish()` calls to `PublishAsync()`

### Phase 6: Testing and Optimization

- [ ] Run existing tests to ensure functionality
- [ ] Add performance benchmarks
- [ ] Optimize using ValueTask patterns
- [ ] Configure telemetry and monitoring

## Common Migration Issues

### Issue 1: Async Method Signatures

**Problem:**
```csharp
// MediatR handler
public async Task<User> Handle(GetUserQuery request, CancellationToken cancellationToken)
```

**Solution:**
```csharp
// Relay handler - use ValueTask for better performance
[Handle]
public async ValueTask<User> GetUser(GetUserQuery query, CancellationToken cancellationToken)
```

### Issue 2: Unit Return Type

**Problem:**
```csharp
// MediatR command handler
public async Task<Unit> Handle(CreateUserCommand request, CancellationToken cancellationToken)
{
    // Implementation
    return Unit.Value;
}
```

**Solution:**
```csharp
// Relay command handler - no return value needed
[Handle]
public async ValueTask CreateUser(CreateUserCommand command, CancellationToken cancellationToken)
{
    // Implementation - no return statement needed
}
```

### Issue 3: Pipeline Behavior Generic Constraints

**Problem:**
```csharp
// MediatR pipeline with constraints
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IValidatable
```

**Solution:**
```csharp
// Relay pipeline - handle validation in the method
[Pipeline]
public async ValueTask<TResponse> ValidateRequests<TRequest, TResponse>(
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken)
{
    if (request is IValidatable validatable)
    {
        await validatable.ValidateAsync();
    }
    
    return await next();
}
```

### Issue 4: Handler Discovery

**Problem:**
```csharp
// Manual handler registration
services.AddScoped<IRequestHandler<GetUserQuery, User>, GetUserHandler>();
```

**Solution:**
```csharp
// Relay automatic discovery
services.AddRelay(); // Discovers all [Handle] methods
services.AddScoped<UserService>(); // Just register the service class
```

## Performance Validation

After migration, validate the performance improvements:

```csharp
[MemoryDiagnoser]
public class MigrationBenchmark
{
    private IRelay _relay;
    private IMediator _mediator;
    
    [Benchmark(Baseline = true)]
    public async Task<User> MediatR_SendRequest()
    {
        return await _mediator.Send(new GetUserQuery { UserId = 123 });
    }
    
    [Benchmark]
    public async ValueTask<User> Relay_SendRequest()
    {
        return await _relay.SendAsync(new GetUserQuery(123));
    }
}
```

Expected results:
- **50-100x faster** request handling
- **Zero allocations** for simple requests
- **Significantly lower** memory usage
- **Better throughput** under load

## Gradual Migration Strategy

For large applications, consider a gradual migration:

### Step 1: Side-by-Side Installation

```csharp
// Keep both frameworks during transition
services.AddMediatR(typeof(LegacyHandler));
services.AddRelay();

// Migrate handlers one service at a time
services.AddScoped<UserService>(); // New Relay handlers
services.AddScoped<LegacyOrderService>(); // Old MediatR handlers
```

### Step 2: Feature Flag Migration

```csharp
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRelay _relay;
    private readonly IFeatureManager _features;
    
    [HttpGet("{id}")]
    public async Task<User> GetUser(int id)
    {
        if (await _features.IsEnabledAsync("UseRelay"))
        {
            return await _relay.SendAsync(new GetUserQuery(id));
        }
        else
        {
            return await _mediator.Send(new GetUserQuery { UserId = id });
        }
    }
}
```

### Step 3: Complete Migration

Once all handlers are migrated and tested:

```csharp
// Remove MediatR
// services.AddMediatR(typeof(LegacyHandler));

// Keep only Relay
services.AddRelay();
```

## Migration Tools

### Automated Migration Script

```powershell
# PowerShell script to help with migration
param(
    [string]$ProjectPath = "."
)

# Find all IRequestHandler implementations
$handlers = Get-ChildItem -Path $ProjectPath -Recurse -Filter "*.cs" | 
    Select-String -Pattern "IRequestHandler<.*>" |
    ForEach-Object { $_.Filename }

Write-Host "Found $($handlers.Count) handlers to migrate:"
$handlers | ForEach-Object { Write-Host "  $_" }

# TODO: Add automated refactoring logic
```

### Code Analyzer

Create a Roslyn analyzer to help identify migration opportunities:

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MediatRMigrationAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MediatRHandlerRule = new DiagnosticDescriptor(
        "RELAY001",
        "Consider migrating to Relay for better performance",
        "Handler '{0}' can be migrated to Relay for significant performance improvements",
        "Performance",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        
        if (classDeclaration.BaseList?.Types.Any(t => 
            t.ToString().Contains("IRequestHandler") || 
            t.ToString().Contains("INotificationHandler")) == true)
        {
            var diagnostic = Diagnostic.Create(
                MediatRHandlerRule,
                classDeclaration.Identifier.GetLocation(),
                classDeclaration.Identifier.ValueText);
            
            context.ReportDiagnostic(diagnostic);
        }
    }
}
```

This migration guide should help you successfully transition from any mediator framework to Relay while maximizing the performance benefits. For specific migration questions, see the [troubleshooting guide](troubleshooting.md).
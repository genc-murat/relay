# Simple CRUD API Example

This example demonstrates a basic CRUD (Create, Read, Update, Delete) API using Relay in an ASP.NET Core application.

## Features Demonstrated

- Basic request/response handlers
- Command handlers (no response)
- Dependency injection setup
- Controller integration
- Error handling
- Basic validation pipeline

## Project Structure

```
simple-crud-api/
├── src/
│   └── SimpleCrudApi/
│       ├── Controllers/
│       │   └── UsersController.cs
│       ├── Models/
│       │   ├── User.cs
│       │   ├── Requests/
│       │   └── Responses/
│       ├── Services/
│       │   └── UserService.cs
│       ├── Pipelines/
│       │   └── ValidationPipeline.cs
│       ├── Data/
│       │   └── InMemoryUserRepository.cs
│       └── Program.cs
└── tests/
    └── SimpleCrudApi.Tests/
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Your favorite IDE (Visual Studio, VS Code, Rider)

### Running the Application

1. Navigate to the project directory:
   ```bash
   cd docs/examples/simple-crud-api/src/SimpleCrudApi
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

3. Open your browser to `https://localhost:7001/swagger` to see the API documentation.

## Code Overview

### 1. Domain Models

```csharp
// Models/User.cs
public record User
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
```

### 2. Request/Response Types

```csharp
// Models/Requests/UserRequests.cs
public record GetUserQuery(int Id) : IRequest<User?>;

public record GetUsersQuery(int Page = 1, int PageSize = 10) : IRequest<IEnumerable<User>>;

public record CreateUserCommand(string Name, string Email) : IRequest<User>;

public record UpdateUserCommand(int Id, string Name, string Email) : IRequest<User?>;

public record DeleteUserCommand(int Id) : IRequest;

// Notifications
public record UserCreatedNotification(User User) : INotification;
public record UserUpdatedNotification(User User) : INotification;
public record UserDeletedNotification(int UserId) : INotification;
```

### 3. Handlers

```csharp
// Services/UserService.cs
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [Handle]
    public async ValueTask<User?> GetUser(GetUserQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", query.Id);
        return await _repository.GetByIdAsync(query.Id, cancellationToken);
    }

    [Handle]
    public async ValueTask<IEnumerable<User>> GetUsers(GetUsersQuery query, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting users - Page: {Page}, PageSize: {PageSize}", 
            query.Page, query.PageSize);
        return await _repository.GetPagedAsync(query.Page, query.PageSize, cancellationToken);
    }

    [Handle]
    public async ValueTask<User> CreateUser(CreateUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating user: {Name} ({Email})", command.Name, command.Email);
        
        var user = new User
        {
            Id = 0, // Will be set by repository
            Name = command.Name,
            Email = command.Email,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _repository.CreateAsync(user, cancellationToken);
        
        // Publish notification (will be handled by notification handlers)
        await _relay.PublishAsync(new UserCreatedNotification(createdUser), cancellationToken);
        
        return createdUser;
    }

    [Handle]
    public async ValueTask<User?> UpdateUser(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating user: {UserId}", command.Id);
        
        var existingUser = await _repository.GetByIdAsync(command.Id, cancellationToken);
        if (existingUser == null)
            return null;

        var updatedUser = existingUser with
        {
            Name = command.Name,
            Email = command.Email,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _repository.UpdateAsync(updatedUser, cancellationToken);
        
        if (result != null)
        {
            await _relay.PublishAsync(new UserUpdatedNotification(result), cancellationToken);
        }
        
        return result;
    }

    [Handle]
    public async ValueTask DeleteUser(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting user: {UserId}", command.Id);
        
        var deleted = await _repository.DeleteAsync(command.Id, cancellationToken);
        
        if (deleted)
        {
            await _relay.PublishAsync(new UserDeletedNotification(command.Id), cancellationToken);
        }
    }
}
```

### 4. Notification Handlers

```csharp
// Services/NotificationHandlers.cs
public class UserNotificationHandlers
{
    private readonly ILogger<UserNotificationHandlers> _logger;

    public UserNotificationHandlers(ILogger<UserNotificationHandlers> logger)
    {
        _logger = logger;
    }

    [Notification]
    public async ValueTask OnUserCreated(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User created: {UserId} - {Name}", 
            notification.User.Id, notification.User.Name);
        
        // Could send welcome email, update analytics, etc.
        await Task.Delay(10, cancellationToken); // Simulate work
    }

    [Notification]
    public async ValueTask OnUserUpdated(UserUpdatedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User updated: {UserId} - {Name}", 
            notification.User.Id, notification.User.Name);
        
        // Could invalidate cache, update search index, etc.
        await Task.Delay(10, cancellationToken); // Simulate work
    }

    [Notification]
    public async ValueTask OnUserDeleted(UserDeletedNotification notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("User deleted: {UserId}", notification.UserId);
        
        // Could clean up related data, update analytics, etc.
        await Task.Delay(10, cancellationToken); // Simulate work
    }
}
```

### 5. Pipeline Behaviors

```csharp
// Pipelines/ValidationPipeline.cs
public class ValidationPipeline
{
    private readonly ILogger<ValidationPipeline> _logger;

    public ValidationPipeline(ILogger<ValidationPipeline> logger)
    {
        _logger = logger;
    }

    [Pipeline(Order = -100)] // Execute early
    public async ValueTask<TResponse> ValidateRequests<TRequest, TResponse>(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Basic validation example
        if (request is CreateUserCommand createCmd)
        {
            if (string.IsNullOrWhiteSpace(createCmd.Name))
                throw new ArgumentException("Name is required", nameof(createCmd.Name));
            
            if (string.IsNullOrWhiteSpace(createCmd.Email))
                throw new ArgumentException("Email is required", nameof(createCmd.Email));
            
            if (!IsValidEmail(createCmd.Email))
                throw new ArgumentException("Invalid email format", nameof(createCmd.Email));
        }

        if (request is UpdateUserCommand updateCmd)
        {
            if (updateCmd.Id <= 0)
                throw new ArgumentException("Invalid user ID", nameof(updateCmd.Id));
            
            if (string.IsNullOrWhiteSpace(updateCmd.Name))
                throw new ArgumentException("Name is required", nameof(updateCmd.Name));
            
            if (!IsValidEmail(updateCmd.Email))
                throw new ArgumentException("Invalid email format", nameof(updateCmd.Email));
        }

        _logger.LogDebug("Validation passed for {RequestType}", typeof(TRequest).Name);
        return await next();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

### 6. Controller

```csharp
// Controllers/UsersController.cs
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IRelay _relay;

    public UsersController(IRelay relay)
    {
        _relay = relay;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(int id, CancellationToken cancellationToken)
    {
        var user = await _relay.SendAsync(new GetUserQuery(id), cancellationToken);
        return user == null ? NotFound() : Ok(user);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        CancellationToken cancellationToken = default)
    {
        var users = await _relay.SendAsync(new GetUsersQuery(page, pageSize), cancellationToken);
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<User>> CreateUser(
        [FromBody] CreateUserRequest request, 
        CancellationToken cancellationToken)
    {
        var user = await _relay.SendAsync(
            new CreateUserCommand(request.Name, request.Email), 
            cancellationToken);
        
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<User>> UpdateUser(
        int id, 
        [FromBody] UpdateUserRequest request, 
        CancellationToken cancellationToken)
    {
        var user = await _relay.SendAsync(
            new UpdateUserCommand(id, request.Name, request.Email), 
            cancellationToken);
        
        return user == null ? NotFound() : Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        await _relay.SendAsync(new DeleteUserCommand(id), cancellationToken);
        return NoContent();
    }
}

// Request DTOs for API
public record CreateUserRequest(string Name, string Email);
public record UpdateUserRequest(string Name, string Email);
```

### 7. Dependency Injection Setup

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Relay
builder.Services.AddRelay();

// Register application services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserNotificationHandlers>();
builder.Services.AddScoped<ValidationPipeline>();

// Register repository
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

## Testing

### Unit Tests

```csharp
// tests/SimpleCrudApi.Tests/UserServiceTests.cs
public class UserServiceTests
{
    [Test]
    public async Task GetUser_WithValidId_ReturnsUser()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        var logger = new Mock<ILogger<UserService>>();
        var relay = RelayTestHarness.CreateTestRelay();
        
        var expectedUser = new User { Id = 1, Name = "Murat Genc", Email = "murat@gencmurat.com" };
        repository.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(expectedUser);

        var service = new UserService(repository.Object, logger.Object);

        // Act
        var result = await service.GetUser(new GetUserQuery(1), CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedUser));
        repository.Verify(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Tests

```csharp
// tests/SimpleCrudApi.Tests/IntegrationTests.cs
public class UsersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UsersControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Test]
    public async Task CreateUser_WithValidData_ReturnsCreatedUser()
    {
        // Arrange
        var request = new CreateUserRequest("Jane Doe", "jane@example.com");
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/users", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var user = JsonSerializer.Deserialize<User>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(user.Name, Is.EqualTo("Jane Doe"));
        Assert.That(user.Email, Is.EqualTo("jane@example.com"));
    }
}
```

## Key Learning Points

1. **Attribute-Based Registration**: Handlers are registered using `[Handle]` and `[Notification]` attributes
2. **ValueTask Usage**: Prefer `ValueTask<T>` over `Task<T>` for better performance
3. **Pipeline Behaviors**: Cross-cutting concerns like validation can be handled in pipelines
4. **Notifications**: Use notifications for side effects and event-driven patterns
5. **Dependency Injection**: Relay integrates seamlessly with .NET's built-in DI container

## Next Steps

- Explore the [E-commerce Platform](../ecommerce-platform/) example for more advanced patterns
- Learn about [performance optimization](../high-throughput-api/) techniques
- See [real-time features](../realtime-chat/) with streaming responses

## Performance Notes

This simple example already demonstrates significant performance improvements over traditional mediator frameworks:

- **Zero reflection** at runtime
- **Direct method calls** to handlers
- **Minimal allocations** for request processing
- **Efficient pipeline execution**

Run the included benchmarks to see the performance characteristics in your environment.
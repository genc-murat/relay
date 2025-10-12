# Relay Minimal API Sample

A comprehensive example demonstrating how to use the Relay framework with ASP.NET Core Minimal APIs.

## Features Demonstrated

- **Scenario-Based Configuration**: Uses `AddRelayForScenario(RelayScenario.WebApi)`
- **Request/Response Pattern**: Clean separation of requests, responses, and handlers
- **Validation**: Automatic validation using `IValidationRule<T>`
- **Logging**: Integrated logging in handlers
- **Swagger/OpenAPI**: Full API documentation
- **In-Memory Database**: Simple data storage for demo purposes
- **Clean Architecture**: Feature-based folder structure

## Project Structure

```
Relay.MinimalApiSample/
├── Features/
│   ├── Users/
│   │   ├── CreateUser.cs              # Request & Response DTOs
│   │   ├── CreateUserHandler.cs       # Request Handler
│   │   ├── CreateUserValidator.cs     # Validation Rules
│   │   ├── GetUser.cs                 # Get single user
│   │   ├── GetUserHandler.cs
│   │   ├── GetAllUsers.cs             # Get all users
│   │   └── GetAllUsersHandler.cs
│   └── Products/
│       ├── CreateProduct.cs           # Request & Response DTOs
│       ├── CreateProductHandler.cs    # Request Handler
│       ├── CreateProductValidator.cs  # Validation Rules
│       ├── GetProduct.cs              # Get single product
│       ├── GetProductHandler.cs
│       ├── GetAllProducts.cs          # Get all products
│       └── GetAllProductsHandler.cs
├── Infrastructure/
│   └── InMemoryDatabase.cs            # Simple in-memory storage
├── Models/
│   ├── User.cs                        # Domain models
│   └── Product.cs
├── Program.cs                         # Application entry point
├── appsettings.json                   # Configuration
└── Relay.MinimalApiSample.csproj      # Project file
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 / VS Code / Rider

### Running the Application

1. **Navigate to the project directory:**
   ```bash
   cd samples/Relay.MinimalApiSample
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **Open Swagger UI:**
   Navigate to: `https://localhost:5001/swagger` (or the port shown in console)

## API Endpoints

### Users

#### Get All Users
```http
GET /api/users
```

**Response:**
```json
{
  "users": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "John Doe",
      "email": "john@example.com",
      "isActive": true
    }
  ]
}
```

#### Get User by ID
```http
GET /api/users/{id}
```

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "John Doe",
  "email": "john@example.com",
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z"
}
```

#### Create User
```http
POST /api/users
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com"
}
```

**Validation Rules:**
- Name: Required, 2-100 characters
- Email: Required, valid email format

**Success Response (201 Created):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "John Doe",
  "email": "john@example.com",
  "createdAt": "2024-01-15T10:30:00Z"
}
```

**Validation Error Response (400 Bad Request):**
```json
{
  "isValid": false,
  "errors": [
    "Name is required",
    "Email must be a valid email address"
  ]
}
```

### Products

#### Get All Products
```http
GET /api/products
```

**Response:**
```json
{
  "products": [
    {
      "id": "7d9a85f64-1234-4562-b3fc-2c963f66afa6",
      "name": "Laptop",
      "price": 1299.99,
      "stock": 50
    }
  ]
}
```

#### Get Product by ID
```http
GET /api/products/{id}
```

**Response:**
```json
{
  "id": "7d9a85f64-1234-4562-b3fc-2c963f66afa6",
  "name": "Laptop",
  "description": "High-performance laptop",
  "price": 1299.99,
  "stock": 50
}
```

#### Create Product
```http
POST /api/products
Content-Type: application/json

{
  "name": "Laptop",
  "description": "High-performance laptop",
  "price": 1299.99,
  "stock": 50
}
```

**Validation Rules:**
- Name: Required, minimum 3 characters
- Description: Required
- Price: Must be greater than zero
- Stock: Cannot be negative

**Success Response (201 Created):**
```json
{
  "id": "7d9a85f64-1234-4562-b3fc-2c963f66afa6",
  "name": "Laptop",
  "price": 1299.99,
  "stock": 50
}
```

## Handler Registration

This sample uses **automatic handler registration** via the **Relay Source Generator**:

```csharp
// Register Relay with common features
// The source generator automatically discovers and registers all handlers at compile-time
builder.Services.AddRelayWithFeatures();
```

### How Source Generator Works

The **Relay Source Generator** automatically handles handler registration at compile-time by:
1. Discovering all classes that implement `IRequestHandler<,>`
2. Generating optimized registration code
3. Creating type-safe dispatchers
4. Eliminating runtime reflection

This happens automatically during compilation - no manual registration needed!

### Source Generator Configuration

The source generator is enabled in `.csproj` as an analyzer:

```xml
<ProjectReference Include="..\..\src\Relay.SourceGenerator\Relay.SourceGenerator.csproj">
  <ReferenceOutputAssembly>false</ReferenceOutputAssembly>
  <OutputItemType>Analyzer</OutputItemType>
</ProjectReference>
```

This reference tells MSBuild to run the source generator during compilation, which automatically discovers and registers all your handlers.

## Key Relay Concepts Demonstrated

### 1. Automatic Handler Registration

Handlers are automatically discovered and registered by the source generator:
- Each handler implements `IRequestHandler<TRequest, TResponse>`
- Source generator discovers all handlers at compile-time
- Registered as `Transient` services automatically
- No manual registration required

### 2. Feature-Based Configuration

```csharp
// Register Relay with common features
builder.Services.AddRelayWithFeatures();
```

This automatically configures:
- ✅ Core Relay services
- ✅ Validation pipeline
- ✅ Exception handlers
- ✅ Pre/post processors
- ✅ Telemetry and logging

### 3. Request/Response Pattern

**Define Request & Response:**
```csharp
public record CreateUserRequest(string Name, string Email)
    : IRequest<CreateUserResponse>;

public record CreateUserResponse(Guid Id, string Name, string Email, DateTime CreatedAt);
```

**Implement Handler:**
```csharp
public class CreateUserHandler : IRequestHandler<CreateUserRequest, CreateUserResponse>
{
    public ValueTask<CreateUserResponse> HandleAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // Implementation
    }
}
```

### 4. Validation

```csharp
public class CreateUserValidator : IValidationRule<CreateUserRequest>
{
    public ValueTask<ValidationResult> ValidateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required");

        return ValueTask.FromResult(
            errors.Count == 0
                ? ValidationResult.Success()
                : ValidationResult.Failure(errors));
    }
}
```

### 5. Using IRelay in Minimal APIs

```csharp
app.MapPost("/api/users", async (CreateUserRequest request, IRelay relay) =>
{
    var response = await relay.SendAsync(request);
    return Results.Created($"/api/users/{response.Id}", response);
});
```

Or get IRelay once:
```csharp
var relay = app.Services.GetRequiredService<IRelay>();

app.MapPost("/api/users", async (CreateUserRequest request) =>
{
    var response = await relay.SendAsync(request);
    return Results.Created($"/api/users/{response.Id}", response);
});
```

## Testing with cURL

### Create a User
```bash
curl -X POST https://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Alice Smith","email":"alice@example.com"}'
```

### Get All Users
```bash
curl https://localhost:5001/api/users
```

### Get User by ID
```bash
curl https://localhost:5001/api/users/{id}
```

### Create a Product
```bash
curl -X POST https://localhost:5001/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Keyboard","description":"Mechanical keyboard","price":89.99,"stock":100}'
```

## Advanced Features

### Source Generator Integration

The Relay source generator automatically discovers all handlers at compile-time and generates optimized registration code. No manual registration needed!

### Validation Pipeline

All requests implementing `IRequest<T>` with corresponding `IValidationRule<T>` validators will be automatically validated before reaching the handler.

### Exception Handling

Exceptions in handlers are automatically caught and transformed into appropriate HTTP responses when using the WebApi scenario.

### Telemetry & Logging

All requests are automatically logged with timing information when using the WebApi scenario:
- Request started
- Validation time
- Handler execution time
- Request completed

## Next Steps

To extend this sample:

1. **Add Authentication**: Implement JWT or other auth mechanisms
2. **Add Persistence**: Replace InMemoryDatabase with EF Core or Dapper
3. **Add Caching**: Use IMemoryCache or Redis
4. **Add Message Broker**: Integrate Relay.MessageBroker for async messaging
5. **Add AI Optimization**: Use `AddRelayWithAdvancedFeatures` for AI-powered optimization

## Learn More

- [Relay Framework Documentation](../../README.md)
- [Centralized Service Registration](../../CENTRALIZED_SERVICE_REGISTRATION.md)
- [Source Generator Integration](../../SOURCE_GENERATOR_INTEGRATION.md)

## License

This sample is part of the Relay framework and follows the same license.

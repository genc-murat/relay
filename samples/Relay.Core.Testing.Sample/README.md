# Relay.Core.Testing Sample Application

This sample application demonstrates the comprehensive testing capabilities of the Relay.Core.Testing framework. It showcases various testing patterns, scenarios, and best practices for testing Relay-based applications.

## Overview

The sample includes:

- **Domain Models**: User and Product entities with associated commands, queries, and events
- **Services**: Repository and email service implementations
- **Request Handlers**: Command and query handlers for the domain operations
- **Comprehensive Tests**: Examples of unit tests, integration tests, and scenario-based tests
- **Mocking Examples**: Demonstration of dependency mocking and verification
- **Error Handling**: Tests for exception scenarios and error recovery
- **Performance Testing**: Examples of load testing and performance validation

## Key Features Demonstrated

### 1. Basic Testing with Mocks

```csharp
[Fact]
public async Task CreateUser_ShouldCreateUserAndSendWelcomeEmail()
{
    // Arrange
    var command = new CreateUserCommand { Name = "John Doe", Email = "john@example.com" };

    // Setup mocks
    var mockUserRepo = new Mock<IUserRepository>();
    var mockEmailService = new Mock<IEmailService>();

    mockUserRepo.Setup(x => x.CreateAsync(It.IsAny<User>()))
        .ReturnsAsync((User u) => { u.Id = Guid.NewGuid(); return u; });

    Services.AddSingleton(mockUserRepo.Object);
    Services.AddSingleton(mockEmailService.Object);

    // Act
    var result = await Relay.SendCommand(command);

    // Assert
    Assert.NotNull(result);
    mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
}
```

### 2. Scenario-Based Testing

```csharp
[Fact]
public async Task UserLifecycleScenario_ShouldHandleCompleteUserWorkflow()
{
    var scenario = new UserLifecycleScenario();
    await scenario.SetupAsync(Services);

    var result = await scenario.ExecuteAsync();

    Assert.True(result.Success);
    Assert.Contains("UserCreated", result.ExecutedSteps);
    Assert.Contains("UserUpdated", result.ExecutedSteps);
}
```

### 3. Error Handling and Recovery

```csharp
[Fact]
public async Task UpdateUser_ShouldHandleUserNotFound()
{
    var mockUserRepo = new Mock<IUserRepository>();
    mockUserRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
        .ReturnsAsync((User?)null);

    Services.AddSingleton(mockUserRepo.Object);

    var command = new UpdateUserCommand { UserId = Guid.NewGuid() };

    await Assert.ThrowsAsync<InvalidOperationException>(
        () => Relay.SendCommand(command));
}
```

### 4. Performance Testing

```csharp
[Fact]
public async Task PerformanceScenario_ShouldMeetPerformanceRequirements()
{
    var scenario = new PerformanceScenario();
    await scenario.SetupAsync(Services);

    var result = await scenario.ExecuteAsync();

    Assert.True(result.ExecutionTime < TimeSpan.FromSeconds(5));
    Assert.True(result.OperationsCompleted > 0);
}
```

## Running the Sample

### Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### Build and Run Tests

```bash
# Navigate to the sample directory
cd samples/Relay.Core.Testing.Sample

# Build the project
dotnet build

# Run all tests
dotnet test

# Run specific test
dotnet test --filter "UserTests.CreateUser_ShouldCreateUserAndSendWelcomeEmail"

# Run with detailed output
dotnet test -v n
```

### Test Categories

- **UserTests**: Basic CRUD operations with mocking
- **ScenarioTests**: Complex workflow testing
- **PerformanceTests**: Load and performance validation
- **ErrorHandlingTests**: Exception scenarios and recovery

## Architecture

### Domain Layer
- `Models.cs`: Domain entities and contracts
- `Services.cs`: Business logic and data access interfaces
- `Handlers.cs`: Request handlers for commands and queries

### Test Layer
- `UserTests.cs`: Unit and integration tests for user operations
- `ScenarioTests.cs`: Scenario-based testing examples
- Custom scenario classes demonstrating different testing patterns

### Key Patterns

1. **Dependency Injection**: Services registered in test setup
2. **Mock Verification**: Ensuring correct interactions between components
3. **Scenario Templates**: Reusable test scenarios for complex workflows
4. **Error Simulation**: Testing error conditions and recovery logic
5. **Performance Validation**: Measuring and asserting performance characteristics

## Learning Path

1. **Start with UserTests**: Understand basic mocking and assertion patterns
2. **Explore ScenarioTests**: Learn scenario-based testing approaches
3. **Study Error Handling**: See how to test exception scenarios
4. **Review Performance Tests**: Understand load testing patterns
5. **Customize for Your Domain**: Adapt patterns to your specific use cases

## Best Practices Demonstrated

- **Test Isolation**: Each test is independent with proper cleanup
- **Mock Verification**: Verifying exact behavior of dependencies
- **Scenario Organization**: Grouping related operations into scenarios
- **Error Coverage**: Testing both success and failure paths
- **Performance Awareness**: Including performance requirements in tests
- **Maintainable Tests**: Clear naming and organization conventions

## Extending the Sample

To extend this sample for your own domain:

1. Add your domain models to `Models.cs`
2. Implement your services in `Services.cs`
3. Create request handlers in `Handlers.cs`
4. Add corresponding tests following the established patterns
5. Create custom scenarios for complex workflows

This sample serves as a comprehensive reference for testing Relay-based applications with the Relay.Core.Testing framework.
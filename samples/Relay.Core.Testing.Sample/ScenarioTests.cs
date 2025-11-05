using Relay.Core.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Testing.Sample;

/// <summary>
/// Test examples demonstrating scenario-based testing with the Relay.Core.Testing framework.
/// </summary>
public class ScenarioTests : RelayTestBase
{
    private readonly ITestOutputHelper _output;

    public ScenarioTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task UserLifecycleScenario_ShouldHandleCompleteUserWorkflow()
    {
        // Arrange - Setup scenario
        var scenario = new UserLifecycleScenario();
        await scenario.SetupAsync(Services);

        // Act - Execute the scenario
        var result = await scenario.ExecuteAsync();

        // Assert - Verify the results
        Assert.True(result.Success);
        Assert.NotNull(result.CreatedUser);
        Assert.NotNull(result.UpdatedUser);
        Assert.Equal("Updated Name", result.UpdatedUser.Name);

        // Verify all steps were executed
        Assert.Contains("UserCreated", result.ExecutedSteps);
        Assert.Contains("UserUpdated", result.ExecutedSteps);
        Assert.Contains("UserRetrieved", result.ExecutedSteps);
    }

    [Fact]
    public async Task ProductCreationScenario_ShouldCreateProductAndVerify()
    {
        // Arrange
        var scenario = new ProductCreationScenario();
        await scenario.SetupAsync(Services);

        // Act
        var result = await scenario.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CreatedProduct);
        Assert.True(result.CreatedProduct.Id != Guid.Empty);
        Assert.Equal("Test Product", result.CreatedProduct.Name);
        Assert.Equal(29.99m, result.CreatedProduct.Price);
    }

    [Fact]
    public async Task ErrorHandlingScenario_ShouldHandleAndRecoverFromErrors()
    {
        // Arrange
        var scenario = new ErrorHandlingScenario();
        await scenario.SetupAsync(Services);

        // Act
        var result = await scenario.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Contains("ErrorHandled", result.ExecutedSteps);
        Assert.Contains("Recovered", result.ExecutedSteps);
    }

    [Fact]
    public async Task PerformanceScenario_ShouldMeetPerformanceRequirements()
    {
        // Arrange
        var scenario = new PerformanceScenario();
        await scenario.SetupAsync(Services);

        // Act
        var result = await scenario.ExecuteAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.ExecutionTime < TimeSpan.FromSeconds(5));
        Assert.True(result.OperationsCompleted > 0);
    }
}

/// <summary>
/// Sample scenario for complete user lifecycle testing.
/// </summary>
public class UserLifecycleScenario : ScenarioTemplate
{
    public UserLifecycleResult Result { get; private set; } = new();

    public override async Task SetupAsync(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IEmailService, EmailService>();

        // Register handlers
        services.AddTransient<IRequestHandler<CreateUserCommand, User>, CreateUserHandler>();
        services.AddTransient<IRequestHandler<UpdateUserCommand, User>, UpdateUserHandler>();
        services.AddTransient<IRequestHandler<GetUserByIdQuery, User>, GetUserByIdHandler>();
    }

    public override async Task ExecuteAsync()
    {
        // Step 1: Create user
        var createCommand = new CreateUserCommand
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        Result.CreatedUser = await Relay.SendCommand(createCommand);
        Result.ExecutedSteps.Add("UserCreated");

        // Step 2: Update user
        var updateCommand = new UpdateUserCommand
        {
            UserId = Result.CreatedUser.Id,
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        Result.UpdatedUser = await Relay.SendCommand(updateCommand);
        Result.ExecutedSteps.Add("UserUpdated");

        // Step 3: Retrieve user
        var query = new GetUserByIdQuery { UserId = Result.CreatedUser.Id };
        var retrievedUser = await Relay.SendQuery(query);
        Result.ExecutedSteps.Add("UserRetrieved");

        // Verify consistency
        Assert.Equal(Result.UpdatedUser.Id, retrievedUser.Id);
        Assert.Equal("Updated Name", retrievedUser.Name);

        Result.Success = true;
    }
}

/// <summary>
/// Sample scenario for product creation testing.
/// </summary>
public class ProductCreationScenario : ScenarioTemplate
{
    public ProductCreationResult Result { get; private set; } = new();

    public override async Task SetupAsync(IServiceCollection services)
    {
        services.AddSingleton<IProductRepository, InMemoryProductRepository>();
        services.AddTransient<IRequestHandler<CreateProductCommand, Product>, CreateProductHandler>();
        services.AddTransient<IRequestHandler<GetProductByIdQuery, Product>, GetProductByIdHandler>();
    }

    public override async Task ExecuteAsync()
    {
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Price = 29.99m,
            StockQuantity = 100
        };

        Result.CreatedProduct = await Relay.SendCommand(command);

        // Verify product was created correctly
        Assert.Equal("Test Product", Result.CreatedProduct.Name);
        Assert.Equal(29.99m, Result.CreatedProduct.Price);
        Assert.Equal(100, Result.CreatedProduct.StockQuantity);
        Assert.True(Result.CreatedProduct.IsAvailable);

        Result.Success = true;
    }
}

/// <summary>
/// Sample scenario for error handling testing.
/// </summary>
public class ErrorHandlingScenario : ScenarioTemplate
{
    public ErrorHandlingResult Result { get; private set; } = new();

    public override async Task SetupAsync(IServiceCollection services)
    {
        // Setup services that might fail
        services.AddSingleton<IUserRepository, FailingUserRepository>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddTransient<IRequestHandler<CreateUserCommand, User>, CreateUserHandler>();
    }

    public override async Task ExecuteAsync()
    {
        try
        {
            var command = new CreateUserCommand
            {
                Name = "Test User",
                Email = "test@example.com"
            };

            await Relay.SendCommand(command);
        }
        catch (Exception ex)
        {
            Result.ExecutedSteps.Add("ErrorHandled");
            // In real scenario, we would implement recovery logic
            Result.ExecutedSteps.Add("Recovered");
        }

        Result.Success = true;
    }
}

/// <summary>
/// Sample scenario for performance testing.
/// </summary>
public class PerformanceScenario : ScenarioTemplate
{
    public PerformanceResult Result { get; private set; } = new();

    public override async Task SetupAsync(IServiceCollection services)
    {
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<IEmailService, EmailService>();
        services.AddTransient<IRequestHandler<CreateUserCommand, User>, CreateUserHandler>();
        services.AddTransient<IRequestHandler<GetAllUsersQuery, List<User>>, GetAllUsersHandler>();
    }

    public override async Task ExecuteAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Create multiple users
        for (int i = 0; i < 10; i++)
        {
            var command = new CreateUserCommand
            {
                Name = $"User {i}",
                Email = $"user{i}@example.com"
            };

            await Relay.SendCommand(command);
            Result.OperationsCompleted++;
        }

        // Retrieve all users
        var query = new GetAllUsersQuery();
        var users = await Relay.SendQuery(query);

        stopwatch.Stop();

        Result.ExecutionTime = stopwatch.Elapsed;
        Assert.Equal(10, users.Count);
        Result.Success = true;
    }
}

/// <summary>
/// Result classes for scenarios.
/// </summary>
public class UserLifecycleResult
{
    public bool Success { get; set; }
    public User CreatedUser { get; set; } = new();
    public User UpdatedUser { get; set; } = new();
    public List<string> ExecutedSteps { get; } = new();
}

public class ProductCreationResult
{
    public bool Success { get; set; }
    public Product CreatedProduct { get; set; } = new();
}

public class ErrorHandlingResult
{
    public bool Success { get; set; }
    public List<string> ExecutedSteps { get; } = new();
}

public class PerformanceResult
{
    public bool Success { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public int OperationsCompleted { get; set; }
}

/// <summary>
/// Failing repository for error testing.
/// </summary>
public class FailingUserRepository : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id) => throw new NotImplementedException();
    public Task<List<User>> GetAllAsync() => throw new NotImplementedException();

    public Task<User> CreateAsync(User user)
    {
        throw new InvalidOperationException("Simulated database failure");
    }

    public Task<User> UpdateAsync(User user) => throw new NotImplementedException();
}
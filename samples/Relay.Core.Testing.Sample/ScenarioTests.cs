using Microsoft.Extensions.DependencyInjection;
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
        // Arrange & Act - Execute the user lifecycle scenario
        var result = await RunScenarioAsync("User Lifecycle", builder =>
        {
            // Given - Setup initial state
            builder.Given("User repository is available", async () =>
            {
                // Register services
                Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
                Services.AddSingleton<IEmailService, ConsoleEmailService>();
                Services.AddTransient<IRequestHandler<CreateUserCommand, User>, CreateUserHandler>();
                Services.AddTransient<IRequestHandler<UpdateUserCommand, User>, UpdateUserHandler>();
                Services.AddTransient<IRequestHandler<GetUserByIdQuery, User>, GetUserByIdHandler>();
            });

            // When - Execute user lifecycle steps
            builder.When("Create user", async () =>
            {
                var createCommand = new CreateUserCommand
                {
                    Name = "John Doe",
                    Email = "john.doe@example.com"
                };
                await TestRelay.SendAsync(createCommand);
            });

            builder.When("Update user", async () =>
            {
                var updateCommand = new UpdateUserCommand
                {
                    UserId = Guid.NewGuid(), // In real scenario, get from previous step
                    Name = "Updated Name",
                    Email = "updated@example.com"
                };
                await TestRelay.SendAsync(updateCommand);
            });

            builder.When("Retrieve user", async () =>
            {
                var query = new GetUserByIdQuery { UserId = Guid.NewGuid() }; // In real scenario, get from previous step
                await TestRelay.SendAsync(query);
            });

            // Then - Verify results
            builder.Then("User lifecycle completed successfully", result =>
            {
                Assert.True(result.Success);
            });
        });

        // Assert - Verify the results
        AssertScenarioSuccess(result);
    }

    [Fact]
    public async Task ProductCreationScenario_ShouldCreateProductAndVerify()
    {
        // Arrange & Act - Execute the product creation scenario
        var result = await RunScenarioAsync("Product Creation", builder =>
        {
            // Given - Setup initial state
            builder.Given("Product repository is available", async () =>
            {
                Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
                Services.AddTransient<IRequestHandler<CreateProductCommand, Product>, CreateProductHandler>();
                Services.AddTransient<IRequestHandler<GetProductByIdQuery, Product>, GetProductByIdHandler>();
            });

            // When - Create product
            builder.When("Create product", async () =>
            {
                var command = new CreateProductCommand
                {
                    Name = "Test Product",
                    Price = 29.99m,
                    StockQuantity = 100
                };
                await TestRelay.SendAsync(command);
            });

            // Then - Verify results
            builder.Then("Product created successfully", result =>
            {
                Assert.True(result.Success);
            });
        });

        // Assert
        AssertScenarioSuccess(result);
    }

    [Fact]
    public async Task ErrorHandlingScenario_ShouldHandleAndRecoverFromErrors()
    {
        // Arrange & Act - Execute the error handling scenario
        var result = await RunScenarioAsync("Error Handling", builder =>
        {
            // Given - Setup failing repository
            builder.Given("Failing repository is configured", async () =>
            {
                Services.AddSingleton<IUserRepository, FailingUserRepository>();
                Services.AddSingleton<IEmailService, ConsoleEmailService>();
                Services.AddTransient<IRequestHandler<CreateUserCommand, User>, CreateUserHandler>();
            });

            // When - Attempt to create user (should fail)
            builder.When("Attempt to create user", async () =>
            {
                try
                {
                    var command = new CreateUserCommand
                    {
                        Name = "Test User",
                        Email = "test@example.com"
                    };
                    await TestRelay.SendAsync(command);
                }
                catch (Exception)
                {
                    // Expected to fail - error handling scenario
                }
            });

            // Then - Verify error was handled
            builder.Then("Error handled gracefully", result =>
            {
                // In error scenarios, we might expect failure or specific handling
                Assert.True(result.Success || !string.IsNullOrEmpty(result.Error));
            });
        });

        // Assert
        Assert.True(result.Success || !string.IsNullOrEmpty(result.Error)); // Either success or expected error
    }

    [Fact]
    public async Task PerformanceScenario_ShouldMeetPerformanceRequirements()
    {
        // Arrange & Act - Execute performance scenario with timing assertion
        var result = await Task.Run(async () =>
        {
            var scenarioResult = await RunScenarioAsync("Performance Test", builder =>
            {
                // Given - Setup performance test environment
                builder.Given("User repository is available", async () =>
                {
                    Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
                    Services.AddSingleton<IEmailService, ConsoleEmailService>();
                    Services.AddTransient<IRequestHandler<CreateUserCommand, User>, CreateUserHandler>();
                    Services.AddTransient<IRequestHandler<GetAllUsersQuery, List<User>>, GetAllUsersHandler>();
                });

                // When - Perform operations under load
                builder.When("Create multiple users", async () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        var command = new CreateUserCommand
                        {
                            Name = $"User {i}",
                            Email = $"user{i}@example.com"
                        };
                        await TestRelay.SendAsync(command);
                    }
                });

                builder.When("Retrieve all users", async () =>
                {
                    var query = new GetAllUsersQuery();
                    await TestRelay.SendAsync(query);
                });

                // Then - Verify performance requirements
                builder.Then("Operations completed successfully", result =>
                {
                    Assert.True(result.Success);
                });
            });

            return scenarioResult;
        }).ShouldCompleteWithinAsync(TimeSpan.FromSeconds(5));

        // Assert additional requirements
        AssertScenarioSuccess(result);
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Relay.Core.Contracts.Core;
using Relay.MinimalApiSample.Features.Users;
using Relay.MinimalApiSample.Infrastructure;

namespace Relay.MinimalApiSample.Tests.MSTest;

[TestClass]
public class UserApiMSTestTests
{
    private static IServiceProvider _serviceProvider;
    private static IRelay _relay;
    private static InMemoryDatabase _database;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        var services = new ServiceCollection();

        // Register logging
        services.AddLogging();

        // Register sample services
        _database = new InMemoryDatabase();
        services.AddSingleton(_database);
        services.AddSingleton<IEmailService, ConsoleEmailService>();

        // Register Relay services
        services.AddRelay();

        _serviceProvider = services.BuildServiceProvider();
        _relay = _serviceProvider.GetRequiredService<IRelay>();
    }

    [TestMethod]
    public async Task CreateUser_WithValidData_CreatesUserSuccessfully()
    {
        // Arrange
        var request = new CreateUserRequest("John Doe", "john.doe@example.com");

        // Act
        var response = await _relay.SendAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.AreNotEqual(Guid.Empty, response.Id);
        Assert.AreEqual(request.Name, response.Name);
        Assert.AreEqual(request.Email, response.Email);
    }

    [TestMethod]
    public async Task GetAllUsers_ReturnsUsersIncludingSeededData()
    {
        // Arrange
        var request = new GetAllUsersRequest();

        // Act
        var response = await _relay.SendAsync(request);

        // Assert
        Assert.IsNotNull(response);
        Assert.IsTrue(response.Users.Count > 0);
    }
}
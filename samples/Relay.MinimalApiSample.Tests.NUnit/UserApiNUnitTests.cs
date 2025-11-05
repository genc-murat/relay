using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Relay.Core.Contracts.Core;
using Relay.MinimalApiSample.Features.Users;
using Relay.MinimalApiSample.Infrastructure;

namespace Relay.MinimalApiSample.Tests.NUnit;

[TestFixture]
public class UserApiNUnitTests
{
    private IServiceProvider _serviceProvider;
    private IRelay _relay;
    private InMemoryDatabase _database;

    [OneTimeSetUp]
    public void OneTimeSetUp()
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

    [Test]
    public async Task CreateUser_WithValidData_CreatesUserSuccessfully()
    {
        // Arrange
        var request = new CreateUserRequest("John Doe", "john.doe@example.com");

        // Act
        var response = await _relay.SendAsync(request);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(response.Name, Is.EqualTo(request.Name));
        Assert.That(response.Email, Is.EqualTo(request.Email));
    }

    [Test]
    public async Task GetAllUsers_ReturnsUsersIncludingSeededData()
    {
        // Arrange
        var request = new GetAllUsersRequest();

        // Act
        var response = await _relay.SendAsync(request);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Users, Is.Not.Empty);
    }
}
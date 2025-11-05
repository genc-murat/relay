using Relay.Core.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Relay.Core.Testing.Sample;

/// <summary>
/// Comprehensive test examples demonstrating the Relay.Core.Testing framework.
/// </summary>
public class UserTests : RelayTestBase
{
    private readonly ITestOutputHelper _output;

    public UserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task CreateUser_ShouldCreateUserAndSendWelcomeEmail()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        // Setup mocks
        var mockUserRepo = new Mock<IUserRepository>();
        var mockEmailService = new Mock<IEmailService>();

        mockUserRepo
            .Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { u.Id = Guid.NewGuid(); u.CreatedAt = DateTime.UtcNow; u.IsActive = true; return u; });

        mockEmailService
            .Setup(x => x.SendWelcomeEmailAsync(command.Email, command.Name))
            .Returns(Task.CompletedTask);

        Services.AddSingleton(mockUserRepo.Object);
        Services.AddSingleton(mockEmailService.Object);

        // Act
        var result = await Relay.SendCommand(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.Name, result.Name);
        Assert.Equal(command.Email, result.Email);
        Assert.True(result.IsActive);
        Assert.True(result.CreatedAt > DateTime.UtcNow.AddMinutes(-1));

        // Verify mocks
        mockUserRepo.Verify(x => x.CreateAsync(It.Is<User>(u =>
            u.Name == command.Name && u.Email == command.Email)), Times.Once);
        mockEmailService.Verify(x => x.SendWelcomeEmailAsync(command.Email, command.Name), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_ShouldUpdateUserAndSendNotificationEmail()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Name = "Jane Doe",
            Email = "jane.doe@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var command = new UpdateUserCommand
        {
            UserId = existingUser.Id,
            Name = "Jane Smith",
            Email = "jane.smith@example.com"
        };

        // Setup mocks
        var mockUserRepo = new Mock<IUserRepository>();
        var mockEmailService = new Mock<IEmailService>();

        mockUserRepo
            .Setup(x => x.GetByIdAsync(existingUser.Id))
            .ReturnsAsync(existingUser);

        mockUserRepo
            .Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        mockEmailService
            .Setup(x => x.SendUserUpdatedEmailAsync(command.Email, command.Name))
            .Returns(Task.CompletedTask);

        Services.AddSingleton(mockUserRepo.Object);
        Services.AddSingleton(mockEmailService.Object);

        // Act
        var result = await Relay.SendCommand(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingUser.Id, result.Id);
        Assert.Equal(command.Name, result.Name);
        Assert.Equal(command.Email, result.Email);
        Assert.Equal(existingUser.CreatedAt, result.CreatedAt);
        Assert.Equal(existingUser.IsActive, result.IsActive);

        // Verify mocks
        mockUserRepo.Verify(x => x.GetByIdAsync(existingUser.Id), Times.Once);
        mockUserRepo.Verify(x => x.UpdateAsync(It.Is<User>(u =>
            u.Id == existingUser.Id && u.Name == command.Name && u.Email == command.Email)), Times.Once);
        mockEmailService.Verify(x => x.SendUserUpdatedEmailAsync(command.Email, command.Name), Times.Once);
    }

    [Fact]
    public async Task GetUserById_ShouldReturnUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new User
        {
            Id = userId,
            Name = "Test User",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var query = new GetUserByIdQuery { UserId = userId };

        // Setup mocks
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo
            .Setup(x => x.GetByIdAsync(userId))
            .ReturnsAsync(expectedUser);

        Services.AddSingleton(mockUserRepo.Object);

        // Act
        var result = await Relay.SendQuery(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedUser.Id, result.Id);
        Assert.Equal(expectedUser.Name, result.Name);
        Assert.Equal(expectedUser.Email, result.Email);

        // Verify mocks
        mockUserRepo.Verify(x => x.GetByIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = Guid.NewGuid(), Name = "User 1", Email = "user1@example.com", IsActive = true },
            new User { Id = Guid.NewGuid(), Name = "User 2", Email = "user2@example.com", IsActive = true },
            new User { Id = Guid.NewGuid(), Name = "User 3", Email = "user3@example.com", IsActive = false }
        };

        var query = new GetAllUsersQuery();

        // Setup mocks
        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(users);

        Services.AddSingleton(mockUserRepo.Object);

        // Act
        var result = await Relay.SendQuery(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, u => u.Name == "User 1");
        Assert.Contains(result, u => u.Name == "User 2");
        Assert.Contains(result, u => u.Name == "User 3");

        // Verify mocks
        mockUserRepo.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateUser_ShouldHandleExceptionFromRepository()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        // Setup mocks to throw exception
        var mockUserRepo = new Mock<IUserRepository>();
        var mockEmailService = new Mock<IEmailService>();

        mockUserRepo
            .Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        Services.AddSingleton(mockUserRepo.Object);
        Services.AddSingleton(mockEmailService.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Relay.SendCommand(command));

        Assert.Equal("Database error", exception.Message);

        // Verify mocks
        mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Once);
        mockEmailService.Verify(x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUser_ShouldHandleUserNotFound()
    {
        // Arrange
        var command = new UpdateUserCommand
        {
            UserId = Guid.NewGuid(),
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        // Setup mocks
        var mockUserRepo = new Mock<IUserRepository>();
        var mockEmailService = new Mock<IEmailService>();

        mockUserRepo
            .Setup(x => x.GetByIdAsync(command.UserId))
            .ReturnsAsync((User?)null);

        Services.AddSingleton(mockUserRepo.Object);
        Services.AddSingleton(mockEmailService.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Relay.SendCommand(command));

        Assert.Equal("User not found", exception.Message);

        // Verify mocks
        mockUserRepo.Verify(x => x.GetByIdAsync(command.UserId), Times.Once);
        mockUserRepo.Verify(x => x.UpdateAsync(It.IsAny<User>()), Times.Never);
        mockEmailService.Verify(x => x.SendUserUpdatedEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
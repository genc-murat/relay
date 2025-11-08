using Relay.Core.Contracts.Requests;

namespace Relay.Core.Testing.Sample;

/// <summary>
/// Sample services for the testing framework demonstration.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<List<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
}

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product> CreateAsync(Product product);
}

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string email, string name);
    Task SendUserUpdatedEmailAsync(string email, string name);
}

public class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<Guid, User> _users = new();

    public Task<User?> GetByIdAsync(Guid id)
    {
        _users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<List<User>> GetAllAsync()
    {
        return Task.FromResult(_users.Values.ToList());
    }

    public Task<User> CreateAsync(User user)
    {
        user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;
        user.IsActive = true;
        _users[user.Id] = user;
        return Task.FromResult(user);
    }

    public Task<User> UpdateAsync(User user)
    {
        _users[user.Id] = user;
        return Task.FromResult(user);
    }
}

public class InMemoryProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _products = new();

    public Task<Product?> GetByIdAsync(Guid id)
    {
        _products.TryGetValue(id, out var product);
        return Task.FromResult(product);
    }

    public Task<Product> CreateAsync(Product product)
    {
        product.Id = Guid.NewGuid();
        product.IsAvailable = product.StockQuantity > 0;
        _products[product.Id] = product;
        return Task.FromResult(product);
    }
}

public class EmailService : IEmailService
{
    public Task SendWelcomeEmailAsync(string email, string name)
    {
        // Simulate sending email
        Console.WriteLine($"Sending welcome email to {email} for user {name}");
        return Task.CompletedTask;
    }

    public Task SendUserUpdatedEmailAsync(string email, string name)
    {
        // Simulate sending email
        Console.WriteLine($"Sending user updated email to {email} for user {name}");
        return Task.CompletedTask;
    }
}

public class ConsoleEmailService : IEmailService
{
    public Task SendWelcomeEmailAsync(string email, string name)
    {
        // Simulate sending email to console
        Console.WriteLine($"[EMAIL] Welcome {name} at {email}");
        return Task.CompletedTask;
    }

    public Task SendUserUpdatedEmailAsync(string email, string name)
    {
        // Simulate sending email to console
        Console.WriteLine($"[EMAIL] User {name} updated at {email}");
        return Task.CompletedTask;
    }
}

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
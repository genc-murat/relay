using Relay.MinimalApiSample.Models;
using System.Collections.Concurrent;

namespace Relay.MinimalApiSample.Infrastructure;

/// <summary>
/// Simple in-memory database for demo purposes
/// </summary>
public class InMemoryDatabase
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();
    private readonly ConcurrentDictionary<Guid, Product> _products = new();

    public InMemoryDatabase()
    {
        SeedData();
    }

    public ConcurrentDictionary<Guid, User> Users => _users;
    public ConcurrentDictionary<Guid, Product> Products => _products;

    private void SeedData()
    {
        // Seed users
        var user1 = new User
        {
            Id = Guid.NewGuid(),
            Name = "John Doe",
            Email = "john@example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            IsActive = true
        };

        var user2 = new User
        {
            Id = Guid.NewGuid(),
            Name = "Jane Smith",
            Email = "jane@example.com",
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            IsActive = true
        };

        _users.TryAdd(user1.Id, user1);
        _users.TryAdd(user2.Id, user2);

        // Seed products
        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Laptop",
            Description = "High-performance laptop",
            Price = 1299.99m,
            Stock = 50,
            CreatedAt = DateTime.UtcNow.AddDays(-60)
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Mouse",
            Description = "Wireless mouse",
            Price = 29.99m,
            Stock = 200,
            CreatedAt = DateTime.UtcNow.AddDays(-45)
        };

        _products.TryAdd(product1.Id, product1);
        _products.TryAdd(product2.Id, product2);
    }
}

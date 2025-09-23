using MinimalApi.Models;
using System.Collections.Concurrent;

namespace MinimalApi.Services;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _nextId = 1;

    public InMemoryUserRepository()
    {
        var seedUser = new User
        {
            Id = _nextId++,
            Name = "Murat Genc",
            Email = "murat@gencmurat.com",
            CreatedAt = DateTime.UtcNow
        };
        _users.TryAdd(seedUser.Id, seedUser);
    }

    public ValueTask<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(id, out var user);
        return ValueTask.FromResult(user);
    }

    public ValueTask<IEnumerable<User>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var skip = (page - 1) * pageSize;
        var users = _users.Values
            .OrderBy(u => u.Id)
            .Skip(skip)
            .Take(pageSize);

        return ValueTask.FromResult(users);
    }

    public ValueTask<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        var newUser = user with { Id = _nextId++ };
        _users.TryAdd(newUser.Id, newUser);
        return ValueTask.FromResult(newUser);
    }

    public ValueTask<User?> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (_users.TryGetValue(user.Id, out var existingUser))
        {
            _users.TryUpdate(user.Id, user, existingUser);
            return ValueTask.FromResult<User?>(user);
        }

        return ValueTask.FromResult<User?>(null);
    }

    public ValueTask<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var removed = _users.TryRemove(id, out _);
        return ValueTask.FromResult(removed);
    }
}